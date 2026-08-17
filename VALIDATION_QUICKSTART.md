# MVP Validation & Quickstart Guide

**Document**: Manual validation steps and quickstart scenario for document management MVP  
**Status**: Ready for testing (post-T038-T041)  
**Date**: 2026-08-14

---

## Part 1: Pre-Flight Validation Checklist

### 1.1 Build Verification

```powershell
cd ContosoDashboard
dotnet clean
dotnet restore
dotnet build
```

**Expected Output**:
```
✅ Build succeeded with 0 errors
⚠️ 53 informational warnings (expected for nullable reference types)
⏱️ Build time: 10-15 seconds
```

### 1.2 Database Verification

```powershell
# Verify migrations
dotnet ef migrations list

# Expected: 2 migrations shown
# - AddDocumentManagementFeature
# - SeedStaticData

# Apply migrations (if not already applied)
dotnet ef database update
```

**Expected Database State**:
- 5 new tables: Documents, DocumentTags, DocumentShares, DocumentAuditLogs, UserStorageQuotas
- 21 indexes created
- Seed data loaded: 4 users (admin, project manager, team lead, employee), 1 project

### 1.3 Project Structure Verification

```powershell
# Verify file structure
ls -R Controllers/
ls -R Pages/
ls -R Tests/

# Expected:
# Controllers/DocumentApiController.cs (8 endpoints)
# Pages/DocumentUpload.razor, Documents.razor, DocumentDetail.razor
# Tests/Unit/DocumentServiceTests.cs, Integration/DocumentApiControllerIntegrationTests.cs
```

### 1.4 DI Container Verification

Check `Program.cs` includes:
```csharp
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IDocumentAuthorizationService, DocumentAuthorizationService>();
builder.Services.AddScoped<IDocumentAuditService, DocumentAuditService>();
builder.Services.AddScoped<IQuotaService, QuotaService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
```

**Verification**: No registration errors on startup

---

## Part 2: Quickstart Scenario

### 2.1 Start Application

```powershell
cd ContosoDashboard
dotnet run
```

**Expected Output**:
```
info: Microsoft.AspNetCore.Hosting.Diagnostics
      Request starting HTTP/1.1 GET https://localhost:5001/
info: Microsoft.AspNetCore.Server.Kestrel[21]
      Kestrel is listening on https://localhost:5001
```

**Browser**: Navigate to `https://localhost:5001`

### 2.2 Login

```
URL: https://localhost:5001/pages/login
Email: admin@contoso.com
Password: (from authentication provider)
Click: Login
Expected redirect: Home page
```

### 2.3 Navigate to Documents Page

```
Click: Documents (from navigation menu)
Expected: 
- Page title: "Documents"
- Empty state: "No documents found"
- Upload button visible
- Quota progress bar shows 0%
```

### 2.4 Upload First Document

**Step 1: Click Upload Button**
```
Button: "Upload Document"
Navigate to: /documents/upload
Expected: Form loads with all fields
```

**Step 2: Fill Form**
```
Title: "Q4 Financial Report"
Description: "Quarterly financial analysis for October-December 2026"
Category: "Reports"
Tags: "quarterly, financial, Q4"
File: Select any PDF < 500MB (sample available in repo)
```

**Step 3: Submit Upload**
```
Button: "Upload Document"
Expected:
✅ Progress bar shows 0-100%
✅ Success message appears
✅ Auto-redirect to /documents after 2 seconds
✅ Document appears in list
✅ File stored in: AppData/uploads/1/1/[GUID].pdf
✅ Database record created in Documents table
✅ Audit log entry created: Action="Upload", Result="Success"
✅ UserStorageQuota updated: UsedBytes += fileSize
```

### 2.5 Verify Upload in Database

**SQL Verification**:
```sql
-- Check document created
SELECT DocumentId, Title, UploadedByUserId, FileSize, IsDeleted 
FROM Documents 
WHERE DocumentId = 1;

-- Expected: 1 row, IsDeleted = 0, FileSize = actual file size

-- Check audit log
SELECT Action, UserId, Result, Timestamp 
FROM DocumentAuditLogs 
WHERE DocumentId = 1 
ORDER BY Timestamp DESC;

-- Expected: "Upload" action, Success result

-- Check quota updated
SELECT UsedBytes, QuotaBytes 
FROM UserStorageQuotas 
WHERE UserId = 1;

-- Expected: UsedBytes = uploaded file size, QuotaBytes = 5,368,709,120
```

