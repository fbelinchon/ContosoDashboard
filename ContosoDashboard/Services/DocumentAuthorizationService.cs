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
    /// Implements 4-layer IDOR prevention for document access control
    /// Verifies ownership, sharing relationships, and role-based permissions
    /// </summary>
    public class DocumentAuthorizationService : IDocumentAuthorizationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentAuthorizationService> _logger;

        public DocumentAuthorizationService(ApplicationDbContext context, ILogger<DocumentAuthorizationService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// User can view if: (1) owner, (2) share recipient, (3) admin
        /// </summary>
        public async Task<bool> CanViewDocumentAsync(int documentId, int userId)
        {
            try
            {
                var document = await _context.Documents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DocumentId == documentId);

                if (document == null)
                {
                    _logger.LogWarning("Document not found for view check. DocumentId: {DocumentId}, UserId: {UserId}", 
                        documentId, userId);
                    return false;
                }

                // Owner can always view
                if (document.UploadedByUserId == userId)
                {
                    return true;
                }

                // Admin can always view
                if (await IsAdministratorAsync(userId))
                {
                    return true;
                }

                // Check if document is shared with user (not revoked)
                var isShared = await _context.DocumentShares
                    .AsNoTracking()
                    .AnyAsync(s => s.DocumentId == documentId 
                        && s.SharedWithUserId == userId 
                        && !s.IsRevoked);

                return isShared;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking view permission. DocumentId: {DocumentId}, UserId: {UserId}", 
                    documentId, userId);
                return false;
            }
        }

        /// <summary>
        /// User can download if: (1) can view (inherits view permissions)
        /// </summary>
        public async Task<bool> CanDownloadDocumentAsync(int documentId, int userId)
        {
            return await CanViewDocumentAsync(documentId, userId);
        }

        /// <summary>
        /// User can edit if: (1) owner or (2) admin
        /// Editing includes renaming, changing category, modifying tags
        /// </summary>
        public async Task<bool> CanEditDocumentAsync(int documentId, int userId)
        {
            try
            {
                var document = await _context.Documents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DocumentId == documentId);

                if (document == null)
                {
                    return false;
                }

                // Owner can edit
                if (document.UploadedByUserId == userId)
                {
                    return true;
                }

                // Admin can edit
                return await IsAdministratorAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking edit permission. DocumentId: {DocumentId}, UserId: {UserId}", 
                    documentId, userId);
                return false;
            }
        }

        /// <summary>
        /// User can delete if: (1) owner or (2) admin
        /// Delete means soft-delete (mark IsDeleted = true)
        /// </summary>
        public async Task<bool> CanDeleteDocumentAsync(int documentId, int userId)
        {
            return await CanEditDocumentAsync(documentId, userId);
        }

        /// <summary>
        /// User can restore soft-deleted document if: (1) owner or (2) admin
        /// Restore within 30-day window only (enforced by service layer)
        /// </summary>
        public async Task<bool> CanRestoreDocumentAsync(int documentId, int userId)
        {
            return await CanEditDocumentAsync(documentId, userId);
        }

        /// <summary>
        /// User can share if: (1) owner or (2) admin
        /// Share means creating DocumentShare entries (other users can access via reciprocal FK)
        /// </summary>
        public async Task<bool> CanShareDocumentAsync(int documentId, int userId)
        {
            return await CanEditDocumentAsync(documentId, userId);
        }

        /// <summary>
        /// User can revoke share if: (1) initiated share (SharedByUserId) or (2) admin
        /// Revoke means setting IsRevoked = true on DocumentShare
        /// </summary>
        public async Task<bool> CanRevokeShareAsync(int shareId, int userId)
        {
            try
            {
                var share = await _context.DocumentShares
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.ShareId == shareId);

                if (share == null)
                {
                    return false;
                }

                // Share initiator (grantor) can revoke
                if (share.SharedByUserId == userId)
                {
                    return true;
                }

                // Admin can revoke
                return await IsAdministratorAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking revoke permission. ShareId: {ShareId}, UserId: {UserId}", 
                    shareId, userId);
                return false;
            }
        }

        /// <summary>
        /// Check if user is member of a project (can access project documents)
        /// </summary>
        public async Task<bool> IsProjectMemberAsync(int projectId, int userId)
        {
            try
            {
                // User is member if: (1) project manager or (2) in ProjectMembers
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    return false;
                }

                // Project manager is implicitly a member
                var project = await _context.Projects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.ProjectManagerId == userId);

                if (project != null)
                {
                    return true;
                }

                // Check ProjectMembers table
                var isMember = await _context.ProjectMembers
                    .AsNoTracking()
                    .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

                return isMember;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking project membership. ProjectId: {ProjectId}, UserId: {UserId}", 
                    projectId, userId);
                return false;
            }
        }

        /// <summary>
        /// Check if user is the project manager
        /// </summary>
        public async Task<bool> IsProjectManagerAsync(int projectId, int userId)
        {
            try
            {
                var project = await _context.Projects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.ProjectManagerId == userId);

                return project != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking project manager status. ProjectId: {ProjectId}, UserId: {UserId}", 
                    projectId, userId);
                return false;
            }
        }

        /// <summary>
        /// Check if user is system administrator
        /// </summary>
        public async Task<bool> IsAdministratorAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.Role == UserRole.Administrator);

                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking admin status. UserId: {UserId}", userId);
                return false;
            }
        }
    }
}
