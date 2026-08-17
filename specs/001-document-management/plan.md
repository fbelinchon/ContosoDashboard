# Implementation Plan: Document Upload and Management

**Branch**: `001-document-management`  
**Date**: 2026-08-14  
**Status**: Ready for Task Breakdown  
**Spec**: [spec.md](spec.md)  
**Research**: [research.md](research.md)  
**Data Model**: [data-model.md](data-model.md)  
**API Contracts**: [contracts/api-contracts.md](contracts/api-contracts.md)  
**Quickstart**: [quickstart.md](quickstart.md)

## Executive Summary

The Document Upload and Management feature adds centralized document storage to ContosoDashboard, addressing the business need for controlled document management across Contoso employees. The feature provides secure, role-based access to uploaded documents with compliance support (GDPR), accessibility (WCAG 2.1 AA), and a clear upgrade path to cloud storage (Azure Blob Storage).

**Implementation approach**: 
- **Phase 1 (P1 - Weeks 1-4)**: Core upload/download/search with soft-delete, quota enforcement, audit logging
- **Phase 2 (P2 - Weeks 5-8)**: Sharing, task integration, GDPR data subject rights, accessibility polish
- **Phase 3 (P3 - Weeks 9-10)**: Dashboard widgets, localization translations, advanced reporting

**MVP scope**: Users can upload documents, organize by category/project, search, and download with full soft-delete recovery and audit compliance.

## Technical Context

**Technology Stack** (leveraging existing ContosoDashboard architecture):
- **Runtime**: .NET 10 / C# 14
- **UI Framework**: Blazor Server
- **Database**: SQL Server LocalDB with Entity Framework Core
- **Authentication**: Cookie-based mock auth with claims-based identity (existing)
- **API Pattern**: RESTful HTTP endpoints
- **Testing**: xUnit + Moq (unit), Selenium (integration)

**Storage Architecture**: 
- **Offline-First**: Local filesystem with interface abstraction (`IFileStorageService`)
- **Cloud Migration Ready**: Azure Blob Storage can be swapped via dependency injection
- **File Path Pattern**: `{userId}/{projectId}/{guid}.{ext}` (works for both local and cloud)

**Scale & Scope**:
- **Users**: Contoso employees (50-200 in training context)
- **Documents per User**: 5-50 typical; scale tested to 500
- **Storage**: 5 GB per user, 100 GB organization total
- **Performance**: Upload < 30s (25 MB), search < 2s, preview < 3s

**Key Constraints**:
- Training context (no cloud dependencies required)
- Offline-capable (works without internet)
- Clean architecture (layer separation for testability)
- Security-first (IDOR prevention, path traversal defense, authorization at multiple layers)

## Constitution Check

**Constitution File**: [.specify/memory/constitution.md](.specify/memory/constitution.md)

**Principle Alignment**:

| Constitution Principle | Alignment | Notes |
|---|---|---|
| I. Spec-Driven Development | ✅ **FULL** | Comprehensive spec.md completed before planning; all 92 requirements specified |
| II. Clean Architecture | ✅ **FULL** | Layered design: Models → Services → Controllers/Pages (dependency flow inward) |
| III. Security by Design | ✅ **FULL** | IDOR prevention via service-layer auth checks; path traversal defense via GUID filenames |
| IV. Test-First Approach | ✅ **FULL** | TDD discipline required during implementation; unit tests for services, integration tests for endpoints |
| V. Observability & Diagnostics | ✅ **FULL** | Comprehensive audit logging (DocumentAuditLog); all operations logged for GDPR compliance |

**Governance Compliance**:
- ✅ All 92 functional requirements are testable with acceptance criteria
- ✅ Non-negotiable security principles enforced (IDOR, auth layers)
- ✅ Code review gates: spec compliance, layer separation, test coverage 80%+
- ✅ Quality gates before merge: tests green, no security issues, architecture verified

**Architecture Principles**:
- ✅ **Offline-First with Cloud Migration Path**: IFileStorageService abstraction enables future Azure migration without business logic changes
- ✅ **Data Isolation & Multi-Tenancy Ready**: Service layer enforces authorization; schema supports per-user and per-org isolation

**Gate Evaluation**: ✅ **PASS** - Feature design fully complies with Constitution; no justifications needed.

