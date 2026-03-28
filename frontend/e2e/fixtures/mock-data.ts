/**
 * Mock data fixtures for E2E tests.
 *
 * These fixtures provide consistent API responses for Playwright route
 * interception, ensuring tests are deterministic and backend-independent.
 *
 * IMPORTANT: These are test fixtures only — NOT production mock data.
 * All production data MUST come from real APIs per project rules.
 */

/* ------------------------------------------------------------------ */
/*  Auth Fixtures                                                      */
/* ------------------------------------------------------------------ */

export const VALID_USER = {
  email: 'admin@tendex.test',
  password: 'Test@12345678',
  tenantId: 'tenant-001',
} as const

export const AUTH_TOKEN_RESPONSE = {
  accessToken: 'eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.e2e-test-token',
  refreshToken: 'rt_e2e_test_refresh_token_abc123',
  tokenType: 'Bearer',
  expiresIn: 3600,
  sessionId: 'session-e2e-001',
  user: {
    id: 'user-001',
    email: 'admin@tendex.test',
    firstName: 'Ahmed',
    lastName: 'Al-Rashidi',
    tenantId: 'tenant-001',
    mfaEnabled: false,
    roles: ['Admin', 'CommitteeChair'],
    permissions: [
      'rfp.create',
      'rfp.edit',
      'rfp.view',
      'evaluation.technical',
      'evaluation.financial',
      'committee.manage',
    ],
  },
  mfaRequired: false,
}

export const AUTH_MFA_RESPONSE = {
  ...AUTH_TOKEN_RESPONSE,
  mfaRequired: true,
  accessToken: '',
  refreshToken: '',
}

export const MFA_VERIFY_RESPONSE = {
  success: true,
  tokenResponse: AUTH_TOKEN_RESPONSE,
}

export const OPERATOR_USER = {
  email: 'operator@tendex.test',
  password: 'Operator@12345678',
  tenantId: 'system',
}

export const OPERATOR_TOKEN_RESPONSE = {
  ...AUTH_TOKEN_RESPONSE,
  user: {
    ...AUTH_TOKEN_RESPONSE.user,
    id: 'user-operator-001',
    email: 'operator@tendex.test',
    firstName: 'Fahad',
    lastName: 'Al-Otaibi',
    tenantId: 'system',
    roles: ['SuperAdmin'],
    permissions: [
      'operator.dashboard',
      'operator.tenants',
      'operator.impersonation',
      'operator.purchase-orders',
    ],
  },
}

/* ------------------------------------------------------------------ */
/*  Dashboard Fixtures                                                 */
/* ------------------------------------------------------------------ */

export const DASHBOARD_STATS = {
  activeCompetitions: 12,
  completedCompetitions: 45,
  pendingEvaluations: 8,
  pendingTasks: 5,
  totalOffers: 156,
  complianceRate: 94.5,
}

export const DASHBOARD_COMPETITIONS = [
  {
    id: 'comp-001',
    referenceNumber: 'RFP-2026-001',
    projectName: 'Digital Transformation Project',
    projectNameAr: 'مشروع التحول الرقمي',
    status: 'technical_evaluation',
    offersCount: 5,
    estimatedValue: 2500000,
    submissionDeadline: '2026-04-15T23:59:59Z',
  },
  {
    id: 'comp-002',
    referenceNumber: 'RFP-2026-002',
    projectName: 'Cybersecurity Enhancement',
    projectNameAr: 'تعزيز الأمن السيبراني',
    status: 'receiving_offers',
    offersCount: 3,
    estimatedValue: 1800000,
    submissionDeadline: '2026-05-01T23:59:59Z',
  },
]

export const DASHBOARD_TASKS = [
  {
    id: 'task-001',
    title: 'Review Technical Evaluation',
    titleAr: 'مراجعة التقييم الفني',
    priority: 'high',
    dueDate: '2026-03-30T17:00:00Z',
    slaStatus: 'approaching',
    competitionRef: 'RFP-2026-001',
  },
]

/* ------------------------------------------------------------------ */
/*  RFP Fixtures                                                       */
/* ------------------------------------------------------------------ */

