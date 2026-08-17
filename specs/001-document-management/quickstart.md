# Quickstart: Document Upload and Management Feature

**Feature**: 001-document-management  
**Date**: 2026-08-14  
**Purpose**: End-to-end validation guide proving feature works as specified

## Prerequisites

- Visual Studio 2022+ or VS Code with C# extension
- .NET 10 SDK installed
- SQL Server LocalDB running (installed with Visual Studio)
- ContosoDashboard repository cloned and synced
- Can run `dotnet build` and `dotnet run` successfully

## Setup

### 1. Database Migrations

Before running the application, apply Entity Framework Core migrations to create document tables:

```powershell
cd ContosoDashboard
dotnet ef migrations add "AddDocumentFeature" --context ApplicationDbContext
dotnet ef database update
```

This creates the following tables in ContosoDashboard.db:
- Document
- DocumentTag
- DocumentShare
- DocumentAuditLog
- UserStorageQuota

### 2. File Storage Directory

Create the upload directory (outside wwwroot for security):

```powershell
mkdir ContosoDashboard/AppData/uploads
```

This directory must exist before upload operations proceed.

### 3. Seed Initial Data (Optional)

Pre-load test documents into the database:

```powershell
dotnet run --seed-documents
```

Or run manually in C# during startup to create sample documents.

## End-to-End Validation Scenarios

### Scenario 1: Basic Upload (Acceptance Criteria)

**Goal**: Validate that a user can upload a document, see it in their list, and download it.

**Test Steps**:

1. **Start Application**
   ```powershell
   cd ContosoDashboard
   dotnet run
   ```
   - Navigate to https://localhost:5001 (or configured port)
   - Expected: Dashboard loads without errors

2. **Login**
   - Click "Login" (or redirect from auth requirement)
   - Select "Camille Nicole" (Project Manager user) from dropdown
   - Click "Login"
   - Expected: Redirected to authenticated dashboard

3. **Navigate to Documents**
   - Click "Documents" in main navigation menu
   - Expected: "My Documents" view loads with empty list (first time)
   - Verify page elements: "Upload Document" button, search box, category filter

4. **Upload a Test Document**
   - Click "Upload Document"
   - Select a file from your computer:
     - Try: PDF file (test-report.pdf, 1-5 MB)
     - Try: Word document (meeting-notes.docx, 500 KB - 2 MB)
   - Enter Title: "Q3 Financial Report"
   - Enter Description: "Financial results for Q3 2026"
   - Select Category: "Reports"
   - Optional: Enter tags "finance, quarterly"
   - Optional: Select a project from dropdown
   - Click "Upload"
   - Expected: Progress indicator shows upload progress (0-100%)
   - Expected: Within 30 seconds, success message appears
   - Expected: Document appears in "My Documents" list with correct metadata

5. **Verify Document Metadata**
   - Document list shows:
     - Title: "Q3 Financial Report"
     - Category: "Reports"
     - Upload Date: Today's date
     - File Size: Correct byte count (e.g., "2.5 MB")
     - Uploaded By: "Camille Nicole"

6. **Download Document**
   - Click document row or "Download" button
   - Expected: Browser downloads the file with original filename
   - Verify file contents match original

7. **Verify Audit Log**
   - Login as Administrator (admin@contoso.com)
   - Navigate to Admin → Audit Logs
   - Filter by Action = "Upload"
   - Expected: Document upload appears with:
     - Document: "Q3 Financial Report"
     - User: "Camille Nicole"
     - Action: "Upload"
     - Result: "Success"

**Acceptance**: ✅ if all steps complete without errors

**Related Requirements**: FR-001 through FR-013, FR-039, FR-042

---

### Scenario 2: Storage Quota Enforcement (Non-Functional)

**Goal**: Validate that 5 GB per-user quota is enforced; users cannot exceed quota.

**Test Steps**:

1. **Prepare Test Files**
   - Create a test file that is 4.8 GB (simulate: create small file and multiply)
   - OR: Use database seeding to create fake 4.8 GB of consumed quota

