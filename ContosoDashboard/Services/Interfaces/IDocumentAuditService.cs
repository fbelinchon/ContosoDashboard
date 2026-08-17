using System.Threading.Tasks;

namespace ContosoDashboard.Services.Interfaces
{
    /// <summary>
    /// GDPR-compliant audit logging for document operations
    /// Tracks all actions for compliance and forensic analysis
    /// </summary>
    public interface IDocumentAuditService
    {
        /// <summary>
        /// Log a document operation (Upload, Download, Delete, Restore, Share, Unshare, Preview)
        /// </summary>
        /// <param name="action">Operation performed (e.g., "Upload", "Download")</param>
        /// <param name="userId">User performing the action</param>
        /// <param name="documentId">Document affected (nullable for org-level ops)</param>
        /// <param name="fileSize">File size in bytes for tracking data flows</param>
        /// <param name="ipAddress">Client IP address for forensics</param>
        /// <param name="result">Success/Failure/Blocked status</param>
        /// <param name="details">Additional context as JSON or text (max 1024 chars)</param>
        Task LogOperationAsync(string action, int userId, int? documentId, long? fileSize, 
            string ipAddress, string result = "Success", string details = null);

        /// <summary>
        /// Get audit logs for a specific document (for transparency/DSAR)
        /// </summary>
        Task<object[]> GetDocumentAuditLogsAsync(int documentId);

        /// <summary>
        /// Get audit logs for a user's actions (for user-initiated DSAR request)
        /// </summary>
        Task<object[]> GetUserAuditLogsAsync(int userId);

        /// <summary>
        /// Get audit logs for a specific action type (e.g., all downloads in timeframe)
        /// </summary>
        Task<object[]> GetAuditLogsByActionAsync(string action, System.DateTime fromDate, System.DateTime toDate);
    }
}
