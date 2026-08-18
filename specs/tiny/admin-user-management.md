# Small Change: Admin User Management Panel

**Date**: 2026-08-18  
**Status**: ✅ fully completed  
**Complexity**: small

## What

Create an admin-only Users management page (`/admin/users`) where administrators can select any user from a simple dropdown (name + role) and view/edit only the `IsInternalUser` checkbox. All other profile data displays as read-only. If admin attempts to mark an Administrator as external, show error message and block the change. Audit logging enabled.

## Context

| File | Role |
|------|------|
| `ContosoDashboard/Pages/Admin/Users.razor` | Will be created — main admin users page with dropdown and form |
| `ContosoDashboard/Services/UserService.cs` | Will be modified — add authorization check and method to update IsInternalUser for other users |
| `ContosoDashboard/Shared/NavMenu.razor` | Will be modified — add "Users" link to Admin menu (visible only to Administrators) |
| `ContosoDashboard/Pages/Admin/_Imports.razor` | Will be created — imports for Admin pages |

## Requirements

1. Create `/admin/users` page accessible only to Administrators (Authorize attribute)
2. Display simple dropdown with all users (name + role format), default to current user
3. Show selected user's profile data in read-only fields: DisplayName, Email, Department, JobTitle, PhoneNumber, Role, AvailabilityStatus
4. Only `IsInternalUser` checkbox is editable (no label, just checkbox)
5. Save button only enabled when form is dirty (user made changes)
6. Show success message "Usuario actualizado correctamente" on successful save
7. Prevent marking Administrators as external: show error message "No es posible indicar un administrador como externo"
8. Log admin changes to IsInternalUser (audit trail)
9. Add "Users" menu item to navbar (visible only to Admins)

## Plan

1. Create `Pages/Admin/Users.razor` with Authorize=Administrator
2. Add dropdown to select users (DisplayName + Role), default to current user
3. Bind selected user's data to read-only fields
4. Add `IsInternalUser` checkbox (edit only)
5. Track dirty state to enable/disable Save button
6. Add admin-only method to UserService: `UpdateUserIsInternalAsync()` with validation and audit logging
7. Show error if trying to mark admin as external
8. Show success message after save
9. Update `NavMenu.razor` with "Users" admin link

## Tasks

- [x] Create `Pages/Admin/Users.razor` with Authorize=Administrator
- [x] Add dropdown with all users (DisplayName + Role)
- [x] Bind read-only profile fields
- [x] Add IsInternalUser checkbox (edit only)
- [x] Implement dirty form tracking
- [x] Add UserService method: UpdateUserIsInternalAsync() with validation + audit log
- [x] Show error if marking admin as external
- [x] Show success message: "Usuario actualizado correctamente"
- [x] Update NavMenu.razor with Users link
- [x] Test: Save button only enabled when dirty
- [x] Test: Cannot mark admin as external (shows error)
- [x] Verify no compilation errors

## Done When

- [x] All tasks checked off
- [x] Admins can access `/admin/users`
- [x] Dropdown shows all users (name + role format)
- [x] Save button only enabled when IsInternalUser changed
- [x] Can toggle checkbox and save successfully
- [x] Success message displays: "Usuario actualizado correctamente"
- [x] Error message shown if marking admin as external
- [x] Non-admins cannot access the page (403) — enforced with `[Authorize(Roles = "Administrator")]`
- [x] Admin changes logged in audit trail (UserAuditLog table)
- [x] No compilation errors
- [x] Authorization hardened: `[Authorize(Roles = "Administrator")]` instead of generic `[Authorize]`
- [x] UserAuditLog model created for audit trail tracking
- [x] Migration created and applied to database
