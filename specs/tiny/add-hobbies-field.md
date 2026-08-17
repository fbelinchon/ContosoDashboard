# TinySpec: User Hobbies Field

**Branch**: `feature/user-hobbies`  
**Date**: 2026-08-17  
**Status**: ✅ complete  
**Complexity**: small

## What

Add a "Hobbies" text field to the user profile that allows employees to enter and update their personal interests and hobbies. This field appears on the Profile page alongside other user profile information (Department, Job Title, etc.) and helps build a more complete employee profile for team member discovery.

## Context

| File | Role |
|------|------|
| `Models/User.cs` | Will be modified — add `Hobbies` property |
| `Data/ApplicationDbContext.cs` | Context — DbSet already exists for Users |
| `Pages/Profile.razor` | Will be modified — add hobbies input field in form |
| `Services/UserService.cs` | Context — used to save/load user data |
| `Data/Migrations/[timestamp]_AddUserHobbiesField.cs` | Will be created — EF Core migration |

## Requirements

1. Add a `Hobbies` field to the `User` model with a maximum length of 500 characters
2. Display the hobbies field in the Profile page form with a textarea input
3. Allow users to edit and save their hobbies through the Profile form
4. Create an EF Core migration to add the database column
5. Hobbies field should be optional (nullable)

## Plan

1. Add `Hobbies` string property to `Models/User.cs` with `[MaxLength(500)]` attribute
2. Create EF Core migration: `dotnet ef migrations add AddUserHobbiesField`
3. Add textarea input for hobbies in `Pages/Profile.razor` after Job Title field
4. Update the migration context in `Data/ApplicationDbContext.cs` (if needed)
5. Test the form submission and data persistence
6. Apply migration: `dotnet ef database update`

## Tasks

- [x] Add `Hobbies` property to User.cs model
- [x] Create EF Core migration for new database column
- [x] Add hobbies textarea field to Profile.razor form
- [x] Add character counter/display to hobbies field
- [x] Test profile save with hobbies data
- [x] Verify database column created successfully
- [x] Run build to confirm no errors

## Done When

- [x] All tasks checked off
- [x] Build succeeds with 0 errors
- [x] Profile page displays hobbies field
- [x] User can save and retrieve hobbies
- [x] No validation errors on form submission
- [x] Database migration applies cleanly

---

## Implementation Notes

- Hobbies field placed after Job Title in the form to maintain logical grouping
- Use textarea (not text input) to allow multi-line hobbies list
- Character limit: 500 chars (allows for ~3-5 hobbies with descriptions)
- No required validation — hobbies are completely optional
- Consider adding placeholder text: "e.g., Photography, Hiking, Cooking"
