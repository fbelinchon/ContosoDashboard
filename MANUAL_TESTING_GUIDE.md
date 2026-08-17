# MVP Manual Testing Guide

**Document**: Testing procedures for Document Upload/Download/Quota/Authorization features  
**Date**: 2026-08-14  
**Status**: Ready for T038+ (API Controller) implementation  

---

## Part 1: Service Layer Testing (Current - Can Test Now)

### 1.1 Test Database & Models

**Objective**: Verify database schema created correctly

```powershell
# Navigate to project
cd ContosoDashboard

# Check database
dotnet ef database info --context ApplicationDbContext

# Expected output: Shows 2 migrations applied, tables visible
```

**Verification Checklist**:
- ✅ 5 document-related tables exist
- ✅ 21 indexes created
- ✅ Foreign keys properly configured
- ✅ Seed data loaded (4 users, 1 project)

---

### 1.2 Test File Storage Service

**Setup**: Create unit test file

```csharp
// Tests/Unit/FileStorageServiceTests.cs
using Xunit;
using System.IO;
using System.Threading.Tasks;
using ContosoDashboard.Services;
using Microsoft.Extensions.Logging;
using Moq;

public class LocalFileStorageServiceTests
{
    private LocalFileStorageService _service;
    
    public LocalFileStorageServiceTests()
    {
        var mockLogger = new Mock<ILogger<LocalFileStorageService>>();
        _service = new LocalFileStorageService(mockLogger.Object);
    }

    [Fact]
    public async Task SaveFile_CreatesGuidPath()
    {
        // Arrange
        var fileContent = "Test document content";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        
        // Act
        var filePath = await _service.SaveFileAsync(stream, "test.txt", userId: 1, projectId: 1);
        
        // Assert
        Assert.NotNull(filePath);
        Assert.Contains("AppData/uploads", filePath);
        Assert.Contains("1/1/", filePath); // User 1, Project 1
        Assert.EndsWith(".txt", filePath);
    }

    [Fact]
    public async Task FileExists_ReturnsTrueAfterSave()
    {
        // Arrange
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content"));
        var filePath = await _service.SaveFileAsync(stream, "test.txt", 1);
        
        // Act
        var exists = await _service.FileExistsAsync(filePath);
        
        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task GetFile_ReturnsStreamAfterSave()
    {
        // Arrange
        var originalContent = "Test document content";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(originalContent));
        var filePath = await _service.SaveFileAsync(stream, "test.txt", 1);
        
        // Act
        var retrievedStream = await _service.GetFileAsync(filePath);
        
        // Assert
        Assert.NotNull(retrievedStream);
        using (var reader = new StreamReader(retrievedStream))
        {
            var content = await reader.ReadToEndAsync();
            Assert.Equal(originalContent, content);
        }
    }

    [Fact]
    public async Task DeleteFile_SoftDeleteToRecoveryDir()
    {
        // Arrange
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content"));
        var filePath = await _service.SaveFileAsync(stream, "test.txt", 1);
        
        // Act
        var deleted = await _service.DeleteFileAsync(filePath);
        var stillExists = await _service.FileExistsAsync(filePath);
        
        // Assert
        Assert.True(deleted);
        Assert.False(stillExists); // Not in original location
    }

    [Fact]
    public async Task GetFileSize_ReturnsCorrectBytes()
    {
        // Arrange
        var content = "Test content with 26 bytes";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        var filePath = await _service.SaveFileAsync(stream, "test.txt", 1);
        
        // Act
        var size = await _service.GetFileSizeAsync(filePath);
        
        // Assert
        Assert.Equal(26L, size);
    }
}
```

**Run Tests**:
```powershell
dotnet test --filter "LocalFileStorageServiceTests"
```

**Expected Results**: All 5 tests pass ✅

---

### 1.3 Test Authorization Service

**Setup**: Create authorization tests