export const RFP_LIST = [
  {
    id: 'rfp-001',
    referenceNumber: 'RFP-2026-001',
    projectName: 'Digital Transformation Project',
    projectNameAr: 'مشروع التحول الرقمي',
    status: 'draft',
    estimatedValue: 2500000,
    createdAt: '2026-03-01T10:00:00Z',
    updatedAt: '2026-03-25T14:30:00Z',
  },
  {
    id: 'rfp-002',
    referenceNumber: 'RFP-2026-002',
    projectName: 'Cybersecurity Enhancement',
    projectNameAr: 'تعزيز الأمن السيبراني',
    status: 'published',
    estimatedValue: 1800000,
    createdAt: '2026-03-10T08:00:00Z',
    updatedAt: '2026-03-20T16:00:00Z',
  },
]

export const RFP_CREATE_RESPONSE = {
  id: 'rfp-new-001',
  referenceNumber: 'RFP-2026-003',
  status: 'draft',
  createdAt: '2026-03-28T10:00:00Z',
}

/* ------------------------------------------------------------------ */
/*  Evaluation Fixtures                                                */
/* ------------------------------------------------------------------ */

export const COMPETITION_EVALUATIONS = [
  {
    id: 'eval-001',
    competitionId: 'comp-001',
    referenceNumber: 'RFP-2026-001',
    projectName: 'Digital Transformation Project',
    projectNameAr: 'مشروع التحول الرقمي',
    status: 'in_progress',
    technicalWeight: 70,
    financialWeight: 30,
    vendorsCount: 5,
    evaluatedCount: 3,
  },
]

export const TECHNICAL_EVALUATION_DETAIL = {
  id: 'eval-001',
  competitionId: 'comp-001',
  referenceNumber: 'RFP-2026-001',
  projectName: 'Digital Transformation Project',
  projectNameAr: 'مشروع التحول الرقمي',
  status: 'in_progress',
  technicalWeight: 70,
  minimumScore: 60,
}

export const EVALUATION_CRITERIA = [
  {
    id: 'crit-001',
    name: 'Technical Approach',
    nameAr: 'المنهجية الفنية',
    weight: 30,
    maxScore: 100,
    description: 'Evaluation of the proposed technical approach',
  },
  {
    id: 'crit-002',
    name: 'Team Qualifications',
    nameAr: 'مؤهلات الفريق',
    weight: 25,
    maxScore: 100,
    description: 'Assessment of team experience and qualifications',
  },
  {
    id: 'crit-003',
    name: 'Project Timeline',
    nameAr: 'الجدول الزمني',
    weight: 15,
    maxScore: 100,
    description: 'Feasibility of proposed timeline',
  },
]

export const EVALUATION_VENDORS = [
  {
    id: 'vendor-001',
    name: 'TechSolutions Co.',
    nameAr: 'شركة الحلول التقنية',
    crNumber: '1010123456',
    status: 'qualified',
  },
  {
    id: 'vendor-002',
    name: 'Digital Systems Ltd.',
    nameAr: 'شركة الأنظمة الرقمية',
    crNumber: '1010654321',
    status: 'qualified',
  },
  {
    id: 'vendor-003',
    name: 'SmartIT Corp.',
    nameAr: 'شركة سمارت آي تي',
    crNumber: '1010789012',
    status: 'pending',
  },
]

export const TECHNICAL_SCORES = [
  {
    id: 'ts-001',
    vendorId: 'vendor-001',
    criterionId: 'crit-001',
    score: 85,
    justification: 'Strong technical approach with clear methodology',
    evaluatorId: 'user-001',
  },
]

export const FINANCIAL_OFFERS = [
  {
    id: 'fo-001',
    vendorId: 'vendor-001',
    totalAmount: 2300000,
    vatAmount: 345000,
    grandTotal: 2645000,
    currency: 'SAR',
    submittedAt: '2026-03-15T10:00:00Z',
  },
  {
    id: 'fo-002',
    vendorId: 'vendor-002',
    totalAmount: 2100000,
    vatAmount: 315000,
    grandTotal: 2415000,
    currency: 'SAR',
    submittedAt: '2026-03-15T11:00:00Z',
  },
]

