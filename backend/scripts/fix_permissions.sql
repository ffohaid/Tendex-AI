-- Fix missing permissions in tenant databases
-- Compare Backend required permissions vs DB existing permissions
-- Missing permissions: tasks.view, active_directory.manage, ai.settings_manage, ai.settings_view,
-- approvals.approve, approvals.create, approvals.reject, approvals.view,
-- award.approve, award.manage, award.view, competitions.manage, competitions.publish,
-- evaluation.create, minutes.create, minutes.sign, minutes.view,
-- notifications.view, organization.edit, organization.manage, organization.view,
-- workflow.create, workflow.delete, workflow.edit

-- Insert missing permissions
DECLARE @now DATETIME2 = GETUTCDATE();

-- Tasks
IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'tasks.view')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'tasks.view', N'عرض المهام', 'View Tasks', 'Tasks', 'View pending tasks in task center', @now, '00000000-0000-0000-0000-000000000000');

-- Active Directory
IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'active_directory.manage')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'active_directory.manage', N'إدارة الدليل النشط', 'Manage Active Directory', 'ActiveDirectory', 'Manage Active Directory settings', @now, '00000000-0000-0000-0000-000000000000');

-- AI Settings
IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'ai.settings_view')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'ai.settings_view', N'عرض إعدادات الذكاء الاصطناعي', 'View AI Settings', 'AI', 'View AI configuration settings', @now, '00000000-0000-0000-0000-000000000000');

IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'ai.settings_manage')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'ai.settings_manage', N'إدارة إعدادات الذكاء الاصطناعي', 'Manage AI Settings', 'AI', 'Manage AI configuration settings', @now, '00000000-0000-0000-0000-000000000000');

-- Approvals
IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'approvals.view')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'approvals.view', N'عرض الاعتمادات', 'View Approvals', 'Approvals', 'View approval requests', @now, '00000000-0000-0000-0000-000000000000');

IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'approvals.create')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'approvals.create', N'إنشاء اعتمادات', 'Create Approvals', 'Approvals', 'Create approval requests', @now, '00000000-0000-0000-0000-000000000000');

IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'approvals.approve')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'approvals.approve', N'اعتماد الطلبات', 'Approve Requests', 'Approvals', 'Approve approval requests', @now, '00000000-0000-0000-0000-000000000000');

IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'approvals.reject')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'approvals.reject', N'رفض الطلبات', 'Reject Requests', 'Approvals', 'Reject approval requests', @now, '00000000-0000-0000-0000-000000000000');

-- Award
IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'award.view')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'award.view', N'عرض الترسية', 'View Award', 'Award', 'View award decisions', @now, '00000000-0000-0000-0000-000000000000');

IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'award.manage')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'award.manage', N'إدارة الترسية', 'Manage Award', 'Award', 'Manage award decisions', @now, '00000000-0000-0000-0000-000000000000');

IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'award.approve')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'award.approve', N'اعتماد الترسية', 'Approve Award', 'Award', 'Approve award decisions', @now, '00000000-0000-0000-0000-000000000000');

-- Competitions (missing)
IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'competitions.manage')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'competitions.manage', N'إدارة المنافسات', 'Manage Competitions', 'Competitions', 'Full management of competitions', @now, '00000000-0000-0000-0000-000000000000');

IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'competitions.publish')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'competitions.publish', N'نشر المنافسات', 'Publish Competitions', 'Competitions', 'Publish competitions', @now, '00000000-0000-0000-0000-000000000000');

-- Evaluation (missing)
IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'evaluation.create')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'evaluation.create', N'إنشاء تقييم', 'Create Evaluation', 'Evaluation', 'Create evaluation sessions', @now, '00000000-0000-0000-0000-000000000000');

-- Minutes
IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'minutes.view')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'minutes.view', N'عرض المحاضر', 'View Minutes', 'Minutes', 'View evaluation minutes', @now, '00000000-0000-0000-0000-000000000000');

IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'minutes.create')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'minutes.create', N'إنشاء محاضر', 'Create Minutes', 'Minutes', 'Create evaluation minutes', @now, '00000000-0000-0000-0000-000000000000');

IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'minutes.sign')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'minutes.sign', N'توقيع المحاضر', 'Sign Minutes', 'Minutes', 'Sign evaluation minutes', @now, '00000000-0000-0000-0000-000000000000');

-- Notifications
IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'notifications.view')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'notifications.view', N'عرض الإشعارات', 'View Notifications', 'Notifications', 'View notifications', @now, '00000000-0000-0000-0000-000000000000');

-- Organization
IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'organization.view')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'organization.view', N'عرض بيانات الجهة', 'View Organization', 'Organization', 'View organization details', @now, '00000000-0000-0000-0000-000000000000');

IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'organization.edit')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'organization.edit', N'تعديل بيانات الجهة', 'Edit Organization', 'Organization', 'Edit organization details', @now, '00000000-0000-0000-0000-000000000000');

IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'organization.manage')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'organization.manage', N'إدارة الجهة', 'Manage Organization', 'Organization', 'Full management of organization', @now, '00000000-0000-0000-0000-000000000000');

-- Workflow (missing)
IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'workflow.create')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'workflow.create', N'إنشاء مسارات اعتماد', 'Create Workflow', 'Workflow', 'Create workflow paths', @now, '00000000-0000-0000-0000-000000000000');

IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'workflow.edit')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'workflow.edit', N'تعديل مسارات اعتماد', 'Edit Workflow', 'Workflow', 'Edit workflow paths', @now, '00000000-0000-0000-0000-000000000000');

IF NOT EXISTS (SELECT 1 FROM [identity].Permissions WHERE Code = 'workflow.delete')
INSERT INTO [identity].Permissions (Id, Code, NameAr, NameEn, Module, Description, CreatedAt, CreatedBy) VALUES (NEWID(), 'workflow.delete', N'حذف مسارات اعتماد', 'Delete Workflow', 'Workflow', 'Delete workflow paths', @now, '00000000-0000-0000-0000-000000000000');

-- Now assign ALL permissions to the Tenant Primary Admin role
DECLARE @adminRoleId UNIQUEIDENTIFIER;
SELECT @adminRoleId = Id FROM [identity].Roles WHERE NameEn = 'Tenant Primary Admin';

IF @adminRoleId IS NOT NULL
BEGIN
    -- Insert permissions that are not yet assigned to admin role
    INSERT INTO [identity].RolePermissions (Id, RoleId, PermissionId, CreatedAt, CreatedBy)
    SELECT NEWID(), @adminRoleId, p.Id, @now, '00000000-0000-0000-0000-000000000000'
    FROM [identity].Permissions p
    WHERE NOT EXISTS (
        SELECT 1 FROM [identity].RolePermissions rp 
        WHERE rp.RoleId = @adminRoleId AND rp.PermissionId = p.Id
    );
END

SELECT 'Done - Permissions added and assigned to admin role' AS Result;
GO
