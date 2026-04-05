/**
 * usePermissions composable
 *
 * Provides a centralized, reusable API for checking user permissions
 * across all components and views. Integrates with the auth store
 * and the new flexible permission matrix.
 *
 * Usage:
 *   const { can, canAny, canAll, isAdmin, isOwner } = usePermissions()
 *   if (can('rfp.create')) { ... }
 *   v-if="can('users.manage')"
 */
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'

/* ── Permission Code Constants ── */
export const PERMISSIONS = {
  // Organization
  ORG_VIEW: 'organization.view',
  ORG_UPDATE: 'organization.update',
  ORG_BRANDING: 'organization.branding',

  // Users
  USERS_VIEW: 'users.view',
  USERS_CREATE: 'users.create',
  USERS_UPDATE: 'users.update',
  USERS_DELETE: 'users.delete',
  USERS_INVITE: 'users.invite',
  USERS_MANAGE: 'users.manage',

  // Roles
  ROLES_VIEW: 'roles.view',
  ROLES_CREATE: 'roles.create',
  ROLES_UPDATE: 'roles.update',
  ROLES_DELETE: 'roles.delete',
  ROLES_MANAGE: 'roles.manage',

  // Permission Matrix
  MATRIX_VIEW: 'matrix.view',
  MATRIX_UPDATE: 'matrix.update',
  MATRIX_MANAGE: 'matrix.manage',

  // RFP / Competition
  RFP_VIEW: 'rfp.view',
  RFP_CREATE: 'rfp.create',
  RFP_UPDATE: 'rfp.update',
  RFP_DELETE: 'rfp.delete',
  RFP_APPROVE: 'rfp.approve',
  RFP_SUBMIT: 'rfp.submit',
  RFP_PUBLISH: 'rfp.publish',

  // Committees
  COMMITTEES_VIEW: 'committees.view',
  COMMITTEES_CREATE: 'committees.create',
  COMMITTEES_UPDATE: 'committees.update',
  COMMITTEES_DELETE: 'committees.delete',
  COMMITTEES_MANAGE: 'committees.manage',

  // Offers
  OFFERS_VIEW: 'offers.view',
  OFFERS_CREATE: 'offers.create',
  OFFERS_EVALUATE: 'offers.evaluate',
  OFFERS_APPROVE: 'offers.approve',

  // Evaluation
  EVALUATION_VIEW: 'evaluation.view',
  EVALUATION_TECHNICAL: 'evaluation.technical',
  EVALUATION_FINANCIAL: 'evaluation.financial',
  EVALUATION_APPROVE: 'evaluation.approve',

  // Approvals
  APPROVALS_VIEW: 'approvals.view',
  APPROVALS_APPROVE: 'approvals.approve',
  APPROVALS_REJECT: 'approvals.reject',

  // Inquiries
  INQUIRIES_VIEW: 'inquiries.view',
  INQUIRIES_CREATE: 'inquiries.create',
  INQUIRIES_RESPOND: 'inquiries.respond',

  // Workflow
  WORKFLOW_VIEW: 'workflow.view',
  WORKFLOW_CREATE: 'workflow.create',
  WORKFLOW_UPDATE: 'workflow.update',
  WORKFLOW_DELETE: 'workflow.delete',
  WORKFLOW_MANAGE: 'workflow.manage',

  // Task Center
  TASKS_VIEW: 'tasks.view',
  TASKS_CREATE: 'tasks.create',
  TASKS_UPDATE: 'tasks.update',
  TASKS_ASSIGN: 'tasks.assign',

  // AI Assistant
  AI_VIEW: 'ai.view',
  AI_USE: 'ai.use',
  AI_CONFIGURE: 'ai.configure',

  // Reports
  REPORTS_VIEW: 'reports.view',
  REPORTS_EXPORT: 'reports.export',

  // Knowledge Base
  KB_VIEW: 'knowledgebase.view',
  KB_MANAGE: 'knowledgebase.manage',

  // Templates
  TEMPLATES_VIEW: 'templates.view',
  TEMPLATES_CREATE: 'templates.create',
  TEMPLATES_UPDATE: 'templates.update',
  TEMPLATES_DELETE: 'templates.delete',

  // Notifications
  NOTIFICATIONS_VIEW: 'notifications.view',
  NOTIFICATIONS_MANAGE: 'notifications.manage',

  // Audit
  AUDIT_VIEW: 'audit.view',

  // Purchase Orders
  PO_VIEW: 'purchaseorders.view',
  PO_CREATE: 'purchaseorders.create',
  PO_APPROVE: 'purchaseorders.approve',

  // Guarantees
  GUARANTEES_VIEW: 'guarantees.view',
  GUARANTEES_CREATE: 'guarantees.create',
  GUARANTEES_APPROVE: 'guarantees.approve',

  // Contracts
  CONTRACTS_VIEW: 'contracts.view',
  CONTRACTS_CREATE: 'contracts.create',
  CONTRACTS_APPROVE: 'contracts.approve',
  CONTRACTS_SIGN: 'contracts.sign',
} as const

