# Document Management API Contracts

**Feature**: 001-document-management  
**Date**: 2026-08-14  
**Architecture**: RESTful HTTP API (Blazor Server backend services)

## Overview

This document specifies the HTTP API contracts for the Document Management feature. These contracts define the request/response format for all document operations accessible to the frontend (Blazor UI) and potentially external services.

## Base URL

```
http://localhost:5000/api/documents
```

(Actual port configured in launchSettings.json)

## Authentication

All endpoints require authenticated session (cookie-based mock auth already in place via ContosoDashboard).

**Request Header**: Automatically included by Blazor Server (same-origin requests)

## Error Response Format

All error responses follow this format:

```json
{
  "error": {
    "code": "DOCUMENT_NOT_FOUND",
    "message": "The document with ID 42 does not exist or you don't have permission to access it",
    "statusCode": 404,
    "details": {
      "documentId": 42
    }
  }
}
```

**Common Error Codes**:
- `UNAUTHORIZED` (401): User not authenticated
- `FORBIDDEN` (403): User authenticated but not authorized for this resource
- `DOCUMENT_NOT_FOUND` (404): Document ID doesn't exist or is soft-deleted
- `INVALID_UPLOAD` (400): File type/size validation failed
- `QUOTA_EXCEEDED` (402): User or organization quota full
- `VIRUS_SCAN_FAILED` (422): File failed virus scan
- `INTERNAL_ERROR` (500): Unexpected server error

## Endpoint Contracts

### 1. List User Documents

**Endpoint**: `GET /api/documents`

**Purpose**: Retrieve all documents uploaded by the current user (excluding soft-deleted)

**Query Parameters**:
- `category` (optional, string): Filter by category: "Project Documents", "Team Resources", "Personal Files", "Reports", "Presentations", "Other"
- `projectId` (optional, integer): Filter by associated project
- `sortBy` (optional, string): Sort field: "title", "uploadDate", "category", "fileSize" (default: "uploadDate")
- `sortOrder` (optional, string): "asc" or "desc" (default: "desc")
- `pageNumber` (optional, integer): Pagination page (default: 1)
- `pageSize` (optional, integer): Results per page (default: 20, max: 100)

**Response** (200 OK):
```json
{
  "documents": [
    {
      "documentId": 1,
      "title": "Q3 Report",
      "description": "Q3 2026 financial report",
      "category": "Reports",
      "fileSize": 2502656,
      "uploadDate": "2026-08-10T14:30:00Z",
      "projectId": 5,
      "projectName": "Financial Planning",
      "uploadedByName": "Camille Nicole",
      "tags": ["finance", "quarterly"],
      "isSharedWithMe": false,
      "sharedCount": 2
    }
  ],
  "totalCount": 15,
  "pageNumber": 1,
  "pageSize": 20
}
```

**Error Cases**:
- 401 Unauthorized (not logged in)

---

### 2. Search Documents

**Endpoint**: `POST /api/documents/search`

**Purpose**: Full-text search across document titles, descriptions, tags, uploader names

**Request Body**:
```json
{
  "query": "budget",
  "category": null,
  "projectId": null,
  "uploadedByUserId": null,
  "dateRangeStart": "2026-08-01",
  "dateRangeEnd": "2026-08-31",
  "pageNumber": 1,
  "pageSize": 20
}
```

**Response** (200 OK):
```json
{
  "results": [
    {
      "documentId": 3,
      "title": "2026 Budget Plan",
      "description": "Annual budget allocation across departments",
      "category": "Reports",
      "fileSize": 1048576,
      "uploadDate": "2026-08-05T10:00:00Z",
      "projectId": null,
      "uploadedByName": "Floris Kregel",
      "tags": ["budget", "2026"],
      "searchRelevance": 0.95
    }
  ],
  "totalMatches": 7,
  "pageNumber": 1,
  "pageSize": 20,
  "executionTimeMs": 245
}
```

**Performance Requirement**: Must complete within 2000 ms.

**Error Cases**:
- 400 Bad Request (invalid query format)
- 401 Unauthorized

---

### 3. Upload Document

**Endpoint**: `POST /api/documents/upload`

**Purpose**: Upload a single document file with metadata

**Request** (multipart/form-data):
```
Form Fields:
  - file (file): The document file (required)
  - title (string): Document title (required, 1-255 chars)
  - description (string): Optional description (optional, max 5000 chars)
  - category (string): Required category (required)
  - projectId (integer): Associated project (optional)
  - tags (string, comma-separated): Custom tags (optional)
```

**Response** (201 Created):
```json
{
  "documentId": 42,
  "title": "Meeting Notes",
  "category": "Team Resources",
  "fileSize": 512000,
  "uploadDate": "2026-08-14T15:45:00Z",
  "projectId": 5,
  "tags": ["meeting", "august"],
  "message": "Document uploaded successfully"
}
```

