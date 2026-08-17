using System.Collections.Generic;
using System.Threading.Tasks;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services.Interfaces
{
    /// <summary>
    /// High-level document management service orchestrating all lower-level services
    /// Handles upload, download, delete, restore, share, search operations
    /// Coordinates: file storage, authorization, audit logging, quota management
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>
        /// Upload a new document with security & quota checks
        /// </summary>
        /// <param name="userId">User uploading the document</param>
        /// <param name="title">Document title</param>
        /// <param name="description">Document description</param>
        /// <param name="category">Document category (e.g., "Invoice", "Report")</param>
        /// <param name="fileStream">File content stream</param>
        /// <param name="fileName">Original file name</param>
        /// <param name="projectId">Optional project association</param>
        /// <param name="tags">Optional tags for organization</param>
        /// <param name="ipAddress">Client IP for audit trail</param>
        /// <returns>DocumentId if successful, 0 if failed (quota exceeded, etc.)</returns>
        Task<int> UploadDocumentAsync(int userId, string title, string description, string category,
            System.IO.Stream fileStream, string fileName, int? projectId = null, 
            string[] tags = null, string ipAddress = "0.0.0.0");

        /// <summary>
        /// Download a document file (with authorization & audit check)
        /// </summary>
        /// <returns>File stream ready for download, or null if unauthorized/not found</returns>
        Task<(System.IO.Stream fileStream, string mimeType, string fileName)> DownloadDocumentAsync(
            int documentId, int userId, string ipAddress = "0.0.0.0");

        /// <summary>
        /// Get document details (with authorization check)
        /// </summary>
        Task<Document> GetDocumentAsync(int documentId, int userId);

        /// <summary>
        /// List documents accessible by user (owner, shared, or admin)
        /// </summary>
        /// <param name="userId">User requesting list</param>
        /// <param name="projectId">Optional filter by project</param>
        /// <param name="category">Optional filter by category</param>
        /// <param name="tag">Optional filter by tag</param>
        /// <param name="searchQuery">Optional search in title/description</param>
        /// <returns>List of accessible documents</returns>
        Task<List<Document>> ListDocumentsAsync(int userId, int? projectId = null, 
            string category = null, string tag = null, string searchQuery = null);

        /// <summary>
        /// Soft-delete document (mark IsDeleted = true)
        /// </summary>
        Task<bool> DeleteDocumentAsync(int documentId, int userId, string ipAddress = "0.0.0.0");

        /// <summary>
        /// Restore soft-deleted document (within 30-day window)
        /// </summary>
        Task<bool> RestoreDocumentAsync(int documentId, int userId, string ipAddress = "0.0.0.0");

        /// <summary>
        /// Update document metadata (title, description, category, tags)
        /// </summary>
        Task<bool> UpdateDocumentAsync(int documentId, int userId, Document updatedDoc);

        /// <summary>
        /// Share document with another user
        /// </summary>
        /// <returns>ShareId if successful, 0 if failed</returns>
        Task<int> ShareDocumentAsync(int documentId, int userId, int shareWithUserId, string ipAddress = "0.0.0.0");

        /// <summary>
        /// Revoke document sharing with a user
        /// </summary>
        Task<bool> RevokeShareAsync(int shareId, int userId, string ipAddress = "0.0.0.0");

        /// <summary>
        /// Get all shares for a document (for transparency)
        /// </summary>
        Task<List<DocumentShare>> GetDocumentSharesAsync(int documentId, int userId);

        /// <summary>
        /// Get user's quota status (used/total bytes)
        /// </summary>
        Task<(long usedBytes, long quotaBytes, int percentageUsed)> GetQuotaStatusAsync(int userId);
    }
}