```csharp
// Tests/Unit/DocumentAuthorizationServiceTests.cs
using Xunit;
using System.Threading.Tasks;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.Extensions.Logging;
using Moq;

public class DocumentAuthorizationServiceTests
{
    private ApplicationDbContext _context;
    private DocumentAuthorizationService _service;
    
    public DocumentAuthorizationServiceTests()
    {
        // Setup in-memory database
        _context = new ApplicationDbContext(
            new Microsoft.EntityFrameworkCore.DbContextOptions<ApplicationDbContext>());
        var mockLogger = new Mock<ILogger<DocumentAuthorizationService>>();
        _service = new DocumentAuthorizationService(_context, mockLogger.Object);
    }

    [Fact]
    public async Task CanViewDocument_OwnerCanAlways()
    {
        // Arrange
        var userId = 1;
        var documentId = 1; // Document uploaded by userId 1
        
        // Act
        var canView = await _service.CanViewDocumentAsync(documentId, userId);
        
        // Assert
        Assert.True(canView);
    }

    [Fact]
    public async Task CanViewDocument_NonOwnerBlocked()
    {
        // Arrange
        var documentOwnerId = 1;
        var attemptingUserId = 2;
        var documentId = 1;
        
        // Act
        var canView = await _service.CanViewDocumentAsync(documentId, attemptingUserId);
        
        // Assert
        Assert.False(canView); // No share, not admin
    }

    [Fact]
    public async Task CanEditDocument_OnlyOwner()
    {
        // Arrange
        var ownerId = 1;
        var attemptingUserId = 2;
        var documentId = 1;
        
        // Act
        var canEdit1 = await _service.CanEditDocumentAsync(documentId, ownerId);
        var canEdit2 = await _service.CanEditDocumentAsync(documentId, attemptingUserId);
        
        // Assert
        Assert.True(canEdit1); // Owner
        Assert.False(canEdit2); // Non-owner
    }

    [Fact]
    public async Task IsAdministrator_ChecksRole()
    {
        // Arrange
        var adminUserId = 1; // Seed data admin
        var regularUserId = 4;
        
        // Act
        var isAdmin1 = await _service.IsAdministratorAsync(adminUserId);
        var isAdmin2 = await _service.IsAdministratorAsync(regularUserId);
        
        // Assert
        Assert.True(isAdmin1);
        Assert.False(isAdmin2);
    }
}
```

**Run Tests**:
```powershell
dotnet test --filter "DocumentAuthorizationServiceTests"
```

---

### 1.4 Test Quota Service

**Setup**: Create quota tests

```csharp
// Tests/Unit/QuotaServiceTests.cs
using Xunit;
using System.Threading.Tasks;
using ContosoDashboard.Data;
using ContosoDashboard.Services;
using Microsoft.Extensions.Logging;
using Moq;

public class QuotaServiceTests
{
    private ApplicationDbContext _context;
    private QuotaService _service;
    
    private const long GB_5 = 5_368_709_120;
    private const long MB_100 = 104_857_600;
    
    public QuotaServiceTests()
    {
        _context = new ApplicationDbContext(
            new Microsoft.EntityFrameworkCore.DbContextOptions<ApplicationDbContext>());
        var mockLogger = new Mock<ILogger<QuotaService>>();
        _service = new QuotaService(_context, mockLogger.Object);
    }

    [Fact]
    public async Task CanUpload_AllowsUnderQuota()
    {
        // Arrange
        var userId = 1;
        var fileSize = MB_100; // 100 MB < 5 GB
        
        // Act
        var canUpload = await _service.CanUploadAsync(userId, fileSize);
        
        // Assert
        Assert.True(canUpload);
    }

    [Fact]
    public async Task CanUpload_BlocksOverQuota()
    {
        // Arrange
        var userId = 2;
        var fileSize = GB_5 + 1; // 1 byte over limit
        
        // Act
        var canUpload = await _service.CanUploadAsync(userId, fileSize);
        
        // Assert
        Assert.False(canUpload);
    }

    [Fact]
    public async Task GetQuotaPercentage_CalculatesCorrectly()
    {
        // Arrange
        var userId = 1;
        await _service.AddUsageAsync(userId, GB_5 / 2); // 50% usage
        
        // Act
        var percentage = await _service.GetQuotaPercentageAsync(userId);
        
        // Assert
        Assert.Equal(50, percentage);
    }

    [Fact]
    public async Task AddUsage_UpdatesQuota()
    {
        // Arrange
        var userId = 1;
        var fileSize = MB_100;
        
        // Act
        await _service.AddUsageAsync(userId, fileSize);
        var (usedBytes, quotaBytes) = await _service.GetQuotaStatusAsync(userId);
        
        // Assert
        Assert.Equal(fileSize, usedBytes);
        Assert.Equal(GB_5, quotaBytes);
    }

    [Fact]
    public async Task RemoveUsage_ReducesQuota()
    {
        // Arrange
        var userId = 1;
        var fileSize = MB_100;
        await _service.AddUsageAsync(userId, fileSize);
        
        // Act
        await _service.RemoveUsageAsync(userId, fileSize);
        var (usedBytes, _) = await _service.GetQuotaStatusAsync(userId);
        
        // Assert
        Assert.Equal(0L, usedBytes);
    }

    [Fact]
    public async Task RecalculateQuota_SumsDocumentSizes()
    {
        // Arrange
        var userId = 1;
        // (In real test: create multiple documents with various sizes)
        
        // Act
        await _service.RecalculateQuotaAsync(userId);
        var (usedBytes, _) = await _service.GetQuotaStatusAsync(userId);
        
        // Assert
        Assert.True(usedBytes >= 0); // Should match document sum
    }
}
```

