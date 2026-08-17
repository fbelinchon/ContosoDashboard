# Tasks: Document Upload and Management

**Feature Branch**: `001-document-management`  
**Input**: Design documents from `specs/001-document-management/`  
**Prerequisites**: [plan.md](plan.md) ✅, [spec.md](spec.md) ✅, [research.md](research.md) ✅, [data-model.md](data-model.md) ✅, [contracts/api-contracts.md](contracts/api-contracts.md) ✅

**Status**: Ready for Implementation  
**Total Tasks**: 87  
**Estimated Duration**: 10 weeks (Phases 1-3)

## Format: `[ID] [P?] [Story] Description`

- **[ID]**: Task identifier (T001, T002, etc.) in execution order
- **[P]**: Parallelizable (can run simultaneously on different files)
- **[Story]**: User story label (US1, US2, etc.) - only for story phases
- **File Paths**: All paths shown relative to `ContosoDashboard/` directory

---

## Phase 0: Setup & Project Initialization

**Purpose**: Project structure, database configuration, service interfaces

**Duration**: 1-2 days

- [x] T001 Create database migrations folder structure in `Data/Migrations/`
- [x] T002 Create services folder structure: `Services/DocumentService.cs`, `Services/Interfaces/`
- [x] T003 Create pages folder structure: `Pages/Documents.razor`, `Pages/Admin/`
- [x] T004 Create models folder structure: `Models/Document.cs`, `Models/DocumentTag.cs`, etc.
- [x] T005 Create test folder structure: `Tests/Unit/`, `Tests/Integration/`
- [x] T006 Create upload storage directory: `AppData/uploads/` (external to wwwroot)
- [x] T007 Create CSS file for document pages: `wwwroot/css/documents.css`
- [x] T008 Create JavaScript file for upload queue: `wwwroot/js/upload-queue.js`
- [ ] T009 Seed initial test data: mock users, projects, categories in ApplicationDbContext

**Checkpoint**: Project structure ready, can compile without errors

---

## Phase 1: Foundational Infrastructure (P1 - Blocking Prerequisites)

**Purpose**: Core data access, services, and authorization that ALL user stories depend on

⚠️ **CRITICAL**: No user story implementation begins until this phase is 100% complete

### Entities & Data Access

- [x] T010 [P] Create `Models/Document.cs` with properties: DocumentId (PK), Title, Description, Category, FilePath, FileSize, MimeType, UploadDate, UploadedByUserId (FK), ProjectId (FK, nullable), IsDeleted (bit), DeletedDate, CreatedDate, ModifiedDate, RowVersion (concurrency), navigation properties
- [x] T011 [P] Create `Models/DocumentTag.cs` with TagId (PK), DocumentId (FK), TagName
- [x] T012 [P] Create `Models/DocumentShare.cs` with ShareId (PK), DocumentId (FK), SharedByUserId (FK), SharedWithUserId (FK), SharedDate, IsRevoked (bit), RevokedDate
- [x] T013 [P] Create `Models/DocumentAuditLog.cs` with LogId (PK, bigint), DocumentId (FK, nullable), UserId (FK), Action (string), Timestamp, IpAddress, Result (enum: Success/Failure/Blocked), Details, FileSize
- [x] T014 [P] Create `Models/UserStorageQuota.cs` with QuotaId (PK), UserId (UNIQUE FK), UsedBytes, QuotaBytes (default 5GB), LastCalculated
- [x] T015 [P] Modify `Models/User.cs` to add navigation properties: ICollection<Document>, ICollection<DocumentAuditLog>, ICollection<UserStorageQuota>
- [x] T016 Create EF Core migration: `Data/Migrations/[timestamp]_AddDocumentFeature.cs` (covers all 5 entities + indexes)
- [x] T017 [P] Add 21 database indexes to migration (Document: DocumentId, UploadedByUserId, ProjectId, UploadDate, IsDeleted, Category; DocumentTag: DocumentId, TagName; DocumentShare: DocumentId, SharedWithUserId, IsRevoked; DocumentAuditLog: DocumentId, UserId, Action, Timestamp; UserStorageQuota: UserId)
- [x] T018 Update `Data/ApplicationDbContext.cs` to add DbSet properties for all 5 new entities
- [x] T019 Run EF Core update: `dotnet ef database update` to verify schema creation

**Checkpoint**: Database schema ready with all tables and indexes

### Storage Abstraction & File Access