## Project Structure

### Documentation (this feature)

```
specs/001-document-management/
├── spec.md                          # Feature specification (92 requirements)
├── plan.md                          # This implementation plan
├── research.md                      # Technical decisions and rationale
├── data-model.md                    # Entity definitions and relationships
├── quickstart.md                    # End-to-end validation scenarios
├── contracts/
│   └── api-contracts.md             # RESTful API request/response contracts
└── checklists/
    └── requirements.md              # Quality validation checklist
```

### Source Code (repository structure)

```
ContosoDashboard/
├── Data/
│   ├── ApplicationDbContext.cs      # [MODIFY] Add DbSet<Document>, migrations
│   └── Migrations/
│       └── [NEW] AddDocumentFeature.cs
│
├── Models/
│   ├── Document.cs                  # [NEW] Core entity
│   ├── DocumentTag.cs               # [NEW] Tag classification
│   ├── DocumentShare.cs             # [NEW] Sharing relationships
│   ├── DocumentAuditLog.cs          # [NEW] Audit trail
│   ├── UserStorageQuota.cs          # [NEW] Quota tracking
│   └── [MODIFY] User.cs             # Add navigation properties for documents
│
├── Services/
│   ├── DocumentService.cs           # [NEW] Document business logic
│   ├── IFileStorageService.cs       # [NEW] File storage abstraction
│   ├── LocalFileStorageService.cs   # [NEW] Local filesystem implementation
│   ├── DocumentAuditService.cs      # [NEW] Audit logging
│   ├── DocumentSharingService.cs    # [NEW] Sharing logic
│   ├── QuotaService.cs              # [NEW] Quota enforcement
│   └── [MODIFY] NotificationService.cs # Add document sharing notifications
│
├── Pages/
│   ├── Documents.razor              # [NEW] Document list/search page
│   ├── DocumentDetail.razor         # [NEW] Document preview/metadata page
│   ├── DocumentUpload.razor         # [NEW] Upload form
│   └── Admin/
│       ├── DocumentAudit.razor      # [NEW] Admin audit log view
│       └── StorageQuota.razor       # [NEW] Admin quota management
│
├── Controllers/
│   └── DocumentApiController.cs     # [NEW] RESTful API endpoints
│
├── AppData/
│   └── uploads/                     # [NEW] File storage directory
│
├── wwwroot/
│   ├── css/
│   │   └── documents.css            # [NEW] Document UI styling
│   └── js/
│       └── upload-queue.js          # [NEW] Client-side upload queue manager
│
└── Tests/
    ├── Unit/
    │   ├── DocumentServiceTests.cs  # [NEW] Service logic tests
    │   ├── QuotaServiceTests.cs     # [NEW] Quota enforcement tests
    │   └── FileStorageTests.cs      # [NEW] File storage abstraction tests
    │
    └── Integration/
        ├── DocumentApiTests.cs      # [NEW] API endpoint tests
        └── DocumentSharingTests.cs  # [NEW] Sharing workflow tests
```

## Design Approach

### 1. Layered Architecture

```
Presentation Layer (Blazor Pages/Components)
├─ Documents.razor (list, search, filter)
├─ DocumentDetail.razor (preview, metadata, audit)
├─ DocumentUpload.razor (upload form, queue visualization)
│
Business Logic Layer (Services)
├─ DocumentService (CRUD, soft-delete, restore)
├─ DocumentSharingService (share/unshare, access control)
├─ QuotaService (enforce quotas, calculate usage)
├─ DocumentAuditService (log all operations)
│
Persistence Layer (Entity Framework Core)
├─ ApplicationDbContext (DbSets for all entities)
├─ Document, DocumentTag, DocumentShare, DocumentAuditLog
│
Data Access Layer (File Storage Abstraction)
├─ IFileStorageService interface
├─ LocalFileStorageService (filesystem implementation)
└─ [Future] AzureBlobStorageService (cloud implementation)
```

**Dependency Flow**: UI → Services → EF Core → Database / File Storage

### 2. API Endpoint Structure