**Run Tests**:
```powershell
dotnet test --filter "QuotaServiceTests"
```

---

### 1.5 Test Audit Logging Service

**Setup**: Create audit tests

```csharp
// Tests/Unit/DocumentAuditServiceTests.cs
using Xunit;
using System;
using System.Threading.Tasks;
using ContosoDashboard.Data;
using ContosoDashboard.Services;
using Microsoft.Extensions.Logging;
using Moq;

public class DocumentAuditServiceTests
{
    private ApplicationDbContext _context;
    private DocumentAuditService _service;
    
    public DocumentAuditServiceTests()
    {
        _context = new ApplicationDbContext(
            new Microsoft.EntityFrameworkCore.DbContextOptions<ApplicationDbContext>());
        var mockLogger = new Mock<ILogger<DocumentAuditService>>();
        _service = new DocumentAuditService(_context, mockLogger.Object);
    }

    [Fact]
    public async Task LogOperation_CreatesAuditEntry()
    {
        // Arrange
        var userId = 1;
        var documentId = 1;
        var action = "Upload";
        var fileSize = 1024L;
        
        // Act
        await _service.LogOperationAsync(action, userId, documentId, fileSize, 
            "192.168.1.1", "Success", "File: test.pdf");
        
        // Assert - verify audit log was created
        var logs = await _service.GetDocumentAuditLogsAsync(documentId);
        Assert.NotEmpty(logs);
        Assert.Single(logs);
    }

    [Fact]
    public async Task GetDocumentAuditLogs_ReturnsAllActionsForDoc()
    {
        // Arrange
        var documentId = 1;
        
        // Act - log multiple actions
        await _service.LogOperationAsync("Upload", 1, documentId, 1024, "192.168.1.1", "Success");
        await _service.LogOperationAsync("Download", 1, documentId, 1024, "192.168.1.1", "Success");
        await _service.LogOperationAsync("Share", 1, documentId, 0, "192.168.1.1", "Success");
        
        // Assert
        var logs = await _service.GetDocumentAuditLogsAsync(documentId);
        Assert.Equal(3, logs.Length);
    }

    [Fact]
    public async Task GetUserAuditLogs_ReturnAllUserActions()
    {
        // Arrange
        var userId = 1;
        
        // Act - log actions by user
        await _service.LogOperationAsync("Upload", userId, 1, 1024, "192.168.1.1", "Success");
        await _service.LogOperationAsync("Download", userId, 2, 1024, "192.168.1.1", "Success");
        await _service.LogOperationAsync("Delete", userId, 3, 1024, "192.168.1.1", "Success");
        
        // Assert
        var logs = await _service.GetUserAuditLogsAsync(userId);
        Assert.Equal(3, logs.Length);
    }

    [Fact]
    public async Task LogOperation_TracksFailure()
    {
        // Arrange
        var userId = 1;
        var documentId = 1;
        
        // Act
        await _service.LogOperationAsync("Upload", userId, documentId, 0, 
            "192.168.1.1", "Failure", "File storage error");
        
        // Assert
        var logs = await _service.GetDocumentAuditLogsAsync(documentId);
        var failureLog = logs[0] as dynamic;
        Assert.Equal("Failure", failureLog.Result);
    }
}
```

**Run Tests**:
```powershell
dotnet test --filter "DocumentAuditServiceTests"
```

---

## Part 2: End-to-End Workflow Testing (Post-T038 API Controller)

### 2.1 Complete Upload-Download-Delete Workflow

**Objective**: Test entire lifecycle in sequence

**Steps**:

1. **Start Application**
```powershell
dotnet run
# Expected: App starts on https://localhost:5001
```

2. **Login** (Navigate to app)
```
URL: https://localhost:5001
- Go to Login page
- Use: admin@contoso.com / (password from auth state provider)
- Click Login
- Redirect to home page
```

