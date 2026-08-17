# Data Model: Document Upload and Management

**Feature**: 001-document-management  
**Date**: 2026-08-14  
**Architecture**: Entity Framework Core with SQL Server LocalDB

## Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                           Document                              │
├─────────────────────────────────────────────────────────────────┤
│ DocumentId (PK, int, identity)                                  │
│ Title (nvarchar(255), not null)                                 │
│ Description (nvarchar(max), nullable)                           │
│ Category (nvarchar(50), not null)                               │
│ FilePath (nvarchar(500), not null) [GUID-based path]            │
│ FileSize (bigint, not null) [bytes]                             │
│ MimeType (nvarchar(255), not null)                              │
│ UploadDate (datetime2, not null, default=now)                   │
│ UploadedByUserId (int, not null) [FK to User]                   │
│ ProjectId (int, nullable) [FK to Project]                       │
│ IsDeleted (bit, not null, default=0)                            │
│ DeletedDate (datetime2, nullable) [timestamp of soft delete]     │
│ CreatedDate (datetime2, not null, default=now)                  │
│ ModifiedDate (datetime2, not null, default=now)                 │
│ RowVersion (rowversion) [optimistic concurrency]                │
└─────────────────────────────────────────────────────────────────┘
                 ↑                               ↑
                 │                               │
                 │ UploadedByUserId              │ ProjectId
                 │ (FK)                          │ (FK)
                 │                               │
        ┌────────┴──────────┐         ┌─────────┴───────────┐
        │                   │         │                     │
    [User]             [Project]      │         ┌──────────────────────┐
                                      │         │   DocumentTag        │
    ┌──────────────────────────────────────────┼──────────────────────┐
    │               DocumentTag                 │                      │
    ├──────────────────────────────────────────┼──────────────────────┤
    │ TagId (PK, int, identity)                │ TagName              │
    │ DocumentId (FK, int)                     │ (nvarchar(100))      │
    │ TagName (nvarchar(100), not null)        │                      │
    └──────────────────────────────────────────┼──────────────────────┘
                                               │
    ┌──────────────────────────────────────────┴──────────────────────┐
    │               DocumentShare                                     │
    ├─────────────────────────────────────────────────────────────────┤
    │ ShareId (PK, int, identity)                                     │
    │ DocumentId (FK, int)                                            │
    │ SharedByUserId (FK, int) [who shared it]                        │
    │ SharedWithUserId (FK, int) [who received it]                    │
    │ SharedDate (datetime2, not null, default=now)                   │
    │ IsRevoked (bit, not null, default=0) [revoked shares]           │
    │ RevokedDate (datetime2, nullable)                               │
    └─────────────────────────────────────────────────────────────────┘

    ┌─────────────────────────────────────────────────────────────────┐
    │             DocumentAuditLog                                    │
    ├─────────────────────────────────────────────────────────────────┤
    │ LogId (PK, bigint, identity)                                    │
    │ DocumentId (FK, int, nullable) [null for system-level actions]  │
    │ UserId (FK, int, not null)                                      │
    │ Action (nvarchar(50), not null)                                 │
    │   Values: Upload, Download, Delete, Restore, Share, Unshare,   │
    │           Preview, Replace, EditMetadata, Purge                 │
    │ Timestamp (datetime2, not null, default=now)                    │
    │ IpAddress (nvarchar(45), nullable) [support IPv6]               │
    │ Result (nvarchar(50), not null)                                 │
    │   Values: Success, Failure, Blocked                             │
    │ Details (nvarchar(max), nullable)                               │
    │ FileSize (bigint, nullable) [size of accessed document]         │
    └─────────────────────────────────────────────────────────────────┘
