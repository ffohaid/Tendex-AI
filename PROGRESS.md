# Tendex AI - Progress Tracker

## Last Updated: 2026-04-06

## Completed Tasks

### Task: Rich Text Editor Integration & Inquiry System Enhancement
**Date:** 2026-04-05 to 2026-04-06
**Status:** ✅ Completed

#### Changes Made:

1. **TipTap Rich Text Editor Component**
   - Created reusable `RichTextEditor.vue` component with full toolbar
   - Supports: text formatting, headings, lists, alignment, tables, links, images, colors, blockquotes, code blocks
   - Full RTL support for Arabic language
   - Integrated in: BookletEditorView (13 sections), Step3Content (new booklet), InquiriesView (responses)

2. **Inquiry System Enhancement**
   - Added direct reply option using rich text editor
   - Improved assignment mechanism (committee or specific person with dynamic search)
   - Dynamic user search by name or email
   - Two modes: "Manual Reply" and "AI-Generated Reply"
   - Ability to edit AI-generated reply in rich text editor
   - Buttons: "Send for Approval" and "Direct Approval" (based on permissions)

3. **Login Issue Fix**
   - Reset password for ahmed@mof.gov.sa to: Admin@12345
   - Unlocked the account

4. **Task Center Navigation Fix**
   - Modified GetPendingTasksQueryHandler.cs to return correct action URLs
   - Improved navigateToTask in TaskCenterView.vue
   - Clicking "Authority Approval" task now navigates to /approvals instead of dashboard

5. **Approvals Page Translation Fix**
   - Added getActionRequiredLabel and getTypeLabel functions in ApprovalsView.vue
   - Added missing translation keys in ar.json and en.json
   - Fixed: "waiting_for_prior_step" → "بانتظار الخطوة السابقة"
   - Fixed: "approval" → "اعتماد"
   - Fixed: Review button now correctly navigates to RFP edit page

#### Files Modified:
- `frontend/src/components/RichTextEditor.vue` (NEW)
- `frontend/src/views/InquiriesView.vue`
- `frontend/src/views/approvals/ApprovalsView.vue`
- `frontend/src/views/TaskCenterView.vue`
- `frontend/src/views/rfp/BookletEditorView.vue`
- `frontend/src/views/rfp/steps/Step3Content.vue`
- `frontend/src/locales/ar.json`
- `frontend/src/locales/en.json`
- `backend/src/Application/Tasks/Queries/GetPendingTasks/GetPendingTasksQueryHandler.cs`

#### TipTap Dependencies Added:
- @tiptap/vue-3, @tiptap/starter-kit, @tiptap/extension-text-align
- @tiptap/extension-table, @tiptap/extension-color, @tiptap/extension-text-style
- @tiptap/extension-highlight, @tiptap/extension-link, @tiptap/extension-image
- @tiptap/extension-task-list, @tiptap/extension-task-item

---

## Next Steps
- Update project documentation (PRD, Architecture, Operational Guide) to reflect new features
- Continue with remaining platform development tasks

## 2026-04-08: Fix Role Permissions (Critical Bug Fix)

### Problem
- Procurement Manager role had only 5 permissions (competitions only) in RolePermissions table
- Committee Chair, Committee Member, Financial Controller, Sector Representative had 0 permissions
- This caused "Access Denied" when users tried to create RFPs or access other features
- The UI showed 40 permissions in the role details (from PermissionMatrixRules) but the actual JWT token only loaded from RolePermissions

### Root Cause
- RolePermissions table was not properly seeded during tenant provisioning
- Only Tenant Primary Admin (95 perms), Member (30 perms), and Viewer (13 perms) had adequate permissions

### Fix Applied
- Created comprehensive SQL script (infrastructure/sql/fix_role_permissions.sql)
- Assigned proper permissions to all 7 roles across all 5 tenant databases:
  - Procurement Manager: 62 permissions
  - Committee Chair: 40 permissions
  - Sector Representative: 33 permissions
  - Member: 28 permissions
  - Financial Controller: 27 permissions
  - Committee Member: 25 permissions
  - Viewer: 16 permissions
  - Tenant Primary Admin: 95 permissions (unchanged)