3. **Navigate to Documents** (after T039 Blazor page)
```
Click: Documents (from nav menu)
Expected: 
- Empty list message
- "Upload Document" button
- Search box
- Category filter
```

4. **Upload Document Test 1: Valid File**
```
Click: Upload Document button
Form:
  Title: "Q4 Financial Report"
  Description: "Quarterly financial summary for Oct-Dec 2026"
  Category: "Reports"
  File: Select any PDF (< 500MB)
  Tags: "quarterly" "financial"
  
Click: Upload

Expected:
✅ Progress indicator shows
✅ Success message appears
✅ Document appears in list
✅ File stored in AppData/uploads/1/1/[GUID].pdf
✅ Database record created
✅ Audit log entry created: "Upload" → Success
```

5. **Upload Document Test 2: Oversized File**
```
Click: Upload Document button
File: Select file > 500MB

Expected:
✅ Upload blocked
✅ Error message: "File exceeds 500MB limit"
✅ Audit log entry: "Upload" → Blocked → "File exceeds limit"
```

6. **Upload Document Test 3: Quota Exceeded**
```
(After first legitimate upload, upload files totaling ~5GB+)

Expected:
✅ Last upload blocked
✅ Error message: "Quota exceeded"
✅ Remaining quota shows red warning
✅ Audit log: Multiple uploads, last one → Blocked
```

7. **Download Document**
```
Click: Download icon on uploaded document
Click: Confirm

Expected:
✅ File downloaded to default download folder
✅ Filename preserved (title + extension)
✅ MIME type correct (PDF opens as PDF, etc.)
✅ Audit log: "Download" → Success
```

8. **View Document Details**
```
Click: Document title/row

Expected page shows:
✅ Document metadata (title, category, tags)
✅ Upload date/time
✅ Uploader name (admin@contoso.com)
✅ File size
✅ Download button
✅ Delete button
✅ Share button
```

9. **Share Document**
```
On Document Detail:
Click: Share button
Select: "camille.nicole@contoso.com"
Click: Share

Expected:
✅ Share created
✅ Audit log: "Share" → Success
✅ Recipient count shown
```

10. **Delete (Soft-Delete) Document**
```
On Document Detail:
Click: Delete button
Click: Confirm deletion

Expected:
✅ Document removed from list (own view)
✅ Audit log: "Delete" → Success
✅ File NOT removed from disk (soft-delete)
✅ Remaining quota updated (+fileSize)
```

11. **Restore Document (Within 30 Days)**
```
(Same session, document just deleted)
Click: Show Deleted Documents (checkbox)
On deleted document:
Click: Restore button
Click: Confirm

Expected:
✅ Document reappears in list
✅ Audit log: "Restore" → Success
✅ Quota adjusted again (-fileSize)
```

12. **Restore Document (After 30 Days - Should Fail)**
```
(Simulate in test by manually setting DeletedDate in DB)
UPDATE Documents SET DeletedDate = DATEADD(day, -31, GETUTCDATE()) 
WHERE DocumentId = 1;

Click: Restore

Expected:
✅ Error message: "Recovery window (30 days) expired"
✅ Audit log: "Restore" → Blocked → "Recovery window expired"
```

---

### 2.2 Authorization Testing

**Test: Non-Owner Cannot Access**

```powershell
# In separate browser (or incognito window)
# Login as: floris.kregel@contoso.com (different user)

# Try to access document uploaded by admin
# Direct URL: /documents/detail/1
# Expected: 401 Unauthorized OR not in list
```

**Test: Share Recipient Can Access**

```
# Login as: ni.kang@contoso.com (recipient of share from T2.1 step 9)
# Navigate to Documents
Expected:
✅ Shared document appears in list
✅ Badge shows "Shared with me"
✅ Can download
✅ Cannot delete/edit (owner only)
```

**Test: Admin Can Override**

```
# Login as: admin@contoso.com
# Navigate to Documents
Expected:
✅ Can see own documents
✅ Can see shared documents
✅ (After T044: can see all documents as admin)
```

---

### 2.3 Quota Tracking UI

**Test: Quota Display**

```
On any Documents page:
Quota section should show:
- "Storage: 2.5 GB of 5.0 GB (50%)"
- Visual progress bar
- Remaining: 2.5 GB

If > 80% used:
- Progress bar turns yellow

If > 95% used:
- Progress bar turns red
- Warning message appears
```

**Test: Quota Enforcement**

