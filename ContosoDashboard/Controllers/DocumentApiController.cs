using System.Security.Claims;
using ContosoDashboard.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContosoDashboard.Controllers;

/// <summary>
/// REST API endpoints for document upload, download, and management operations.
/// All endpoints require [Authorize] for Layer 1 IDOR prevention.
/// Layer 3 service-level authorization checks implemented in DocumentService.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentApiController(IDocumentService documentService) : ControllerBase
{
    private readonly IDocumentService _documentService = documentService;

    /// <summary>
    /// Get current user ID from claims.
    /// </summary>
    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim?.Value, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get client IP address for audit logging.
    /// </summary>
    private string GetClientIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.ToString().Split(',')[0];
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// POST /api/documents/upload
    /// Upload a new document with metadata and tags.
    /// 
    /// Form fields:
    /// - file: IFormFile (required, < 500MB)
    /// - title: string (required, max 200 chars)
    /// - description: string (optional, max 1000 chars)
    /// - category: string (optional, max 100 chars)
    /// - projectId: int? (optional, for project-specific docs)
    /// - tags: string (optional, comma-separated, max 10 tags)
    /// 
    /// Response: 
    /// - 201 Created: { documentId, message }
    /// - 400 Bad Request: File validation errors
    /// - 401 Unauthorized: User not authenticated
    /// - 413 Payload Too Large: File > 500MB
    /// </summary>
    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload(IFormFile file, string title, string? description = null, 
        string? category = null, int? projectId = null, string? tags = null)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized(new { message = "User not authenticated" });

        // Validate input
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "File is required" });

        if (string.IsNullOrWhiteSpace(title))
            return BadRequest(new { message = "Title is required" });

        if (title.Length > 200)
            return BadRequest(new { message = "Title must not exceed 200 characters" });

        // Parse tags
        var tagList = string.IsNullOrWhiteSpace(tags)
            ? []
            : tags.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).Take(10).ToArray();

        // Call document service
        var documentId = await _documentService.UploadDocumentAsync(
            fileStream: file.OpenReadStream(),
            fileName: file.FileName,
            title: title,
            description: description,
            category: category,
            projectId: projectId,
            userId: userId,
            tags: tagList
        );

        if (documentId == 0)
            return BadRequest(new { message = "Upload failed. Check file size (max 500MB) and quota." });

        return Created($"/api/documents/{documentId}", new { documentId, message = "Document uploaded successfully" });
    }

    /// <summary>
    /// GET /api/documents/{id}/download
    /// Download a document file with authorization check.
    /// 
    /// Query parameters:
    /// - id: int (path parameter, document ID)
    /// 
    /// Response:
    /// - 200 OK: File stream with correct MIME type
    /// - 401 Unauthorized: User not authenticated
    /// - 403 Forbidden: User does not have access to document
    /// - 404 Not Found: Document not found or soft-deleted
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized(new { message = "User not authenticated" });

        try
        {
            var (stream, mimeType, fileName) = await _documentService.DownloadDocumentAsync(id, userId);

            if (stream == null)
                return NotFound(new { message = "Document not found or access denied" });

            return File(stream, mimeType, fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Download failed", error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/documents
    /// List documents with pagination and filtering.
    /// Only returns documents user has access to (owned, shared, or admin).
    /// 
    /// Query parameters:
    /// - projectId: int? (filter by project)
    /// - category: string? (filter by category)
    /// - tag: string? (filter by tag)
    /// - search: string? (search in title/description)
    /// - skip: int (pagination offset, default 0)
    /// - take: int (page size, default 20, max 100)
    /// 
    /// Response:
    /// - 200 OK: { items: Document[], total: int, hasMore: bool }
    /// - 401 Unauthorized: User not authenticated
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(int? projectId = null, string? category = null, 
        string? tag = null, string? search = null, int skip = 0, int take = 20)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized(new { message = "User not authenticated" });

        // Validate pagination
        take = Math.Min(take, 100);
        skip = Math.Max(skip, 0);

        try
        {
            var documents = await _documentService.ListDocumentsAsync(
                userId: userId,
                projectId: projectId,
                category: category,
                tag: tag,
                searchQuery: search
            );

            var total = documents.Count;
            var items = documents.Skip(skip).Take(take).ToList();

            return Ok(new
            {
                items,
                total,
                skip,
                take,
                hasMore = (skip + take) < total
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "List failed", error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/documents/{id}
    /// Get document details with metadata and audit trail visibility.
    /// 
    /// Query parameters:
    /// - id: int (path parameter, document ID)
    /// 
    /// Response:
    /// - 200 OK: { documentId, title, category, tags, shares, uploadedBy, uploadDate }
    /// - 401 Unauthorized: User not authenticated
    /// - 403 Forbidden: User does not have access
    /// - 404 Not Found: Document not found
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized(new { message = "User not authenticated" });

        try
        {
            var document = await _documentService.GetDocumentAsync(id, userId);

            if (document == null)
                return NotFound(new { message = "Document not found or access denied" });

            return Ok(document);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Get failed", error = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/documents/{id}
    /// Soft-delete a document (mark IsDeleted, preserve file for 30 days).
    /// Only document owner or admin can delete.
    /// 
    /// Query parameters:
    /// - id: int (path parameter, document ID)
    /// 
    /// Response:
    /// - 204 No Content: Document soft-deleted successfully
    /// - 401 Unauthorized: User not authenticated
    /// - 403 Forbidden: User does not own document
    /// - 404 Not Found: Document not found
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized(new { message = "User not authenticated" });

        try
        {
            var success = await _documentService.DeleteDocumentAsync(id, userId);

            if (!success)
                return NotFound(new { message = "Document not found or access denied" });

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Delete failed", error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/documents/{id}/restore
    /// Restore a soft-deleted document (within 30 days).
    /// Only document owner or admin can restore.
    /// 
    /// Query parameters:
    /// - id: int (path parameter, document ID)
    /// 
    /// Response:
    /// - 204 No Content: Document restored successfully
    /// - 401 Unauthorized: User not authenticated
    /// - 403 Forbidden: User does not own document
    /// - 404 Not Found: Document not found or not deleted
    /// - 410 Gone: Document recovery window (30 days) expired
    /// </summary>
    [HttpPost("{id}/restore")]
    public async Task<IActionResult> Restore(int id)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized(new { message = "User not authenticated" });

        try
        {
            var success = await _documentService.RestoreDocumentAsync(id, userId);

            if (!success)
                return NotFound(new { message = "Document not found, access denied, or recovery window expired" });

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Restore failed", error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/documents/{id}/share
    /// Share a document with another user.
    /// Only document owner or admin can share.
    /// 
    /// Request body:
    /// { "sharedWithUserId": int }
    /// 
    /// Response:
    /// - 201 Created: { shareId }
    /// - 400 Bad Request: Cannot self-share or invalid user
    /// - 401 Unauthorized: User not authenticated
    /// - 403 Forbidden: User does not own document
    /// - 404 Not Found: Document not found
    /// </summary>
    [HttpPost("{id}/share")]
    public async Task<IActionResult> Share(int id, [FromBody] ShareRequest request)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized(new { message = "User not authenticated" });

        if (request?.SharedWithUserId == 0)
            return BadRequest(new { message = "SharedWithUserId is required" });

        try
        {
            var shareId = await _documentService.ShareDocumentAsync(id, userId, request!.SharedWithUserId);

            if (shareId == 0)
                return BadRequest(new { message = "Share failed. Check permissions or user ID." });

            return Created($"/api/documents/{id}/shares/{shareId}", new { shareId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Share failed", error = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/documents/{id}/share/{shareId}
    /// Revoke a document share (set IsRevoked = true).
    /// Only share initiator or admin can revoke.
    /// 
    /// Query parameters:
    /// - id: int (path parameter, document ID)
    /// - shareId: int (path parameter, share ID)
    /// 
    /// Response:
    /// - 204 No Content: Share revoked successfully
    /// - 401 Unauthorized: User not authenticated
    /// - 403 Forbidden: User did not initiate share
    /// - 404 Not Found: Share not found
    /// </summary>
    [HttpDelete("{id}/share/{shareId}")]
    public async Task<IActionResult> RevokeShare(int id, int shareId)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized(new { message = "User not authenticated" });

        try
        {
            var success = await _documentService.RevokeShareAsync(shareId, userId);

            if (!success)
                return NotFound(new { message = "Share not found or access denied" });

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Revoke failed", error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/documents/quota/status
    /// Get current user's storage quota status.
    /// 
    /// Response:
    /// - 200 OK: { usedBytes, quotaBytes, percentageUsed, remainingBytes }
    /// - 401 Unauthorized: User not authenticated
    /// </summary>
    [HttpGet("quota/status")]
    public async Task<IActionResult> QuotaStatus()
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized(new { message = "User not authenticated" });

        try
        {
            var (usedBytes, quotaBytes, percentageUsed) = await _documentService.GetQuotaStatusAsync(userId);
            var remainingBytes = quotaBytes - usedBytes;

            return Ok(new
            {
                usedBytes,
                quotaBytes,
                percentageUsed,
                remainingBytes,
                remaining = FormatBytes(remainingBytes),
                used = FormatBytes(usedBytes),
                quota = FormatBytes(quotaBytes)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Quota status failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Format bytes to human-readable size (B, KB, MB, GB).
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Request body model for share endpoint.
/// </summary>
public class ShareRequest
{
    public int SharedWithUserId { get; set; }
}
