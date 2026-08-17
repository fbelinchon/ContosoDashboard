# Research Phase: Document Upload and Management

**Feature**: 001-document-management  
**Date**: 2026-08-14  
**Status**: Research Complete - All clarifications resolved

## Findings Summary

All technical unknowns and design decisions have been resolved during the Clarification phase. This research phase documents the finalized direction for each key area.

## 1. Storage Architecture (Offline-First with Cloud Migration Path)

**Decision**: Local filesystem storage with interface abstraction enabling Azure Blob Storage migration

**Rationale**: 
- Training context requires offline-first capability without cloud dependencies
- Interface abstraction (IFileStorageService) eliminates refactoring at business logic layer
- Path pattern works identically for local filesystem and Azure blobs: `{userId}/{projectId}/{guid}.{ext}`
- No implementation changes needed for cloud migration—only configuration change

**Implementation Pattern**:
```
Local Implementation: LocalFileStorageService (System.IO.File)
↓
Interface: IFileStorageService { UploadAsync(), DownloadAsync(), DeleteAsync(), GetUrlAsync() }
↓
Future Azure: AzureBlobStorageService (Azure.Storage.Blobs SDK)
```

**File Path Strategy**: 
- Prevent path traversal: Use GUID-based filenames instead of user-supplied names
- Prevent orphaned records: Generate unique path → save file → save metadata to DB
- Storage location: `AppData/uploads/` (outside wwwroot for security)
- Document URLs: Served through authenticated controller endpoints only

## 2. Data Persistence & Entity Framework

**Decision**: Entity Framework Core with SQL Server LocalDB for training; extension-ready for cloud

**Rationale**:
- Aligns with existing ContosoDashboard architecture (User, Project, Task already use EF Core)
- LocalDB suitable for training without external dependencies
- EF Core supports async/await patterns for responsive UI
- Schema design supports multi-tenancy patterns for future scaling

**Key Entity Decisions**:
- **DocumentId**: Integer primary key (consistency with User and Project tables)
- **Category**: Text values, not enum (flexibility for admin customization)
- **FilePath**: Stores GUID-based paths securely; no direct file access from URLs
- **IsDeleted + DeletedDate**: Soft-delete pattern with 30-day recovery window
- **Audit Trail**: DocumentAuditLog tracks all operations for GDPR compliance

**Relationships**:
- Document → User (uploader)
- Document → Project (optional, for project-specific docs)
- Document → DocumentTag (many-to-many for flexible categorization)
- Document → DocumentShare (track sharing relationships)

## 3. Upload Queue Management

**Decision**: Sequential upload processing with client-side queue visualization

**Rationale**:
- Sequential processing prevents database lock contention and file conflicts
- Single active upload simplifies transaction management and error handling
- Client-side queue UI provides clear visibility into pending uploads
- Concurrent upload attempt patterns are rare in practice; users comfortable with queuing

**Architecture**:
- Upload manager maintains queue in-memory or via browser IndexedDB
- Single worker processes queue items sequentially
- Each queued item displays: filename, size, progress percentage, position in queue
- User can cancel individual queued items or the in-progress upload
- Auto-retry logic for transient failures (network timeout, virus scan delay)

## 4. Virus & Malware Scanning Strategy

**Decision**: Placeholder for scanning service; can be integrated or mocked

**Rationale**:
- Specification requires "scan before storage" but training context can use mock implementation
- Abstraction pattern: IVirusScanner interface with MockVirusScanner for training
- Production implementation: ClamAV, YARA, or commercial scanning service
- Non-blocking: File accepted if scanner unavailable (security trade-off documented)

**Implementation**:
- After file saved to temporary location, invoke scanner
- Scanner returns clean/infected status
- If clean: move to permanent location, save metadata to DB
- If infected: quarantine file, log incident, notify admin, reject upload

## 5. Authentication & Authorization Architecture

**Decision**: Extend existing mock auth system; leverage existing claims-based identity