**Error Cases**:
- 400 Bad Request (validation failed: missing title, invalid category)
- 401 Unauthorized (not authenticated)
- 402 Payment Required / 413 Payload Too Large (quota exceeded or file > 25 MB)
- 415 Unsupported Media Type (file type not in whitelist)
- 422 Unprocessable Entity (virus scan failed / quarantined)
- 500 Internal Server Error

**Validation**:
- File size: ≤ 25 MB
- File type: PDF, Office (.docx, .xlsx, .pptx), .txt, JPEG, PNG
- Title: 1-255 characters, not empty
- Category: must be one of predefined values
- User quota: (UsedBytes + FileSize) ≤ 5 GB
- Organization quota: (org total + FileSize) ≤ 100 GB

---

### 4. Download Document

**Endpoint**: `GET /api/documents/{documentId}/download`

**Purpose**: Download a document file with authorization check

**URL Parameters**:
- `documentId` (integer): Document to download

**Response** (200 OK):
- Content-Type: Depends on file (e.g., "application/pdf", "application/vnd.ms-word")
- Content-Disposition: attachment; filename="original-name.pdf"
- Body: Raw file bytes

**Side Effects**:
- Logs "Download" action in DocumentAuditLog
- Increments download counter (if tracked)

**Error Cases**:
- 401 Unauthorized (not logged in)
- 403 Forbidden (logged in but not authorized to access this document)
- 404 Not Found (document doesn't exist or is soft-deleted)

---

### 5. Preview Document

**Endpoint**: `GET /api/documents/{documentId}/preview`

**Purpose**: Retrieve document metadata for preview (without downloading file)

**URL Parameters**:
- `documentId` (integer): Document to preview

**Response** (200 OK):
```json
{
  "documentId": 1,
  "title": "Q3 Report",
  "description": "Q3 2026 financial report",
  "category": "Reports",
  "fileSize": 2502656,
  "mimeType": "application/pdf",
  "uploadDate": "2026-08-10T14:30:00Z",
  "uploadedByUserId": 5,
  "uploadedByName": "Camille Nicole",
  "projectId": 5,
  "projectName": "Financial Planning",
  "tags": ["finance", "quarterly"],
  "accessHistory": [
    {
      "userId": 3,
      "userName": "Ni Kang",
      "accessDate": "2026-08-14T10:00:00Z",
      "action": "Download"
    }
  ]
}
```

**Side Effects**:
- Logs "Preview" action in DocumentAuditLog

**Error Cases**:
- 401 Unauthorized
- 403 Forbidden
- 404 Not Found

---

### 6. Update Document Metadata

**Endpoint**: `PATCH /api/documents/{documentId}`

**Purpose**: Update document metadata (title, description, category, tags)

**Request Body**:
```json
{
  "title": "Q3 2026 Report",
  "description": "Updated financial report for Q3",
  "category": "Reports",
  "tags": ["finance", "quarterly", "2026"]
}
```

**Response** (200 OK):
```json
{
  "documentId": 1,
  "title": "Q3 2026 Report",
  "description": "Updated financial report for Q3",
  "category": "Reports",
  "tags": ["finance", "quarterly", "2026"],
  "modifiedDate": "2026-08-14T16:00:00Z",
  "message": "Document metadata updated"
}
```

**Authorization**: Only document owner can update metadata.

**Error Cases**:
- 400 Bad Request (validation failed)
- 401 Unauthorized
- 403 Forbidden (not document owner)
- 404 Not Found

---

### 7. Delete Document (Soft Delete)

**Endpoint**: `DELETE /api/documents/{documentId}`

**Purpose**: Soft-delete a document (30-day recovery window)

**Request Body** (optional):
```json
{
  "reason": "No longer needed"
}
```

**Response** (204 No Content)

**Side Effects**:
- Sets IsDeleted = 1, DeletedDate = now()
- Logs "Delete" action in DocumentAuditLog
- Document excluded from user lists; admin can still view in audit trail

**Authorization**: Document owner or Project Manager (if project doc) or Admin

**Error Cases**:
- 401 Unauthorized
- 403 Forbidden (not authorized to delete)
- 404 Not Found

---

### 8. Restore Document

**Endpoint**: `POST /api/documents/{documentId}/restore`

**Purpose**: Restore a soft-deleted document (within 30-day window)

**Response** (200 OK):
```json
{
  "documentId": 1,
  "title": "Q3 Report",
  "message": "Document restored successfully",
  "recoveryWindowExpiresAt": "2026-09-13T14:30:00Z"
}
```

**Side Effects**:
- Sets IsDeleted = 0, DeletedDate = NULL
- Logs "Restore" action in DocumentAuditLog
- Document reappears in user lists

**Authorization**: Document owner or Admin

**Error Cases**:
- 401 Unauthorized
- 403 Forbidden
- 404 Not Found
- 410 Gone (deletion recovery window expired; must contact admin)

---

### 9. Share Document

**Endpoint**: `POST /api/documents/{documentId}/share`

**Purpose**: Share document with specific user(s)

**Request Body**:
```json
{
  "sharedWithUserIds": [3, 7],
  "message": "Please review the budget plan"
}
```

**Response** (201 Created):
```json
{
  "documentId": 1,
  "sharedWithCount": 2,
  "shares": [
    {
      "shareId": 100,
      "sharedWithUserId": 3,
      "sharedWithUserName": "Ni Kang",
      "sharedDate": "2026-08-14T16:10:00Z"
    }
  ],
  "message": "Document shared successfully"
}
```

**Side Effects**:
- Creates DocumentShare records
- Sends in-app notification to shared-with users
- Logs "Share" action in DocumentAuditLog

**Authorization**: Document owner

**Error Cases**:
- 400 Bad Request (invalid user IDs, self-sharing)
- 401 Unauthorized
- 403 Forbidden (not document owner)
- 404 Not Found

---

### 10. Unshare Document

**Endpoint**: `DELETE /api/documents/{documentId}/share/{userId}`

**Purpose**: Revoke document sharing with a specific user

**Response** (204 No Content)

**Side Effects**:
- Sets IsRevoked = 1 on DocumentShare record
- Logs "Unshare" action in DocumentAuditLog
- Document removed from recipient's "Shared with Me" list

**Authorization**: Document owner or recipient

**Error Cases**:
- 401 Unauthorized
- 403 Forbidden
- 404 Not Found (share doesn't exist)

---

### 11. List Shared with Me

**Endpoint**: `GET /api/documents/shared-with-me`

**Purpose**: Retrieve documents shared with current user

**Query Parameters**:
- Same as List User Documents (sorting, filtering, pagination)

**Response** (200 OK):
```json
{
  "documents": [
    {
      "documentId": 2,
      "title": "Budget Plan",
      "description": "2026 budget allocation",
      "uploadedByName": "Floris Kregel",
      "sharedDate": "2026-08-10T10:00:00Z",
      "category": "Reports",
      "fileSize": 1048576
    }
  ],
  "totalCount": 5,
  "pageNumber": 1,
  "pageSize": 20
}
```

**Error Cases**:
- 401 Unauthorized

---

### 12. Get Document Statistics

**Endpoint**: `GET /api/documents/statistics`

**Purpose**: Retrieve user's document storage quota and usage

**Response** (200 OK):
```json
{
  "usedBytes": 2684354560,
  "quotaBytes": 5368709120,
  "usedPercent": 50.0,
  "documentCount": 15,
  "organizationUsedBytes": 45365202944,
  "organizationQuotaBytes": 107374182400,
  "organizationUsedPercent": 42.2
}
```

**Error Cases**:
- 401 Unauthorized

---

### 13. Get Audit Log (Admin Only)

**Endpoint**: `GET /api/admin/documents/audit-log`

**Purpose**: Retrieve audit logs for compliance and investigation

**Query Parameters**:
- `documentId` (optional): Filter by document
- `userId` (optional): Filter by actor
- `action` (optional): Filter by action type
- `dateFrom` (optional): Start date (ISO 8601)
- `dateTo` (optional): End date (ISO 8601)
- `pageNumber` (optional): Pagination (default: 1)
- `pageSize` (optional): Results per page (default: 50, max: 500)

**Response** (200 OK):
```json
{
  "logs": [
    {
      "logId": 1001,
      "documentId": 1,
      "documentTitle": "Q3 Report",
      "userId": 3,
      "userName": "Ni Kang",
      "action": "Download",
      "timestamp": "2026-08-14T10:30:00Z",
      "ipAddress": "192.168.1.100",
      "result": "Success",
      "details": null,
      "fileSize": 2502656
    }
  ],
  "totalCount": 1250,
  "pageNumber": 1,
  "pageSize": 50
}
```

**Authorization**: Admin only

**Error Cases**:
- 401 Unauthorized
- 403 Forbidden (not admin)

---

## Data Types & Formats

| Type | Format | Example |
|------|--------|---------|
| DateTime | ISO 8601 UTC | "2026-08-14T15:45:00Z" |
| FileSize | Bytes (integer) | 2502656 |
| Quota | Bytes (long) | 5368709120 (5 GB) |
| MimeType | RFC 2045 | "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" |
| UUID/GUID | 36-char hex | "550e8400-e29b-41d4-a716-446655440000" |

## Rate Limiting (Future)

Not implemented in MVP, but contracts reserved for future enhancement:
- X-RateLimit-Limit: 1000
- X-RateLimit-Remaining: 999
- X-RateLimit-Reset: 1692118800

## Versioning

All endpoints use implicit v1 (no /v1/ prefix). Future versions will use /api/v2/documents if breaking changes needed.
