using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models
{
    /// <summary>
    /// Audit log entry for user profile changes (admin actions)
    /// Tracks administrative changes to user records for compliance and forensics
    /// </summary>
    [Table("UserAuditLogs")]
    public class UserAuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long LogId { get; set; }

        /// <summary>
        /// User whose profile was modified
        /// </summary>
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        /// <summary>
        /// Administrator who performed the action
        /// </summary>
        public int AdminUserId { get; set; }

        [ForeignKey(nameof(AdminUserId))]
        public virtual User AdminUser { get; set; }

        /// <summary>
        /// Action performed (e.g., "UpdateIsInternalUser", "UpdateRole", etc.)
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Action { get; set; }

        /// <summary>
        /// When the action occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// IP address of the request (if available)
        /// </summary>
        [StringLength(50)]
        public string IpAddress { get; set; }

        /// <summary>
        /// Result of the action (Success, Failure, Blocked, etc.)
        /// </summary>
        [StringLength(20)]
        public string Result { get; set; } = "Success";

        /// <summary>
        /// Previous value (JSON format)
        /// </summary>
        public string OldValue { get; set; }

        /// <summary>
        /// New value (JSON format)
        /// </summary>
        public string NewValue { get; set; }

        /// <summary>
        /// Additional details about the operation
        /// </summary>
        public string Details { get; set; }
    }
}