### 2.6 View Document List

**Navigate**: /documents

**Verify Display**:
```
✅ Document appears in table
✅ Title: "Q4 Financial Report"
✅ Category badge: "Reports"
✅ File size displayed correctly
✅ Upload date shown
✅ Quota bar shows updated percentage
✅ Pagination controls visible (if multiple docs)
✅ Search box functional
✅ Category filter works
```

### 2.7 Download Document

**Step 1: Click Download Button**
```
Table row action: Download icon
Expected:
✅ File downloaded to Downloads folder
✅ Filename preserved (title + extension)
✅ Correct MIME type (opens as PDF)
✅ Audit log: Action="Download", Result="Success"
```

### 2.8 View Document Details

**Step 1: Click Document Title**
```
Click: "Q4 Financial Report" link
Navigate to: /documents/1
```

**Step 2: Verify Details Page**
```
Expected elements:
✅ Document title displayed
✅ Description shown
✅ File size: "X.XX MB"
✅ MIME type: "application/pdf"
✅ Category badge: "Reports"
✅ Upload date/time
✅ Uploader: "admin@contoso.com"
✅ Tags section showing: "quarterly", "financial", "Q4"
✅ Sharing section: "Not shared"
✅ Activity history section with Upload entry
✅ Download button
✅ Share button
✅ Delete button
```

### 2.9 Share Document

**Step 1: Click Share Button**
```
Button: "Share"
Modal appears: "Share Document"
```

**Step 2: Select Recipient**
```
Dropdown: "Share with user"
Select: "Camille Nicole" (UserId = 2)
Button: "Share"
Expected:
✅ Modal closes
✅ Success message: "Document shared successfully!"
✅ Page refreshes
✅ Sharing section updated: "1 shared"
✅ Recipient listed with share date
✅ Audit log: Action="Share", Result="Success"
```

**Step 3: Verify in Database**
```sql
SELECT ShareId, DocumentId, SharedWithUserId, SharedByUserId, IsRevoked 
FROM DocumentShares 
WHERE DocumentId = 1;

-- Expected: 1 row, SharedWithUserId=2, SharedByUserId=1, IsRevoked=0
```

### 2.10 Test IDOR Prevention

**Step 1: Logout as Admin**
```
Action: Logout
```

**Step 2: Login as Different User**
```
Email: floris.kregel@contoso.com (UserId = 4)
```

**Step 3: Try to Access Shared Document**
```
Direct URL: https://localhost:5001/documents/1
Expected:
✅ Document NOT visible in /documents list (not owner, not shared with this user)
✅ If typed directly: 401/403 or error message
✅ Audit log: No entry (operation blocked before logging)
```

**Step 4: Login as Share Recipient**
```
Email: camille.nicole@contoso.com (UserId = 2)
```

**Step 5: Verify Recipient Can Access**
```
Navigate: /documents
Expected:
✅ Shared document appears in list
✅ Can download
✅ Cannot delete (owner only)
✅ Cannot edit (owner only)
✅ Cannot revoke share (owner initiator only)
```

### 2.11 Soft-Delete & Restore

**Step 1: Return to Admin, Delete Document**
```
Login as: admin@contoso.com
Navigate to: /documents/1
Button: "Delete"
Confirm: "Are you sure?"
Expected:
✅ Document removed from list (soft-deleted)
✅ File NOT deleted from disk (AppData/uploads still has file)
✅ Audit log: Action="Delete", Result="Success"
✅ Quota updated: UsedBytes restored (document no longer counts)
✅ IsDeleted = 1 in database
```

**Step 2: Check Database**
```sql
SELECT DocumentId, Title, IsDeleted, DeletedDate 
FROM Documents 
WHERE DocumentId = 1;

-- Expected: IsDeleted = 1, DeletedDate = current UTC time
```