| HTTP Method | Path | Purpose | Auth |
|---|---|---|---|
| GET | /api/documents | List user's documents | User |
| POST | /api/documents/search | Full-text search | User |
| POST | /api/documents/upload | Upload document | User |
| GET | /api/documents/{id}/download | Download file | User (IDOR check) |
| GET | /api/documents/{id}/preview | View metadata | User (IDOR check) |
| PATCH | /api/documents/{id} | Update metadata | Owner |
| DELETE | /api/documents/{id} | Soft-delete | Owner |
| POST | /api/documents/{id}/restore | Restore document | Owner |
| POST | /api/documents/{id}/share | Share document | Owner |
| DELETE | /api/documents/{id}/share/{userId} | Revoke share | Owner or Recipient |
| GET | /api/documents/shared-with-me | List shared docs | User |
| GET | /api/documents/statistics | User quota info | User |
| GET | /api/admin/documents/audit-log | Admin audit view | Admin |

### 3. Security Architecture (Multi-Layer)

**Layer 1: Middleware** (ASP.NET Core Pipeline)
- Require authenticated session
- Set security headers (CSP, X-Frame-Options, etc.)

**Layer 2: Page/Controller Authorization**
- `[Authorize]` attribute on all document pages
- Role-based checks for admin endpoints
- Example: `[Authorize(Roles = "Administrator")]`

**Layer 3: Service-Level Authorization** (most critical)
- **DocumentService.GetDocument()** verifies:
  - Is user the document owner? OR
  - Is user in project team? OR  
  - Is document shared with user? OR
  - Is user admin?
  - If none: throw `ForbiddenAccessException`
- Same for Download, Preview, Delete, Edit operations

**Layer 4: Data Layer Filtering**
- EF Core queries scoped to authorized documents
- Example: `context.Documents.Where(d => d.UploadedByUserId == userId && !d.IsDeleted)`

**Security Patterns**:
- ✅ **IDOR Prevention**: Every document access checks authorization
- ✅ **Path Traversal Prevention**: Use GUID-based filenames, never user input directly in paths
- ✅ **SQL Injection Prevention**: EF Core parameterized queries
- ✅ **Privilege Escalation Prevention**: Never trust user role claims; verify in service layer

### 4. Quota Enforcement Strategy

**Pre-Upload Check**:
```csharp
var userUsed = context.Documents
    .Where(d => d.UploadedByUserId == userId && !d.IsDeleted)
    .Sum(d => d.FileSize);

var orgUsed = context.Documents
    .Where(d => !d.IsDeleted)
    .Sum(d => d.FileSize);

if (userUsed + fileSize > 5_368_709_120) // 5 GB
    throw new QuotaExceededException("User quota full");

if (orgUsed + fileSize > 107_374_182_400) // 100 GB  
    throw new QuotaExceededException("Organization quota full");

// Proceed with upload
```

**Real-Time Display**:
- Percentage bar: `(usedBytes / quotaBytes) * 100`
- Tooltip: "1.2 GB of 5.0 GB used (24%)"
- Warning at 90%: "You're approaching your storage limit"

### 5. Soft-Delete & Purge Lifecycle

**User Deletes Document** (T=0):
```
Document.IsDeleted = 1
Document.DeletedDate = 2026-08-14 14:30:00
Log: Action="Delete", Result="Success"
```

**User Restores within 30 days** (T=< 30 days):
```
Document.IsDeleted = 0
Document.DeletedDate = NULL
Log: Action="Restore", Result="Success"
```

**Auto-Purge Scheduled Job** (T=30 days):
```
Daily: For each Document where DeletedDate < (today - 30 days):
  - Delete physical file from filesystem
  - Hard-delete from database (cascade: Tags, Shares)
  - Log: Action="Purge", Result="Success"
```

### 6. Upload Queue Architecture

**Client-Side** (Blazor Component State):
- Queue: List<QueuedUploadItem>
- Current: QueuedUploadItem (one uploading)
- Worker: ProcessUploadQueue() task

**User Flow**:
1. Select multiple files
2. UI shows upload panel with queued items
3. Worker processes queue: one item at a time
4. For each item:
   - Show progress 0-100%
   - Handle success: remove from queue, add to document list
   - Handle error: show error message, allow retry or skip
5. When all complete or cancelled: close panel