- [x] T020 [P] Create `Services/Interfaces/IFileStorageService.cs` with methods: SaveAsync(userId, projectId, fileName, fileStream), GetAsync(filePath), DeleteAsync(filePath), ExistsAsync(filePath)
- [x] T021 [P] Create `Services/LocalFileStorageService.cs` implementing IFileStorageService with System.IO operations
- [x] T022 Test LocalFileStorageService: save file, verify on disk, retrieve, delete operations

**Checkpoint**: File storage abstraction working, tests passing

### Authorization & Security Framework

- [x] T023 Create `Services/Interfaces/IAuthorizationService.cs` with methods: CanViewDocument(userId, documentId), CanEditDocument(userId, documentId), CanDeleteDocument(userId, documentId), CanShareDocument(userId, documentId)
- [x] T024 Create `Services/DocumentAuthorizationService.cs` implementing 4-layer authorization checks (user owner OR project member OR shared with OR admin)
- [x] T025 [P] Add authorization utility: IDOR prevention checks in service layer
- [x] T026 Verify authorization service with unit tests (positive/negative IDOR scenarios)

**Checkpoint**: Authorization layer working, security tests passing

### Audit Logging Infrastructure

- [x] T027 Create `Services/Interfaces/IDocumentAuditService.cs` with method: LogAction(documentId?, userId, action, result, details, ipAddress)
- [x] T028 Create `Services/DocumentAuditService.cs` implementing comprehensive audit logging
- [x] T029 Unit test DocumentAuditService: log various actions, query by document/user/date range

**Checkpoint**: Audit logging functional, all operations loggable

### Quota Management Foundation

- [x] T030 Create `Services/Interfaces/IQuotaService.cs` with methods: CheckQuotaAsync(userId, fileSize), GetUserQuotaAsync(userId), RecalculateUserQuotaAsync(userId)
- [x] T031 Create `Services/QuotaService.cs` with pre-upload validation (5GB/user, 100GB/org)
- [x] T032 Unit test QuotaService: validation logic, edge cases (exactly at limit, over limit)

**Checkpoint**: Quota enforcement ready for Phase 2+ use

### Dependency Injection & Configuration

- [x] T033 Register all services in `Program.cs`: DocumentService, FileStorageService, AuthorizationService, AuditService, QuotaService
- [x] T034 Configure dependency injection with scoped lifetimes where appropriate
- [x] T035 Add configuration for upload limits, quota defaults, storage paths in `appsettings.json`

**Checkpoint**: DI container configured, services injectable

---

## Phase 2: Core Upload & Download (P1 - User Stories 1, 2, 3)

**Purpose**: MVP feature - users can upload, organize, search, and download documents with quota enforcement

**Duration**: 4 weeks

### User Story 1: Employee Uploads Personal Documents (P1)

**Goal**: Users can upload documents with title, description, category, and see them in their list

**Independent Test**: Upload a document, verify it appears in list with correct metadata, download it

### Implementation for User Story 1

- [x] T036 [P] [US1] Create `Services/Interfaces/IDocumentService.cs` with methods: UploadDocumentAsync, GetDocumentAsync, ListDocumentsAsync, DeleteDocumentAsync, RestoreDocumentAsync, SearchDocumentsAsync
- [x] T037 [P] [US1] Create `Services/DocumentService.cs` implementing core CRUD operations:
  - UploadDocumentAsync: validate file (type, size), check quota, save file, create metadata record, log audit
  - GetDocumentAsync: authorization check, return metadata
  - ListDocumentsAsync: return user's non-deleted documents with pagination
  - DeleteDocumentAsync: soft-delete (set IsDeleted=1, DeletedDate=now), audit log
  - RestoreDocumentAsync: clear IsDeleted flag if within 30 days, audit log
- [x] T038 [P] [US1] Create `Controllers/DocumentApiController.cs` with endpoints:
  - `POST /api/documents/upload` - multipart form-data, calls DocumentService.UploadDocumentAsync
  - `GET /api/documents/{id}/download` - serves file, authorization check, logs download
  - `DELETE /api/documents/{id}` - soft-delete endpoint
  - `POST /api/documents/{id}/restore` - restore endpoint
  - `GET /api/documents/statistics` - return user's quota info
- [x] T039 [P] [US1] Create `Pages/DocumentUpload.razor` - upload form with:
  - File selector
  - Title input (required)
  - Description textarea (optional)
  - Category dropdown (Reports, Meeting Notes, Research, Other)
  - Upload button with progress indicator
  - Error message display
- [x] T040 [P] [US1] Create `Pages/Documents.razor` - document list view with:
  - Document table: Title, Category, Size, Upload Date, Uploader
  - Download button for each document
  - Delete button (with confirmation)
  - Search box
  - Category filter
  - Pagination (20 per page)
  - Empty state message when no documents