**Step 3: Restore Document**
```
Option 1 (if UI implements): Show deleted documents checkbox
Option 2 (via API): POST /api/documents/1/restore

Expected:
✅ Document reappears in list
✅ Quota restored: UsedBytes recalculated
✅ Audit log: Action="Restore", Result="Success"
✅ IsDeleted = 0 in database
```

### 2.12 Test 30-Day Recovery Window

**Step 1: Manually Mark as Deleted Long Ago**
```sql
UPDATE Documents 
SET IsDeleted = 1, DeletedDate = DATEADD(day, -31, GETUTCDATE()) 
WHERE DocumentId = 1;

-- Document now 31 days deleted (past 30-day window)
```

**Step 2: Try to Restore**
```
POST /api/documents/1/restore
Expected:
✅ 410 Gone status
✅ Error message: "Recovery window (30 days) expired"
✅ Audit log: Action="Restore", Result="Blocked", Details="Recovery window expired"
```

### 2.13 Test Quota Enforcement

**Step 1: Upload Multiple Files to Approach 5GB Limit**
```
Assume quota limit: 5GB (5,368,709,120 bytes)

Upload file 1: 2GB
Upload file 2: 2GB
Upload file 3: 1GB
Total: 5GB (exactly at limit)

Expected:
✅ Each upload succeeds
✅ Quota shows 100%
✅ Progress bar is red
✅ Warning message appears: "⚠️ Storage Warning: You are using 100% of your storage"
```

**Step 2: Try to Upload When Quota Exceeded**
```
Attempt upload: 100MB file
Expected:
✅ Upload blocked
✅ Error message: "Insufficient quota. You have 0 B remaining."
✅ Audit log: Action="Upload", Result="Blocked", Details="Quota exceeded"
✅ File NOT stored
✅ No database record created
```

**Step 3: Delete Document to Free Space**
```
Delete: 1GB file (brings quota to 80%)
Expected:
✅ Quota recalculated: 80%
✅ Progress bar yellow (warning threshold)
✅ Upload again succeeds
```

### 2.14 Search & Filter

**Navigate**: /documents

**Test Category Filter**
```
Upload documents with categories: Reports, Invoices, Meeting Notes
Filter: "Reports"
Expected:
✅ Only Reports documents shown
✅ Other categories hidden
✅ Count updates
```

**Test Tag Filter**
```
Tags on docs:
- Doc 1: "urgent", "client"
- Doc 2: "archived"
- Doc 3: "urgent", "internal"

Filter: "urgent"
Expected:
✅ Doc 1, Doc 3 shown
✅ Doc 2 hidden
```

**Test Search**
```
Search: "Financial"
Expected:
✅ Documents with "Financial" in title shown
✅ Documents with "Financial" in description shown
✅ Case-insensitive
✅ Partial matches work
```

---

## Part 3: Validation Checklist

### Feature Completion

- [ ] API Controller (T038)
  - [ ] POST /upload (201 Created)
  - [ ] GET /documents (list with pagination)
  - [ ] GET /documents/{id} (details)
  - [ ] GET /documents/{id}/download (file retrieval)
  - [ ] DELETE /documents/{id} (soft-delete)
  - [ ] POST /documents/{id}/restore (restore)
  - [ ] POST /documents/{id}/share (sharing)
  - [ ] DELETE /documents/{id}/share/{shareId} (revoke)
  - [ ] GET /quota/status (quota display)

- [ ] Blazor Pages (T039-T041)
  - [ ] DocumentUpload.razor (form with validation)
  - [ ] Documents.razor (list with filters)
  - [ ] DocumentDetail.razor (details with actions)

### Security Validation

- [ ] [Authorize] attributes on all endpoints
- [ ] Layer 1: Middleware authorization working
- [ ] Layer 2: Role-based checks (if applicable)
- [ ] Layer 3: Service-level ownership verification
- [ ] Layer 4: Query filtering (soft-delete exclusion)
- [ ] IDOR attempts blocked (non-owner cannot access)
- [ ] Path traversal prevention verified (GUID paths)
- [ ] SQL injection prevention (parameterized queries)
- [ ] XSS prevention (Razor encoding)

### Database Validation