/* ── System Role Constants ── */
export const SYSTEM_ROLES = {
  OWNER: 'Tenant Owner',
  OWNER_ALT: 'Owner',
  PRIMARY_ADMIN: 'Tenant Primary Admin',
  ADMIN: 'Super Admin',
  ADMIN_ALT: 'System Administrator',
  SECTOR_REP: 'Sector Representative',
  FINANCIAL_CONTROLLER: 'Financial Controller',
  AUDITOR: 'Auditor',
  COMMITTEE_MEMBER: 'Committee Member',
  VIEWER: 'Viewer',
} as const

/* ── Operator Role Aliases (these users must NOT access tenant features) ── */
const OPERATOR_ROLE_ALIASES = [
  'OperatorPrimaryAdmin',
  'Operator Super Admin',
  'OperatorSuperAdmin',
  'SuperAdmin',
] as const

/* ── Route Permission Map ── */
export const ROUTE_PERMISSIONS: Record<string, string | string[]> = {
  // Settings
  'settings-organization': PERMISSIONS.ORG_VIEW,
  'settings-users': PERMISSIONS.USERS_VIEW,
  'settings-roles': PERMISSIONS.ROLES_VIEW,
  'settings-permissions-matrix': [PERMISSIONS.MATRIX_VIEW, PERMISSIONS.ROLES_MANAGE],

  // RFP / Competition
  'rfp-list': PERMISSIONS.RFP_VIEW,
  'rfp-new': PERMISSIONS.RFP_CREATE,
  'rfp-create': PERMISSIONS.RFP_CREATE,
  'rfp-edit': PERMISSIONS.RFP_UPDATE,

  // Committees
  'committees-permanent': PERMISSIONS.COMMITTEES_VIEW,
  'committees-temporary': PERMISSIONS.COMMITTEES_VIEW,

  // Offers
  'supplier-offers': PERMISSIONS.OFFERS_VIEW,
  'supplier-offer-detail': PERMISSIONS.OFFERS_VIEW,

  // Evaluation
  'technical-evaluation': PERMISSIONS.EVALUATION_VIEW,
  'technical-evaluation-detail': PERMISSIONS.EVALUATION_TECHNICAL,
  'technical-comparison': PERMISSIONS.EVALUATION_TECHNICAL,
  'financial-evaluation': PERMISSIONS.EVALUATION_VIEW,
  'financial-evaluation-detail': PERMISSIONS.EVALUATION_FINANCIAL,
  'financial-comparison': PERMISSIONS.EVALUATION_FINANCIAL,
  'comprehensive-evaluation': PERMISSIONS.EVALUATION_VIEW,
  'comprehensive-evaluation-detail': PERMISSIONS.EVALUATION_VIEW,

  // Approvals
  'approvals': PERMISSIONS.APPROVALS_VIEW,

  // Inquiries
  'inquiries': PERMISSIONS.INQUIRIES_VIEW,

  // Reports
  'reports': PERMISSIONS.REPORTS_VIEW,
  'export-reports': PERMISSIONS.REPORTS_EXPORT,

  // AI
  'ai-assistant': PERMISSIONS.AI_VIEW,

  // Workflow
  'workflow-templates': PERMISSIONS.WORKFLOW_VIEW,
  'workflow-designer': PERMISSIONS.WORKFLOW_CREATE,
  'workflow-edit': PERMISSIONS.WORKFLOW_UPDATE,

  // Task Center
  'task-center': PERMISSIONS.TASKS_VIEW,

  // Knowledge Base
  'knowledge-base': PERMISSIONS.KB_VIEW,

  // Templates
  'competition-templates': PERMISSIONS.TEMPLATES_VIEW,
  'booklet-templates': PERMISSIONS.TEMPLATES_VIEW,
  'booklet-editor': PERMISSIONS.TEMPLATES_CREATE,

  // Notifications
  'notifications': PERMISSIONS.NOTIFICATIONS_VIEW,

  // Purchase Orders
  'purchase-orders': PERMISSIONS.PO_VIEW,
  'purchase-order-create': PERMISSIONS.PO_CREATE,
  'purchase-order-detail': PERMISSIONS.PO_VIEW,
}

