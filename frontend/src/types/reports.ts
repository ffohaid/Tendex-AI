/**
 * Report type definitions for Tendex AI Platform.
 * Covers report summaries, chart data, and filter options.
 */

/* ──────────────────────────────────────────────
 * Report Summary KPIs
 * ──────────────────────────────────────────── */
export interface ReportSummary {
  totalCompetitions: number
  totalOffers: number
  averageCycleTimeDays: number
  complianceRate: number
  totalBudget: number
  completedCompetitions: number
  cancelledCompetitions: number
  averageOffersPerCompetition: number
}

/* ──────────────────────────────────────────────
 * Monthly Trend Data
 * ──────────────────────────────────────────── */
export interface MonthlyTrendItem {
  month: string
  competitions: number
  offers: number
  budget: number
}

/* ──────────────────────────────────────────────
 * Status Distribution
 * ──────────────────────────────────────────── */
export interface StatusDistributionItem {
  status: string
  count: number
  percentage: number
}

/* ──────────────────────────────────────────────
 * Department Performance
 * ──────────────────────────────────────────── */
export interface DepartmentPerformance {
  departmentNameAr: string
  departmentNameEn: string
  competitionsCount: number
  averageCycleTimeDays: number
  complianceRate: number
}

/* ──────────────────────────────────────────────
 * Report Filters
 * ──────────────────────────────────────────── */
export interface ReportFilters {
  dateFrom: string | null
  dateTo: string | null
  status: string | null
  department: string | null
}

/* ──────────────────────────────────────────────
 * Full Report Data
 * ──────────────────────────────────────────── */
export interface ReportData {
  summary: ReportSummary
  monthlyTrends: MonthlyTrendItem[]
  statusDistribution: StatusDistributionItem[]
  departmentPerformance: DepartmentPerformance[]
}
