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
