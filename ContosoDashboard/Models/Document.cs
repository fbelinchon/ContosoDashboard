using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace ContosoDashboard.Models
{
    /// <summary>
    /// Represents a document uploaded by a user
    /// </summary>
    [Table("Documents")]
    public class Document
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DocumentId { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; }

        [StringLength(1024)]
        public string Description { get; set; }

        /// <summary>
        /// Category for organizing documents (text-based, e.g., "Reports", "Meeting Notes", "Research")
        /// </summary>
        [StringLength(100)]
        public string Category { get; set; }

        /// <summary>
        /// GUID-based file path to prevent path traversal attacks
        /// Format: AppData/uploads/{userId}/{projectId?}/{guid}.{ext}
        /// </summary>
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// MIME type of the file (e.g., "application/pdf", "text/plain")
        /// </summary>
        [StringLength(100)]
        public string MimeType { get; set; }

        /// <summary>
        /// When the document was uploaded
        /// </summary>
        public DateTime UploadDate { get; set; }

        /// <summary>
        /// User who uploaded the document
        /// </summary>
        public int UploadedByUserId { get; set; }

        [ForeignKey(nameof(UploadedByUserId))]
        public virtual User UploadedByUser { get; set; }

        /// <summary>
        /// Associated project (optional)
        /// </summary>
        public int? ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public virtual Project Project { get; set; }

        /// <summary>
        /// Soft-delete flag (GDPR compliance: allows 30-day recovery)
        /// </summary>
        [Column(TypeName = "bit")]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// When the document was soft-deleted (null if not deleted)
        /// </summary>
        public DateTime? DeletedDate { get; set; }

        /// <summary>
        /// System timestamp for record creation
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// System timestamp for last modification
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optimistic concurrency control
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; }

        // Navigation properties
        public virtual ICollection<DocumentTag> Tags { get; set; } = new List<DocumentTag>();
        public virtual ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
        public virtual ICollection<DocumentAuditLog> AuditLogs { get; set; } = new List<DocumentAuditLog>();
    }
}
