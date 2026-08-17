using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models
{
    /// <summary>
    /// Tracks storage quota usage per user for enforcing document upload limits
    /// </summary>
    [Table("UserStorageQuotas")]
    public class UserStorageQuota
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int QuotaId { get; set; }

        /// <summary>
        /// User this quota applies to (unique index for fast lookups)
        /// </summary>
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        /// <summary>
        /// Total bytes currently used by this user
        /// Calculated from sum of Document.FileSize where UploadedByUserId = UserId AND IsDeleted = false
        /// </summary>
        public long UsedBytes { get; set; } = 0;

        /// <summary>
        /// Quota limit in bytes (default: 5 GB = 5,368,709,120 bytes)
        /// </summary>
        public long QuotaBytes { get; set; } = 5_368_709_120; // 5 GB

        /// <summary>
        /// When the quota calculation was last updated
        /// Used for caching and performance optimization
        /// </summary>
        public DateTime LastCalculated { get; set; } = DateTime.UtcNow;
    }
}
