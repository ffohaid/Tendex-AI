/**
 * TypeScript type definitions for the Operator (Super Admin) Dashboard.
 *
 * These types mirror the backend DTOs from
 * TendexAI.Application.Features.OperatorDashboard.Dtos.
 *
 * All data is fetched dynamically from the API — NO mock data.
 */

/* ------------------------------------------------------------------ */
/*  Dashboard Summary (KPIs)                                           */
/* ------------------------------------------------------------------ */

/** Aggregated summary for the operator dashboard. */
export interface OperatorDashboardSummaryDto {
  totalTenants: number
  activeTenants: number
  suspendedTenants: number
  pendingProvisioningTenants: number
  renewalWindowTenants: number
  cancelledTenants: number
  archivedTenants: number
  totalActiveSubscriptions: number
  totalFeatureFlags: number
  totalAiConfigurations: number
  totalAuditLogEntries: number
  totalImpersonationSessions: number
  tenantStatusDistribution: TenantStatusDistributionDto[]
  monthlyTenantRegistrations: MonthlyTrendDto[]
  expiringSubscriptions: ExpiringSubscriptionDto[]
}

/** Tenant count per lifecycle status. */
export interface TenantStatusDistributionDto {
  statusName: string
  statusValue: number
  count: number
}

/** Monthly trend data point. */
export interface MonthlyTrendDto {
  month: string
  count: number
}

/** Subscription approaching expiry. */
export interface ExpiringSubscriptionDto {
  tenantId: string
  tenantNameAr: string
  tenantNameEn: string
  tenantIdentifier: string
  subscriptionExpiresAt: string | null
  daysUntilExpiry: number
}

/* ------------------------------------------------------------------ */
/*  Tenant Usage Statistics                                            */
/* ------------------------------------------------------------------ */

/** Per-tenant usage statistics. */
export interface TenantUsageStatisticsDto {
  tenantId: string
  tenantNameAr: string
  tenantNameEn: string
  tenantIdentifier: string
  statusName: string
  activeFeatureFlags: number
  totalFeatureFlags: number
  aiConfigurationsCount: number
  auditLogEntriesCount: number
  subscriptionExpiresAt: string | null
  createdAt: string
}

/* ------------------------------------------------------------------ */
/*  System Health                                                      */
/* ------------------------------------------------------------------ */

/** System health status. */
export interface SystemHealthStatusDto {
  overallStatus: string
  checkedAt: string
  services: ServiceHealthDto[]
}

/** Individual service health. */
export interface ServiceHealthDto {
  serviceName: string
  status: string
  description: string | null
  responseTimeMs: number
}

/* ------------------------------------------------------------------ */
/*  Resource Consumption Trends                                        */
/* ------------------------------------------------------------------ */

/** Resource consumption trends. */
export interface ResourceConsumptionTrendsDto {
  dailyAuditLogEntries: DailyCountDto[]
  dailyNewTenants: DailyCountDto[]
  featureAdoptionRates: FeatureAdoptionDto[]
  aiProviderUsage: AiProviderUsageDto[]
}

/** Daily count data point. */
export interface DailyCountDto {
  date: string
  count: number
}

/** Feature adoption rate. */
export interface FeatureAdoptionDto {
  featureKey: string
  featureNameAr: string
  featureNameEn: string
  enabledCount: number
  totalTenants: number
  adoptionRate: number
}

/** AI provider usage distribution. */
export interface AiProviderUsageDto {
  providerName: string
  configurationsCount: number
  activeCount: number
}

/* ------------------------------------------------------------------ */
/*  Paginated Response                                                 */
/* ------------------------------------------------------------------ */

/** Paginated result wrapper (reused from tenant types). */
export interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

/* ------------------------------------------------------------------ */
/*  Query Parameters                                                   */
/* ------------------------------------------------------------------ */

/** Query parameters for tenant usage statistics endpoint. */
export interface TenantUsageParams {
  page?: number
  pageSize?: number
  search?: string
}