**Rationale**:
- ContosoDashboard already has mock auth (cookie-based, 8-hour sliding expiration)
- Users already have role claims (Administrator, Project Manager, Team Lead, Employee)
- Existing AuthService and CustomAuthenticationStateProvider provide context
- Add document access checks at service layer before returning data

**Authorization Tiers**:
1. **Middleware**: General auth (user must be logged in)
2. **Page/Controller**: Role-based (certain pages require Admin role)
3. **Service**: Business logic verification (employee can only see own docs unless shared)
4. **Data Layer**: Query filtering (EF Core queries scoped to authorized documents)

## 6. Search & Discovery Implementation

**Decision**: Full-text search using EF Core LINQ with local database indexing

**Rationale**:
- LocalDB supports indexed queries on Title, Description, Tags
- LINQ-to-SQL enables type-safe queries from C# business layer
- Performance: Index on Title, Category, ProjectId, UploadedByUserId
- 2-second SLA achievable for 500 documents with proper indexing

**Query Strategy**:
- IndexedDb for browser-side offline search (nice-to-have, not MVP)
- Server-side LINQ queries with IQueryable for lazy evaluation
- Pagination (top 50 results per page) to keep response size manageable

## 7. Soft-Delete & Data Retention Pattern

**Decision**: 30-day soft-delete recovery window with auto-purge scheduled job

**Rationale**:
- GDPR "right to erasure" requires 30-day notice period (users can change minds)
- Soft-delete enables recovery without admin intervention
- Auto-purge after 30 days (scheduled background job) ensures compliance
- Audit trail preserved: log shows deletion and purge events

**Implementation**:
- Delete action: Set IsDeleted=true, DeletedDate=now, log in audit trail
- Recovery: Set IsDeleted=false, log restore event
- Auto-purge: Scheduled job runs daily, finds documents where DeletedDate < 30-days-ago, hard-deletes file and DB record
- Deleted documents excluded from user UI but visible in admin audit dashboard

## 8. Storage Quota Enforcement

**Decision**: Per-user (5 GB) and organization-level (100 GB) quotas with real-time display

**Rationale**:
- Per-user quota prevents single user from exhausting org storage
- Org-level quota prevents runaway growth across all users
- Real-time display: query sum(DocumentFileSize) for current user and org
- Quota check before accepting upload: if (usedBytes + newFileSize > quotaBytes) reject

**Quota Display UI**:
- Upload page: "You've used 2.3 GB of 5.0 GB (46%)"
- Admin dashboard: Org quota "42 GB of 100 GB used (42%)" with per-user breakdown
- Admin can adjust quotas per user or organization

## 9. Accessibility (WCAG 2.1 Level AA)

**Decision**: Semantic HTML + ARIA labels + keyboard navigation + color contrast

**Rationale**:
- WCAG 2.1 AA is enterprise standard for accessibility
- Blazor Server supports ARIA attributes and semantic HTML
- Keyboard navigation requires no special framework support
- Automated testing (axe DevTools, WAVE) validates compliance during CI

**Focus Areas**:
- File upload input: Proper label association, focus indicators
- Progress indicators: Announce upload progress to screen readers
- Error messages: Clear text descriptions, not color-coded alone
- Document list: Table structure with header, sort buttons with aria-sort
- Preview modal: Closable with Escape, focus trap implemented

## 10. Localization & Internationalization (i18n)

**Decision**: Externalize all UI strings to resource files; support locale-aware formatting

**Rationale**:
- Externalized strings enable translation without code changes
- .NET provides ResourceManager and localization middleware built-in
- Blazor supports .NET globalization for locale-aware date/time/number formatting
- RTL (Arabic, Hebrew) layout support requires CSS flexbox/grid (no special code)

**String Categories**:
- UI labels and buttons → resource file
- Error messages → resource file with parameterization
- Success messages → resource file
- Format strings (date, time, number, file size) → culture-aware formatting

