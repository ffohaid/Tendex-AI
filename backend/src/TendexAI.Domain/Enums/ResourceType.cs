namespace TendexAI.Domain.Enums;

/// <summary>
/// Defines the types of resources that can be controlled by the permission matrix.
/// Used as the second dimension of the flexible permission matrix.
/// Each resource type maps to a specific module or entity in the system.
/// </summary>
public enum ResourceType
{
    // ═══════════════════════════════════════════════════════════════
    //  Global Resources (ResourceScope.Global)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Organization settings and branding.</summary>
    Organization = 1,

    /// <summary>User accounts management.</summary>
    Users = 2,

    /// <summary>Roles and permissions management.</summary>
    Roles = 3,

    /// <summary>Permission matrix management (meta-permission).</summary>
    PermissionMatrix = 4,

    /// <summary>Dashboard and analytics.</summary>
    Dashboard = 5,

    /// <summary>Reports generation and export.</summary>
    Reports = 6,

    /// <summary>AI assistant usage.</summary>
    AiAssistant = 7,

    /// <summary>AI configuration and settings.</summary>
    AiConfiguration = 8,

    /// <summary>Knowledge base and documents.</summary>
    KnowledgeBase = 9,

    /// <summary>Workflow engine definitions.</summary>
    WorkflowDefinitions = 10,

    /// <summary>Audit logs and trail.</summary>
    AuditLogs = 11,

    /// <summary>System settings and features.</summary>
    SystemSettings = 12,

    /// <summary>User invitations.</summary>
    Invitations = 13,

    /// <summary>Notifications management.</summary>
    Notifications = 14,

    // ═══════════════════════════════════════════════════════════════
    //  Competition Resources (ResourceScope.Competition)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Competition (RFP) entity itself.</summary>
    Competition = 100,

    /// <summary>Booklet (terms and specifications).</summary>
    Booklet = 101,

    /// <summary>RFP sections and content.</summary>
    RfpSections = 102,

    /// <summary>Bill of Quantities items.</summary>
    BoqItems = 103,

    /// <summary>Evaluation criteria.</summary>
    EvaluationCriteria = 104,

    /// <summary>Supplier offers.</summary>
    Offers = 105,

    /// <summary>Technical evaluation.</summary>
    TechnicalEvaluation = 106,

    /// <summary>Financial evaluation.</summary>
    FinancialEvaluation = 107,

    /// <summary>Award recommendation.</summary>
    AwardRecommendation = 108,

    /// <summary>Contracts.</summary>
    Contracts = 109,

    /// <summary>Inquiries from suppliers.</summary>
    Inquiries = 110,

    /// <summary>Guarantees and bonds.</summary>
    Guarantees = 111,

    /// <summary>Grievances and appeals.</summary>
    Grievances = 112,

    /// <summary>Evaluation minutes and reports.</summary>
    EvaluationMinutes = 113,

    /// <summary>RFP attachments.</summary>
    Attachments = 114,

    // ═══════════════════════════════════════════════════════════════
    //  Committee Resources (ResourceScope.Committee)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Committee entity management.</summary>
    Committee = 200,

    /// <summary>Committee members management.</summary>
    CommitteeMembers = 201,

    /// <summary>Committee meetings and minutes.</summary>
    CommitteeMeetings = 202,

    /// <summary>Committee tasks and assignments.</summary>
    CommitteeTasks = 203,

    // ═══════════════════════════════════════════════════════════════
    //  Task Center Resources
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Task center and task management.</summary>
    Tasks = 300,

    /// <summary>Approval tasks.</summary>
    ApprovalTasks = 301
}
