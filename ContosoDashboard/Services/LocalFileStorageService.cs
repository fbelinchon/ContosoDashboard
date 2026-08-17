using System;
using System.IO;
using System.Threading.Tasks;
using ContosoDashboard.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ContosoDashboard.Services
{
    /// <summary>
    /// Local file storage implementation using system filesystem
    /// Stores files in AppData/uploads/ directory with GUID-based naming for security
    /// Supports soft-delete recovery from AppData/downloads/ directory
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly ILogger<LocalFileStorageService> _logger;
        private readonly string _uploadsBasePath;
        private readonly string _deletedFilesPath;

        public LocalFileStorageService(ILogger<LocalFileStorageService> logger)
        {
            _logger = logger;
            
            // Get the application root directory
            var appRoot = AppContext.BaseDirectory;
            _uploadsBasePath = Path.Combine(appRoot, "AppData", "uploads");
            _deletedFilesPath = Path.Combine(appRoot, "AppData", "downloads");

            // Ensure directories exist
            Directory.CreateDirectory(_uploadsBasePath);
            Directory.CreateDirectory(_deletedFilesPath);

            _logger.LogInformation("LocalFileStorageService initialized. Uploads: {UploadsPath}, Deleted: {DeletedPath}", 
                _uploadsBasePath, _deletedFilesPath);
        }

        /// <summary>
        /// Save file with GUID-based name to prevent collisions and path traversal attacks
        /// </summary>
        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, int userId, int? projectId = null)
        {
            try
            {
                // Create user and project subdirectories
                var userDir = Path.Combine(_uploadsBasePath, userId.ToString());
                var projectDir = projectId.HasValue 
                    ? Path.Combine(userDir, projectId.ToString()) 
                    : userDir;

                Directory.CreateDirectory(projectDir);

                // Generate GUID-based filename and preserve original extension
                var fileExtension = Path.GetExtension(fileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(projectDir, uniqueFileName);

                // Save file to disk
                using (var fileOnDisk = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    await fileStream.CopyToAsync(fileOnDisk);
                }

                // Return relative path for database storage
                var relativePath = Path.GetRelativePath(
                    Path.Combine(_uploadsBasePath, ".."), 
                    filePath
                ).Replace("\\", "/");

                _logger.LogInformation("File saved successfully. UserId: {UserId}, ProjectId: {ProjectId}, FileName: {FileName}, Path: {FilePath}",
                    userId, projectId ?? 0, fileName, filePath);

                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file. UserId: {UserId}, FileName: {FileName}", userId, fileName);
                throw;
            }
        }

        /// <summary>
        /// Retrieve file for download
        /// </summary>
        public async Task<Stream> GetFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(
                    Path.Combine(_uploadsBasePath, ".."),
                    filePath
                );

                // Security: Ensure path is within uploads directory
                var fullResolvedPath = Path.GetFullPath(fullPath);
                var uploadsResolvedPath = Path.GetFullPath(_uploadsBasePath);

                if (!fullResolvedPath.StartsWith(uploadsResolvedPath, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Path traversal attempt detected. Requested: {FilePath}", filePath);
                    return null;
                }

                if (!File.Exists(fullResolvedPath))
                {
                    _logger.LogWarning("File not found. Path: {FilePath}", filePath);
                    return null;
                }

                // Return readable stream
                var stream = new FileStream(fullResolvedPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                _logger.LogInformation("File retrieved. Path: {FilePath}", filePath);
                return await Task.FromResult(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file. Path: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Delete file (move to soft-delete directory for recovery window)
        /// </summary>
        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(
                    Path.Combine(_uploadsBasePath, ".."),
                    filePath
                );

                var fullResolvedPath = Path.GetFullPath(fullPath);
                var uploadsResolvedPath = Path.GetFullPath(_uploadsBasePath);

                if (!fullResolvedPath.StartsWith(uploadsResolvedPath, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Path traversal attempt in delete. Requested: {FilePath}", filePath);
                    return false;
                }

                if (!File.Exists(fullResolvedPath))
                {
                    _logger.LogWarning("File not found for deletion. Path: {FilePath}", filePath);
                    return false;
                }

                // Move to soft-delete directory for recovery
                var deletedFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fullResolvedPath)}";
                var deletedPath = Path.Combine(_deletedFilesPath, deletedFileName);

                File.Move(fullResolvedPath, deletedPath, overwrite: false);

                _logger.LogInformation("File soft-deleted. Original: {FilePath}, Deleted: {DeletedPath}", filePath, deletedPath);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file. Path: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Check if file exists in uploads directory
        /// </summary>
        public Task<bool> FileExistsAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(
                    Path.Combine(_uploadsBasePath, ".."),
                    filePath
                );

                var fullResolvedPath = Path.GetFullPath(fullPath);
                var uploadsResolvedPath = Path.GetFullPath(_uploadsBasePath);

                if (!fullResolvedPath.StartsWith(uploadsResolvedPath, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(false);
                }

                return Task.FromResult(File.Exists(fullResolvedPath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file existence. Path: {FilePath}", filePath);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Get file size in bytes
        /// </summary>
        public Task<long> GetFileSizeAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(
                    Path.Combine(_uploadsBasePath, ".."),
                    filePath
                );

                var fullResolvedPath = Path.GetFullPath(fullPath);
                var uploadsResolvedPath = Path.GetFullPath(_uploadsBasePath);

                if (!fullResolvedPath.StartsWith(uploadsResolvedPath, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(0L);
                }

                if (!File.Exists(fullResolvedPath))
                {
                    return Task.FromResult(0L);
                }

                var fileInfo = new FileInfo(fullResolvedPath);
                return Task.FromResult(fileInfo.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file size. Path: {FilePath}", filePath);
                return Task.FromResult(0L);
            }
        }

        /// <summary>
        /// Restore a soft-deleted file from recovery directory
        /// </summary>
        public async Task<string> RestoreFileAsync(string filePath, string restorePath = null)
        {
            try
            {
                // Find the soft-deleted file in the recovery directory
                // This is a simplified implementation - production would need more robust recovery logic
                
                var directoryInfo = new DirectoryInfo(_deletedFilesPath);
                var files = directoryInfo.GetFiles("*" + Path.GetExtension(filePath));

                if (files.Length == 0)
                {
                    _logger.LogWarning("No soft-deleted file found for restoration. OriginalPath: {FilePath}", filePath);
                    return null;
                }

                // For MVP, restore the most recently deleted file with matching extension
                var fileToRestore = files[files.Length - 1];
                var targetPath = restorePath ?? filePath;
                var targetFullPath = Path.Combine(
                    Path.Combine(_uploadsBasePath, ".."),
                    targetPath
                );

                Directory.CreateDirectory(Path.GetDirectoryName(targetFullPath));
                File.Copy(fileToRestore.FullName, targetFullPath, overwrite: true);

                _logger.LogInformation("File restored. From: {DeletedPath}, To: {RestoredPath}", fileToRestore.FullName, targetFullPath);
                return targetPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring file. Path: {FilePath}", filePath);
                throw;
            }
        }
    }
}
