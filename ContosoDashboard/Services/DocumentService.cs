using System;
using System.Collections.Generic;
using System.IO;
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
    /// Orchestrates all document management operations
    /// Coordinates file storage, authorization, audit logging, quota enforcement
    /// Ensures consistency and security across all document operations
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly IDocumentAuthorizationService _authorization;
        private readonly IDocumentAuditService _audit;
        private readonly IQuotaService _quota;
        private readonly ILogger<DocumentService> _logger;

        // Default values
        private const string DEFAULT_CATEGORY = "General";
        private const long MAX_FILE_SIZE = 500 * 1024 * 1024; // 500 MB per file

        public DocumentService(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            IDocumentAuthorizationService authorization,
            IDocumentAuditService audit,
            IQuotaService quota,
            ILogger<DocumentService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
            _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
            _audit = audit ?? throw new ArgumentNullException(nameof(audit));
            _quota = quota ?? throw new ArgumentNullException(nameof(quota));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Upload document with full validation, storage, and quota management
        /// </summary>
        public async Task<int> UploadDocumentAsync(int userId, string title, string description, 
            string category, Stream fileStream, string fileName, int? projectId = null, 
            string[] tags = null, string ipAddress = "0.0.0.0")
        {
            var documentId = 0;
            try
            {
                // 1. Validation
                if (fileStream == null || fileStream.Length == 0)
                {
                    _logger.LogWarning("Upload validation failed: empty file. UserId: {UserId}", userId);
                    await _audit.LogOperationAsync("Upload", userId, null, 0, ipAddress, "Blocked", "Empty file");
                    return 0;
                }

                if (fileStream.Length > MAX_FILE_SIZE)
                {
                    _logger.LogWarning("Upload validation failed: file too large. UserId: {UserId}, Size: {Size}", 
                        userId, fileStream.Length);
                    await _audit.LogOperationAsync("Upload", userId, null, fileStream.Length, ipAddress, "Blocked", "File exceeds 500MB limit");
                    return 0;
                }

                // 2. Quota check (pre-upload validation)
                if (!await _quota.CanUploadAsync(userId, fileStream.Length))
                {
                    _logger.LogWarning("Upload quota exceeded. UserId: {UserId}, FileSize: {FileSize}", 
                        userId, fileStream.Length);
                    await _audit.LogOperationAsync("Upload", userId, null, fileStream.Length, ipAddress, "Blocked", "Quota exceeded");
                    return 0;
                }

                // 3. Save file to storage (get path)
                fileStream.Position = 0; // Reset stream position
                var filePath = await _fileStorage.SaveFileAsync(fileStream, fileName, userId, projectId);

                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.LogError("File storage failed. UserId: {UserId}, FileName: {FileName}", userId, fileName);
                    await _audit.LogOperationAsync("Upload", userId, null, fileStream.Length, ipAddress, "Failure", "File storage error");
                    return 0;
                }

                // 4. Determine MIME type
                var mimeType = GetMimeType(fileName);

                // 5. Create Document record in database
                var document = new Document
                {
                    Title = title ?? Path.GetFileNameWithoutExtension(fileName),
                    Description = description ?? string.Empty,
                    Category = category ?? DEFAULT_CATEGORY,
                    FilePath = filePath,
                    FileSize = fileStream.Length,
                    MimeType = mimeType,
                    UploadDate = DateTime.UtcNow,
                    UploadedByUserId = userId,
                    ProjectId = projectId,
                    IsDeleted = false,
                    DeletedDate = null,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();
                documentId = document.DocumentId;

                // 6. Add tags if provided
                if (tags != null && tags.Length > 0)
                {
                    foreach (var tag in tags.Where(t => !string.IsNullOrWhiteSpace(t)).Take(10)) // Max 10 tags
                    {
                        var docTag = new DocumentTag
                        {
                            DocumentId = documentId,
                            TagName = tag.ToLower().Trim()
                        };
                        _context.DocumentTags.Add(docTag);
                    }
                    await _context.SaveChangesAsync();
                }

                // 7. Update quota
                await _quota.AddUsageAsync(userId, fileStream.Length);

                // 8. Log successful upload
                await _audit.LogOperationAsync("Upload", userId, documentId, fileStream.Length, ipAddress, "Success", 
                    $"File: {fileName}, Size: {fileStream.Length} bytes");

                _logger.LogInformation("Document uploaded successfully. DocumentId: {DocumentId}, UserId: {UserId}, FileSize: {FileSize}",
                    documentId, userId, fileStream.Length);

                return documentId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document. UserId: {UserId}, FileName: {FileName}", userId, fileName);
                await _audit.LogOperationAsync("Upload", userId, documentId, 0, ipAddress, "Failure", $"Error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Download document with authorization and audit logging
        /// </summary>
        public async Task<(Stream fileStream, string mimeType, string fileName)> DownloadDocumentAsync(
            int documentId, int userId, string ipAddress = "0.0.0.0")
        {
            try
            {
                // 1. Authorization check
                if (!await _authorization.CanDownloadDocumentAsync(documentId, userId))
                {
                    _logger.LogWarning("Download unauthorized. DocumentId: {DocumentId}, UserId: {UserId}", documentId, userId);
                    await _audit.LogOperationAsync("Download", userId, documentId, 0, ipAddress, "Blocked", "Unauthorized");
                    return (null, null, null);
                }

                // 2. Get document
                var document = await _context.Documents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

                if (document == null)
                {
                    _logger.LogWarning("Document not found for download. DocumentId: {DocumentId}", documentId);
                    await _audit.LogOperationAsync("Download", userId, documentId, 0, ipAddress, "Failure", "Document not found");
                    return (null, null, null);
                }

                // 3. Retrieve file from storage
                var fileStream = await _fileStorage.GetFileAsync(document.FilePath);
                if (fileStream == null)
                {
                    _logger.LogError("File retrieval failed. DocumentId: {DocumentId}, FilePath: {FilePath}", 
                        documentId, document.FilePath);
                    await _audit.LogOperationAsync("Download", userId, documentId, document.FileSize, ipAddress, "Failure", "File not found in storage");
                    return (null, null, null);
                }

                // 4. Log download
                await _audit.LogOperationAsync("Download", userId, documentId, document.FileSize, ipAddress, "Success", 
                    $"File: {document.Title}, Size: {document.FileSize} bytes");

                _logger.LogInformation("Document downloaded. DocumentId: {DocumentId}, UserId: {UserId}", documentId, userId);

                return (fileStream, document.MimeType, document.Title + GetFileExtension(document.FilePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document. DocumentId: {DocumentId}, UserId: {UserId}", documentId, userId);
                await _audit.LogOperationAsync("Download", userId, documentId, 0, ipAddress, "Failure", $"Error: {ex.Message}");
                return (null, null, null);
            }
        }

        /// <summary>
        /// Get document details with authorization check
        /// </summary>
        public async Task<Document> GetDocumentAsync(int documentId, int userId)
        {
            try
            {
                if (!await _authorization.CanViewDocumentAsync(documentId, userId))
                {
                    return null;
                }

                return await _context.Documents
                    .AsNoTracking()
                    .Include(d => d.Tags)
                    .Include(d => d.Shares)
                    .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document. DocumentId: {DocumentId}, UserId: {UserId}", documentId, userId);
                return null;
            }
        }

        /// <summary>
        /// List all documents accessible by user
        /// </summary>
        public async Task<List<Document>> ListDocumentsAsync(int userId, int? projectId = null, 
            string category = null, string tag = null, string searchQuery = null)
        {
            try
            {
                var query = _context.Documents
                    .AsNoTracking()
                    .Include(d => d.Tags)
                    .Where(d => !d.IsDeleted);

                // User can see: (1) documents they uploaded, (2) documents shared with them, (3) as admin, all documents
                var isAdmin = await _authorization.IsAdministratorAsync(userId);

                if (!isAdmin)
                {
                    // Non-admin: only owned or shared documents
                    var sharedDocIds = await _context.DocumentShares
                        .AsNoTracking()
                        .Where(s => s.SharedWithUserId == userId && !s.IsRevoked)
                        .Select(s => s.DocumentId)
                        .ToListAsync();

                    query = query.Where(d => d.UploadedByUserId == userId || sharedDocIds.Contains(d.DocumentId));
                }

                // Apply filters
                if (projectId.HasValue)
                {
                    query = query.Where(d => d.ProjectId == projectId);
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(d => d.Category == category);
                }

                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    var searchLower = searchQuery.ToLower();
                    query = query.Where(d => d.Title.ToLower().Contains(searchLower) 
                        || d.Description.ToLower().Contains(searchLower));
                }

                if (!string.IsNullOrWhiteSpace(tag))
                {
                    var tagLower = tag.ToLower();
                    query = query.Where(d => d.Tags.Any(t => t.TagName == tagLower));
                }

                var documents = await query
                    .OrderByDescending(d => d.UploadDate)
                    .ToListAsync();

                _logger.LogInformation("Listed {Count} documents for user {UserId}", documents.Count, userId);
                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing documents for user {UserId}", userId);
                return new List<Document>();
            }
        }

        /// <summary>
        /// Soft-delete document
        /// </summary>
        public async Task<bool> DeleteDocumentAsync(int documentId, int userId, string ipAddress = "0.0.0.0")
        {
            try
            {
                if (!await _authorization.CanDeleteDocumentAsync(documentId, userId))
                {
                    _logger.LogWarning("Delete unauthorized. DocumentId: {DocumentId}, UserId: {UserId}", documentId, userId);
                    await _audit.LogOperationAsync("Delete", userId, documentId, 0, ipAddress, "Blocked", "Unauthorized");
                    return false;
                }

                var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId);
                if (document == null)
                {
                    return false;
                }

                document.IsDeleted = true;
                document.DeletedDate = DateTime.UtcNow;
                document.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Don't delete file immediately - just mark as deleted for soft-delete recovery
                // File will be purged by scheduled job after 30 days

                // Update quota to remove this file
                await _quota.RemoveUsageAsync(userId, document.FileSize);

                // Audit log
                await _audit.LogOperationAsync("Delete", userId, documentId, document.FileSize, ipAddress, "Success", 
                    $"Soft-deleted, recoverable within 30 days");

                _logger.LogInformation("Document soft-deleted. DocumentId: {DocumentId}, UserId: {UserId}", documentId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document. DocumentId: {DocumentId}, UserId: {UserId}", documentId, userId);
                await _audit.LogOperationAsync("Delete", userId, documentId, 0, ipAddress, "Failure", $"Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restore soft-deleted document (within 30-day window)
        /// </summary>
        public async Task<bool> RestoreDocumentAsync(int documentId, int userId, string ipAddress = "0.0.0.0")
        {
            try
            {
                if (!await _authorization.CanRestoreDocumentAsync(documentId, userId))
                {
                    _logger.LogWarning("Restore unauthorized. DocumentId: {DocumentId}, UserId: {UserId}", documentId, userId);
                    await _audit.LogOperationAsync("Restore", userId, documentId, 0, ipAddress, "Blocked", "Unauthorized");
                    return false;
                }

                var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId && d.IsDeleted);
                if (document == null)
                {
                    return false;
                }

                // Check if within 30-day recovery window
                var daysDeleted = (DateTime.UtcNow - document.DeletedDate.Value).TotalDays;
                if (daysDeleted > 30)
                {
                    _logger.LogWarning("Restore failed: recovery window expired. DocumentId: {DocumentId}, DaysDeleted: {DaysDeleted}", 
                        documentId, daysDeleted);
                    await _audit.LogOperationAsync("Restore", userId, documentId, 0, ipAddress, "Blocked", "Recovery window (30 days) expired");
                    return false;
                }

                document.IsDeleted = false;
                document.DeletedDate = null;
                document.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Restore quota
                await _quota.AddUsageAsync(userId, document.FileSize);

                // Audit log
                await _audit.LogOperationAsync("Restore", userId, documentId, document.FileSize, ipAddress, "Success", 
                    "Document restored from soft-delete");

                _logger.LogInformation("Document restored. DocumentId: {DocumentId}, UserId: {UserId}", documentId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring document. DocumentId: {DocumentId}, UserId: {UserId}", documentId, userId);
                await _audit.LogOperationAsync("Restore", userId, documentId, 0, ipAddress, "Failure", $"Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update document metadata
        /// </summary>
        public async Task<bool> UpdateDocumentAsync(int documentId, int userId, Document updatedDoc)
        {
            try
            {
                if (!await _authorization.CanEditDocumentAsync(documentId, userId))
                {
                    return false;
                }

                var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);
                if (document == null)
                {
                    return false;
                }

                document.Title = updatedDoc.Title ?? document.Title;
                document.Description = updatedDoc.Description ?? document.Description;
                document.Category = updatedDoc.Category ?? document.Category;
                document.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Document updated. DocumentId: {DocumentId}, UserId: {UserId}", documentId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document. DocumentId: {DocumentId}, UserId: {UserId}", documentId, userId);
                return false;
            }
        }

        /// <summary>
        /// Share document with another user
        /// </summary>
        public async Task<int> ShareDocumentAsync(int documentId, int userId, int shareWithUserId, string ipAddress = "0.0.0.0")
        {
            try
            {
                if (!await _authorization.CanShareDocumentAsync(documentId, userId))
                {
                    await _audit.LogOperationAsync("Share", userId, documentId, 0, ipAddress, "Blocked", "Unauthorized");
                    return 0;
                }

                if (userId == shareWithUserId)
                {
                    _logger.LogWarning("Cannot share document with self. DocumentId: {DocumentId}, UserId: {UserId}", documentId, userId);
                    await _audit.LogOperationAsync("Share", userId, documentId, 0, ipAddress, "Blocked", "Cannot share with self");
                    return 0;
                }

                var share = new DocumentShare
                {
                    DocumentId = documentId,
                    SharedByUserId = userId,
                    SharedWithUserId = shareWithUserId,
                    SharedDate = DateTime.UtcNow,
                    IsRevoked = false
                };

                _context.DocumentShares.Add(share);
                await _context.SaveChangesAsync();

                await _audit.LogOperationAsync("Share", userId, documentId, 0, ipAddress, "Success", 
                    $"Shared with UserId: {shareWithUserId}");

                _logger.LogInformation("Document shared. DocumentId: {DocumentId}, SharedBy: {UserId}, SharedWith: {ShareWithUserId}",
                    documentId, userId, shareWithUserId);

                return share.ShareId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing document. DocumentId: {DocumentId}, UserId: {UserId}", documentId, userId);
                await _audit.LogOperationAsync("Share", userId, documentId, 0, ipAddress, "Failure", $"Error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Revoke document share
        /// </summary>
        public async Task<bool> RevokeShareAsync(int shareId, int userId, string ipAddress = "0.0.0.0")
        {
            try
            {
                if (!await _authorization.CanRevokeShareAsync(shareId, userId))
                {
                    await _audit.LogOperationAsync("Unshare", userId, null, 0, ipAddress, "Blocked", "Unauthorized");
                    return false;
                }

                var share = await _context.DocumentShares.FirstOrDefaultAsync(s => s.ShareId == shareId && !s.IsRevoked);
                if (share == null)
                {
                    return false;
                }

                share.IsRevoked = true;
                share.RevokedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _audit.LogOperationAsync("Unshare", userId, share.DocumentId, 0, ipAddress, "Success", 
                    $"Share revoked with UserId: {share.SharedWithUserId}");

                _logger.LogInformation("Document share revoked. ShareId: {ShareId}, UserId: {UserId}", shareId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking share. ShareId: {ShareId}, UserId: {UserId}", shareId, userId);
                await _audit.LogOperationAsync("Unshare", userId, null, 0, ipAddress, "Failure", $"Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get all shares for a document
        /// </summary>
        public async Task<List<DocumentShare>> GetDocumentSharesAsync(int documentId, int userId)
        {
            try
            {
                if (!await _authorization.CanEditDocumentAsync(documentId, userId))
                {
                    return new List<DocumentShare>();
                }

                return await _context.DocumentShares
                    .AsNoTracking()
                    .Where(s => s.DocumentId == documentId && !s.IsRevoked)
                    .Include(s => s.SharedWithUser)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shares for document. DocumentId: {DocumentId}", documentId);
                return new List<DocumentShare>();
            }
        }

        /// <summary>
        /// Get user's quota status
        /// </summary>
        public async Task<(long usedBytes, long quotaBytes, int percentageUsed)> GetQuotaStatusAsync(int userId)
        {
            var (usedBytes, quotaBytes) = await _quota.GetQuotaStatusAsync(userId);
            var percentage = await _quota.GetQuotaPercentageAsync(userId);
            return (usedBytes, quotaBytes, percentage);
        }

        // Helper methods
        private string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".zip" => "application/zip",
                ".csv" => "text/csv",
                _ => "application/octet-stream"
            };
        }

        private string GetFileExtension(string filePath)
        {
            return Path.GetExtension(filePath);
        }
    }
}
