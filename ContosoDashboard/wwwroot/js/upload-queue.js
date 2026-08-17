// Document Upload Queue Manager
// Manages sequential file uploads with progress tracking

class UploadQueue {
    constructor(options = {}) {
        this.queue = [];
        this.currentUpload = null;
        this.isProcessing = false;
        this.maxFileSize = options.maxFileSize || 26_214_400; // 25 MB
        this.uploadEndpoint = options.uploadEndpoint || '/api/documents/upload';
        this.onProgress = options.onProgress || (() => {});
        this.onComplete = options.onComplete || (() => {});
        this.onError = options.onError || (() => {});
    }

    /**
     * Add files to upload queue
     * @param {File[]} files - Array of file objects
     * @param {Object} metadata - Metadata to attach (title, category, etc.)
     */
    addFiles(files, metadata = {}) {
        const queueItems = Array.from(files).map((file, index) => ({
            id: this.generateId(),
            file: file,
            metadata: metadata,
            status: 'queued', // queued, uploading, completed, error
            progress: 0,
            error: null,
            position: this.queue.length + index + 1,
        }));

        this.queue.push(...queueItems);
        this.onProgress({ queue: this.queue });
        this.processQueue();
        return queueItems;
    }

    /**
     * Process the upload queue sequentially
     */
    async processQueue() {
        if (this.isProcessing || this.queue.length === 0) {
            return;
        }

        this.isProcessing = true;

        while (this.queue.length > 0) {
            const item = this.queue.find(i => i.status === 'queued');
            if (!item) break;

            try {
                item.status = 'uploading';
                item.position = 1;
                this.onProgress({ queue: this.queue });

                await this.uploadFile(item);

                item.status = 'completed';
                this.onComplete(item);
            } catch (error) {
                item.status = 'error';
                item.error = error.message;
                this.onError(item);
            }

            // Move item to end of completed items
            this.queue = this.queue.filter(i => i.id !== item.id);
            this.updatePositions();
            this.onProgress({ queue: this.queue });
        }

        this.isProcessing = false;
    }

    /**
     * Upload a single file
     * @param {Object} item - Queue item with file and metadata
     * @returns {Promise}
     */
    uploadFile(item) {
        return new Promise((resolve, reject) => {
            // Validate file
            if (item.file.size > this.maxFileSize) {
                reject(new Error(`File exceeds maximum size of ${this.formatBytes(this.maxFileSize)}`));
                return;
            }

            // Create form data
            const formData = new FormData();
            formData.append('file', item.file);
            formData.append('title', item.metadata.title || item.file.name);
            formData.append('description', item.metadata.description || '');
            formData.append('category', item.metadata.category || 'Other');
            formData.append('projectId', item.metadata.projectId || '');

            if (item.metadata.tags) {
                formData.append('tags', item.metadata.tags);
            }

            // Create XMLHttpRequest to track upload progress
            const xhr = new XMLHttpRequest();

            // Track upload progress
            xhr.upload.addEventListener('progress', (event) => {
                if (event.lengthComputable) {
                    const percentComplete = (event.loaded / event.total) * 100;
                    item.progress = Math.round(percentComplete);
                    this.onProgress({ queue: this.queue, currentItem: item });
                }
            });

            // Handle completion
            xhr.addEventListener('load', () => {
                if (xhr.status === 200 || xhr.status === 201) {
                    try {
                        const response = JSON.parse(xhr.responseText);
                        item.response = response;
                        resolve(response);
                    } catch (e) {
                        reject(new Error('Invalid response from server'));
                    }
                } else {
                    try {
                        const error = JSON.parse(xhr.responseText);
                        reject(new Error(error.message || `Upload failed with status ${xhr.status}`));
                    } catch (e) {
                        reject(new Error(`Upload failed with status ${xhr.status}`));
                    }
                }
            });

            // Handle errors
            xhr.addEventListener('error', () => {
                reject(new Error('Network error during upload'));
            });

            xhr.addEventListener('abort', () => {
                reject(new Error('Upload cancelled'));
            });

            // Send request
            xhr.open('POST', this.uploadEndpoint);
            xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
            xhr.send(formData);

            // Store xhr reference for potential cancellation
            item.xhr = xhr;
        });
    }

    /**
     * Cancel upload for a queue item
     * @param {string} itemId - Queue item ID
     */
    cancelUpload(itemId) {
        const item = this.queue.find(i => i.id === itemId);
        if (item) {
            if (item.xhr) {
                item.xhr.abort();
            }
            item.status = 'cancelled';
            this.queue = this.queue.filter(i => i.id !== itemId);
            this.updatePositions();
            this.onProgress({ queue: this.queue });
        }
    }

    /**
     * Retry a failed upload
     * @param {string} itemId - Queue item ID
     */
    retryUpload(itemId) {
        const item = this.queue.find(i => i.id === itemId);
        if (item && item.status === 'error') {
            item.status = 'queued';
            item.error = null;
            item.progress = 0;
            this.onProgress({ queue: this.queue });
            this.processQueue();
        }
    }

    /**
     * Update position numbers for queued items
     */
    updatePositions() {
        this.queue.forEach((item, index) => {
            if (item.status === 'queued') {
                item.position = index + 1;
            }
        });
    }

    /**
     * Generate unique ID
     */
    generateId() {
        return `upload-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    }

    /**
     * Format bytes to human readable format
     */
    formatBytes(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
    }

    /**
     * Clear all completed/cancelled items from queue
     */
    clearCompleted() {
        this.queue = this.queue.filter(i => i.status === 'queued' || i.status === 'uploading');
    }

    /**
     * Get queue statistics
     */
    getStats() {
        return {
            total: this.queue.length,
            queued: this.queue.filter(i => i.status === 'queued').length,
            uploading: this.queue.filter(i => i.status === 'uploading').length,
            completed: this.queue.filter(i => i.status === 'completed').length,
            error: this.queue.filter(i => i.status === 'error').length,
        };
    }
}

// Export for use in modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = UploadQueue;
}