```

## Entity Definitions

### 1. Document (Core Entity)

Represents a single uploaded file with all metadata.

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| DocumentId | int (PK) | NOT NULL, IDENTITY(1,1) | Unique identifier |
| Title | nvarchar(255) | NOT NULL | User-provided name |
| Description | nvarchar(MAX) | NULL | Optional metadata |
| Category | nvarchar(50) | NOT NULL, CHECK (Category IN ('Project Documents', 'Team Resources', 'Personal Files', 'Reports', 'Presentations', 'Other')) | Fixed category list |
| FilePath | nvarchar(500) | NOT NULL, UNIQUE | Path on filesystem: `{userId}/{projectId}/{guid}.{ext}` |
| FileSize | bigint | NOT NULL | Bytes; used for quota enforcement |
| MimeType | nvarchar(255) | NOT NULL | RFC 2045 MIME type; enables preview type detection |
| UploadDate | datetime2 | NOT NULL, DEFAULT(GETUTCDATE()) | When uploaded |
| UploadedByUserId | int (FK→User) | NOT NULL | Document owner |
| ProjectId | int (FK→Project) | NULL | Project association (optional) |
| IsDeleted | bit | NOT NULL, DEFAULT(0) | Soft-delete flag for 30-day recovery |
| DeletedDate | datetime2 | NULL | When marked for deletion; null if not deleted |
| CreatedDate | datetime2 | NOT NULL, DEFAULT(GETUTCDATE()) | Audit trail |
| ModifiedDate | datetime2 | NOT NULL, DEFAULT(GETUTCDATE()) | Last update; updated on metadata edits |
| RowVersion | rowversion | NOT NULL | Optimistic concurrency control |

**Indexes**:
- PK: DocumentId
- IX_Document_UploadedByUserId (find user's docs quickly)
- IX_Document_ProjectId (find project docs quickly)
- IX_Document_Category (filter by category)
- IX_Document_UploadDate (sort/filter by date)
- IX_Document_IsDeleted (exclude deleted in queries)
- UNIQUE: FilePath (prevent duplicate file paths)

### 2. DocumentTag (Classification)

Tags enable flexible document categorization beyond fixed categories.

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| TagId | int (PK) | NOT NULL, IDENTITY(1,1) | Unique identifier |
| DocumentId | int (FK→Document) | NOT NULL | Reference to document |
| TagName | nvarchar(100) | NOT NULL | User-defined tag |

**Indexes**:
- PK: TagId
- FK: DocumentId (find all tags for a document)
- IX_DocumentTag_TagName (search by tag)
- Composite: (DocumentId, TagName) UNIQUE (one instance per doc+tag)

**Relationship**:
- One Document can have many Tags (0..*)
- One Tag belongs to one Document
- Cascade delete: if Document deleted, all Tags deleted

### 3. DocumentShare (Collaboration)

Tracks document sharing relationships for secure access control.

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| ShareId | int (PK) | NOT NULL, IDENTITY(1,1) | Unique identifier |
| DocumentId | int (FK→Document) | NOT NULL | Document being shared |
| SharedByUserId | int (FK→User) | NOT NULL | Who initiated the share |
| SharedWithUserId | int (FK→User) | NOT NULL | Who received access |
| SharedDate | datetime2 | NOT NULL, DEFAULT(GETUTCDATE()) | When share occurred |
| IsRevoked | bit | NOT NULL, DEFAULT(0) | True if share withdrawn |
| RevokedDate | datetime2 | NULL | When revoked; null if active |

**Indexes**:
- PK: ShareId
- FK: DocumentId (find all shares for a document)
- FK: SharedWithUserId (find all docs shared with me)
- Composite: (DocumentId, SharedWithUserId) UNIQUE (one share per recipient per doc)
- IX_DocumentShare_IsRevoked (active shares only)

**Relationship**:
- One Document can have many Shares (0..*)
- One User can receive many Shares (0..*)
- Cascade delete: if Document deleted, all Shares deleted

### 4. DocumentAuditLog (Compliance)

Comprehensive audit trail for GDPR, security investigation, and usage analytics.

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| LogId | bigint (PK) | NOT NULL, IDENTITY(1,1) | Unique identifier |
| DocumentId | int (FK→Document) | NULL | Document affected; null for system events |
| UserId | int (FK→User) | NOT NULL | Actor (user performing action) |
| Action | nvarchar(50) | NOT NULL, CHECK (Action IN ('Upload', 'Download', 'Delete', 'Restore', 'Share', 'Unshare', 'Preview', 'Replace', 'EditMetadata', 'Purge', 'ScanInitiated', 'ScanPassed', 'ScanFailed', 'QuotaExceeded')) | Event type |
| Timestamp | datetime2 | NOT NULL, DEFAULT(GETUTCDATE()) | When action occurred (UTC) |
| IpAddress | nvarchar(45) | NULL | IPv4 or IPv6 address of requestor |
| Result | nvarchar(50) | NOT NULL, CHECK (Result IN ('Success', 'Failure', 'Blocked')) | Outcome of action |
| Details | nvarchar(MAX) | NULL | Free-form log message; error details if Failure |
| FileSize | bigint | NULL | Size of document at time of action |

**Indexes**:
- PK: LogId
- IX_DocumentAuditLog_DocumentId (audit trail for specific document)
- IX_DocumentAuditLog_UserId (all actions by a user)
- IX_DocumentAuditLog_Timestamp (retrieve logs by date range)
- IX_DocumentAuditLog_Action (filter by action type)
- Composite: (DocumentId, Timestamp) (retrieve document events in order)

**No Foreign Keys on Delete**: AuditLog must persist even if Document deleted (compliance requirement).

### 5. UserStorageQuota (Administrative)

Tracks storage usage per user to enforce 5 GB per-user and 100 GB organization quotas.

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| QuotaId | int (PK) | NOT NULL, IDENTITY(1,1) | Unique identifier |
| UserId | int (FK→User) | NOT NULL, UNIQUE | One quota record per user |
| UsedBytes | bigint | NOT NULL, DEFAULT(0) | Sum of FileSize for all non-deleted docs |
| QuotaBytes | bigint | NOT NULL, DEFAULT(5368709120) | 5 GB = 5 * 1024^3 bytes |
| LastCalculated | datetime2 | NOT NULL, DEFAULT(GETUTCDATE()) | When quota was last computed |

**Indexes**:
- PK: QuotaId
- UNIQUE: UserId (lookup user's quota)

**Calculated Field** (not stored):
- UsedPercent = (UsedBytes / QuotaBytes) * 100

**Organization Total** (computed at runtime):
- Sum all user UsedBytes to get organization usage
- Organization quota: 100 GB = 107374182400 bytes

## Key Business Rules

### Upload Validation

1. **File Type Check**: Reject if MIME type not in whitelist (PDF, Office, text, JPEG, PNG)
2. **File Size Check**: Reject if FileSize > 25 MB
3. **User Quota Check**: Reject if (UsedBytes + FileSize) > UserQuotaBytes
4. **Organization Quota Check**: Reject if (total org UsedBytes + FileSize) > 100 GB
5. **Virus Scan**: Scan before persisting to database; quarantine if infected

### Soft-Delete Lifecycle

1. **Initial State**: IsDeleted = 0, DeletedDate = NULL
2. **User Deletes**: IsDeleted = 1, DeletedDate = now()
3. **User Restores** (within 30 days): IsDeleted = 0, DeletedDate = NULL
4. **Auto-Purge** (after 30 days): Hard-delete Document record and file from filesystem
   - Scheduled job: daily check for DeletedDate < 30 days ago
   - Log purge event in AuditLog
   - Cascade delete: Remove all Tags, Shares, and final AuditLog entries for this doc

### Access Control

1. **Document Owner Access**: Document.UploadedByUserId == User.UserId
2. **Project Team Access**: Document.ProjectId == Project.ProjectId AND User in Project.Members
3. **Shared Access**: Check DocumentShare where SharedWithUserId == User.UserId AND IsRevoked = 0
4. **Admin Access**: User.Role == Administrator (access all documents)
5. **Team Lead Access**: User.Role == TeamLead AND User in Document.UploadedByUser.ReportingStructure

All queries must filter: `WHERE IsDeleted = 0` (exclude soft-deleted docs)

### Quota Calculation

**Per-User Used Bytes**:
```sql
SELECT SUM(FileSize) 
FROM Document 
WHERE UploadedByUserId = @UserId AND IsDeleted = 0
```

**Organization Total**:
```sql
SELECT SUM(FileSize) 
FROM Document 
WHERE IsDeleted = 0
```

Update UserStorageQuota.UsedBytes daily or on-demand before upload validation.

## Database Schema Generation

### Entity Framework Code-First Approach

```csharp
// Models/Document.cs
public class Document {
    [Key] public int DocumentId { get; set; }
    [Required, MaxLength(255)] public string Title { get; set; }
    [MaxLength(-1)] public string Description { get; set; }
    [Required, MaxLength(50)] public string Category { get; set; }
    [Required, MaxLength(500)] public string FilePath { get; set; }
    public long FileSize { get; set; }
    [Required, MaxLength(255)] public string MimeType { get; set; }
    public DateTime UploadDate { get; set; }
    public int UploadedByUserId { get; set; }
    public int? ProjectId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; }
    
    // Navigation properties
    public User UploadedBy { get; set; }
    public Project Project { get; set; }
    public ICollection<DocumentTag> Tags { get; set; } = new List<DocumentTag>();
    public ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
}
```

Migrations will be generated using: `dotnet ef migrations add AddDocumentFeature`

## Validation Rules

| Entity | Field | Rule |
|--------|-------|------|
| Document | Title | Required, 1-255 chars |
| Document | FileSize | Required, > 0, ≤ 26,843,545,600 bytes (25 GB sanity check) |
| Document | FilePath | Required, valid filesystem path format |
| Document | MimeType | Required, match known MIME types |
| Document | UploadDate | Must be ≤ now (prevent future dates) |
| Document | DeletedDate | Must be > UploadDate if present; must be ≤ now |
| DocumentTag | TagName | Required, 1-100 chars |
| DocumentShare | SharedWithUserId | Must ≠ SharedByUserId |
| DocumentAuditLog | IpAddress | Must be valid IPv4 or IPv6 format |

## Concurrency Strategy

**Optimistic Concurrency**: RowVersion (SQL rowversion/timestamp) on Document entity prevents lost updates during concurrent edits of metadata.

**Conflict Resolution**: If edit-conflict detected (RowVersion mismatch):
- Retry user interaction: show current state and allow merge/overwrite decision
- Log conflict in audit trail

No distributed locking needed (training context, single server).
