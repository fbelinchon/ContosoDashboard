using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models
{
    /// <summary>
    /// Audit log entry for document operations (GDPR compliance)
    /// Tracks all document-related actions for compliance and forensics
    /// </summary>
    [Table("DocumentAuditLogs")]
    public class DocumentAuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long LogId { get; set; }

        /// <summary>
        /// Reference to the document (nullable for operations without specific document)
        /// </summary>
        public int? DocumentId { get; set; }

        [ForeignKey(nameof(DocumentId))]
        public virtual Document Document { get; set; }

        /// <summary>
        /// User who performed the action
        /// </summary>
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        /// <summary>
        /// Action performed (e.g., "Upload", "Download", "Delete", "Restore", "Share", "Unshare")
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Action { get; set; }

        /// <summary>
        /// When the action occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// IP address of the request
        /// </summary>
        [StringLength(50)]
        public string IpAddress { get; set; }

        /// <summary>
        /// Result of the action (Success, Failure, Blocked, etc.)
        /// </summary>
        [StringLength(20)]
        public string Result { get; set; } = "Success";

        /// <summary>
        /// Additional details about the operation (JSON or text)
        /// </summary>
        [StringLength(1024)]
        public string Details { get; set; }

        /// <summary>
        /// File size involved in the operation (for upload/download tracking)
        /// </summary>
        public long? FileSize { get; set; }
    }
}