- Cleared Redis cache to force re-evaluation

### Verification
- Tested login as khalid@mof.gov.sa (Procurement Manager)
- Successfully accessed RFP creation page (previously showed Access Denied)
- All menu items visible and accessible

## 2026-04-09: Font Change - IBMPlexSansArabic (Final Fix)
- Changed platform font from "Diodrum Arabic" to "IBMPlexSansArabic, sans-serif"
- Used official IBM woff2 font files (self-hosted, 8 weights: Thin, ExtraLight, Light, Regular, Text, Medium, SemiBold, Bold)
- Added @font-face declarations in main.css with font-family: IBMPlexSansArabic (no spaces, no quotes)
- Updated --font-sans CSS variable to: IBMPlexSansArabic, sans-serif
- Rebuilt Docker container from scratch with --no-cache
- Verified font loading and rendering in browser (weights 400, 500, 600, 700 loaded)

## 2026-04-09: Font Change to Cairo (Google Fonts CDN)

### Changes Made:
- **frontend/index.html**: Added Google Fonts CDN preconnect and stylesheet links for Cairo font (weights: 300, 400, 500, 600, 700)
- **frontend/src/assets/css/main.css**: Updated `--font-sans` from `IBMPlexSansArabic` to `"Cairo", sans-serif`. Removed all `@font-face` declarations for IBMPlexSansArabic.
- **Removed**: All 8 self-hosted IBMPlexSansArabic woff2 files from `frontend/public/fonts/`
- **Docker**: Rebuilt and redeployed frontend container with new font configuration
- **Commits**: `b1a02e3` (font change) and `797554d` (cleanup old font files)

### Technical Approach:
- Cairo font is loaded via Google Fonts CDN (no self-hosted files needed)
- Tailwind CSS v4 `@theme` block updated with `--font-sans: "Cairo", sans-serif`
- Font supports both Arabic and Latin characters with weights 300-700

## 2026-04-16: New Tendex AI Logo Integration

### Changes Made:

1. **Favicon Update**
   - Replaced default favicon with Tendex green icon (Tendexicon-01.svg)
   - Green diamond-shaped icon with circuit pattern visible in browser tabs

2. **Logo Files Added** (`frontend/public/logos/`)
   - 3 icon variants: Tendexicon-01 (green), Tendexicon-02 (white), Tendexicon-03 (navy)
   - 4 typography variants: Tendexnewtypograpgy-01 (horizontal light bg), -02 (horizontal dark bg), -06 (vertical light bg), -07 (vertical dark bg)

3. **AppHeader.vue** (Main Navigation Header)
   - Replaced `pi pi-bolt` icon with Tendex logo image (`/logos/Tendexicon-01.svg`)
   - Logo appears when no tenant branding is loaded (default fallback)

4. **LoginView.vue** (Authentication Screen)
   - Replaced generic icon with vertical Tendex AI logo (`/logos/Tendexnewtypograpgy-07.svg`)
   - Logo displays on dark background with white text
   - Removed unused `primaryColor` computed property (TypeScript strict mode fix)

5. **OperatorLayout.vue** (Operator/Super Admin Portal)
   - Replaced `pi pi-bolt` icon with white Tendex icon (`/logos/Tendexicon-02.svg`)
   - White icon on dark sidebar gradient background

