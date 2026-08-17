using System;
using System.Threading.Tasks;

namespace ContosoDashboard.Services.Interfaces
{
    /// <summary>
    /// Enforces per-user storage quota (5GB default)
    /// Validates upload size before accepting files
    /// Tracks usage from DocumentSize sum
    /// </summary>
    public interface IQuotaService
    {
        /// <summary>
        /// Check if user has available quota for upload
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="fileSizeInBytes">File size to validate</param>
        /// <returns>True if upload allowed, false if would exceed quota</returns>
        Task<bool> CanUploadAsync(int userId, long fileSizeInBytes);

        /// <summary>
        /// Get user's current quota usage
        /// </summary>
        /// <returns>Tuple of (UsedBytes, QuotaBytes)</returns>
        Task<(long usedBytes, long quotaBytes)> GetQuotaStatusAsync(int userId);

        /// <summary>
        /// Get percentage of quota used (0-100)
        /// </summary>
        Task<int> GetQuotaPercentageAsync(int userId);

        /// <summary>
        /// Add bytes to user's quota usage (called after successful upload)
        /// </summary>
        Task AddUsageAsync(int userId, long fileSizeInBytes);

        /// <summary>
        /// Subtract bytes from usage (called after file deletion)
        /// </summary>
        Task RemoveUsageAsync(int userId, long fileSizeInBytes);

        /// <summary>
        /// Recalculate quota from sum of active documents
        /// Used periodically or after database migrations
        /// </summary>
        Task RecalculateQuotaAsync(int userId);

        /// <summary>
        /// Update user's quota limit (admin only, for future organization-level limits)
        /// </summary>
        Task SetQuotaAsync(int userId, long quotaBytes);

        /// <summary>
        /// Get remaining bytes before hitting quota limit
        /// </summary>
        Task<long> GetRemainingQuotaAsync(int userId);
    }
}