2. **Attempt Quota-Exceeding Upload**
   - Login as employee user
   - View Document Statistics (profile page)
   - Expected: Shows "Used: 4.8 GB of 5.0 GB (96%)"
   - Attempt to upload 300 MB file
   - Expected: Upload rejected with error:
     - "Your document storage quota is full (5.0 GB exceeded by 300 MB)"
   - Expected: File not created; audit log shows rejected upload

3. **Verify Organization Quota**
   - (Admin view): Check organization total quota
   - Expected: Shows "42 GB of 100 GB used (42%)"

**Acceptance**: ✅ if quota enforcement works as described

**Related Requirements**: FR-050 through FR-054

---

### Scenario 3: Search & Filtering (Performance)

**Goal**: Validate search returns results within 2 seconds; filtering works correctly.

**Test Steps**:

1. **Upload Multiple Documents** (if not pre-seeded)
   - Upload 10-20 documents across different categories
   - Vary: titles, descriptions, tags, projects

2. **Test Category Filter**
   - Click filter "Category: Reports"
   - Expected: List shows only Reports category documents
   - Execution time: < 1 second

3. **Test Full-Text Search**
   - Type "financial" in search box
   - Expected: Results include documents with "financial" in title/description/tags
   - Expected: Results appear within 2 seconds
   - Verify precision: correct documents returned

4. **Test Sort Functionality**
   - Click "Sort by: Upload Date (Newest First)"
   - Expected: Documents reorder with most recent first
   - Click "Sort by: File Size (Largest)"
   - Expected: Documents reorder by size descending

5. **Test Pagination**
   - If more than 20 documents, navigate to page 2
   - Expected: Next page loads and shows different set of documents

**Acceptance**: ✅ if search completes in < 2s and filtering works correctly

**Related Requirements**: FR-014 through FR-022

---

### Scenario 4: Document Deletion & Recovery (Data Lifecycle)

**Goal**: Validate soft-delete with 30-day recovery window works correctly.

**Test Steps**:

1. **Soft-Delete a Document**
   - Click document and select "Delete"
   - Confirm: "Delete? This document will be recoverable for 30 days"
   - Click "Confirm Delete"
   - Expected: Document disappears from user's list
   - Expected: Success message: "Document deleted. You can restore it for 30 days"

2. **Verify Document Not Visible**
   - Expected: Deleted document no longer appears in "My Documents"
   - Expected: Search does not return deleted document

3. **Restore Deleted Document**
   - Logout → Login again (verify persistence across sessions)
   - Navigate back to Documents
   - Expected: Still not visible in main list

4. **Access Recovery UI** (if implemented)
   - Click "Trash" or "Recently Deleted" section
   - Expected: Shows deleted document
   - Expected: Shows "Expires in 29 days" countdown

5. **Restore Document**
   - Click "Restore" on deleted document
   - Expected: Document reappears in "My Documents" list
   - Expected: Audit log shows "Restore" action

6. **Verify Auto-Purge After 30 Days** (simulated test)
   - Manually run scheduled purge job: `dotnet run --purge-expired`
   - Expected: 30-day-old deleted documents are permanently removed
   - Expected: User cannot restore them
   - Expected: Audit log shows "Purge" action

**Acceptance**: ✅ if soft-delete and recovery work as described

**Related Requirements**: FR-027 through FR-031, FR-082

---

### Scenario 5: Document Sharing & Notifications (Collaboration)

**Goal**: Validate document sharing works; recipients get notifications; access control enforced.

**Test Steps**:

1. **Share Document with Another User**
   - Login as Camille Nicole (PM)
   - Open a document
   - Click "Share"
   - Search for and select "Ni Kang" (Employee)
   - Click "Share"
   - Expected: Success message: "Document shared with Ni Kang"

2. **Verify Share Appears in Recipient's List**
   - Logout Camille
   - Login as Ni Kang
   - Click "Documents" → "Shared with Me"
   - Expected: Document from Camille appears in list
   - Expected: Shows "Shared by: Camille Nicole"

