using System;
using System.Collections.Generic;
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
    /// GDPR-compliant audit logging for all document operations
    /// Ensures audit logs are never cascade-deleted to preserve compliance records
    /// </summary>
    public class DocumentAuditService : IDocumentAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentAuditService> _logger;

        public DocumentAuditService(ApplicationDbContext context, ILogger<DocumentAuditService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Log a document operation to audit table
        /// </summary>
        public async Task LogOperationAsync(string action, int userId, int? documentId, long? fileSize,
            string ipAddress, string result = "Success", string details = null)
        {
            try
            {
                var auditLog = new DocumentAuditLog
                {
                    DocumentId = documentId,
                    UserId = userId,
                    Action = action ?? "Unknown",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = ipAddress ?? "0.0.0.0",
                    Result = result ?? "Success",
                    Details = details ?? string.Empty,
                    FileSize = fileSize ?? 0
                };

                _context.DocumentAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Audit log created. Action: {Action}, UserId: {UserId}, DocumentId: {DocumentId}, Result: {Result}",
                    action, userId, documentId ?? 0, result);
            }
            catch (Exception ex)
            {
                // Don't throw - audit failure shouldn't block the operation
                // But log the audit failure
                _logger.LogError(ex, "Failed to create audit log. Action: {Action}, UserId: {UserId}, DocumentId: {DocumentId}",
                    action, userId, documentId ?? 0);
            }
        }

        /// <summary>
        /// Retrieve audit logs for a specific document (for transparency)
        /// </summary>
        public async Task<object[]> GetDocumentAuditLogsAsync(int documentId)
        {
            try
            {
                var logs = await _context.DocumentAuditLogs
                    .AsNoTracking()
                    .Where(al => al.DocumentId == documentId)
                    .OrderByDescending(al => al.Timestamp)
                    .Select(al => new
                    {
                        al.LogId,
                        al.Action,
                        User = al.User.DisplayName,
                        al.Timestamp,
                        al.IpAddress,
                        al.Result,
                        al.Details,
                        al.FileSize
                    })
                    .ToArrayAsync();

                _logger.LogInformation("Retrieved {Count} audit logs for document {DocumentId}", logs.Length, documentId);
                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs for document {DocumentId}", documentId);
                return Array.Empty<object>();
            }
        }

        /// <summary>
        /// Retrieve all audit logs for a user (for DSAR - Data Subject Access Request)
        /// </summary>
        public async Task<object[]> GetUserAuditLogsAsync(int userId)
        {
            try
            {
                var logs = await _context.DocumentAuditLogs
                    .AsNoTracking()
                    .Where(al => al.UserId == userId)
                    .OrderByDescending(al => al.Timestamp)
                    .Select(al => new
                    {
                        al.LogId,
                        al.Action,
                        Document = al.Document.Title,
                        al.Timestamp,
                        al.IpAddress,
                        al.Result,
                        al.Details,
                        al.FileSize
                    })
                    .ToArrayAsync();

                _logger.LogInformation("Retrieved {Count} audit logs for user {UserId} (DSAR)", logs.Length, userId);
                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs for user {UserId}", userId);
                return Array.Empty<object>();
            }
        }

        /// <summary>
        /// Retrieve audit logs for a specific action type within a date range
        /// </summary>
        public async Task<object[]> GetAuditLogsByActionAsync(string action, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var logs = await _context.DocumentAuditLogs
                    .AsNoTracking()
                    .Where(al => al.Action == action && al.Timestamp >= fromDate && al.Timestamp <= toDate)
                    .OrderByDescending(al => al.Timestamp)
                    .Select(al => new
                    {
                        al.LogId,
                        User = al.User.DisplayName,
                        Document = al.Document.Title,
                        al.Timestamp,
                        al.IpAddress,
                        al.Result,
                        al.Details,
                        al.FileSize
                    })
                    .ToArrayAsync();

                _logger.LogInformation("Retrieved {Count} audit logs for action {Action} between {FromDate} and {ToDate}",
                    logs.Length, action, fromDate, toDate);
                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs for action {Action}", action);
                return Array.Empty<object>();
            }
        }
    }
}
