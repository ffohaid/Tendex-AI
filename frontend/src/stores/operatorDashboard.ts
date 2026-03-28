/**
 * Operator Dashboard Pinia Store.
 *
 * Manages state for the Super Admin operator dashboard module:
 * - Dashboard summary KPIs
 * - Tenant usage statistics with pagination
 * - System health status
 * - Resource consumption trends
 *
 * All data is fetched dynamically from the backend — NO mock data.
 */
import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import * as dashboardService from '@/services/operatorDashboardService'
import type {
  OperatorDashboardSummaryDto,
  TenantUsageStatisticsDto,
  SystemHealthStatusDto,
  ResourceConsumptionTrendsDto,
  TenantUsageParams,
} from '@/types/operatorDashboard'

export const useOperatorDashboardStore = defineStore('operatorDashboard', () => {
  /* ---------------------------------------------------------------- */
  /*  State                                                            */
  /* ---------------------------------------------------------------- */

  /** Dashboard summary */
  const summary = ref<OperatorDashboardSummaryDto | null>(null)

  /** Tenant usage statistics */
  const tenantUsageItems = ref<TenantUsageStatisticsDto[]>([])
  const tenantUsageTotalCount = ref(0)
  const tenantUsageTotalPages = ref(0)
  const tenantUsageCurrentPage = ref(1)
  const tenantUsagePageSize = ref(10)
  const tenantUsageSearch = ref('')

  /** System health */
  const systemHealth = ref<SystemHealthStatusDto | null>(null)

  /** Resource trends */
  const resourceTrends = ref<ResourceConsumptionTrendsDto | null>(null)

  /** UI state */
  const isLoadingSummary = ref(false)
  const isLoadingTenantUsage = ref(false)
  const isLoadingHealth = ref(false)
  const isLoadingTrends = ref(false)
  const error = ref<string | null>(null)

  /* ---------------------------------------------------------------- */
  /*  Getters                                                          */
  /* ---------------------------------------------------------------- */

  /** Whether any section is currently loading. */
  const isLoading = computed(
    () =>
      isLoadingSummary.value ||
      isLoadingTenantUsage.value ||
      isLoadingHealth.value ||
      isLoadingTrends.value,
  )

  /** Overall system health status label. */
  const overallHealthStatus = computed(() => systemHealth.value?.overallStatus ?? 'Unknown')

  /** Whether the system is fully healthy. */
  const isSystemHealthy = computed(() => overallHealthStatus.value === 'Healthy')

  /* ---------------------------------------------------------------- */
  /*  Actions                                                          */
  /* ---------------------------------------------------------------- */

  /**
   * Fetches the dashboard summary KPIs.
   */
  async function loadSummary(): Promise<void> {
    isLoadingSummary.value = true
    error.value = null
    try {
      summary.value = await dashboardService.fetchDashboardSummary()
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to load dashboard summary'
      error.value = message
      console.error('[OperatorDashboard] loadSummary error:', err)
    } finally {
      isLoadingSummary.value = false
    }
  }

  /**
   * Fetches tenant usage statistics with current pagination/search state.
   */
  async function loadTenantUsage(): Promise<void> {
    isLoadingTenantUsage.value = true
    error.value = null
    try {
      const params: TenantUsageParams = {
        page: tenantUsageCurrentPage.value,
        pageSize: tenantUsagePageSize.value,
        search: tenantUsageSearch.value || undefined,
      }
      const result = await dashboardService.fetchTenantUsageStatistics(params)
      tenantUsageItems.value = result.items
      tenantUsageTotalCount.value = result.totalCount
      tenantUsageTotalPages.value = result.totalPages
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to load tenant usage statistics'
      error.value = message
      console.error('[OperatorDashboard] loadTenantUsage error:', err)
    } finally {
      isLoadingTenantUsage.value = false
    }
  }

  /**
   * Fetches the system health status.
   */
  async function loadSystemHealth(): Promise<void> {
    isLoadingHealth.value = true
    error.value = null
    try {
      systemHealth.value = await dashboardService.fetchSystemHealthStatus()
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to load system health'
      error.value = message
      console.error('[OperatorDashboard] loadSystemHealth error:', err)
    } finally {
      isLoadingHealth.value = false
    }
  }

  /**
   * Fetches resource consumption trends.
   */
  async function loadResourceTrends(): Promise<void> {
    isLoadingTrends.value = true
    error.value = null
    try {
      resourceTrends.value = await dashboardService.fetchResourceConsumptionTrends()
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to load resource trends'
      error.value = message
      console.error('[OperatorDashboard] loadResourceTrends error:', err)
    } finally {
      isLoadingTrends.value = false
    }
  }

  /**
   * Loads all dashboard data in parallel.
   */
  async function loadAll(): Promise<void> {
    await Promise.allSettled([
      loadSummary(),
      loadTenantUsage(),
      loadSystemHealth(),
      loadResourceTrends(),
    ])
  }

  /**
   * Updates tenant usage search and reloads.
   */
  async function searchTenantUsage(searchTerm: string): Promise<void> {
    tenantUsageSearch.value = searchTerm
    tenantUsageCurrentPage.value = 1
    await loadTenantUsage()
  }

  /**
   * Changes tenant usage page and reloads.
   */
  async function changeTenantUsagePage(page: number): Promise<void> {
    tenantUsageCurrentPage.value = page
    await loadTenantUsage()
  }

  /**
   * Resets all state.
   */
  function $reset(): void {
    summary.value = null
    tenantUsageItems.value = []
    tenantUsageTotalCount.value = 0
    tenantUsageTotalPages.value = 0
    tenantUsageCurrentPage.value = 1
    tenantUsageSearch.value = ''
    systemHealth.value = null
    resourceTrends.value = null
    error.value = null
  }

  return {
    // State
    summary,
    tenantUsageItems,
    tenantUsageTotalCount,
    tenantUsageTotalPages,
    tenantUsageCurrentPage,
    tenantUsagePageSize,
    tenantUsageSearch,
    systemHealth,
    resourceTrends,
    isLoadingSummary,
    isLoadingTenantUsage,
    isLoadingHealth,
    isLoadingTrends,
    error,
    // Getters
    isLoading,
    overallHealthStatus,
    isSystemHealthy,
    // Actions
    loadSummary,
    loadTenantUsage,
    loadSystemHealth,
    loadResourceTrends,
    loadAll,
    searchTenantUsage,
    changeTenantUsagePage,
    $reset,
  }
})