3. **Verify Access Control**
   - Ni Kang downloads document → Expected: Success
   - Ni Kang cannot edit metadata (greyed out/forbidden button)
   - Ni Kang cannot delete document (greyed out/forbidden button)

4. **Verify Notification**
   - Ni Kang's inbox/notifications shows:
     - "Camille Nicole shared a document with you: [document title]"
   - Click notification → navigates to shared document

5. **Test Unshare**
   - Logout Ni Kang
   - Login as Camille
   - Open document → Click "Share"
   - Find Ni Kang's share → Click "Revoke"
   - Expected: "Share revoked"
   - Logout Camille, Login Ni Kang
   - Expected: Document no longer appears in "Shared with Me"

**Acceptance**: ✅ if sharing, notifications, and revocation work

**Related Requirements**: FR-032 through FR-034, FR-041

---

### Scenario 6: Task Integration (Feature Integration)

**Goal**: Validate documents can be attached to tasks and appear in task detail view.

**Test Steps**:

1. **Navigate to a Task**
   - Go to Tasks view
   - Select any task detail page

2. **Attach Existing Document**
   - Click "Attach Document"
   - Search for and select a document
   - Click "Attach"
   - Expected: Document appears in task's "Attached Documents" section

3. **Upload New Document from Task**
   - Click "Upload New Document" (from task page)
   - Upload a file with metadata
   - Select "Attach to this task"
   - Expected: Document immediately appears in task's attachments
   - Expected: Document automatically associated with task's project

4. **View Document from Task**
   - Click document in task's attachment list
   - Expected: Opens document preview/detail page
   - Expected: Shows task reference: "Attached to Task: [task title]"

5. **Verify Project Association**
   - Open document detail
   - Expected: Shows project association is auto-set to task's project

**Acceptance**: ✅ if task-document integration works

**Related Requirements**: FR-035 through FR-037

---

### Scenario 7: Accessibility Validation (WCAG 2.1 AA)

**Goal**: Validate keyboard navigation and screen reader compatibility.

**Test Steps**:

1. **Keyboard-Only Navigation**
   - Close mouse/trackpad or intentionally avoid using them
   - Navigate entire document upload flow:
     - Tab through form fields (Title, Description, Category, File selector)
     - Tab to Upload button
     - Press Enter to submit
     - Expected: All form controls reachable via Tab key
     - Expected: Focus indicators visible on each element

2. **Test with Screen Reader** (if available: NVDA on Windows)
   - Enable screen reader
   - Navigate to Documents page
   - Read document list
   - Expected: Screen reader announces:
     - "Document list, containing 5 items"
     - "Row 1: Q3 Report, Reports, 2 MB, uploaded 2026-08-14"
     - Etc.
   - Open document
   - Interact with buttons
   - Expected: All interactive elements announced correctly

3. **Color Contrast Check**
   - Use automated tool (axe DevTools, WAVE)
   - Expected: No color contrast violations (4.5:1 minimum for text)

4. **Zoom Test**
   - View document preview at 200% zoom
   - Expected: Content remains readable and functional
   - No horizontal scroll unless necessary

**Acceptance**: ✅ if keyboard navigation and screen reader support verified

**Related Requirements**: FR-059 through FR-074

---

### Scenario 8: GDPR Compliance - Data Subject Rights (Legal)

**Goal**: Validate data subject access request (DSAR) and erasure rights.

**Test Steps**:

1. **Data Subject Access Request**
   - Login as any employee
   - Go to Account Settings
   - Click "Download My Data"
   - Expected: System generates JSON/CSV file containing:
     - All documents uploaded by this user
     - All documents shared with this user
     - Metadata: title, size, upload date, shared dates
   - Expected: Download completes within 5 seconds
   - Expected: File is machine-readable (valid JSON/CSV)

2. **Request Deletion**
   - From Account Settings, click "Request Data Deletion"
   - Confirm: "This will permanently delete all your documents after 30 days"
   - Click "Request Deletion"
   - Expected: Message: "Your deletion request has been submitted. Documents will be permanently deleted in 30 days"