export const COMPARISON_MATRIX = {
  type: 'technical',
  criteria: EVALUATION_CRITERIA,
  vendors: EVALUATION_VENDORS.slice(0, 2),
  scores: [
    { vendorId: 'vendor-001', criterionId: 'crit-001', score: 85, weightedScore: 25.5 },
    { vendorId: 'vendor-001', criterionId: 'crit-002', score: 90, weightedScore: 22.5 },
    { vendorId: 'vendor-001', criterionId: 'crit-003', score: 75, weightedScore: 11.25 },
    { vendorId: 'vendor-002', criterionId: 'crit-001', score: 80, weightedScore: 24.0 },
    { vendorId: 'vendor-002', criterionId: 'crit-002', score: 85, weightedScore: 21.25 },
    { vendorId: 'vendor-002', criterionId: 'crit-003', score: 80, weightedScore: 12.0 },
  ],
  totals: [
    { vendorId: 'vendor-001', totalWeightedScore: 59.25, rank: 1 },
    { vendorId: 'vendor-002', totalWeightedScore: 57.25, rank: 2 },
  ],
}

/* ------------------------------------------------------------------ */
/*  Operator Dashboard Fixtures                                        */
/* ------------------------------------------------------------------ */

export const OPERATOR_DASHBOARD_STATS = {
  totalTenants: 25,
  activeTenants: 22,
  totalUsers: 450,
  totalCompetitions: 180,
  monthlyRevenue: 125000,
  systemHealth: 'healthy',
}

export const TENANT_LIST = [
  {
    id: 'tenant-001',
    name: 'Ministry of Digital Government',
    nameAr: 'وزارة الحكومة الرقمية',
    status: 'active',
    usersCount: 45,
    competitionsCount: 12,
    subscriptionEndDate: '2027-03-01T00:00:00Z',
    plan: 'enterprise',
  },
  {
    id: 'tenant-002',
    name: 'Ministry of Finance',
    nameAr: 'وزارة المالية',
    status: 'active',
    usersCount: 30,
    competitionsCount: 8,
    subscriptionEndDate: '2026-12-31T00:00:00Z',
    plan: 'professional',
  },
]

/* ------------------------------------------------------------------ */
/*  Impersonation Fixtures                                             */
/* ------------------------------------------------------------------ */

export const IMPERSONATION_USERS_SEARCH = {
  items: [
    {
      id: 'user-target-001',
      email: 'target.user@ministry.gov.sa',
      firstName: 'Mohammed',
      lastName: 'Al-Harbi',
      tenantId: 'tenant-001',
      tenantName: 'Ministry of Digital Government',
      roles: ['CommitteeMember'],
    },
    {
      id: 'user-target-002',
      email: 'another.user@finance.gov.sa',
      firstName: 'Khalid',
      lastName: 'Al-Dosari',
      tenantId: 'tenant-002',
      tenantName: 'Ministry of Finance',
      roles: ['Admin'],
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 10,
}

export const IMPERSONATION_CONSENT_RESPONSE = {
  id: 'consent-001',
  requestedByUserId: 'user-operator-001',
  targetUserId: 'user-target-001',
  status: 'pending',
  reason: 'Support ticket #12345 - User unable to access evaluation',
  createdAt: '2026-03-28T10:00:00Z',
  expiresAt: '2026-03-28T11:00:00Z',
}

export const IMPERSONATION_SESSION_RESPONSE = {
  sessionId: 'imp-session-001',
  adminUserId: 'user-operator-001',
  targetUserId: 'user-target-001',
  targetTenantId: 'tenant-001',
  accessToken: 'eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.impersonation-token',
  startedAt: '2026-03-28T10:05:00Z',
  expiresAt: '2026-03-28T11:05:00Z',
}

export const IMPERSONATION_SESSIONS_LIST = {
  items: [
    {
      id: 'imp-session-001',
      adminUserId: 'user-operator-001',
      adminName: 'Fahad Al-Otaibi',
      targetUserId: 'user-target-001',
      targetName: 'Mohammed Al-Harbi',
      targetTenantId: 'tenant-001',
      status: 'active',
      reason: 'Support ticket #12345',
      startedAt: '2026-03-28T10:05:00Z',
      endedAt: null,
    },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 10,
}