## 11. GDPR Compliance Data Subject Rights

**Decision**: Implement data portability, erasure, and access transparency endpoints

**Rationale**:
- Data Subject Access Request (DSAR): Users download all personal data
- Right to Erasure: Users request deletion; system complies within 30 days
- Access Transparency: Users see audit log of who downloaded/accessed their documents
- Privacy Notice: Clear explanation of data collection and rights

**Implementation**:
- User Account page: "Download my data" button → generates JSON/CSV of documents and metadata
- User Account page: "Request deletion" initiates 30-day countdown
- Admin page: View all DSARs and manage erasure requests
- Document detail page: Show access history (who downloaded, when)

## 12. Audit Logging for Compliance

**Decision**: Structured audit logs with timestamp, actor, action, result, IP address

**Rationale**:
- Meet GDPR audit requirements (who, what, when, where)
- Enable forensic investigation of data breaches or unauthorized access
- Support admin reporting: "Most downloaded documents", "Access patterns"
- Log retention: Match document retention (keep indefinitely, or per policy)

**Log Entry Structure**:
```json
{
  "LogId": 12345,
  "DocumentId": 789,
  "UserId": "user123@contoso.com",
  "Action": "Download",
  "Timestamp": "2026-08-14T14:32:45Z",
  "IpAddress": "192.168.1.100",
  "Result": "Success",
  "Details": "Downloaded file: report-Q3-2026.pdf (2.4 MB)"
}
```

## 13. Task & Project Integration Design

**Decision**: Document picker/uploader in task and project detail pages; linked but independent features

**Rationale**:
- Task integration: Document selector (attach existing) + quick upload (new doc) on task page
- Project integration: Document list auto-filtered by project on project page
- Independence: Document feature functions without task integration (can be added later)
- Database: Task.DocumentId foreign key links documents to tasks

## 14. Dashboard Widget Design

**Decision**: Server-rendered recent documents widget; show last 5 user uploads

**Rationale**:
- Server-side rendering (Blazor Server) enables real-time updates
- Last 5 provides quick access without overwhelming the dashboard
- Sort by upload date (descending) to show most recent
- Click opens document detail page

## Technical Stack Finalization

**Runtime**: .NET 10 / C# 14 (already in use by ContosoDashboard)
**UI Framework**: Blazor Server (already in use)
**Database**: SQL Server LocalDB with Entity Framework Core (already in use)
**Authentication**: Cookie-based mock auth with claims-based identity (already in use)
**Testing**: xUnit + Moq for unit tests, Selenium for integration tests
**Deployment**: Local development only for training context
**API Pattern**: RESTful endpoints for downloads (controller), service layer for business logic

## Open Technical Questions Resolved

✅ All technical unknowns from the specification have been addressed through design decisions.

**None remaining.**

## Success Criteria Technical Validation

- ✅ SC-001: 70% adoption - depends on training delivery, not feature implementation
- ✅ SC-002: <30sec document locate - depends on search UX design
- ✅ SC-003: 90% categorization - depends on user training  
- ✅ SC-004: Zero security incidents - depends on IDOR prevention (service-layer checks) + path traversal prevention (GUID filenames)
- ✅ SC-005: 95% upload completion - depends on robust queue + retry logic
- ✅ SC-006: 80% task attachment success - depends on intuitive UI
- ✅ SC-007: 5sec audit reports - depends on database indexes on AuditLog
- ✅ SC-008: 90% search precision - depends on full-text indexing strategy
- ✅ SC-009: Quota enforcement - implemented via pre-upload check
- ✅ SC-010: WCAG 2.1 AA compliance - achieved through semantic HTML + automated testing
- ✅ SC-011: Keyboard navigation - achieved through Blazor form components + custom JS
- ✅ SC-012: Data subject rights - implemented via DSAR endpoints + audit logging
- ✅ SC-013: 30-day purge completion - implemented via scheduled job

All success criteria are technically achievable with the planned architecture.
