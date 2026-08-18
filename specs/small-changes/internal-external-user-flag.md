# Small Change: Internal/External User Flag

**Date**: 2026-08-18  
**Status**: completed  
**Complexity**: small

## What

Add a boolean property `IsInternalUser` to the User profile that indicates whether the user is an internal employee or an external user. Display as an editable checkbox in the Profile page (admins only) and as a read-only badge in Team member cards (visible to all). Enforce that external users cannot be Administrators.

## Context

| File | Role |
|------|------|
| `ContosoDashboard/Models/User.cs` | Will be modified — add `IsInternalUser` property |
| `ContosoDashboard/Pages/Profile.razor` | Will be modified — add checkbox for internal/external flag |
| `ContosoDashboard/Pages/Team.razor` | Will be modified — display internal/external badge on team member cards |
| `ContosoDashboard/Migrations/` | Will create new migration for database schema |
| `ContosoDashboard/Data/ApplicationDbContext.cs` | Context — provides database access |
| `ContosoDashboard/Services/UserService.cs` | Context — handles user data persistence |

## Requirements

1. Add `IsInternalUser` boolean property to the User model with default value `true` (all existing users = internal)
2. Display as editable checkbox on Profile page **only for Administrator users** (read-only badge on own profile if not admin)
3. Create database migration to add the new column to the Users table
4. Display internal/external status as a color-coded badge on Team member cards (visible to all users)
5. Prevent external users from having the Administrator role (validation logic)
6. The checkbox should update when the user saves their profile changes
7. The field should be persisted to the database

## Plan

1. Add `IsInternalUser` property to `User.cs` model with `[Required]` and default value `true`
2. Create a new EF Core migration using `dotnet ef migrations add AddIsInternalUserField`
3. Update `Profile.razor`: 
   - Show checkbox only if current user is Administrator
   - Show read-only badge if not Administrator
4. Update `Team.razor`: Display green badge "Internal" or orange badge "External" for all team members
5. Add validation in `UserService` to prevent setting role to Administrator if `IsInternalUser == false`
6. Test that checkbox is only editable for admins and cannot assign external users as admins

## Tasks

- [x] Add `IsInternalUser` boolean property to `User.cs` model (default `true`)
- [x] Add validation to prevent external users from being Administrators
- [x] Generate database migration `AddIsInternalUserField`
- [x] Update `Profile.razor`: show editable checkbox for Admins, read-only badge for others
- [x] Add internal/external badge to team member cards in `Team.razor` (green=internal, orange=external)
- [x] Test: Admin can edit internal/external flag and see changes persist
- [x] Test: Non-admin users see read-only badge in profile
- [x] Test: Verify cannot assign external user as Administrator
- [x] Verify no compilation errors

## Done When

- [ ] All tasks checked off
- [ ] Administrators can toggle internal/external flag in any user's profile
- [ ] Changes persist after page refresh
- [ ] Non-admins see read-only badge
- [ ] Team cards display colored badges (internal=green, external=orange)
- [ ] Cannot assign external users as Administrators
- [ ] No compilation errors
- [ ] No database schema conflicts