- [x] T041 [P] [US1] Create `Pages/DocumentDetail.razor` - document detail/preview page with:
  - Document metadata: title, description, category, tags, size, upload date, uploader
  - Download button
  - Delete button
  - Restore button (if soft-deleted and within 30 days)
  - Audit log for this document (read-only list)
  - Return to list link
- [x] T042 [US1] Create navigation menu item for Documents in `Pages/Shared/NavMenu.razor`
- [x] T043 [US1] Unit tests for DocumentService:
  - Test successful upload with metadata
  - Test upload rejected (exceeds quota, invalid file type)
  - Test soft-delete and restore within 30 days
  - Test cannot restore after 30 days
  - Test authorization checks (IDOR prevention)
- [x] T044 [US1] Integration tests for upload API:
  - Test end-to-end upload workflow
  - Test error cases (quota exceeded, file too large, invalid type)
  - Test download authorization
- [x] T045 [US1] Manual test: Quickstart Scenario 1 (Basic Upload) - passes all steps

**Checkpoint**: Users can upload, view, and download documents with quota enforcement

### User Story 2: Team Lead Manages Team Member Documents (P1)

**Goal**: Team Leads can view and search documents uploaded by their team members

**Independent Test**: Team Lead views team documents, searches by title/category, sees metadata correctly

### Implementation for User Story 2

