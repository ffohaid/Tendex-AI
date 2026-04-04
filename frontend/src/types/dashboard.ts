/**
 * Dashboard type definitions for Tendex AI Platform.
 * All interfaces follow the PRD v6.1 specifications for
 * the main dashboard (Section 15.1), task center (Section 13.1),
 * and notification system (Section 13.2).
 */

/* ──────────────────────────────────────────────
 * Quick Statistics (KPI Cards)
 * ──────────────────────────────────────────── */
export interface DashboardStats {
  activeCompetitions: number
  completedCompetitions: number
  pendingEvaluations: number
  pendingTasks: number
  totalOffers: number
  complianceRate: number
}

/* ──────────────────────────────────────────────
 * Competition (Active Competitions List)
 * ──────────────────────────────────────────── */
export type CompetitionStatus =
  | 'draft'
  | 'pending_approval'
  | 'approved'
  | 'published'
  | 'receiving_offers'
  | 'technical_evaluation'
  | 'financial_evaluation'
  | 'awarding'
  | 'completed'
  | 'cancelled'

export interface CompetitionPhase {
  id: number
  nameAr: string
  nameEn: string
  status: 'pending' | 'in_progress' | 'completed' | 'delayed'
  startDate: string | null
  endDate: string | null
  slaDeadline: string | null
}

export interface ActiveCompetition {
  id: string
  referenceNumber: string
  titleAr: string
  titleEn: string
  status: CompetitionStatus
  currentPhase: CompetitionPhase
  estimatedBudget: number
  offersCount: number
  createdAt: string
  deadline: string
  progress: number
  isDelayed: boolean
}

/* ──────────────────────────────────────────────
 * Pending Tasks (Unified Task Center)
 * ──────────────────────────────────────────── */
export type TaskType =
  | 'review_booklet'
  | 'evaluate_offer'
  | 'answer_inquiry'
  | 'approve_request'
  | 'prepare_minutes'
  | 'committee_action'

export type TaskPriority = 'low' | 'medium' | 'high' | 'critical'

export type SlaStatus = 'on_track' | 'approaching' | 'exceeded'

export interface PendingTask {
  id: string
  type: TaskType
  titleAr: string
  titleEn: string
  competitionTitleAr: string
  competitionTitleEn: string
  competitionReferenceNumber: string
  assignedAt: string
  slaDeadline: string
  slaStatus: SlaStatus
  remainingTimeSeconds: number
  priority: TaskPriority
  actionRequired: string
  actionUrl: string
}

/* ──────────────────────────────────────────────
 * Notifications
 * ──────────────────────────────────────────── */
export type NotificationChannel = 'in_app' | 'email' | 'both'

export interface Notification {
  id: string
  titleAr: string
  titleEn: string
  bodyAr: string
  bodyEn: string
  channel: NotificationChannel
  isRead: boolean
  createdAt: string
  actionUrl: string | null
  type: string
}

/* ──────────────────────────────────────────────
 * Active Committees
 * ──────────────────────────────────────────── */
export type CommitteeType = 'permanent' | 'temporary'
export type CommitteeStatus = 'active' | 'expired' | 'suspended'

export interface ActiveCommittee {
  id: string
  nameAr: string
  nameEn: string
  type: CommitteeType
  status: CommitteeStatus
  membersCount: number
  expiryDate: string | null
  competitionTitleAr: string | null
  competitionTitleEn: string | null
}

/* ──────────────────────────────────────────────
 * Recent Activity Log
 * ──────────────────────────────────────────── */
export interface RecentActivity {
  id: string
  actionAr: string
  actionEn: string
  userNameAr: string
  userNameEn: string
  timestamp: string
  entityType: string
  entityId: string
}

/* ──────────────────────────────────────────────
 * Performance Metrics (Charts)
 * ──────────────────────────────────────────── */
export interface MonthlyCompetitionData {
  month: string
  count: number
}

export interface CompetitionStatusDistribution {
  status: CompetitionStatus
  count: number
}

export interface PerformanceMetrics {
  averageCycleTimeDays: number
  complianceRate: number
  monthlyCompetitions: MonthlyCompetitionData[]
  statusDistribution: CompetitionStatusDistribution[]
  averageEvaluationTimeDays: number
  slaComplianceRate: number
}

/* ──────────────────────────────────────────────
 * Dashboard API Response (Aggregated)
 * ──────────────────────────────────────────── */
export interface DashboardData {
  stats: DashboardStats
  activeCompetitions: ActiveCompetition[]
  pendingTasks: PendingTask[]
  notifications: Notification[]
  activeCommittees: ActiveCommittee[]
  recentActivities: RecentActivity[]
  performanceMetrics: PerformanceMetrics
}

/* ──────────────────────────────────────────────
 * User Role (for role-based dashboard rendering)
 * ──────────────────────────────────────────── */
export type UserRole =
  | 'procurement_manager'
  | 'system_supervisor'
  | 'department_manager'
  | 'committee_member'
  | 'committee_chair'
  | 'financial_controller'
  | 'sector_representative'

export interface DashboardConfig {
  role: UserRole
  showCompetitions: boolean
  showTasks: boolean
  showCommittees: boolean
  showPerformanceMetrics: boolean
  showRecentActivity: boolean
  showNotifications: boolean
}
