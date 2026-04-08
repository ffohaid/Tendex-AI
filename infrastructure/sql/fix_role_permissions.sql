-- ============================================================
-- Fix Role Permissions for All Roles
-- This script assigns proper permissions to each role
-- based on the platform's permission matrix design
-- ============================================================

-- ============================================================
-- PROCUREMENT MANAGER - مدير المشتريات (40 permissions)
-- Full RFP lifecycle, competitions, committees, inquiries,
-- evaluations, offers, reports, AI, knowledge base, tasks
-- ============================================================
DECLARE @ProcMgrId UNIQUEIDENTIFIER;
SELECT @ProcMgrId = Id FROM [identity].Roles WHERE NameEn = 'Procurement Manager';

-- Clear existing and re-insert all
DELETE FROM [identity].RolePermissions WHERE RoleId = @ProcMgrId;

INSERT INTO [identity].RolePermissions (Id, RoleId, PermissionId, CreatedAt)
SELECT NEWID(), @ProcMgrId, p.Id, GETUTCDATE()
FROM [identity].Permissions p
WHERE p.Code IN (
    -- RFP / Booklet
    'rfp.create', 'rfp.view', 'rfp.edit', 'rfp.delete', 'rfp.approve', 'rfp.export', 'rfp.publish',
    -- Competitions
    'competitions.create', 'competitions.view', 'competitions.edit', 'competitions.delete',
    'competitions.manage_phases', 'competitions.manage', 'competitions.publish',
    -- Committees
    'committees.create', 'committees.view', 'committees.edit', 'committees.delete', 'committees.manage_members',
    -- Offers
    'offers.view', 'offers.open', 'offers.review',
    -- Evaluation
    'evaluation.view', 'evaluation.create', 'evaluation.approve', 'evaluation.export',
    'evaluation.technical_score', 'evaluation.financial_score',
    -- Inquiries
    'inquiries.view', 'inquiries.create', 'inquiries.respond', 'inquiries.manage',
    -- Approvals
    'approvals.view', 'approvals.approve', 'approvals.reject',
    -- Reports
    'reports.view', 'reports.create', 'reports.export',
    -- AI
    'ai.use_assistant', 'ai.generate_content',
    -- Knowledge Base
    'knowledge.view', 'knowledge.upload', 'knowledge.manage',
    -- Tasks
    'tasks.view',
    -- Dashboard
    'dashboard.view', 'dashboard.export',
    -- Files
    'files.view', 'files.upload', 'files.delete',
    -- Workflow
    'workflow.view', 'workflow.create', 'workflow.edit', 'workflow.manage',
    -- Settings (limited)
    'settings.view',
    -- Users (view only)
    'users.view',
    -- Notifications
    'notifications.view',
    -- Templates
    'templates.view', 'templates.create', 'templates.edit', 'templates.delete',
    -- Support
    'support.view', 'support.create'
);

PRINT 'Procurement Manager: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' permissions assigned';

-- ============================================================
-- COMMITTEE CHAIR - رئيس اللجنة (35 permissions)
-- Committee management, evaluation, offers, reports
-- ============================================================
DECLARE @ChairId UNIQUEIDENTIFIER;
SELECT @ChairId = Id FROM [identity].Roles WHERE NameEn = 'Committee Chair';

DELETE FROM [identity].RolePermissions WHERE RoleId = @ChairId;

INSERT INTO [identity].RolePermissions (Id, RoleId, PermissionId, CreatedAt)
SELECT NEWID(), @ChairId, p.Id, GETUTCDATE()
FROM [identity].Permissions p
WHERE p.Code IN (
    -- RFP
    'rfp.view', 'rfp.approve', 'rfp.export',
    -- Competitions
    'competitions.view', 'competitions.manage_phases',
    -- Committees
    'committees.view', 'committees.edit', 'committees.manage_members',
    -- Offers
    'offers.view', 'offers.open', 'offers.review',
    -- Evaluation
    'evaluation.view', 'evaluation.create', 'evaluation.approve', 'evaluation.export',
    'evaluation.technical_score', 'evaluation.financial_score',
    -- Inquiries
    'inquiries.view', 'inquiries.respond', 'inquiries.manage',
    -- Approvals
    'approvals.view', 'approvals.approve', 'approvals.reject',
    -- Reports
    'reports.view', 'reports.create', 'reports.export',
    -- AI
    'ai.use_assistant', 'ai.generate_content',
    -- Knowledge Base
    'knowledge.view', 'knowledge.upload',
    -- Tasks
    'tasks.view',
    -- Dashboard
    'dashboard.view',
    -- Files
    'files.view', 'files.upload',
    -- Notifications
    'notifications.view',
    -- Minutes
    'minutes.view', 'minutes.create', 'minutes.sign',
    -- Support
    'support.view', 'support.create'
);

