using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TendexAI.Infrastructure.Persistence.Migrations.Tenant;

/// <summary>
/// Seeds the Permissions table with comprehensive platform permissions
/// and updates system role descriptions to Arabic.
/// Also assigns default permissions to system roles.
/// </summary>
public partial class SeedPermissionsAndUpdateRoles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ═══════════════════════════════════════════════════════════════
        //  Step 1: Seed Permissions
        // ═══════════════════════════════════════════════════════════════

        // -- Module: Dashboard --
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[Permissions] (Id, Code, NameAr, NameEn, Module, Description, CreatedAt)
            VALUES
            (NEWID(), 'dashboard.view', N'عرض لوحة التحكم', 'View Dashboard', 'Dashboard', N'عرض لوحة التحكم والإحصائيات العامة', GETUTCDATE()),
            (NEWID(), 'dashboard.export', N'تصدير تقارير لوحة التحكم', 'Export Dashboard Reports', 'Dashboard', N'تصدير البيانات والتقارير من لوحة التحكم', GETUTCDATE());
        ");

        // -- Module: Users --
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[Permissions] (Id, Code, NameAr, NameEn, Module, Description, CreatedAt)
            VALUES
            (NEWID(), 'users.view', N'عرض المستخدمين', 'View Users', 'Users', N'عرض قائمة المستخدمين وبياناتهم', GETUTCDATE()),
            (NEWID(), 'users.create', N'إنشاء مستخدم', 'Create User', 'Users', N'دعوة مستخدمين جدد للمنصة', GETUTCDATE()),
            (NEWID(), 'users.edit', N'تعديل مستخدم', 'Edit User', 'Users', N'تعديل بيانات المستخدمين', GETUTCDATE()),
            (NEWID(), 'users.delete', N'حذف مستخدم', 'Delete User', 'Users', N'حذف أو تعطيل حسابات المستخدمين', GETUTCDATE()),
            (NEWID(), 'users.manage_roles', N'إدارة أدوار المستخدمين', 'Manage User Roles', 'Users', N'إسناد وإزالة الأدوار من المستخدمين', GETUTCDATE()),
            (NEWID(), 'users.impersonate', N'انتحال صفة مستخدم', 'Impersonate User', 'Users', N'تسجيل الدخول بصفة مستخدم آخر', GETUTCDATE());
        ");

        // -- Module: Roles --
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[Permissions] (Id, Code, NameAr, NameEn, Module, Description, CreatedAt)
            VALUES
            (NEWID(), 'roles.view', N'عرض الأدوار', 'View Roles', 'Roles', N'عرض قائمة الأدوار والصلاحيات', GETUTCDATE()),
            (NEWID(), 'roles.create', N'إنشاء دور', 'Create Role', 'Roles', N'إنشاء أدوار مخصصة جديدة', GETUTCDATE()),
            (NEWID(), 'roles.edit', N'تعديل دور', 'Edit Role', 'Roles', N'تعديل بيانات وصلاحيات الأدوار', GETUTCDATE()),
            (NEWID(), 'roles.delete', N'حذف دور', 'Delete Role', 'Roles', N'حذف أو تعطيل الأدوار المخصصة', GETUTCDATE());
        ");

        // -- Module: RFP (كراسات الشروط) --
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[Permissions] (Id, Code, NameAr, NameEn, Module, Description, CreatedAt)
            VALUES
            (NEWID(), 'rfp.view', N'عرض كراسات الشروط', 'View RFPs', 'RFP', N'عرض قائمة كراسات الشروط', GETUTCDATE()),
            (NEWID(), 'rfp.create', N'إنشاء كراسة شروط', 'Create RFP', 'RFP', N'إنشاء كراسة شروط جديدة', GETUTCDATE()),
            (NEWID(), 'rfp.edit', N'تعديل كراسة شروط', 'Edit RFP', 'RFP', N'تعديل بيانات كراسة الشروط', GETUTCDATE()),
            (NEWID(), 'rfp.delete', N'حذف كراسة شروط', 'Delete RFP', 'RFP', N'حذف كراسة الشروط', GETUTCDATE()),
            (NEWID(), 'rfp.approve', N'اعتماد كراسة شروط', 'Approve RFP', 'RFP', N'اعتماد أو رفض كراسة الشروط', GETUTCDATE()),
            (NEWID(), 'rfp.publish', N'نشر كراسة شروط', 'Publish RFP', 'RFP', N'نشر كراسة الشروط للمنافسة', GETUTCDATE()),
            (NEWID(), 'rfp.export', N'تصدير كراسة شروط', 'Export RFP', 'RFP', N'تصدير كراسة الشروط كملف PDF', GETUTCDATE());
        ");

        // -- Module: Competitions (المنافسات) --
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[Permissions] (Id, Code, NameAr, NameEn, Module, Description, CreatedAt)
            VALUES
            (NEWID(), 'competitions.view', N'عرض المنافسات', 'View Competitions', 'Competitions', N'عرض قائمة المنافسات', GETUTCDATE()),
            (NEWID(), 'competitions.create', N'إنشاء منافسة', 'Create Competition', 'Competitions', N'إنشاء منافسة جديدة', GETUTCDATE()),
            (NEWID(), 'competitions.edit', N'تعديل منافسة', 'Edit Competition', 'Competitions', N'تعديل بيانات المنافسة', GETUTCDATE()),
            (NEWID(), 'competitions.delete', N'حذف منافسة', 'Delete Competition', 'Competitions', N'حذف المنافسة', GETUTCDATE()),
            (NEWID(), 'competitions.manage_phases', N'إدارة مراحل المنافسة', 'Manage Competition Phases', 'Competitions', N'التحكم في مراحل المنافسة وتقدمها', GETUTCDATE());
        ");

        // -- Module: Committees (اللجان) --
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[Permissions] (Id, Code, NameAr, NameEn, Module, Description, CreatedAt)
            VALUES
            (NEWID(), 'committees.view', N'عرض اللجان', 'View Committees', 'Committees', N'عرض قائمة اللجان وأعضائها', GETUTCDATE()),
            (NEWID(), 'committees.create', N'إنشاء لجنة', 'Create Committee', 'Committees', N'إنشاء لجنة جديدة', GETUTCDATE()),
            (NEWID(), 'committees.edit', N'تعديل لجنة', 'Edit Committee', 'Committees', N'تعديل بيانات اللجنة وأعضائها', GETUTCDATE()),
            (NEWID(), 'committees.delete', N'حذف لجنة', 'Delete Committee', 'Committees', N'حذف اللجنة', GETUTCDATE()),
            (NEWID(), 'committees.manage_members', N'إدارة أعضاء اللجنة', 'Manage Committee Members', 'Committees', N'إضافة وإزالة أعضاء اللجنة', GETUTCDATE());
        ");

        // -- Module: Evaluation (الفحص والتقييم) --
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[Permissions] (Id, Code, NameAr, NameEn, Module, Description, CreatedAt)
            VALUES
            (NEWID(), 'evaluation.view', N'عرض التقييمات', 'View Evaluations', 'Evaluation', N'عرض نتائج الفحص والتقييم', GETUTCDATE()),
            (NEWID(), 'evaluation.technical_score', N'التقييم الفني', 'Technical Scoring', 'Evaluation', N'إجراء التقييم الفني للعروض', GETUTCDATE()),
            (NEWID(), 'evaluation.financial_score', N'التقييم المالي', 'Financial Scoring', 'Evaluation', N'إجراء التقييم المالي للعروض', GETUTCDATE()),
            (NEWID(), 'evaluation.approve', N'اعتماد التقييم', 'Approve Evaluation', 'Evaluation', N'اعتماد نتائج التقييم', GETUTCDATE()),
            (NEWID(), 'evaluation.export', N'تصدير التقييم', 'Export Evaluation', 'Evaluation', N'تصدير نتائج التقييم كتقرير', GETUTCDATE());
        ");

        // -- Module: Offers (العروض) --
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[Permissions] (Id, Code, NameAr, NameEn, Module, Description, CreatedAt)
            VALUES
            (NEWID(), 'offers.view', N'عرض العروض', 'View Offers', 'Offers', N'عرض العروض المقدمة', GETUTCDATE()),
            (NEWID(), 'offers.open', N'فتح المظاريف', 'Open Envelopes', 'Offers', N'فتح مظاريف العروض', GETUTCDATE()),
            (NEWID(), 'offers.review', N'مراجعة العروض', 'Review Offers', 'Offers', N'مراجعة وفحص العروض المقدمة', GETUTCDATE());
        ");

        // -- Module: Inquiries (الاستفسارات) --
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[Permissions] (Id, Code, NameAr, NameEn, Module, Description, CreatedAt)
            VALUES
            (NEWID(), 'inquiries.view', N'عرض الاستفسارات', 'View Inquiries', 'Inquiries', N'عرض الاستفسارات الواردة', GETUTCDATE()),
            (NEWID(), 'inquiries.respond', N'الرد على الاستفسارات', 'Respond to Inquiries', 'Inquiries', N'الرد على استفسارات الموردين', GETUTCDATE()),
            (NEWID(), 'inquiries.manage', N'إدارة الاستفسارات', 'Manage Inquiries', 'Inquiries', N'إدارة وتوجيه الاستفسارات', GETUTCDATE());
        ");

        // -- Module: Knowledge Base (قاعدة المعرفة) --
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[Permissions] (Id, Code, NameAr, NameEn, Module, Description, CreatedAt)
            VALUES
            (NEWID(), 'knowledge.view', N'عرض قاعدة المعرفة', 'View Knowledge Base', 'KnowledgeBase', N'عرض المستندات والمعارف', GETUTCDATE()),
            (NEWID(), 'knowledge.upload', N'رفع مستندات', 'Upload Documents', 'KnowledgeBase', N'رفع مستندات جديدة لقاعدة المعرفة', GETUTCDATE()),
            (NEWID(), 'knowledge.manage', N'إدارة قاعدة المعرفة', 'Manage Knowledge Base', 'KnowledgeBase', N'إدارة وتنظيم قاعدة المعرفة', GETUTCDATE());
        ");

        // -- Module: AI (الذكاء الاصطناعي) --
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[Permissions] (Id, Code, NameAr, NameEn, Module, Description, CreatedAt)
            VALUES
            (NEWID(), 'ai.use_assistant', N'استخدام المساعد الذكي', 'Use AI Assistant', 'AI', N'استخدام المساعد الذكي للمنصة', GETUTCDATE()),
            (NEWID(), 'ai.generate_content', N'توليد المحتوى بالذكاء الاصطناعي', 'Generate AI Content', 'AI', N'توليد محتوى باستخدام الذكاء الاصطناعي', GETUTCDATE()),
            (NEWID(), 'ai.configure', N'إعدادات الذكاء الاصطناعي', 'Configure AI', 'AI', N'تكوين إعدادات الذكاء الاصطناعي', GETUTCDATE());
        ");

        // -- Module: Settings (الإعدادات) --
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[Permissions] (Id, Code, NameAr, NameEn, Module, Description, CreatedAt)
            VALUES
            (NEWID(), 'settings.view', N'عرض الإعدادات', 'View Settings', 'Settings', N'عرض إعدادات المنصة', GETUTCDATE()),
            (NEWID(), 'settings.edit', N'تعديل الإعدادات', 'Edit Settings', 'Settings', N'تعديل إعدادات المنصة العامة', GETUTCDATE()),
            (NEWID(), 'settings.branding', N'إدارة العلامة التجارية', 'Manage Branding', 'Settings', N'تعديل شعار وألوان الجهة', GETUTCDATE()),
            (NEWID(), 'settings.smtp', N'إعدادات البريد الإلكتروني', 'Email Settings', 'Settings', N'تكوين إعدادات خادم البريد', GETUTCDATE());
        ");

        // -- Module: Reports (التقارير) --
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[Permissions] (Id, Code, NameAr, NameEn, Module, Description, CreatedAt)
            VALUES
            (NEWID(), 'reports.view', N'عرض التقارير', 'View Reports', 'Reports', N'عرض التقارير والإحصائيات', GETUTCDATE()),
            (NEWID(), 'reports.export', N'تصدير التقارير', 'Export Reports', 'Reports', N'تصدير التقارير بصيغ مختلفة', GETUTCDATE()),
            (NEWID(), 'reports.create', N'إنشاء تقارير', 'Create Reports', 'Reports', N'إنشاء تقارير مخصصة', GETUTCDATE());
        ");

        // -- Module: Workflow (سير العمل) --
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[Permissions] (Id, Code, NameAr, NameEn, Module, Description, CreatedAt)
            VALUES
            (NEWID(), 'workflow.view', N'عرض مسارات الاعتماد', 'View Workflows', 'Workflow', N'عرض مسارات الاعتماد وسير العمل', GETUTCDATE()),
            (NEWID(), 'workflow.manage', N'إدارة مسارات الاعتماد', 'Manage Workflows', 'Workflow', N'إنشاء وتعديل مسارات الاعتماد', GETUTCDATE()),
            (NEWID(), 'workflow.approve', N'اعتماد المهام', 'Approve Tasks', 'Workflow', N'اعتماد أو رفض المهام في سير العمل', GETUTCDATE());
        ");

        // -- Module: AuditLog (سجل التدقيق) --
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[Permissions] (Id, Code, NameAr, NameEn, Module, Description, CreatedAt)
            VALUES
            (NEWID(), 'audit.view', N'عرض سجل التدقيق', 'View Audit Log', 'AuditLog', N'عرض سجل العمليات والتدقيق', GETUTCDATE()),
            (NEWID(), 'audit.export', N'تصدير سجل التدقيق', 'Export Audit Log', 'AuditLog', N'تصدير سجل التدقيق', GETUTCDATE());
        ");

        // ═══════════════════════════════════════════════════════════════
        //  Step 2: Update Role Descriptions to Arabic
        // ═══════════════════════════════════════════════════════════════

        migrationBuilder.Sql(@"
            UPDATE [identity].[Roles] SET Description = N'رئيس اللجنة المسؤول عن إدارة أعمال اللجنة واعتماد قراراتها' WHERE NormalizedName = 'COMMITTEE CHAIR';
            UPDATE [identity].[Roles] SET Description = N'عضو في اللجنة يشارك في أعمال الفحص والتقييم والتصويت' WHERE NormalizedName = 'COMMITTEE MEMBER';
            UPDATE [identity].[Roles] SET Description = N'مالك الجهة الحكومية المسؤول عن الإشراف العام والاعتمادات النهائية' WHERE NormalizedName = 'TENANT OWNER';
            UPDATE [identity].[Roles] SET Description = N'مدير النظام الأعلى بصلاحيات كاملة لإدارة جميع جوانب المنصة' WHERE NormalizedName = 'SUPER ADMIN';
            UPDATE [identity].[Roles] SET Description = N'مدير النظام المسؤول عن إدارة المستخدمين والإعدادات' WHERE NormalizedName = 'TENANT ADMIN';
            UPDATE [identity].[Roles] SET Description = N'مدير المشتريات المسؤول عن إدارة عمليات الشراء والمنافسات' WHERE NormalizedName = 'PROCUREMENT MANAGER';
            UPDATE [identity].[Roles] SET Description = N'مستعرض بصلاحيات القراءة فقط لعرض البيانات دون تعديل' WHERE NormalizedName = 'VIEWER';
        ");

        // ═══════════════════════════════════════════════════════════════
        //  Step 3: Assign Default Permissions to System Roles
        // ═══════════════════════════════════════════════════════════════

        // Super Admin gets ALL permissions
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[RolePermissions] (Id, RoleId, PermissionId, CreatedAt)
            SELECT NEWID(), r.Id, p.Id, GETUTCDATE()
            FROM [identity].[Roles] r
            CROSS JOIN [identity].[Permissions] p
            WHERE r.NormalizedName = 'SUPER ADMIN'
            AND NOT EXISTS (
                SELECT 1 FROM [identity].[RolePermissions] rp
                WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
            );
        ");

        // Tenant Owner gets all except impersonate and AI configure
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[RolePermissions] (Id, RoleId, PermissionId, CreatedAt)
            SELECT NEWID(), r.Id, p.Id, GETUTCDATE()
            FROM [identity].[Roles] r
            CROSS JOIN [identity].[Permissions] p
            WHERE r.NormalizedName = 'TENANT OWNER'
            AND p.Code NOT IN ('users.impersonate', 'ai.configure')
            AND NOT EXISTS (
                SELECT 1 FROM [identity].[RolePermissions] rp
                WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
            );
        ");

        // Tenant Admin gets user/role/settings management + dashboard + reports
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[RolePermissions] (Id, RoleId, PermissionId, CreatedAt)
            SELECT NEWID(), r.Id, p.Id, GETUTCDATE()
            FROM [identity].[Roles] r
            CROSS JOIN [identity].[Permissions] p
            WHERE r.NormalizedName = 'TENANT ADMIN'
            AND p.Code IN (
                'dashboard.view', 'dashboard.export',
                'users.view', 'users.create', 'users.edit', 'users.delete', 'users.manage_roles',
                'roles.view', 'roles.create', 'roles.edit', 'roles.delete',
                'settings.view', 'settings.edit', 'settings.branding', 'settings.smtp',
                'reports.view', 'reports.export', 'reports.create',
                'audit.view', 'audit.export',
                'ai.use_assistant',
                'rfp.view', 'committees.view', 'competitions.view',
                'evaluation.view', 'offers.view', 'inquiries.view',
                'knowledge.view', 'workflow.view'
            )
            AND NOT EXISTS (
                SELECT 1 FROM [identity].[RolePermissions] rp
                WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
            );
        ");

        // Procurement Manager gets RFP + competitions + committees + evaluation + offers + inquiries
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[RolePermissions] (Id, RoleId, PermissionId, CreatedAt)
            SELECT NEWID(), r.Id, p.Id, GETUTCDATE()
            FROM [identity].[Roles] r
            CROSS JOIN [identity].[Permissions] p
            WHERE r.NormalizedName = 'PROCUREMENT MANAGER'
            AND p.Code IN (
                'dashboard.view',
                'rfp.view', 'rfp.create', 'rfp.edit', 'rfp.approve', 'rfp.publish', 'rfp.export',
                'competitions.view', 'competitions.create', 'competitions.edit', 'competitions.manage_phases',
                'committees.view', 'committees.create', 'committees.edit', 'committees.manage_members',
                'evaluation.view', 'evaluation.approve', 'evaluation.export',
                'offers.view', 'offers.open', 'offers.review',
                'inquiries.view', 'inquiries.respond', 'inquiries.manage',
                'knowledge.view', 'knowledge.upload',
                'ai.use_assistant',
                'reports.view', 'reports.export',
                'workflow.view', 'workflow.approve'
            )
            AND NOT EXISTS (
                SELECT 1 FROM [identity].[RolePermissions] rp
                WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
            );
        ");

        // Committee Chair gets evaluation + offers + committees view
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[RolePermissions] (Id, RoleId, PermissionId, CreatedAt)
            SELECT NEWID(), r.Id, p.Id, GETUTCDATE()
            FROM [identity].[Roles] r
            CROSS JOIN [identity].[Permissions] p
            WHERE r.NormalizedName = 'COMMITTEE CHAIR'
            AND p.Code IN (
                'dashboard.view',
                'rfp.view',
                'committees.view',
                'evaluation.view', 'evaluation.technical_score', 'evaluation.financial_score', 'evaluation.approve', 'evaluation.export',
                'offers.view', 'offers.open', 'offers.review',
                'inquiries.view', 'inquiries.respond',
                'knowledge.view',
                'ai.use_assistant',
                'reports.view',
                'workflow.view', 'workflow.approve'
            )
            AND NOT EXISTS (
                SELECT 1 FROM [identity].[RolePermissions] rp
                WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
            );
        ");

        // Committee Member gets evaluation scoring + offers view
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[RolePermissions] (Id, RoleId, PermissionId, CreatedAt)
            SELECT NEWID(), r.Id, p.Id, GETUTCDATE()
            FROM [identity].[Roles] r
            CROSS JOIN [identity].[Permissions] p
            WHERE r.NormalizedName = 'COMMITTEE MEMBER'
            AND p.Code IN (
                'dashboard.view',
                'rfp.view',
                'committees.view',
                'evaluation.view', 'evaluation.technical_score', 'evaluation.financial_score',
                'offers.view', 'offers.review',
                'inquiries.view',
                'knowledge.view',
                'ai.use_assistant',
                'reports.view',
                'workflow.view'
            )
            AND NOT EXISTS (
                SELECT 1 FROM [identity].[RolePermissions] rp
                WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
            );
        ");

        // Viewer gets read-only access
        migrationBuilder.Sql(@"
            INSERT INTO [identity].[RolePermissions] (Id, RoleId, PermissionId, CreatedAt)
            SELECT NEWID(), r.Id, p.Id, GETUTCDATE()
            FROM [identity].[Roles] r
            CROSS JOIN [identity].[Permissions] p
            WHERE r.NormalizedName = 'VIEWER'
            AND p.Code IN (
                'dashboard.view',
                'rfp.view',
                'committees.view',
                'competitions.view',
                'evaluation.view',
                'offers.view',
                'inquiries.view',
                'knowledge.view',
                'reports.view',
                'workflow.view'
            )
            AND NOT EXISTS (
                SELECT 1 FROM [identity].[RolePermissions] rp
                WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
            );
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Remove all seeded role permissions
        migrationBuilder.Sql(@"DELETE FROM [identity].[RolePermissions];");

        // Remove all seeded permissions
        migrationBuilder.Sql(@"DELETE FROM [identity].[Permissions];");

        // Revert role descriptions to English
        migrationBuilder.Sql(@"
            UPDATE [identity].[Roles] SET Description = 'Committee chair role' WHERE NormalizedName = 'COMMITTEE CHAIR';
            UPDATE [identity].[Roles] SET Description = 'Committee member role' WHERE NormalizedName = 'COMMITTEE MEMBER';
            UPDATE [identity].[Roles] SET Description = 'Tenant owner role' WHERE NormalizedName = 'TENANT OWNER';
            UPDATE [identity].[Roles] SET Description = 'Full system access' WHERE NormalizedName = 'SUPER ADMIN';
            UPDATE [identity].[Roles] SET Description = 'Tenant admin role' WHERE NormalizedName = 'TENANT ADMIN';
            UPDATE [identity].[Roles] SET Description = 'Procurement manager role' WHERE NormalizedName = 'PROCUREMENT MANAGER';
            UPDATE [identity].[Roles] SET Description = 'Viewer role' WHERE NormalizedName = 'VIEWER';
        ");
    }
}