**Progress Reporting**:
- Client sends file in chunks (Blazor has chunking support)
- Server reports progress: `{ "bytesReceived": 5242880, "totalBytes": 10485760 }`
- UI updates progress bar: `(5242880 / 10485760) * 100 = 50%`

### 7. Audit Logging for Compliance

**All Operations Logged**:
```csharp
_auditService.LogAction(
    documentId: doc.DocumentId,
    userId: currentUser.UserId,
    action: "Download",
    timestamp: DateTime.UtcNow,
    ipAddress: HttpContext.Connection.RemoteIpAddress,
    result: "Success",
    details: $"Downloaded {doc.Title} ({doc.FileSize} bytes)"
);
```

**Audit Table Queries**:
- Find all actions by user: `WHERE UserId = X ORDER BY Timestamp DESC`
- Find all actions on document: `WHERE DocumentId = X ORDER BY Timestamp DESC`
- Data subject audit trail: `WHERE UserId = X AND Action IN ('Download', 'Preview', 'Share')`

**GDPR Compliance**:
- ✅ Audit logs retained indefinitely (or per policy)
- ✅ Data subject can request audit log (DSAR fulfillment)
- ✅ Soft-delete with 30-day auto-purge satisfies "right to erasure"
- ✅ Data minimization: only title, description, category, tags (no location, device ID, etc.)

## Implementation Phases

### Phase 0: Setup & Preparation (1-2 days)

- [ ] Create database migrations (Document, DocumentTag, DocumentShare, DocumentAuditLog tables)
- [ ] Create upload directory: `AppData/uploads`
- [ ] Seed test data (10-20 sample documents for testing)
- [ ] Setup Entity Framework DbContext changes
- [ ] Create service interfaces (IFileStorageService, IDocumentService, etc.)

**Deliverables**:
- Working migrations, schema in place
- Service interfaces defined
- Build succeeds without errors

### Phase 1 (P1): Core Upload & Download (Weeks 1-4)

**Scope**: Basic upload/download with quota, soft-delete, audit logging

**User Stories**:
- FR-001 to FR-013: Document upload functionality
- FR-050 to FR-054: Storage quota enforcement
- FR-025 to FR-031: Soft-delete with recovery
- FR-039, FR-042: Audit logging

**Tasks**:
1. Implement DocumentService:
   - UploadDocument() - save file, create metadata, check quota
   - DownloadDocument() - authorization check, serve file
   - DeleteDocument() - soft-delete with timestamp
   - RestoreDocument() - recover within 30-day window
   
2. Implement IFileStorageService:
   - LocalFileStorageService: System.IO.File operations
   - Methods: SaveAsync(), DeleteAsync(), GetAsync()
   
3. Implement DocumentAuditService:
   - LogAction() - insert audit log entry
   - GetAuditLog() - query by document/user/date range

4. Implement QuotaService:
   - CheckQuota() - pre-upload validation
   - GetUserQuotaInfo() - used/available bytes
   
5. Create API endpoints:
   - POST /api/documents/upload
   - GET /api/documents/{id}/download
   - DELETE /api/documents/{id}
   - GET /api/documents/statistics
   
6. Create Blazor Pages:
   - Documents.razor (list view)
   - DocumentDetail.razor (preview/metadata)
   - DocumentUpload.razor (form)
   
7. Unit tests (xUnit):
   - DocumentServiceTests (CRUD, authorization, soft-delete)
   - QuotaServiceTests (quota enforcement)
   - FileStorageTests (save/delete operations)
   
8. Integration tests:
   - DocumentApiTests (upload workflow, error cases)

**Definition of Done**:
- ✅ All P1 user stories passing acceptance criteria
- ✅ 80%+ unit test coverage for services
- ✅ Quickstart scenarios 1, 2, 4 passing
- ✅ No security issues (manual IDOR/injection review)
- ✅ Audit logs complete for all operations
- ✅ Code review passed (spec compliance, architecture, security)

### Phase 2 (P2): Collaboration & Compliance (Weeks 5-8)

**Scope**: Sharing, task integration, GDPR data subject rights, accessibility

**User Stories**:
- FR-032 to FR-034: Document sharing
- FR-035 to FR-037: Task integration
- FR-040, FR-041: Notification integration
- FR-071 to FR-086: GDPR compliance
- FR-059 to FR-074: Accessibility (WCAG 2.1 AA)

