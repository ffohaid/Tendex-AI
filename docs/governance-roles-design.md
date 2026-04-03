# Governance Roles Design - Tendex AI

## Overview

The platform implements a **three-tier governance model** with two immutable (protected) roles
at the top and flexible, per-tenant custom roles below.

---

## Tier 1 — Operator Super Admin (المسؤول الأول - الشركة المشغلة)

| Property | Value |
|----------|-------|
| **RoleKey** | `operator_super_admin` |
| **NameAr** | المسؤول الأول |
| **NameEn** | Operator Super Admin |
| **Scope** | Platform-wide (cross-tenant) |
| **Stored in** | Master/Platform database |
| **Immutable** | Cannot be edited, deleted, or reassigned by anyone except another Operator Super Admin |

### Permissions

- Create, edit, delete, and manage government entities (tenants)
- Create the "Tenant Primary Admin" user for each entity
- Access the Operator Panel (لوحة المشغل) exclusively
- Manage AI settings, system health, subscriptions, consumption
- Impersonate users for support purposes

### Visibility

- **Operator Panel**: Visible ONLY to this role
- **Tenant-level menus**: NOT visible (this role operates at platform level)

---

## Tier 2 — Tenant Primary Admin (المسؤول الأول بالجهة)

| Property | Value |
|----------|-------|
| **RoleKey** | `tenant_primary_admin` |
| **NameAr** | المسؤول الأول بالجهة |
| **NameEn** | Tenant Primary Admin |
| **Scope** | Single tenant only |
| **Stored in** | Tenant database |
| **Immutable** | Cannot be edited, deleted, or have permissions modified via the matrix |

### Permissions

- Invite users to this tenant ONLY
- Manage roles and permissions matrix for this tenant ONLY
- Create, edit, delete approval workflows for this tenant ONLY
- Create, edit, delete committees for this tenant ONLY
- Manage organization settings, branding, and configuration
- Access knowledge base management
- Access reports and analytics dashboard
- Full access to all tenant-level features
- **CANNOT** access any other tenant's data

### Visibility (Exclusive menus — visible ONLY to this role)

- مسارات الاعتماد (Approval Workflows)
- إدارة اللجان (Committee Management)
- مصفوفة الصلاحيات (Permissions Matrix)
- إدارة المستخدمين (User Management)
- الإعدادات (Settings)
- قاعدة المعرفة (Knowledge Base)
- التقارير (Reports)
- لوحة التحكم العامة للجهة (Tenant Dashboard)

---

## Tier 3 — Flexible Roles (الأدوار المرنة)

These roles are **per-tenant** and fully customizable by the Tenant Primary Admin.

### Default Seeded Roles (can be modified per tenant)

| RoleKey | NameAr | NameEn | Default Purpose |
|---------|--------|--------|-----------------|
| `procurement_manager` | مدير المشتريات | Procurement Manager | Manages procurement processes |
| `financial_controller` | المراقب المالي | Financial Controller | Financial oversight and approval |
| `sector_representative` | ممثل القطاع | Sector Representative | Represents a specific sector |
| `committee_chair` | رئيس اللجنة | Committee Chair | Leads committee activities |
| `committee_member` | عضو اللجنة | Committee Member | Participates in committee work |
| `member` | عضو | Member | General operational member |
| `viewer` | مستعرض | Viewer | Read-only access |

### Characteristics

- Tenant Primary Admin can create new custom roles
- Permissions are controlled via the Permission Matrix
- Committee roles add another dimension of permissions
- Each tenant can have completely different role configurations

---

## Implementation Changes Required

### 1. Backend — Domain Layer

- Add `IsProtected` property to `Role` entity (cannot be edited/deleted)
- Add `RoleKey` property to `Role` entity for programmatic identification
- Update `SystemRole` enum to include `OperatorSuperAdmin` and `TenantPrimaryAdmin`
- Add guard clauses to prevent modification of protected roles

### 2. Backend — Seeding

- Update `TenantDatabaseProvisioner.SeedDefaultRolesAsync` to seed `tenant_primary_admin` as protected
- Operator Super Admin is seeded in the master database during initial setup

### 3. Backend — API Protection

- Add authorization policies for Operator-only endpoints
- Add authorization policies for TenantPrimaryAdmin-only endpoints
- Ensure all tenant endpoints validate tenant isolation

### 4. Frontend — Navigation

- Update `navigation.ts` to add `requiredRoles` for admin-only menus
- Update `useSidebarNavigation.ts` to handle the new role hierarchy
- Operator Panel: `requiredRoles: ['OperatorSuperAdmin']`
- Admin menus: `requiredRoles: ['TenantPrimaryAdmin']`

### 5. Frontend — Role Management

- Prevent editing/deleting protected roles in the UI
- Show visual indicator for protected roles (lock icon)

### 6. Permission Matrix

- Auto-grant all permissions to `TenantPrimaryAdmin` (non-editable)
- Only show flexible roles in the matrix editor
