using System;
using System.Linq;
using System.Threading.Tasks;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContosoDashboard.Services
{
    /// <summary>
    /// Manages per-user storage quota (5GB default, configurable per user)
    /// Pre-upload validation prevents exceeding quota
    /// Usage tracked from sum of active (non-deleted) Document.FileSize
    /// </summary>
    public class QuotaService : IQuotaService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuotaService> _logger;

        // Default quota: 5 GB in bytes
        private const long DEFAULT_QUOTA_BYTES = 5_368_709_120;

        public QuotaService(ApplicationDbContext context, ILogger<QuotaService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Pre-upload validation: Check if file would exceed user's quota
        /// </summary>
        public async Task<bool> CanUploadAsync(int userId, long fileSizeInBytes)
        {
            try
            {
                var (usedBytes, quotaBytes) = await GetQuotaStatusAsync(userId);
                var wouldExceed = (usedBytes + fileSizeInBytes) > quotaBytes;

                if (wouldExceed)
                {
                    _logger.LogWarning(
                        "Upload would exceed quota. UserId: {UserId}, FileSize: {FileSize}, Used: {Used}, Quota: {Quota}",
                        userId, fileSizeInBytes, usedBytes, quotaBytes);
                }

                return !wouldExceed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking upload quota. UserId: {UserId}, FileSize: {FileSize}",
                    userId, fileSizeInBytes);
                return false;
            }
        }

        /// <summary>
        /// Get current usage and quota limit for user
        /// </summary>
        public async Task<(long usedBytes, long quotaBytes)> GetQuotaStatusAsync(int userId)
        {
            try
            {
                // Get or create quota entry
                var quota = await _context.UserStorageQuotas
                    .FirstOrDefaultAsync(q => q.UserId == userId);

                if (quota == null)
                {
                    // Create default quota entry for first-time user
                    quota = new UserStorageQuota
                    {
                        UserId = userId,
                        UsedBytes = 0,
                        QuotaBytes = DEFAULT_QUOTA_BYTES,
                        LastCalculated = DateTime.UtcNow
                    };

                    _context.UserStorageQuotas.Add(quota);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Created default quota for user {UserId}", userId);
                }

                // Verify quota usage is current (within 1 hour cache)
                var hoursSinceUpdate = DateTime.UtcNow.Subtract(quota.LastCalculated).TotalHours;
                if (hoursSinceUpdate > 1)
                {
                    // Recalculate from actual documents
                    await RecalculateQuotaAsync(userId);
                    
                    // Reload updated quota
                    quota = await _context.UserStorageQuotas
                        .FirstOrDefaultAsync(q => q.UserId == userId);
                }

                return (quota.UsedBytes, quota.QuotaBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quota status for user {UserId}", userId);
                return (0, DEFAULT_QUOTA_BYTES);
            }
        }

        /// <summary>
        /// Get quota usage as percentage (0-100)
        /// </summary>
        public async Task<int> GetQuotaPercentageAsync(int userId)
        {
            var (usedBytes, quotaBytes) = await GetQuotaStatusAsync(userId);
            if (quotaBytes == 0)
                return 0;

            var percentage = (int)((usedBytes * 100) / quotaBytes);
            return Math.Min(percentage, 100);
        }

        /// <summary>
        /// Add bytes to usage after successful upload
        /// </summary>
        public async Task AddUsageAsync(int userId, long fileSizeInBytes)
        {
            try
            {
                var quota = await _context.UserStorageQuotas
                    .FirstOrDefaultAsync(q => q.UserId == userId);

                if (quota == null)
                {
                    // Create if doesn't exist
                    quota = new UserStorageQuota
                    {
                        UserId = userId,
                        UsedBytes = fileSizeInBytes,
                        QuotaBytes = DEFAULT_QUOTA_BYTES,
                        LastCalculated = DateTime.UtcNow
                    };
                    _context.UserStorageQuotas.Add(quota);
                }
                else
                {
                    quota.UsedBytes += fileSizeInBytes;
                    quota.LastCalculated = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                var percentage = (int)((quota.UsedBytes * 100) / quota.QuotaBytes);
                _logger.LogInformation("Added {FileSize} bytes to quota. UserId: {UserId}, Total: {Total} ({Percentage}%)",
                    fileSizeInBytes, userId, quota.UsedBytes, percentage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to quota. UserId: {UserId}, FileSize: {FileSize}",
                    userId, fileSizeInBytes);
                throw;
            }
        }

        /// <summary>
        /// Remove bytes from usage after file deletion
        /// </summary>
        public async Task RemoveUsageAsync(int userId, long fileSizeInBytes)
        {
            try
            {
                var quota = await _context.UserStorageQuotas
                    .FirstOrDefaultAsync(q => q.UserId == userId);

                if (quota == null)
                {
                    _logger.LogWarning("Quota entry not found for removal. UserId: {UserId}", userId);
                    return;
                }

                quota.UsedBytes = Math.Max(0, quota.UsedBytes - fileSizeInBytes);
                quota.LastCalculated = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var percentage = (int)((quota.UsedBytes * 100) / quota.QuotaBytes);
                _logger.LogInformation("Removed {FileSize} bytes from quota. UserId: {UserId}, Total: {Total} ({Percentage}%)",
                    fileSizeInBytes, userId, quota.UsedBytes, percentage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from quota. UserId: {UserId}, FileSize: {FileSize}",
                    userId, fileSizeInBytes);
                throw;
            }
        }

        /// <summary>
        /// Recalculate quota from actual sum of active documents
        /// Used after database changes or as periodic maintenance
        /// </summary>
        public async Task RecalculateQuotaAsync(int userId)
        {
            try
            {
                // Sum file sizes of all active (non-deleted) documents for user
                var actualUsage = await _context.Documents
                    .AsNoTracking()
                    .Where(d => d.UploadedByUserId == userId && !d.IsDeleted)
                    .SumAsync(d => d.FileSize);

                var quota = await _context.UserStorageQuotas
                    .FirstOrDefaultAsync(q => q.UserId == userId);

                if (quota == null)
                {
                    quota = new UserStorageQuota
                    {
                        UserId = userId,
                        UsedBytes = actualUsage,
                        QuotaBytes = DEFAULT_QUOTA_BYTES,
                        LastCalculated = DateTime.UtcNow
                    };
                    _context.UserStorageQuotas.Add(quota);
                }
                else
                {
                    quota.UsedBytes = actualUsage;
                    quota.LastCalculated = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                var percentage = (int)((actualUsage * 100) / quota.QuotaBytes);
                _logger.LogInformation("Recalculated quota. UserId: {UserId}, Total: {Total} ({Percentage}%)",
                    userId, actualUsage, percentage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating quota for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Update quota limit for user (admin operation)
        /// </summary>
        public async Task SetQuotaAsync(int userId, long quotaBytes)
        {
            try
            {
                var quota = await _context.UserStorageQuotas
                    .FirstOrDefaultAsync(q => q.UserId == userId);

                if (quota == null)
                {
                    quota = new UserStorageQuota
                    {
                        UserId = userId,
                        UsedBytes = 0,
                        QuotaBytes = quotaBytes,
                        LastCalculated = DateTime.UtcNow
                    };
                    _context.UserStorageQuotas.Add(quota);
                }
                else
                {
                    quota.QuotaBytes = quotaBytes;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated quota for user {UserId} to {QuotaBytes} bytes", userId, quotaBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting quota for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Get remaining bytes before hitting quota
        /// </summary>
        public async Task<long> GetRemainingQuotaAsync(int userId)
        {
            var (usedBytes, quotaBytes) = await GetQuotaStatusAsync(userId);
            return Math.Max(0, quotaBytes - usedBytes);
        }
    }
}
