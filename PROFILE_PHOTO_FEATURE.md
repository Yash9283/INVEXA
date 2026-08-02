# Profile Photo Feature

## Overview
Users can now upload, view, and delete their profile photos.

## Features Added

### 1. Database Changes
- Added `ProfilePhoto` column to `Admins` table (nullable string, max 255 characters)
- Migration file: `20260731120000_AddProfilePhotoToAdmin.cs`

### 2. Profile Page Updates
**Location:** `Views/Account/Profile.cshtml`

- Display uploaded photo (circular, 120x120px) or default icon
- Upload form with file input (accepts image files only)
- Delete photo button (only shown if photo exists)
- Success/error messages display

### 3. Controller Actions
**Location:** `Controllers/AccountController.cs`

#### UploadProfilePhoto (POST)
- Validates file type (JPG, JPEG, PNG, GIF only)
- Validates file size (max 5MB)
- Generates unique filename: `{userId}_{guid}.{extension}`
- Deletes old photo if exists
- Saves to `wwwroot/images/profiles/`
- Updates database

#### DeleteProfilePhoto (POST)
- Removes physical file from server
- Sets ProfilePhoto to null in database

### 4. Users Table Display
**Location:** `Views/Account/Users.cshtml`

- Shows profile photo in Users table if uploaded
- Falls back to letter avatar if no photo
- Photo displayed as 44x44px rounded image

## File Structure
```
wwwroot/
  └── images/
      └── profiles/          # Profile photos stored here
          └── {userId}_{guid}.{ext}
```

## Usage

### Upload Photo:
1. Go to Profile page (`/Account/Profile`)
2. Click "Choose File" under Profile Photo section
3. Select an image (JPG, PNG, GIF, max 5MB)
4. Click "Upload Photo"

### Delete Photo:
1. Go to Profile page
2. Click "Remove Photo" button
3. Confirm deletion

## Validation Rules
- **Allowed formats:** JPG, JPEG, PNG, GIF
- **Max file size:** 5MB
- **Storage location:** `wwwroot/images/profiles/`
- **Filename format:** `{userId}_{guid}.{extension}`

## Security Considerations
- Only authenticated users can upload/delete photos
- Users can only manage their own photos
- File type validation prevents non-image uploads
- File size limit prevents DOS attacks
- Unique filenames prevent overwrites

## Database Migration
Run the application - migration will apply automatically on startup.

Or manually run:
```bash
dotnet ef database update
```

## Notes
- Old photos are automatically deleted when new ones are uploaded
- Photos are physically deleted from disk when removed
- Default letter avatar shown when no photo exists
- Photos display in both Profile page and Users management table
