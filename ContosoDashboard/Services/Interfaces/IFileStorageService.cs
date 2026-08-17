using System;
using System.IO;
using System.Threading.Tasks;

namespace ContosoDashboard.Services.Interfaces
{
    /// <summary>
    /// Abstraction for file storage operations supporting both local and cloud storage
    /// Enables switching between LocalFileStorageService and AzureBlobStorageService without changing business logic
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Save a file to storage and return the stored file path
        /// </summary>
        /// <param name="fileStream">The file content to save</param>
        /// <param name="fileName">Original file name (for MIME type detection)</param>
        /// <param name="userId">User ID uploading the file (for organizing storage)</param>
        /// <param name="projectId">Optional project ID for organizing storage by project</param>
        /// <returns>Relative path to the saved file (e.g., "AppData/uploads/1/1/guid.pdf")</returns>
        Task<string> SaveFileAsync(Stream fileStream, string fileName, int userId, int? projectId = null);

        /// <summary>
        /// Retrieve a file for download by its stored path
        /// </summary>
        /// <param name="filePath">The stored file path (returned from SaveFileAsync)</param>
        /// <returns>File stream ready for download, or null if file not found</returns>
        Task<Stream> GetFileAsync(string filePath);

        /// <summary>
        /// Delete a file from storage (soft or hard delete based on implementation)
        /// </summary>
        /// <param name="filePath">The stored file path to delete</param>
        /// <returns>True if deletion successful, false if file not found</returns>
        Task<bool> DeleteFileAsync(string filePath);

        /// <summary>
        /// Check if a file exists in storage
        /// </summary>
        /// <param name="filePath">The stored file path to check</param>
        /// <returns>True if file exists, false otherwise</returns>
        Task<bool> FileExistsAsync(string filePath);

        /// <summary>
        /// Get file size in bytes for quota tracking and display
        /// </summary>
        /// <param name="filePath">The stored file path</param>
        /// <returns>File size in bytes, or 0 if file not found</returns>
        Task<long> GetFileSizeAsync(string filePath);

        /// <summary>
        /// Restore a file from soft-delete storage if available (for recovery within 30-day window)
        /// </summary>
        /// <param name="filePath">The original file path</param>
        /// <param name="restorePath">Optional specific restore path; if null, restore to original location</param>
        /// <returns>The path where file was restored, or null if restore failed</returns>
        Task<string> RestoreFileAsync(string filePath, string restorePath = null);
    }
}