- [ ] 5 tables created correctly
- [ ] 21 indexes present
- [ ] Foreign key relationships working
- [ ] Seed data loaded (4 users, 1 project)
- [ ] Soft-delete strategy (IsDeleted + DeletedDate)
- [ ] Audit logging (DocumentAuditLogs table)
- [ ] Cascade delete behaviors correct

### Performance Validation

- [ ] Upload completes in < 30 seconds (400MB file)
- [ ] List loads in < 2 seconds (1000 docs)
- [ ] Quota recalculation < 5 seconds
- [ ] Search/filter responsive (< 1 second)

### GDPR Compliance

- [ ] Audit logs immutable (no cascade delete)
- [ ] Soft-delete with 30-day recovery window
- [ ] DSAR queries functional (per-user audit logs)
- [ ] Data minimization (only necessary fields)

### Error Handling

- [ ] 400 Bad Request for validation failures
- [ ] 401 Unauthorized for missing auth
- [ ] 403 Forbidden for insufficient permissions
- [ ] 404 Not Found for missing resources
- [ ] 413 Payload Too Large for oversized files
- [ ] Errors logged with context
- [ ] User-friendly error messages

### Browser Compatibility

- [ ] Loads in Chrome
- [ ] Loads in Firefox
- [ ] Loads in Edge
- [ ] Responsive design (mobile, tablet, desktop)
- [ ] No JavaScript console errors

---

## Part 4: Success Criteria

**MVP is complete when ALL of the following are true**:

1. ✅ **Build**: `dotnet build` succeeds with 0 errors
2. ✅ **Database**: Migrations applied, all 5 tables created
3. ✅ **API**: All 9 endpoints (upload, list, get, download, delete, restore, share, revoke, quota) return correct status codes
4. ✅ **Pages**: All 3 Blazor pages (upload, list, detail) render without errors
5. ✅ **Upload Workflow**: File → DB → Storage → Audit → Quota all succeed
6. ✅ **Download Workflow**: Authorization → Retrieval → Audit succeed
7. ✅ **Sharing**: Share creation, revocation, and access control all work
8. ✅ **Soft-Delete**: Document marked deleted, file preserved, restoreable within 30 days
9. ✅ **Quota**: Enforced at upload time, updated correctly, displayed accurately
10. ✅ **Security**: IDOR prevented, 4-layer authorization, path traversal blocked
11. ✅ **Audit**: All operations logged with user, timestamp, action, result
12. ✅ **GDPR**: Soft-delete, audit immutability, DSAR support present
13. ✅ **Tests**: Unit tests pass, integration tests structured (ready for CI/CD)
14. ✅ **Performance**: Upload/list/search all < configured timeouts

---

## Part 5: Post-MVP Next Steps

### Phase 3: Admin Features (T046-T055)

- [ ] Admin dashboard showing all documents
- [ ] Admin audit log viewer
- [ ] Admin quota management (per-user limits)
- [ ] Document usage reports
- [ ] Quota alerts for approaching limits

### Phase 4: Advanced Features (T056-T070)

- [ ] Document versioning
- [ ] Full-text search (Elasticsearch integration)
- [ ] File preview (thumbnails, text preview)
- [ ] Bulk operations (multi-delete, multi-share)
- [ ] Document retention policies
- [ ] External cloud storage (Azure Blob Storage)

### Phase 5: Background Jobs (T071-T080)

- [ ] Auto-purge of soft-deleted documents after 30 days
- [ ] Quota recalculation jobs (nightly)
- [ ] Audit log archival
- [ ] Email notifications on share
- [ ] Document expiration reminders

---

## Passing This Validation

**To pass MVP validation**:

1. Complete **Part 2: Quickstart Scenario** from start to finish
2. Check off all items in **Part 3: Validation Checklist**
3. Verify all **Part 4: Success Criteria**
4. Document any blockers or deviations
5. Sign off with timestamp

**Date Validation Completed**: ________________  
**Validated By**: ________________  
**Status**: ✅ PASS / ❌ FAIL (circle one)  
**Blockers**: (describe any issues)

---

**Status**: Ready for execution  
**Next**: Begin Part 2: Quickstart Scenario once T038-T041 API/Pages complete