3. **Verify Admin Notification**
   - Logout, login as Administrator
   - Go to Admin → Data Subject Requests
   - Expected: Shows pending deletion request for the employee
   - Expected: Shows countdown "expires in 29 days, 23 hours"

4. **Admin Approval/Cancellation**
   - Admin can approve (execute immediately) or cancel request
   - If approved: Expected all employee documents soft-deleted, countdown to purge starts

5. **Verify Privacy Notice**
   - Login as any user
   - Attempt to upload document
   - Expected: Privacy notice displays:
     - "We collect: document title, description, category, tags"
     - "We retain: documents for 30 days after deletion"
     - "Your rights: download data, request deletion, access history"

**Acceptance**: ✅ if DSAR and erasure rights work correctly

**Related Requirements**: FR-071 through FR-086

---

### Scenario 9: Concurrent Uploads (Upload Queue)

**Goal**: Validate sequential upload queuing; multiple files process one at a time.

**Test Steps**:

1. **Select Multiple Files**
   - Click "Upload Document"
   - Hold Ctrl and select 5 files (various sizes: 1MB, 5MB, 15MB, 20MB, 24MB)
   - Fill metadata once (will apply to all)
   - Click "Upload All"

2. **Verify Queue Visualization**
   - Expected: Upload panel shows:
     - File 1: [████████░░] 80% - In Progress
     - File 2: [░░░░░░░░░░] 0% - Queued (Position 2/4)
     - File 3: [░░░░░░░░░░] 0% - Queued (Position 3/4)
     - Etc.

3. **Verify Sequential Processing**
   - Expected: Only one file uploading at a time (not parallel)
   - Expected: As each completes, next starts
   - Expected: No file starts uploading until previous finishes

4. **Test Cancel Queued Item**
   - While File 2 is queued, click cancel icon next to it
   - Expected: File 2 removed from queue
   - Expected: Files 3, 4 move up (Position 2, 3)
   - Expected: Audit log shows cancellation

5. **Verify All Complete**
   - Expected: Total time ≈ sum of individual times (not parallel)
   - Expected: All documents appear in list
   - Expected: Each has correct metadata and file size

**Acceptance**: ✅ if sequential queuing works correctly

**Related Requirements**: FR-002 through FR-004

---

## Testing Checklist

Complete this checklist to sign off on feature validation:

- [ ] Scenario 1: Basic Upload - ✅ PASSED
- [ ] Scenario 2: Storage Quota - ✅ PASSED
- [ ] Scenario 3: Search & Filtering - ✅ PASSED
- [ ] Scenario 4: Delete & Recovery - ✅ PASSED
- [ ] Scenario 5: Sharing & Notifications - ✅ PASSED
- [ ] Scenario 6: Task Integration - ✅ PASSED
- [ ] Scenario 7: Accessibility - ✅ PASSED
- [ ] Scenario 8: GDPR Compliance - ✅ PASSED
- [ ] Scenario 9: Concurrent Uploads - ✅ PASSED

## Performance Targets (Measured)

| Operation | Target | Actual | Status |
|-----------|--------|--------|--------|
| Upload 25 MB file | < 30 sec | ___ | [ ] |
| List 500 documents | < 2 sec | ___ | [ ] |
| Search across 500 docs | < 2 sec | ___ | [ ] |
| Document preview load | < 3 sec | ___ | [ ] |

## Known Limitations

- Virus scanning uses mock implementation; production requires ClamAV or commercial service
- Concurrent uploads queue locally; no persistence across browser restart
- Dashboard widget shows last 5 docs; large datasets not stress-tested
- Localization infrastructure ready but not translated (English only for MVP)

## Troubleshooting

**Upload button disabled**: Verify file selected and title entered
**Quota error**: Check user's used bytes; may need to delete old documents or contact admin
**Search slow**: Ensure database indexes created by migrations (DocumentId, UploadDate, Category)
**Share not working**: Verify recipient user exists and is not the document owner

## Next Steps

After validation passes:
1. Run full xUnit test suite: `dotnet test`
2. Run accessibility automated tools: axe DevTools, WAVE
3. Prepare for feature release and user training