```
(After uploading to near-limit)
Remaining: 100 MB
Try to upload 200 MB file

Expected:
✅ Upload blocked
✅ Error: "Insufficient quota. You have 100 MB remaining."
✅ Quota percentage recalculated
```

---

### 2.4 Search & Filter Testing

**Test: Category Filter**

```
Upload documents with different categories:
- "Reports"
- "Meeting Notes"
- "Invoices"

Click category filter: "Reports"
Expected:
✅ Only "Reports" documents shown
✅ Other categories hidden
✅ Count updates
```

**Test: Tag Filter**

```
Upload documents with tags:
- Document 1: "urgent" "client-review"
- Document 2: "archived" "complete"

Click filter by tag: "urgent"
Expected:
✅ Only Document 1 shown
✅ All others hidden
```

**Test: Search by Title/Description**

```
Search for: "Financial"

Expected:
✅ Documents with "Financial" in title shown
✅ Documents with "Financial" in description shown
✅ Case-insensitive match
✅ Partial matches work
```

---

### 2.5 Audit Trail Testing

**Test: View Audit Logs** (requires T044 admin page)

```
As admin: Navigate to Audit Logs
Filter by:
- Document ID: 1
- Date range: Last 24 hours
- Action: "Upload"

Expected:
✅ Logs sorted by date (newest first)
✅ Shows: Action, User, Timestamp, IP Address, Result, Details
✅ File sizes shown
✅ Success/Failure status clear
```

---

### 2.6 Database Verification

**Test: Check Schema**

```powershell
# Using SQL Server Management Studio or CLI
sqlcmd -S (localdb)\mssqllocaldb -d ContosoDashboard.db -Q "SELECT * FROM Documents;"

Expected output:
✅ Columns: DocumentId, Title, Description, Category, FilePath, FileSize, 
           MimeType, UploadDate, UploadedByUserId, ProjectId, IsDeleted, 
           DeletedDate, CreatedDate, ModifiedDate, RowVersion
✅ Data types correct (int, nvarchar, datetime2, bit, etc.)
✅ Soft-deleted documents have IsDeleted = 1
```

**Test: Check Audit Logs**

```powershell
sqlcmd -S (localdb)\mssqllocaldb -d ContosoDashboard.db -Q "SELECT * FROM DocumentAuditLogs ORDER BY Timestamp DESC LIMIT 10;"

Expected output:
✅ Recent operations logged
✅ Timestamp accurate (UTC)
✅ Action, UserId, DocumentId, Result visible
✅ IpAddress captured
✅ Details field populated
```

**Test: Check Quota**

```powershell
sqlcmd -S (localdb)\mssqllocaldb -d ContosoDashboard.db -Q "SELECT UserId, UsedBytes, QuotaBytes FROM UserStorageQuotas;"

Expected output:
✅ Each user has exactly 1 quota record
✅ UsedBytes matches sum of document FileSize WHERE IsDeleted = 0
✅ QuotaBytes = 5368709120 (5GB)
```

---

## Part 3: Negative Testing (Error Scenarios)

### 3.1 File Storage Errors

| Scenario | Action | Expected Result |
|----------|--------|-----------------|
| **Corrupted file** | Upload file with corrupt header | Error logged, audit "Failure", no DB record |
| **File disappears** | Delete file manually from disk, try download | Error "File not found", audit "Failure" |
| **Permission denied** | AppData folder permissions removed | Error creating directories, audit "Failure" |
| **Disk full** | Fill disk, try upload | Error "Disk full", audit "Blocked" |

### 3.2 Authorization Errors

| Scenario | Action | Expected Result |
|----------|--------|-----------------|
| **IDOR attack** | Try `/documents/{other-user-doc-id}` | 401 Unauthorized, silent block |
| **Share revoked** | Revoke share, share recipient tries access | Cannot see document, audit "Blocked" |
| **Token expired** | Let session expire, try operation | Redirect to login |
| **Role downgrade** | Admin demoted to Employee in DB | Can no longer access admin functions |

### 3.3 Quota Errors

| Scenario | Action | Expected Result |
|----------|--------|-----------------|
| **Exact limit** | Upload file that fills quota exactly | Success at 100%, next upload blocked |
| **Concurrent uploads** | Upload 2 files simultaneously from UI | One succeeds, one blocked for quota |
| **Delete-retry** | Delete file, immediately try restore | Works (quota recalculated) |
| **Quota recalculation** | Manually add documents to DB, refresh | Quota updates correctly |