6. **index.html**
   - Added Arabic meta description
   - Added `theme-color` meta tag (#1B3A5C)

### Files Modified:
- `frontend/public/favicon.svg` (replaced with Tendexicon-01.svg)
- `frontend/public/logos/` (7 new SVG files)
- `frontend/src/components/layout/AppHeader.vue`
- `frontend/src/components/layout/OperatorLayout.vue`
- `frontend/src/views/auth/LoginView.vue`
- `frontend/index.html`

### Commits:
- `dcfc0c2` - feat(ui): add new Tendex AI logo across platform
- `af3f803` - fix(ui): remove unused primaryColor variable in LoginView

### Verification:
- Tested on mof.netaq.pro and edu.netaq.pro
- Logo renders correctly on login page (dark background)
- Favicon updated in browser tab
- All 7 logo SVG files accessible via HTTP (200 OK)

## 2026-04-19: Admin Reset Password Feature

### Overview
Built a complete Admin Reset Password feature allowing tenant administrators to reset passwords for their users directly from the User Management page, following global security best practices.

### Backend Changes

1. **AdminResetPasswordCommand** (NEW)
   - `backend/src/TendexAI.Application/Features/UserManagement/Commands/AdminResetPassword/AdminResetPasswordCommand.cs`
   - MediatR command with UserId, NewPassword, NotifyUser, ForceChangeOnLogin parameters

2. **AdminResetPasswordCommandHandler** (NEW)
   - `backend/src/TendexAI.Application/Features/UserManagement/Commands/AdminResetPassword/AdminResetPasswordCommandHandler.cs`
   - Validates admin permissions, hashes password with BCrypt, updates user record
   - Sends email notification to user if NotifyUser is true
   - Logs action to immutable audit trail
   - Returns success/failure result

3. **AdminResetPasswordRequest** (NEW)
   - Added to `UserManagementRequestModels.cs`
   - Fields: NewPassword, NotifyUser, ForceChangeOnLogin

4. **Endpoint Registration**
   - Added `POST /api/v1/users/{userId}/admin-reset-password` in `UserManagementEndpoints.cs`
   - Protected by `UsersResetPassword` permission policy

5. **Permission Policy**
   - Added `UsersResetPassword` to `PermissionPolicies.cs`
   - Registered in `PermissionAuthorizationExtensions.cs`
   - Added `users.reset_password` permission to all tenant databases and assigned to admin roles

### Frontend Changes

1. **Reset Password Dialog** in `UsersManagementView.vue`
   - Professional dialog showing user info (name, email, avatar)
   - Warning banner about session termination
   - Password and confirm password fields with show/hide toggle
   - Random password generator (12 chars: upper, lower, digits, special)
   - Password strength indicator (weak/medium/strong/very strong) with color coding
   - Password policy checklist (8+ chars, uppercase, lowercase, digit, special char)
   - Notify user via email checkbox (default: checked)
   - Force change on next login checkbox (default: checked)
   - Cancel and Submit buttons with loading state

2. **Reset Password Button** in user table actions
   - Key icon button with tooltip "إعادة تعيين كلمة المرور"
   - Visible only to users with `users.reset_password` permission

3. **Service Function** in `userManagementService.ts`
   - `adminResetPassword(userId, request)` function
   - Types: `AdminResetPasswordRequest`, `AdminResetPasswordResponse`

4. **Translations** (ar.json & en.json)
   - Full Arabic and English translations for all dialog elements
   - 20+ translation keys under `settings.users.resetPassword`

### Bug Fix
- Fixed vue-i18n SyntaxError caused by special characters (`@#$%&*!`) in translation values
- Removed special characters from `policySpecial` translation key in both ar.json and en.json

### Files Modified:
- `backend/src/TendexAI.Application/Features/UserManagement/Commands/AdminResetPassword/AdminResetPasswordCommand.cs` (NEW)
- `backend/src/TendexAI.Application/Features/UserManagement/Commands/AdminResetPassword/AdminResetPasswordCommandHandler.cs` (NEW)
- `backend/src/TendexAI.API/Endpoints/UserManagement/UserManagementEndpoints.cs`
- `backend/src/TendexAI.API/Endpoints/UserManagement/UserManagementRequestModels.cs`
- `backend/src/TendexAI.Infrastructure/Authorization/PermissionPolicies.cs`
- `backend/src/TendexAI.Infrastructure/Authorization/PermissionAuthorizationExtensions.cs`
- `backend/src/TendexAI.Infrastructure/TendexAI.Infrastructure.csproj` (MailKit security update)
- `frontend/src/views/settings/UsersManagementView.vue`
- `frontend/src/services/userManagementService.ts`
- `frontend/src/types/userManagement.ts`
- `frontend/src/locales/ar.json`
- `frontend/src/locales/en.json`

### Commits:
- `b85b2af` - feat(auth): add admin reset password feature with full backend and frontend
- `e8b3f2a` - fix(deps): update MailKit to resolve NU1902 security warning
- `6f2f0e5` - fix(build): resolve CA1311/CA1304 culture-specific string warnings
- `079bf84` - fix(i18n): remove special chars from resetPassword translations causing vue-i18n parse error

### Verification:
- Tested on mof.netaq.pro with ahmed@mof.gov.sa (Tenant Primary Admin)
- Reset password dialog opens correctly with user info
- Random password generator works (produces strong 12-char passwords)
- Password strength indicator shows correct strength levels
- Policy checklist validates in real-time
- Cancel button closes dialog without changes

### Infrastructure Fix:
- Fixed VITE_API_BASE_URL from `https://api.netaq.pro` to `/api` (relative path)
- Reset password for ahmed@mof.gov.sa to `Admin@123456`
- Fixed Docker networking issues between frontend/backend/nginx containers

---

## 2026-04-19: Operator Reset Tenant Admin Password Feature

### Overview
Built a complete feature allowing the Platform Operator (Operator Super Admin) to reset the password of any tenant's primary admin directly from the Operator Dashboard tenant detail page. This is a critical administrative capability for managing government entities on the platform.

### Backend Changes

1. **OperatorResetTenantAdminPasswordCommand** (NEW)
   - `backend/src/TendexAI.Application/Features/Tenants/Commands/OperatorResetTenantAdminPassword/OperatorResetTenantAdminPasswordCommand.cs`
   - CQRS command with TenantId, NewPassword, ConfirmPassword, NotifyAdmin, ForcePasswordChange, Reason, OperatorId, OperatorName, IpAddress

2. **OperatorResetTenantAdminPasswordCommandHandler** (NEW)
   - `backend/src/TendexAI.Application/Features/Tenants/Commands/OperatorResetTenantAdminPassword/OperatorResetTenantAdminPasswordCommandHandler.cs`
   - Validates tenant exists, calls ITenantAdminPasswordResetService for cross-tenant DB access
   - Logs action to immutable audit trail with full details
   - Sends email notification to admin if NotifyAdmin is true

3. **OperatorResetTenantAdminPasswordCommandValidator** (NEW)
   - `backend/src/TendexAI.Application/Features/Tenants/Commands/OperatorResetTenantAdminPassword/OperatorResetTenantAdminPasswordCommandValidator.cs`
   - FluentValidation: min 8 chars, uppercase, lowercase, digit, special char, password match

4. **ITenantAdminPasswordResetService** (NEW - Clean Architecture Interface)
   - `backend/src/TendexAI.Application/Common/Interfaces/ITenantAdminPasswordResetService.cs`
   - Interface in Application layer for cross-tenant password reset

5. **TenantAdminPasswordResetService** (NEW - Infrastructure Implementation)
   - `backend/src/TendexAI.Infrastructure/Services/TenantAdminPasswordResetService.cs`
   - Decrypts tenant connection string, connects to tenant DB
   - Finds primary admin (first user with TenantAdmin role)
   - Hashes password with BCrypt, updates user record
   - Regenerates security stamp to invalidate existing sessions

6. **Endpoint Registration**
   - Added `POST /api/v1/tenants/{id}/reset-admin-password` in `TenantEndpoints.cs`
   - Protected by `tenants.reset_admin_password` permission policy

7. **Permission Policy**
   - Added `TenantsResetAdminPassword` to `PermissionPolicies.cs`
   - Registered in `PermissionAuthorizationExtensions.cs`
   - Added to `DependencyInjection.cs` (ITenantAdminPasswordResetService registration)

### Frontend Changes

1. **Reset Admin Password Dialog** in `TenantDetailView.vue`
   - Quick action card with key icon in tenant detail page
   - Professional dialog showing tenant info (name + admin email)
   - Warning banner about immediate password change and session termination
   - Password and confirm password fields with show/hide toggle
   - Random password generator (12 chars: upper, lower, digits, special)
   - Password strength indicator (weak/medium/strong) with color coding
   - Password policy checklist (8+ chars, uppercase, lowercase, digit, special char)
   - Notify admin via email checkbox (default: checked)
   - Force password change on next login checkbox (default: checked)
   - Cancel and Submit buttons with loading state
   - Full RTL/LTR support

2. **Service & Store**
   - `tenantService.ts`: Added `operatorResetTenantAdminPassword()` API function
   - `tenant.ts` (types): Added `OperatorResetTenantAdminPasswordRequest` type
   - `tenant.ts` (store): Added `operatorResetTenantAdminPassword` store action

3. **Translations** (ar.json & en.json)
   - Full Arabic and English translations for all dialog elements
   - Keys under `tenants.quickActions.resetAdminPassword` and `tenants.resetAdminPassword`

### Database Changes
- Added `tenants.reset_admin_password` permission to both `master_platform` and `tenant_a86f3588` (operator tenant) databases
- Linked permission to `Operator Super Admin` role in both databases

### Files Modified:
- `backend/src/TendexAI.Application/Features/Tenants/Commands/OperatorResetTenantAdminPassword/OperatorResetTenantAdminPasswordCommand.cs` (NEW)
- `backend/src/TendexAI.Application/Features/Tenants/Commands/OperatorResetTenantAdminPassword/OperatorResetTenantAdminPasswordCommandHandler.cs` (NEW)
- `backend/src/TendexAI.Application/Features/Tenants/Commands/OperatorResetTenantAdminPassword/OperatorResetTenantAdminPasswordCommandValidator.cs` (NEW)
- `backend/src/TendexAI.Application/Common/Interfaces/ITenantAdminPasswordResetService.cs` (NEW)
- `backend/src/TendexAI.Infrastructure/Services/TenantAdminPasswordResetService.cs` (NEW)
- `backend/src/TendexAI.Application/Features/Tenants/Dtos/TenantDtos.cs`
- `backend/src/TendexAI.Infrastructure/Authorization/PermissionPolicies.cs`
- `backend/src/TendexAI.Infrastructure/Authorization/PermissionAuthorizationExtensions.cs`
- `backend/src/TendexAI.Infrastructure/DependencyInjection.cs`
- `backend/src/TendexAI.API/Endpoints/TenantEndpoints.cs`
- `frontend/src/types/tenant.ts`
- `frontend/src/services/tenantService.ts`
- `frontend/src/stores/tenant.ts`
- `frontend/src/views/tenants/TenantDetailView.vue`
- `frontend/src/locales/ar.json`
- `frontend/src/locales/en.json`

### Commits:
- `feat: add operator reset tenant admin password feature`
- `fix: resolve CS8602 null reference warning in handler`

### Verification:
- Tested on netaq.pro with admin@netaq.pro (Operator Super Admin)
- Navigated to tenant detail page for Ministry of Health
- Quick action card "إعادة تعيين كلمة مرور المسؤول" visible
- Dialog opens correctly with tenant info (وزارة الصحة - sara@moh.gov.sa)
- Random password generator produces strong 12-char passwords
- Password strength indicator works correctly
- API call succeeds - backend logs confirm: "Successfully reset password for primary admin mohammed@moh.gov.sa of tenant gov-moh-003"
- Audit log entry created successfully
- Note: Email notification shows "host name cannot be empty" error (SMTP not configured yet - non-blocking)