**Tasks**:
1. Implement DocumentSharingService:
   - ShareDocument() - create DocumentShare record, notify recipient
   - UnshareDocument() - revoke access, log action
   - GetSharedWithMe() - list shared documents
   
2. Implement Notification integration:
   - Send in-app notification on share
   - Update NotificationService to include document shares
   
3. Integrate with Task service:
   - AttachDocumentToTask() - link Document to Task
   - GetTaskDocuments() - retrieve attachments
   - Show attachments in Task detail page
   
4. Implement GDPR Data Subject Rights:
   - ExportUserData() - generate JSON/CSV of all personal data
   - RequestUserDeletion() - schedule 30-day countdown
   - GetUserAccessHistory() - documents accessed by user
   - Privacy notice on upload page
   
5. Accessibility implementation:
   - Add semantic HTML (labels, ARIA attributes)
   - Keyboard navigation on all forms
   - Test with axe DevTools, WAVE
   - 200% zoom support on previews
   
6. Unit tests:
   - DocumentSharingServiceTests
   - GDPRServiceTests
   
7. Integration tests:
   - DocumentSharingTests (share/unshare/access)
   - TaskIntegrationTests

**Definition of Done**:
- ✅ All P2 user stories passing
- ✅ Sharing & notifications working end-to-end
- ✅ GDPR endpoints testable and compliant
- ✅ Accessibility automated testing passes (zero critical issues)
- ✅ Quickstart scenarios 3, 5, 6, 8 passing

### Phase 3 (P3): Polish & Analytics (Weeks 9-10)

**Scope**: Dashboard widgets, localization, advanced reporting

**User Stories**:
- FR-039: Dashboard recent documents widget
- FR-075 to FR-078: Localization infrastructure
- FR-044: Admin usage reports

**Tasks**:
1. Dashboard widget:
   - Create RecentDocumentsWidget component
   - Show last 5 documents uploaded by user
   - Click → navigate to document
   
2. Localization infrastructure:
   - Externalize all strings to resource files (Strings.resx)
   - Test with locale-aware date/time/number formatting
   - Prepare for translation (but ship MVP in English only)
   - RTL layout support (CSS ready, no special code)
   
3. Admin reporting:
   - GenerateUsageReport() - most uploaded types, active uploaders, access patterns
   - CreateAuditReport() - detailed operation log for date range
   - Export to CSV
   
4. Performance optimization:
   - Database indexes on Document (Title, Category, UploadDate, UploadedByUserId)
   - Search query optimization
   - Test with 500 documents

**Definition of Done**:
- ✅ Dashboard widget live and displaying correctly
- ✅ All UI strings externalized and locale-ready
- ✅ Admin reports generate within SLA (<5 sec)
- ✅ Quickstart scenarios 7, 9 passing
- ✅ Performance testing: upload < 30s, search < 2s, list < 2s

## Technology Decision Log

| Decision | Rationale | Alternatives Considered |
|----------|-----------|--------------------------|
| **Upload Queue: Sequential** | Prevents DB lock contention; clearer UX | Parallel (complex), Limited concurrent (middle ground) |
| **Soft-Delete with 30-day window** | GDPR right to erasure; prevents accidental loss | Immediate hard-delete (irreversible), Archive only (adds UI) |
| **File Path: GUID-based** | Prevents path traversal; secure; supports both local/cloud | User-supplied names (vulnerable), Hashed names (less readable) |
| **Quota: Per-user + Org-level** | Balances individual use with org constraints | Per-user only (org runaway), Per-org only (unfair distribution) |
| **Audit: Comprehensive logging** | GDPR compliance, forensic investigation | Minimal logging (risk), Log only failures (incomplete) |
| **API: RESTful HTTP** | Matches ContosoDashboard pattern; Blazor Server friendly | GraphQL (overkill), gRPC (not web-friendly) |
| **Storage: IFileStorageService abstraction** | Cloud migration without code changes | Coupled to LocalDB (risky), Manual layer (error-prone) |