---

## Part 4: Performance Testing

### 4.1 Load Test: Large File Upload

```
Test: Upload 400MB file (near 500MB limit)

Expected:
✅ Completes in < 30 seconds
✅ Progress indicator updates smoothly
✅ No timeout errors
✅ File stored correctly
✅ Quota updated
```

### 4.2 Load Test: Large Document List

```
Test: List 1000 documents with pagination

Expected:
✅ Page loads in < 2 seconds
✅ 20 per page displays
✅ Pagination controls work
✅ Search/filter still responsive
```

### 4.3 Load Test: Quota Recalculation

```
Test: User with 500 documents, trigger quota recalc

Expected:
✅ Completes in < 5 seconds
✅ Sum calculation accurate
✅ No database locks
✅ Concurrent reads not blocked
```

---

## Part 5: Browser Developer Tools Testing

### 5.1 Network Tab

Open Browser DevTools (F12) → Network Tab

```
Test: Upload document

Expected HTTP Requests:
✅ POST /api/documents/upload (201 Created)
✅ Payload includes file, metadata, tags
✅ Response includes DocumentId
✅ No 4xx/5xx errors
✅ Response time < 5 seconds
```

### 5.2 Application Tab

DevTools → Application Tab

```
Expected:
✅ Cookies include auth token
✅ LocalStorage (if used) clean
✅ No sensitive data in storage
✅ Session cookie HttpOnly flag set
```

### 5.3 Console Tab

DevTools → Console

```
Expected:
✅ No JavaScript errors
✅ No CORS warnings (same origin)
✅ No 401 auth errors
✅ No deprecation warnings
```

---

## Part 6: Security Checklist

### 6.1 IDOR Prevention

```
✅ Cannot access other user's documents
✅ Cannot edit other user's documents
✅ Cannot delete other user's documents
✅ Cannot revoke others' shares
✅ Directory traversal prevented (GUID paths)
✅ Path traversal validation on all file operations
```

### 6.2 Injection Prevention

```
✅ SQL injection: EF Core parameterized queries
✅ XSS: Razor template encoding
✅ CSRF: Anti-forgery tokens on forms
✅ File name injection: GUID naming scheme
```

### 6.3 Data Protection

```
✅ Files stored outside wwwroot
✅ Soft-delete prevents immediate loss
✅ Audit logs immutable (no cascade delete)
✅ GDPR compliance: DSAR queries work
✅ No sensitive data in logs
```

---

## Testing Summary Checklist

### Phase 2B Testing (After T038 API Controller)

```
Test Category                Status  Notes
─────────────────────────────────────────────
File Upload                  [ ]     Valid file, oversized, quota exceeded
File Download                [ ]     Success, 404, 403
Authorization                [ ]     Owner, share recipient, non-owner
Quota Enforcement            [ ]     Below limit, at limit, over limit
Audit Logging                [ ]     All operations logged
Soft-Delete/Restore          [ ]     Within 30 days, after 30 days
Search/Filter                [ ]     Category, tag, title, description
Database Integrity           [ ]     Schema, relationships, constraints
Error Handling               [ ]     Graceful failures, logging
Performance                  [ ]     Upload speed, list load, recalc time
Security                     [ ]     IDOR, injection, path traversal
GDPR Compliance              [ ]     DSAR, audit trail, soft-delete
```

---

## Manual Testing Execution Order

**Recommended sequence** (post-T038):

1. Service layer unit tests (1-2 hours)
2. Basic upload/download workflow (30 mins)
3. Authorization tests (20 mins)
4. Quota tests (20 mins)
5. Error scenarios (30 mins)
6. Performance tests (30 mins)
7. Security verification (30 mins)
8. GDPR compliance checks (20 mins)

**Total estimated manual testing time**: 4-5 hours

---

## Regression Testing (After Each Change)

**When to re-test**:
- After updating authorization logic
- After modifying file storage
- After changing quota calculations
- After updating audit logging
- Before releasing to production

**Quick regression test suite** (15 minutes):
```
1. Upload valid file                      (2 min)
2. Download file                          (2 min)
3. Soft-delete & restore                  (2 min)
4. Share document                         (2 min)
5. Check audit logs                       (2 min)
6. Verify quota updated                   (2 min)
7. Confirm no console errors              (1 min)
```

---

**Status**: Ready for execution after T038-T041 API/Pages implementation  
**Next**: Execute tests immediately after `dotnet run` succeeds