/* ── Composable ── */
export function usePermissions() {
  const authStore = useAuthStore()

  /**
   * Check if the current user is an Operator Admin.
   * Operator admins must NOT have access to tenant-specific features
   * (RFP, committees, evaluation, knowledge base, task center, etc.).
   * They can only access the operator panel.
   */
  const isOperatorAdmin = computed(() =>
    authStore.userRoles.some(r => (OPERATOR_ROLE_ALIASES as readonly string[]).includes(r))
  )

  /**
   * Check if the current user has a specific permission.
   * Owners and Tenant Admins bypass all permission checks.
   * Operator Admins do NOT get tenant permission bypass.
   */
  function can(permission: string): boolean {
    if (isOperatorAdmin.value) return false
    if (isOwner.value || isAdmin.value) return true
    return authStore.hasPermission(permission)
  }

  /**
   * Check if the current user has ANY of the specified permissions.
   */
  function canAny(permissions: string[]): boolean {
    if (isOperatorAdmin.value) return false
    if (isOwner.value || isAdmin.value) return true
    return permissions.some(p => authStore.hasPermission(p))
  }

  /**
   * Check if the current user has ALL of the specified permissions.
   */
  function canAll(permissions: string[]): boolean {
    if (isOperatorAdmin.value) return false
    if (isOwner.value || isAdmin.value) return true
    return permissions.every(p => authStore.hasPermission(p))
  }

  /**
   * Check if the user can access a specific route by name.
   */
  function canAccessRoute(routeName: string): boolean {
    if (isOperatorAdmin.value) return false
    if (isOwner.value || isAdmin.value) return true
    const required = ROUTE_PERMISSIONS[routeName]
    if (!required) return true // No permission required
    if (Array.isArray(required)) return canAny(required)
    return can(required)
  }

  const isOwner = computed(() =>
    authStore.hasRole(SYSTEM_ROLES.OWNER) ||
    authStore.hasRole(SYSTEM_ROLES.OWNER_ALT) ||
    authStore.hasRole(SYSTEM_ROLES.PRIMARY_ADMIN)
  )
  const isAdmin = computed(() => {
    // Operator admins are NOT tenant admins - they should not bypass tenant permissions
    if (isOperatorAdmin.value) return false
    return authStore.hasRole(SYSTEM_ROLES.ADMIN) ||
      authStore.hasRole(SYSTEM_ROLES.ADMIN_ALT) ||
      isOwner.value
  })
  const isSectorRep = computed(() => authStore.hasRole(SYSTEM_ROLES.SECTOR_REP))
  const isFinancialController = computed(() => authStore.hasRole(SYSTEM_ROLES.FINANCIAL_CONTROLLER))
  const isAuditor = computed(() => authStore.hasRole(SYSTEM_ROLES.AUDITOR))
  const isCommitteeMember = computed(() => authStore.hasRole(SYSTEM_ROLES.COMMITTEE_MEMBER))
  const isViewer = computed(() => authStore.hasRole(SYSTEM_ROLES.VIEWER))

  const userPermissions = computed(() => authStore.userPermissions)
  const userRoles = computed(() => authStore.userRoles)

  return {
    // Permission checks
    can,
    canAny,
    canAll,
    canAccessRoute,

    // Role checks
    isOperatorAdmin,
    isOwner,
    isAdmin,
    isSectorRep,
    isFinancialController,
    isAuditor,
    isCommitteeMember,
    isViewer,

    // Raw data
    userPermissions,
    userRoles,

    // Constants
    PERMISSIONS,
    SYSTEM_ROLES,
  }
}