- [ ] T046 [P] [US2] Extend `DocumentService.ListDocumentsAsync` to accept filter parameter: `forTeamLeadOfTeams` (list of team IDs) to return all documents from team members
- [ ] T047 [P] [US2] Add authorization check in DocumentService: Team Lead can view team member documents
- [ ] T048 [P] [US2] Add `GET /api/documents/team` endpoint to DocumentApiController - returns team's documents with optional team ID filter
- [ ] T049 [P] [US2] Extend `Pages/Documents.razor` with "View Mode" toggle:
  - "My Documents" tab (default, current user's docs only)
  - "Team Documents" tab (if user is Team Lead, shows team member docs)
  - Show "Uploaded by: [Name]" for team documents
  - Filter by team member name
- [ ] T050 [US2] Unit tests for team document listing:
  - Test Team Lead sees all team documents
  - Test Team Lead cannot see other teams' documents
  - Test non-Team-Lead cannot access this feature
- [ ] T051 [US2] Integration tests:
  - Test Team Lead retrieves team documents via API
  - Test authorization (non-Team-Lead gets 403)
- [ ] T052 [US2] Manual test: Quickstart Scenario 5 (adapted for team listing)

**Checkpoint**: Team Leads can manage team member documents with proper authorization

### User Story 3: Project Manager Uploads Project-Related Documents (P1)

**Goal**: Documents can be associated with projects; project members can see project documents

**Independent Test**: Upload document linked to project, verify project members see it, non-members cannot

### Implementation for User Story 3

- [ ] T053 [P] [US3] Modify DocumentService.UploadDocumentAsync to accept optional projectId parameter
- [ ] T053 [P] [US3] Modify `Models/Document.cs` to ensure ProjectId nullable FK to existing Project model
- [ ] T054 [P] [US3] Add `GET /api/documents/project/{projectId}` endpoint to DocumentApiController
- [ ] T055 [P] [US3] Modify Documents.razor to show project association:
  - "Upload to Project" dropdown (optional)
  - Filter documents by project
  - Show "Project: [name]" in document list
- [ ] T056 [P] [US3] Update DocumentService.GetDocumentAsync authorization: allow project members to view project documents
- [ ] T057 [US3] Unit tests:
  - Test project document creation with ProjectId
  - Test project members can view project documents
  - Test non-members cannot view project documents
- [ ] T058 [US3] Integration tests:
  - Test project documents API endpoint
  - Test authorization by project membership
- [ ] T059 [US3] Manual test: Upload a project document, verify only project members see it

**Checkpoint**: Project documents working with team member authorization

### User Story 4: Search & Categorization (P1)

**Goal**: Users can find documents quickly via search and filters

**Independent Test**: Upload 10+ documents, search by keyword, filter by category, verify results < 2s

### Implementation for User Story 4

- [ ] T060 [P] [US4] Extend DocumentService with SearchDocumentsAsync method:
  - Full-text search on Title, Description, Tags, Category
  - Filter by category, project, date range
  - Sort by: title, upload date, file size
  - Return paginated results
  - Target SLA: < 2 seconds for 500 documents
- [ ] T061 [P] [US4] Create `POST /api/documents/search` endpoint accepting query, filters, sort, pagination
- [ ] T062 [P] [US4] Extend `Pages/Documents.razor`:
  - Full-text search input box
  - Category filter dropdown
  - Date range picker (from/to dates)
  - Sort dropdown (Title, Date, Size)
  - Display search results with relevance indicator
  - Show "X results found in 0.5s"
- [ ] T063 [P] [US4] Add database indexes for search performance (documented in T017)
- [ ] T064 [US4] Unit tests for search:
  - Test search by keyword in title, description, tags
  - Test category filter
  - Test date range filter
  - Test sort order
  - Test pagination
- [ ] T065 [US4] Performance tests:
  - Test search against 500 documents, verify < 2s response
  - Test with complex filters, ensure acceptable performance
- [ ] T066 [US4] Manual test: Quickstart Scenario 3 (Search & Filtering) - passes all steps

**Checkpoint**: Search and filtering working with good performance

### Quota Enforcement & Display (P1)

- [ ] T067 [P] [P1] Create quota display component:
  - Show user's quota: "1.2 GB of 5 GB used (24%)"
  - Show progress bar with color: green (0-80%), orange (80-95%), red (95-100%)
  - Warning message at 95%: "You're approaching your storage limit. Contact admin for quota increase."
- [ ] T068 [P] [P1] Add quota check on Documents.razor: display quota bar at top of page
- [ ] T069 [P] [P1] Modify upload form to check quota before submission
  - Disable upload if user at quota
  - Show: "Your quota is full. Delete or archive documents to make space."
- [ ] T070 [US4] Unit tests for quota display and enforcement
- [ ] T071 [US4] Manual test: Quickstart Scenario 2 (Quota Enforcement) - passes all steps

**Checkpoint**: Users can see their quota status; cannot exceed limits

### Soft-Delete & Recovery (P1)

- [ ] T072 [P] [P1] Implement soft-delete recovery window:
  - Show "Recently Deleted" section in Documents.razor (if any deleted docs within 30 days)
  - List deleted documents with "Expires in X days" countdown
  - Restore button for each
  - Cannot restore after 30 days (button disabled)
- [ ] T073 [P] [P1] Create scheduled job for auto-purge:
  - `Services/DocumentPurgeService.cs` with PurgeExpiredDocumentsAsync method
  - Delete physical files for deleted documents past 30-day window
  - Hard-delete from database
  - Log purge action to audit log
  - Schedule to run daily (via hosted service in Program.cs)
- [ ] T074 [US4] Unit tests for soft-delete and purge
- [ ] T075 [US4] Manual test: Quickstart Scenario 4 (Delete & Recovery) - passes all steps

**Checkpoint**: Soft-delete with 30-day recovery window fully functional

### Audit Logging for Compliance (P1)

- [ ] T076 [P] [P1] Ensure all document operations logged via DocumentAuditService:
  - Upload: log filename, size, category, result
  - Download: log document title, size, result
  - Delete/Restore: log action and result
  - View/Preview: log document viewed, result
- [ ] T077 [P] [P1] Create Admin audit log view: `Pages/Admin/DocumentAudit.razor`
  - Table: Timestamp, User, Action, Document, Result, IP Address
  - Filter by: user, action, date range, result
  - Export to CSV
  - Read-only
- [ ] T078 [P] [P1] Add `GET /api/admin/documents/audit-log` endpoint for admin audit view
- [ ] T079 [US4] Unit tests for audit logging completeness
- [ ] T080 [US4] Manual test: Verify audit log entries for all operations

**Checkpoint**: Comprehensive audit logging for compliance

### Phase 2 Completion Testing

- [ ] T081 [P1] Integration test suite: Document lifecycle (upload → list → download → delete → restore)
- [ ] T082 [P1] Security review: IDOR testing, path traversal testing, SQL injection testing
- [ ] T083 [P1] Performance baseline: upload < 30s, search < 2s, list < 2s
- [ ] T084 [P1] Code review: Spec compliance, architecture (layers), 80%+ test coverage

**Checkpoint**: Phase 1 (P1) complete - MVP ready for user testing

---

## Phase 3: Collaboration & Compliance (P2 - User Stories 5, 6, 7)

**Purpose**: Document sharing, task integration, GDPR compliance, accessibility

**Duration**: 4 weeks

### User Story 5: Share Documents with Team (P2)

**Goal**: Users can share documents with specific team members, recipients get notifications

**Independent Test**: Share document with user, recipient sees in "Shared with Me", unshare works

### Implementation for User Story 5

- [ ] T085 [P] [US5] Create `Services/Interfaces/IDocumentSharingService.cs` with methods: ShareDocumentAsync, UnshareDocumentAsync, GetSharedWithMeAsync, GetDocumentSharesAsync
- [ ] T086 [P] [US5] Create `Services/DocumentSharingService.cs` implementing sharing logic:
  - Check owner can share
  - Create DocumentShare record
  - Check for duplicate shares
  - Validate recipient exists
- [ ] T087 [P] [US5] Create `POST /api/documents/{id}/share` endpoint with ShareDto (SharedWithUserId list)
- [ ] T088 [P] [US5] Create `DELETE /api/documents/{id}/share/{userId}` endpoint for unshare
- [ ] T089 [P] [US5] Create `GET /api/documents/shared-with-me` endpoint returning shared documents
- [ ] T090 [P] [US5] Extend Documents.razor with sharing UI:
  - "Share" button/modal for document owner
  - Search and select users to share with
  - List current shares with revoke button
  - "Shared with Me" tab showing documents shared with current user
  - Cannot edit/delete shared documents (read-only except for owner)
- [ ] T091 [US5] Notification on share:
  - Modify NotificationService to handle document shares
  - Send notification: "[User] shared [Document] with you"
  - Link to document detail page
- [ ] T092 [US5] Unit tests for document sharing
- [ ] T093 [US5] Manual test: Quickstart Scenario 5 (Sharing & Notifications) - passes all steps

**Checkpoint**: Document sharing with notifications working

### User Story 6: Attach Documents to Tasks (P2)

**Goal**: Documents can be attached to tasks; shown in task detail; associates document with project

**Independent Test**: Attach existing doc to task, upload new doc and attach, see in task detail

### Implementation for User Story 6

- [ ] T094 [P] [US6] Create `Services/TaskDocumentService.cs` with methods: AttachDocumentToTaskAsync, DetachDocumentAsync, GetTaskDocumentsAsync
- [ ] T094 [P] [US6] Add DocumentId FK reference to TaskItem model (or create TaskDocumentAttachment join table if needed)
- [ ] T095 [P] [US6] Create `POST /api/tasks/{taskId}/documents/{documentId}` endpoint for attachment
- [ ] T096 [P] [US6] Create `DELETE /api/tasks/{taskId}/documents/{documentId}` endpoint for detachment
- [ ] T097 [P] [US6] Extend TaskItem model and EF Core with document relationship
- [ ] T098 [P] [US6] Update Tasks.razor to show attached documents:
  - "Attached Documents" section in task detail
  - Upload new document button (auto-attaches to task, auto-associates with task's project)
  - Attach existing document button (search dialog)
  - List with download links
  - Delete/detach button for owner
- [ ] T099 [US6] Update document detail page to show task associations
- [ ] T100 [US6] Unit tests for task attachment
- [ ] T101 [US6] Manual test: Quickstart Scenario 6 (Task Integration) - passes all steps

**Checkpoint**: Document-task integration working

### User Story 7: GDPR & Accessibility (P2)

**Goal**: EU users' data privacy rights; compliant document UI

**GDPR Data Subject Access & Erasure**:

- [ ] T102 [P] [US7] Create `Services/Interfaces/IGDPRService.cs` with methods: ExportUserDataAsync, RequestUserDeletionAsync, GetUserAccessHistoryAsync, IsUserMarkedForDeletionAsync
- [ ] T103 [P] [US7] Create `Services/GDPRService.cs` implementing GDPR compliance:
  - ExportUserDataAsync: Generate JSON/CSV of user's documents, shares, access history
  - RequestUserDeletionAsync: Create deletion request record with 30-day countdown
  - GetUserAccessHistoryAsync: Query document access logs (downloads, previews, views)
  - IsUserMarkedForDeletionAsync: Check if user deletion pending
- [ ] T104 [P] [US7] Create `GET /api/gdpr/export-data` endpoint - returns user's data export (JSON/CSV)
- [ ] T105 [P] [US7] Create `POST /api/gdpr/request-deletion` endpoint - requests account deletion after 30 days
- [ ] T106 [P] [US7] Create `GET /api/admin/gdpr/pending-deletions` endpoint - admin view of deletion requests
- [ ] T107 [P] [US7] Create `POST /api/admin/gdpr/confirm-deletion/{userId}` endpoint - admin approves deletion
- [ ] T108 [US7] Create Account Settings page: `Pages/Profile.razor` enhancements with:
  - "Download My Data" button (calls GDPR export)
  - "Request Account Deletion" button with confirmation dialog
  - Privacy notice with data retention policy
  - Show deletion request status if pending
- [ ] T109 [US7] Create admin page: `Pages/Admin/GDPRRequests.razor` showing:
  - List of pending deletion requests
  - Countdown for each (30 days)
  - Approve button (executes deletion immediately)
  - Cancel button (cancels deletion request)

**Accessibility (WCAG 2.1 Level AA)**:

- [ ] T110 [P] [US7] Audit semantic HTML for document pages:
  - Form labels with <label> tags
  - Proper heading hierarchy (h1, h2, h3)
  - ARIA attributes for buttons, dialogs, status messages
  - Alt text for any icons/images
- [ ] T111 [P] [US7] Keyboard navigation testing:
  - All form controls reachable via Tab key
  - Focus indicators visible on all interactive elements
  - Enter/Space to activate buttons
  - Escape to close modals
  - Test in Documents.razor, DocumentUpload.razor, DocumentDetail.razor
- [ ] T112 [P] [US7] Screen reader compatibility:
  - Test with NVDA or similar
  - Document list announced correctly
  - Form labels announced
  - Error messages announced
- [ ] T113 [P] [US7] Color contrast verification:
  - Use axe DevTools or WAVE tools
  - Ensure 4.5:1 contrast ratio for text
  - No violations in document.css
- [ ] T114 [P] [US7] Zoom/responsive testing:
  - Test document pages at 200% zoom
  - Ensure content readable
  - No horizontal scroll unless necessary
- [ ] T115 [US7] Localization string externalization:
  - Extract all UI strings to resource files (.resx)
  - Support multiple locales (en-US, es-ES, fr-FR, de-DE, it-IT - structure ready, implement English only for MVP)
  - Date/time formatting locale-aware
  - Number formatting locale-aware
- [ ] T116 [US7] Unit tests for GDPR functionality
- [ ] T117 [US7] Accessibility automated testing: axe DevTools scan, zero critical issues
- [ ] T118 [US7] Manual test: Quickstart Scenario 7 (Accessibility) - passes all steps
- [ ] T119 [US7] Manual test: Quickstart Scenario 8 (GDPR Compliance) - passes all steps

**Checkpoint**: GDPR and accessibility compliance complete

---

## Phase 4: Polish & Analytics (P3 - User Story 8)

**Purpose**: Dashboard integration, admin reporting, performance optimization

**Duration**: 2 weeks

### User Story 8: Dashboard Recent Documents Widget (P3)

**Goal**: Dashboard shows user's last 5 uploaded documents with links

**Independent Test**: Upload documents, verify widget shows correct 5 most recent, click opens document

### Implementation for User Story 8

- [ ] T120 [P] [US8] Create `Services/DashboardService.cs` with method: GetRecentDocumentsAsync(userId, count=5)
- [ ] T121 [P] [US8] Create `GET /api/dashboard/recent-documents` endpoint
- [ ] T122 [P] [US8] Create dashboard widget component: `Pages/Components/RecentDocumentsWidget.razor`
  - Show "Recent Documents" section
  - List of last 5 documents: title, category, upload date
  - "View All Documents" link
  - "No documents yet" message if empty
- [ ] T123 [P] [US8] Integrate widget into main Dashboard page (`Pages/Index.razor`)
  - Position in dashboard layout
  - Refresh on document upload
- [ ] T124 [US8] Unit tests for RecentDocumentsWidget
- [ ] T125 [US8] Manual test: Quickstart Scenario 9 (concurrent uploads) verifies widget updates

**Checkpoint**: Dashboard widget live

### Admin Reporting & Analytics (P3)

- [ ] T126 [P] [P3] Create `Services/ReportingService.cs` with methods:
  - GenerateUsageReportAsync: documents uploaded/deleted, active uploaders, top categories, file size distribution
  - CreateAuditReportAsync: detailed operation log, can filter by date range
  - GetStorageStatsAsync: org quota usage, per-user quotas
- [ ] T127 [P] [P3] Create admin reporting endpoints:
  - `GET /api/admin/reports/usage` - usage statistics
  - `GET /api/admin/reports/audit` - detailed audit log export
  - `GET /api/admin/reports/storage` - storage statistics
- [ ] T128 [P] [P3] Create admin reports page: `Pages/Admin/DocumentReports.razor`
  - Usage report: total documents, active users, top categories, file size stats
  - Audit report: detailed operation log with filters (date range, user, action)
  - Export to CSV
  - Charts showing trends
  - Target response time: < 5 seconds
- [ ] T129 [P3] Unit tests for reporting service
- [ ] T130 [P3] Performance tests: Report generation < 5s for typical dataset

**Checkpoint**: Admin reporting available

### Localization Infrastructure (P3)

- [ ] T131 [P] [P3] Create resource files for document feature strings:
  - `ContosoDashboard.resx` (English - primary)
  - Structure ready for: es.resx, fr.resx, de.resx, it.resx (not translated for MVP)
  - Include all UI strings: button labels, error messages, form labels, notifications
- [ ] T132 [P] [P3] Update all Razor pages to use localized strings:
  - Replace hardcoded strings with `@localizer["StringKey"]`
  - Test with English locale
- [ ] T133 [P] [P3] Configure locale-aware formatting:
  - Date/time formatting per culture
  - Number formatting per culture (decimal separators, thousands)
  - File size display (using invariant culture for bytes/KB/MB)
- [ ] T134 [P] [P3] CSS: Add RTL layout support (right-to-left ready):
  - Use logical properties (start/end) instead of left/right
  - Test CSS doesn't break with text-direction: rtl
  - No hard-coded positioning

**Checkpoint**: Localization infrastructure ready, English fully functional

### Performance Optimization (P3)

- [ ] T135 [P] [P3] Database query optimization:
  - Verify indexes created (from T017)
  - Use `.AsNoTracking()` for read-only queries
  - Eager load navigation properties where needed
  - Profile slow queries
- [ ] T136 [P] [P3] Pagination & lazy loading:
  - Document list: 20 per page, lazy load next pages
  - Audit log: 50 per page
  - Search results: 20 per page
- [ ] T137 [P] [P3] Caching optimization:
  - Cache user quota (invalidate on upload/delete)
  - Cache user's recent documents (invalidate on upload)
  - 5-minute cache duration for most queries
- [ ] T138 [P] [P3] File upload optimization:
  - Support chunked upload for large files
  - Resume capability for interrupted uploads
  - Show upload speed/ETA
- [ ] T139 [P3] Performance tests:
  - Upload 25 MB file: target < 30s
  - List 500 documents: target < 2s
  - Search 500 documents: target < 2s
  - Document preview load: target < 3s
  - Report generation: target < 5s
- [ ] T140 [P3] Load testing: Simulate 50 concurrent users, system stable

**Checkpoint**: Performance targets met

### Final Testing & Documentation (P3)

- [ ] T141 [P3] Full regression test suite: All 9 quickstart scenarios
- [ ] T142 [P3] Security penetration testing:
  - IDOR vulnerability testing
  - Path traversal testing
  - SQL injection testing
  - Authorization bypass testing
- [ ] T143 [P3] Accessibility final audit: axe DevTools full scan, zero critical/serious issues
- [ ] T144 [P3] Code review checklist:
  - ✅ Spec compliance (all 92 requirements met)
  - ✅ Architecture (layers, dependency flow)
  - ✅ Test coverage (target 80%+ for services, 60%+ for pages)
  - ✅ Security (IDOR, auth, audit logging)
  - ✅ Documentation (API docs, user guide, admin guide)
- [ ] T145 [P3] Create API documentation (OpenAPI/Swagger)
- [ ] T146 [P3] Create user guide (how to upload, organize, share documents)
- [ ] T147 [P3] Create admin guide (quota management, audit logs, GDPR requests)
- [ ] T148 [P3] Create troubleshooting guide (common issues, resolutions)

**Checkpoint**: Production ready

---

## Task Summary

| Phase | Tasks | Duration | Status |
|---|---|---|---|
| Phase 0: Setup | T001-T009 | 1-2 days | Planning |
| Phase 1: Foundation | T010-T035 | 1 week | Planning |
| Phase 2: P1 Features | T036-T084 | 3 weeks | Planning |
| Phase 3: P2 Features | T085-T119 | 3 weeks | Planning |
| Phase 4: P3 Features | T120-T148 | 2 weeks | Planning |
| **TOTAL** | **148 tasks** | **10 weeks** | **Ready** |

---

## Parallel Opportunities

Tasks that can run in parallel (same sprint/iteration):
- **Phase 0**: All setup tasks (T001-T009) can run in parallel
- **Phase 1**: Entity creation (T010-T015) can run in parallel; then migration (T016-T019) sequential
- **Phase 1**: Service interfaces (T020-T035) after entities complete
- **Phase 2**: Models/entities for US1-US4 (T036-T043) can start in parallel
- **Phase 2**: API endpoints (T044-T069) can run in parallel after service interfaces ready
- **Phase 3**: Each user story (US5-US8) can run in parallel
- **Phase 4**: Reports (T126-T130), localization (T131-T134), optimization (T135-T140) can run in parallel

---

## Independent Test Criteria per User Story

- **US1 (Upload/Download)**: Can upload document, verify in list, download, confirm audit log entry
- **US2 (Team Documents)**: Team Lead views team member documents; non-Lead cannot access
- **US3 (Project Documents)**: Project member sees project documents; non-member cannot; ProjectId persisted
- **US4 (Search/Categorization)**: Search returns correct results in < 2s; filters work; pagination correct
- **US5 (Sharing)**: Share with user, recipient sees in "Shared with Me"; unshare works; notifications sent
- **US6 (Task Attachment)**: Attach existing doc; upload new and attach; appears in task detail; project auto-linked
- **US7 (GDPR/Accessibility)**: Export data works; deletion request creates 30-day countdown; WCAG audit passes
- **US8 (Dashboard)**: Widget shows last 5 docs; updates after new upload; link works; empty state displays

---

## MVP Scope (Weeks 1-4, Phase 1-2)

**Minimum Viable Product includes**:
- ✅ Upload documents with title, description, category
- ✅ View personal document list with search and filters
- ✅ Download documents
- ✅ Soft-delete with 30-day recovery
- ✅ Storage quota enforcement (5GB/user, 100GB/org)
- ✅ Comprehensive audit logging
- ✅ Team member document visibility (for Team Leads)
- ✅ Project document association
- ✅ Admin audit log view

**NOT in MVP (Phase 3-4)**:
- Document sharing (User Story 5 - P2)
- Task attachment (User Story 6 - P2)
- GDPR/Accessibility full implementation (User Story 7 - P2)
- Dashboard widget (User Story 8 - P3)
- Admin reporting (P3)
- Localization translations (P3)

---

## Definition of Done

Each task is done when:
1. ✅ Code compiles without errors
2. ✅ All acceptance criteria met
3. ✅ Unit tests pass (if applicable)
4. ✅ Integration tests pass (if applicable)
5. ✅ No new security issues introduced
6. ✅ Code review approved
7. ✅ Spec requirement(s) satisfied

---

## Execution Strategy

### Sprint Planning Guidance

**Sprint 1 (Week 1)**: Phase 0 (Setup) + Phase 1 (Entities & Services)
- Goal: Project structure ready, database schema created, service layer defined
- Stories: None (foundational only)

**Sprint 2 (Week 2)**: Phase 1 (Services continued) + Phase 2 US1 (Upload/Download)
- Goal: Core upload/download workflow functional
- Stories: US1 (Employee uploads) MVP

**Sprint 3 (Week 3)**: Phase 2 US2-US4 (Team docs, Project docs, Search)
- Goal: Multi-user scenarios and search working
- Stories: US2, US3, US4

**Sprint 4 (Week 4)**: Phase 2 finishing (Quota, Soft-delete, Audit, Testing)
- Goal: MVP complete and tested
- Stories: P1 acceptance testing, security review

**Sprint 5-6 (Weeks 5-6)**: Phase 3 US5-US6 (Sharing, Task Integration)
- Goal: Collaboration features
- Stories: US5, US6

**Sprint 7 (Week 7)**: Phase 3 US7 (GDPR & Accessibility)
- Goal: Compliance and accessibility
- Stories: US7

**Sprint 8 (Week 8)**: Phase 3 finishing (Testing, docs)
- Goal: Phase 2 release ready
- Stories: P2 acceptance testing

**Sprint 9 (Week 9)**: Phase 4 US8 + Reporting (Dashboard, Reports)
- Goal: Analytics and reporting
- Stories: US8, admin features

**Sprint 10 (Week 10)**: Phase 4 finishing (Localization, optimization, final testing)
- Goal: Production ready, all tests pass
- Stories: P3 completion, full regression testing

---

## Next Steps

1. ✅ **Review this tasks.md** - Team provides feedback on task breakdown and ordering
2. **Sprint Planning** - Assign tasks to sprints and estimate story points
3. **Development Begins** - Phase 0 (Setup) starts immediately
4. **Weekly Standups** - Track progress against phase deliverables and blockers
5. **Feature Release** - MVP released after Sprint 4; P2/P3 follow in subsequent sprints

---

**Status**: ✅ **READY FOR DEVELOPMENT**

All 148 tasks are defined, ordered, and independent-test-ready. Developers can begin Phase 0 (Setup) immediately.
