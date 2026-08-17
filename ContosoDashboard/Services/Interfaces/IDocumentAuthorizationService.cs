using System.Threading.Tasks;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services.Interfaces
{
    /// <summary>
    /// Authorization checks for document operations following 4-layer IDOR prevention pattern
    /// Layer 1: Middleware - [Authorize] attributes
    /// Layer 2: Page/Controller - Role-based checks
    /// Layer 3: Service - Ownership/membership verification (this interface)
    /// Layer 4: Query - EF Core LINQ filtering
    /// </summary>
    public interface IDocumentAuthorizationService
    {
        /// <summary>
        /// Check if user can view a document (owner or recipient of share)
        /// </summary>
        Task<bool> CanViewDocumentAsync(int documentId, int userId);

        /// <summary>
        /// Check if user can download a document (owner or recipient of share)
        /// </summary>
        Task<bool> CanDownloadDocumentAsync(int documentId, int userId);

        /// <summary>
        /// Check if user can edit/modify a document (owner or admin only)
        /// </summary>
        Task<bool> CanEditDocumentAsync(int documentId, int userId);

        /// <summary>
        /// Check if user can delete a document (owner or admin only)
        /// </summary>
        Task<bool> CanDeleteDocumentAsync(int documentId, int userId);

        /// <summary>
        /// Check if user can restore a soft-deleted document (owner or admin only)
        /// </summary>
        Task<bool> CanRestoreDocumentAsync(int documentId, int userId);

        /// <summary>
        /// Check if user can share a document with others (owner or admin only)
        /// </summary>
        Task<bool> CanShareDocumentAsync(int documentId, int userId);

        /// <summary>
        /// Check if user can revoke access to a document (grantor or admin only)
        /// </summary>
        Task<bool> CanRevokeShareAsync(int shareId, int userId);

        /// <summary>
        /// Check if user is project member (for project-scoped document access)
        /// </summary>
        Task<bool> IsProjectMemberAsync(int projectId, int userId);

        /// <summary>
        /// Check if user is project manager (for admin operations)
        /// </summary>
        Task<bool> IsProjectManagerAsync(int projectId, int userId);

        /// <summary>
        /// Check if user is system administrator
        /// </summary>
        Task<bool> IsAdministratorAsync(int userId);
    }
}
