using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models
{
    /// <summary>
    /// Represents a document being shared with another user
    /// </summary>
    [Table("DocumentShares")]
    public class DocumentShare
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ShareId { get; set; }

        /// <summary>
        /// Reference to the document being shared
        /// </summary>
        public int DocumentId { get; set; }

        [ForeignKey(nameof(DocumentId))]
        public virtual Document Document { get; set; }

        /// <summary>
        /// User who initiated the share
        /// </summary>
        public int SharedByUserId { get; set; }

        [ForeignKey(nameof(SharedByUserId))]
        public virtual User SharedByUser { get; set; }

        /// <summary>
        /// User with whom the document is shared
        /// </summary>
        public int SharedWithUserId { get; set; }

        [ForeignKey(nameof(SharedWithUserId))]
        public virtual User SharedWithUser { get; set; }

        /// <summary>
        /// When the share was created
        /// </summary>
        public DateTime SharedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether this share has been revoked
        /// </summary>
        [Column(TypeName = "bit")]
        public bool IsRevoked { get; set; } = false;

        /// <summary>
        /// When the share was revoked (null if not revoked)
        /// </summary>
        public DateTime? RevokedDate { get; set; }
    }
}