## Risk Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|-----------|
| **Concurrent upload data corruption** | Medium | High | Sequential queue; database locks; unit tests |
| **IDOR vulnerabilities (unauthorized access)** | Medium | Critical | Service-layer auth checks; manual security review; penetration test |
| **Path traversal attacks** | Low | Critical | GUID filenames; never use user input in paths; code review |
| **Quota bypass (user exceeds limit)** | Low | Medium | Pre-upload check in service; audit log all uploads |
| **Soft-delete recovery window misunderstood** | Medium | Low | Clear UI messaging ("Recoverable for 30 days"); privacy notice |
| **Performance degradation (500+ docs)** | Low | Medium | Database indexes; pagination (20 per page); load testing |
| **Virus scanning service unavailable** | Low | Medium | Mock implementation for training; log failure; allow override for admin |
| **Accessibility not WCAG 2.1 AA compliant** | Medium | Medium | Automated testing (axe/WAVE); manual screen reader testing; browser zoom testing |

## Success Criteria Mapping

| Success Criteria | Phase | How Measured | Acceptance |
|---|---|---|---|
| **SC-001**: 70% adoption in 3 months | Post-Launch | User survey / feature analytics | Depends on training delivery |
| **SC-002**: < 30 sec to locate document | Phase 2 | User testing; stopwatch | Search returns results < 2 sec; UX intuitive |
| **SC-003**: 90% documents categorized | Phase 1 | Audit sampling | Category required at upload |
| **SC-004**: Zero security incidents | All Phases | Security review; penetration test | IDOR/injection/traversal testing |
| **SC-005**: 95% upload completion rate | Phase 1 | Automated testing | < 5% failures in test suite |
| **SC-006**: 80% task attachment success | Phase 2 | User testing | UI intuitive; integration tests pass |
| **SC-007**: 5 sec audit reports | Phase 3 | Performance testing | Report generation < 5000 ms |
| **SC-008**: 90% search precision | Phase 1 | Unit tests | Correct docs returned in top 10 |
| **SC-009**: Quota enforcement effective | Phase 1 | Unit/integration tests | Rejections work; no bypasses |
| **SC-010**: WCAG 2.1 AA compliance | Phase 2 | axe DevTools, WAVE tools | Zero critical accessibility issues |
| **SC-011**: Keyboard navigation complete | Phase 2 | Manual testing | All workflows accessible without mouse |
| **SC-012**: Data subject rights functional | Phase 2 | Integration tests | DSAR and erasure endpoints work |
| **SC-013**: 30-day purge executed | Phase 3 | Scheduled job testing | Old deletes auto-purged |

## Rollout Strategy

### MVP Release (End of Phase 1)
- Core upload/download/delete for all users
- Soft-delete with recovery
- Storage quota enforcement
- Audit logging (compliance)
- Document list with search & filter
- Project document association

### Production Readiness Checklist
- [ ] All P1 & P2 tests green
- [ ] Security review complete (no IDOR, injection, path traversal)
- [ ] Accessibility audit complete (WCAG 2.1 AA)
- [ ] Documentation complete (API docs, user guide, admin guide)
- [ ] Database backups tested
- [ ] Performance testing passed (upload <30s, search <2s)
- [ ] User training materials prepared
- [ ] Rollback procedure documented

### Post-Launch Support
- Monitor audit logs for unusual activity
- Respond to user support tickets (quota issues, deleted document recovery)
- Phase 2 development continues in parallel (sharing, GDPR endpoints)

## Open Questions (Already Clarified)

✅ All 5 clarification questions have been resolved:
1. Storage quota: 5 GB per user + 100 GB org
2. Deletion lifecycle: Soft-delete with 30-day recovery
3. Accessibility: WCAG 2.1 Level AA + localizable strings
4. Compliance: GDPR for EU users
5. Upload concurrency: Sequential queuing with progress

## Next Steps

1. **Task Breakdown**: Run `/speckit.tasks` to generate detailed task list with dependencies
2. **Story Point Estimation**: Team review of tasks and effort estimation
3. **Sprint Planning**: Assign tasks to sprints and identify critical path
4. **Development Begins**: Phase 1 implementation starts with database setup
5. **Weekly Standups**: Track progress against phase deliverables

---

**Plan Status**: ✅ **READY FOR TASK BREAKDOWN AND IMPLEMENTATION**

This plan is complete and ready for developers to begin Phase 1. All design decisions documented, risks identified, and acceptance criteria clear.