PRINT 'Committee Chair: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' permissions assigned';

-- ============================================================
-- COMMITTEE MEMBER - عضو اللجنة (25 permissions)
-- View and evaluate, limited management
-- ============================================================
DECLARE @MemberId UNIQUEIDENTIFIER;
SELECT @MemberId = Id FROM [identity].Roles WHERE NameEn = 'Committee Member';

DELETE FROM [identity].RolePermissions WHERE RoleId = @MemberId;

INSERT INTO [identity].RolePermissions (Id, RoleId, PermissionId, CreatedAt)
SELECT NEWID(), @MemberId, p.Id, GETUTCDATE()
FROM [identity].Permissions p
WHERE p.Code IN (
    -- RFP
    'rfp.view', 'rfp.export',
    -- Competitions
    'competitions.view',
    -- Committees
    'committees.view',
    -- Offers
    'offers.view', 'offers.review',
    -- Evaluation
    'evaluation.view', 'evaluation.create', 'evaluation.export',
    'evaluation.technical_score', 'evaluation.financial_score',
    -- Inquiries
    'inquiries.view', 'inquiries.respond',
    -- Reports
    'reports.view',
    -- AI
    'ai.use_assistant',
    -- Knowledge Base
    'knowledge.view',
    -- Tasks
    'tasks.view',
    -- Dashboard
    'dashboard.view',
    -- Files
    'files.view', 'files.upload',
    -- Notifications
    'notifications.view',
    -- Minutes
    'minutes.view', 'minutes.sign',
    -- Support
    'support.view', 'support.create'
);

PRINT 'Committee Member: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' permissions assigned';

-- ============================================================
-- FINANCIAL CONTROLLER - المراقب المالي (25 permissions)
-- Financial evaluation, approvals, reports
-- ============================================================
DECLARE @FinCtrlId UNIQUEIDENTIFIER;
SELECT @FinCtrlId = Id FROM [identity].Roles WHERE NameEn = 'Financial Controller';

DELETE FROM [identity].RolePermissions WHERE RoleId = @FinCtrlId;

INSERT INTO [identity].RolePermissions (Id, RoleId, PermissionId, CreatedAt)
SELECT NEWID(), @FinCtrlId, p.Id, GETUTCDATE()
FROM [identity].Permissions p
WHERE p.Code IN (
    -- RFP
    'rfp.view', 'rfp.approve', 'rfp.export',
    -- Competitions
    'competitions.view',
    -- Committees
    'committees.view',
    -- Offers
    'offers.view', 'offers.review',
    -- Evaluation
    'evaluation.view', 'evaluation.approve', 'evaluation.export', 'evaluation.financial_score',
    -- Inquiries
    'inquiries.view',
    -- Approvals
    'approvals.view', 'approvals.approve', 'approvals.reject',
    -- Reports
    'reports.view', 'reports.create', 'reports.export',
    -- AI
    'ai.use_assistant',
    -- Knowledge Base
    'knowledge.view',
    -- Tasks
    'tasks.view',
    -- Dashboard
    'dashboard.view', 'dashboard.export',
    -- Files
    'files.view',
    -- Notifications
    'notifications.view',
    -- Support
    'support.view', 'support.create'
);

PRINT 'Financial Controller: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' permissions assigned';

-- ============================================================
-- SECTOR REPRESENTATIVE - ممثل القطاع (30 permissions)
-- RFP creation, competitions, inquiries, reports
-- ============================================================
DECLARE @SectorRepId UNIQUEIDENTIFIER;
SELECT @SectorRepId = Id FROM [identity].Roles WHERE NameEn = 'Sector Representative';

DELETE FROM [identity].RolePermissions WHERE RoleId = @SectorRepId;

INSERT INTO [identity].RolePermissions (Id, RoleId, PermissionId, CreatedAt)
SELECT NEWID(), @SectorRepId, p.Id, GETUTCDATE()
FROM [identity].Permissions p
WHERE p.Code IN (
    -- RFP
    'rfp.create', 'rfp.view', 'rfp.edit', 'rfp.export',
    -- Competitions
    'competitions.create', 'competitions.view', 'competitions.edit',
    -- Committees
    'committees.view',
    -- Offers
    'offers.view', 'offers.review',
    -- Evaluation
    'evaluation.view', 'evaluation.create', 'evaluation.export',
    'evaluation.technical_score',
    -- Inquiries
    'inquiries.view', 'inquiries.create', 'inquiries.respond',
    -- Approvals
    'approvals.view',
    -- Reports
    'reports.view', 'reports.create', 'reports.export',
    -- AI
    'ai.use_assistant', 'ai.generate_content',
    -- Knowledge Base
    'knowledge.view', 'knowledge.upload',
    -- Tasks
    'tasks.view',
    -- Dashboard
    'dashboard.view',
    -- Files
    'files.view', 'files.upload',
    -- Notifications
    'notifications.view',
    -- Templates
    'templates.view',
    -- Support
    'support.view', 'support.create'
);

PRINT 'Sector Representative: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' permissions assigned';

-- ============================================================
-- MEMBER - عضو (keep existing 30 but ensure completeness)
-- General member with broad view access
-- ============================================================
DECLARE @RegMemberId UNIQUEIDENTIFIER;
SELECT @RegMemberId = Id FROM [identity].Roles WHERE NameEn = 'Member';

DELETE FROM [identity].RolePermissions WHERE RoleId = @RegMemberId;

INSERT INTO [identity].RolePermissions (Id, RoleId, PermissionId, CreatedAt)
SELECT NEWID(), @RegMemberId, p.Id, GETUTCDATE()
FROM [identity].Permissions p
WHERE p.Code IN (
    -- RFP
    'rfp.create', 'rfp.view', 'rfp.edit', 'rfp.export',
    -- Competitions
    'competitions.create', 'competitions.view', 'competitions.edit',
    -- Committees
    'committees.view',
    -- Offers
    'offers.view',
    -- Evaluation
    'evaluation.view', 'evaluation.export',
    -- Inquiries
    'inquiries.view', 'inquiries.create',
    -- Approvals
    'approvals.view',
    -- Reports
    'reports.view',
    -- AI
    'ai.use_assistant', 'ai.generate_content',
    -- Knowledge Base
    'knowledge.view', 'knowledge.upload',
    -- Tasks
    'tasks.view',
    -- Dashboard
    'dashboard.view',
    -- Files
    'files.view', 'files.upload',
    -- Notifications
    'notifications.view',
    -- Templates
    'templates.view',
    -- Workflow
    'workflow.view',
    -- Support
    'support.view', 'support.create'
);

PRINT 'Member: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' permissions assigned';

-- ============================================================
-- VIEWER - مستعرض (keep existing 13 but ensure completeness)
-- Read-only access across the platform
-- ============================================================
DECLARE @ViewerId UNIQUEIDENTIFIER;
SELECT @ViewerId = Id FROM [identity].Roles WHERE NameEn = 'Viewer';

DELETE FROM [identity].RolePermissions WHERE RoleId = @ViewerId;

INSERT INTO [identity].RolePermissions (Id, RoleId, PermissionId, CreatedAt)
SELECT NEWID(), @ViewerId, p.Id, GETUTCDATE()
FROM [identity].Permissions p
WHERE p.Code IN (
    -- RFP
    'rfp.view', 'rfp.export',
    -- Competitions
    'competitions.view',
    -- Committees
    'committees.view',
    -- Offers
    'offers.view',
    -- Evaluation
    'evaluation.view', 'evaluation.export',
    -- Inquiries
    'inquiries.view',
    -- Approvals
    'approvals.view',
    -- Reports
    'reports.view',
    -- Knowledge Base
    'knowledge.view',
    -- Tasks
    'tasks.view',
    -- Dashboard
    'dashboard.view',
    -- Files
    'files.view',
    -- Notifications
    'notifications.view',
    -- Support
    'support.view'
);

PRINT 'Viewer: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' permissions assigned';

-- ============================================================
-- Final verification
-- ============================================================
SELECT r.NameEn, COUNT(rp.PermissionId) as PermCount
FROM [identity].Roles r
LEFT JOIN [identity].RolePermissions rp ON r.Id = rp.RoleId
GROUP BY r.NameEn
ORDER BY PermCount DESC;
