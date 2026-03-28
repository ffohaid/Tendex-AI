/**
 * Purchase Order (PO) Management Pinia Store.
 *
 * Manages state for the Super Admin PO & subscription management module:
 * - PO list with pagination, search, and status filtering
 * - PO CRUD operations
 * - PO status transitions through the lifecycle
 * - Renewal alerts for early warning system
 * - PO aggregate statistics
 */
import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import * as poService from '@/services/purchaseOrderService'
import type {
  PurchaseOrderDto,
  PurchaseOrderListItemDto,
  CreatePurchaseOrderRequest,
  ChangePurchaseOrderStatusRequest,
  PurchaseOrderListParams,
  RenewalAlertDto,
  PoStatus,
} from '@/types/tenant'
import type { PoStatistics } from '@/services/purchaseOrderService'

export const usePurchaseOrderStore = defineStore('purchaseOrder', () => {
  /* ---------------------------------------------------------------- */
  /*  State                                                            */
  /* ---------------------------------------------------------------- */

  /** List state */
  const purchaseOrders = ref<PurchaseOrderListItemDto[]>([])
  const totalCount = ref(0)
  const totalPages = ref(0)
  const currentPage = ref(1)
  const pageSize = ref(10)
  const searchQuery = ref('')
  const statusFilter = ref<PoStatus | null>(null)
  const tenantFilter = ref<string>('')

  /** Detail state */
  const currentPo = ref<PurchaseOrderDto | null>(null)

  /** Renewal alerts */
  const renewalAlerts = ref<RenewalAlertDto[]>([])

  /** Statistics */
  const statistics = ref<PoStatistics | null>(null)

  /** UI state */
  const isLoading = ref(false)
  const isSubmitting = ref(false)
  const error = ref<string | null>(null)
  const successMessage = ref<string | null>(null)

  /* ---------------------------------------------------------------- */
  /*  Getters                                                          */
  /* ---------------------------------------------------------------- */

  const criticalAlerts = computed(() =>
    renewalAlerts.value.filter(
      (a) => a.severity === 'critical' || a.severity === 'expired',
    ),
  )

  const warningAlerts = computed(() =>
    renewalAlerts.value.filter((a) => a.severity === 'warning'),
  )

  const alertsCount = computed(() => renewalAlerts.value.length)

  const hasAlerts = computed(() => renewalAlerts.value.length > 0)

  /* ---------------------------------------------------------------- */
  /*  Actions                                                          */
  /* ---------------------------------------------------------------- */

  /** Load paginated PO list from API. */
  async function loadPurchaseOrders(): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      const params: PurchaseOrderListParams = {
        page: currentPage.value,
        pageSize: pageSize.value,
      }
      if (searchQuery.value) params.search = searchQuery.value
      if (statusFilter.value !== null) params.status = statusFilter.value
      if (tenantFilter.value) params.tenantId = tenantFilter.value

      const result = await poService.fetchPurchaseOrdersList(params)
      purchaseOrders.value = result.items
      totalCount.value = result.totalCount
      totalPages.value = result.totalPages
    } catch (err) {
      error.value = extractError(err)
      purchaseOrders.value = []
    } finally {
      isLoading.value = false
    }
  }

  /** Load PO detail by ID. */
  async function loadPoDetail(id: string): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      currentPo.value = await poService.fetchPurchaseOrderById(id)
    } catch (err) {
      error.value = extractError(err)
      currentPo.value = null
    } finally {
      isLoading.value = false
    }
  }

  /** Create a new PO. */
  async function createPurchaseOrder(
    request: CreatePurchaseOrderRequest,
  ): Promise<PurchaseOrderDto | null> {
    isSubmitting.value = true
    error.value = null

    try {
      const po = await poService.createPurchaseOrder(request)
      successMessage.value = 'purchaseOrders.messages.createSuccess'
      await loadPurchaseOrders()
      return po
    } catch (err) {
      error.value = extractError(err)
      return null
    } finally {
      isSubmitting.value = false
    }
  }

  /** Change PO lifecycle status. */
  async function changePoStatus(
    id: string,
    request: ChangePurchaseOrderStatusRequest,
  ): Promise<PurchaseOrderDto | null> {
    isSubmitting.value = true
    error.value = null

    try {
      const po = await poService.changePurchaseOrderStatus(id, request)
      if (currentPo.value?.id === id) {
        currentPo.value = po
      }
      successMessage.value = 'purchaseOrders.messages.statusChanged'
      await loadPurchaseOrders()
      return po
    } catch (err) {
      error.value = extractError(err)
      return null
    } finally {
      isSubmitting.value = false
    }
  }

  /** Upload PO document. */
  async function uploadDocument(
    id: string,
    file: File,
  ): Promise<string | null> {
    isSubmitting.value = true
    error.value = null

    try {
      const result = await poService.uploadPoDocument(id, file)
      successMessage.value = 'purchaseOrders.messages.documentUploaded'
      return result.documentUrl
    } catch (err) {
      error.value = extractError(err)
      return null
    } finally {
      isSubmitting.value = false
    }
  }

  /** Load renewal alerts. */
  async function loadRenewalAlerts(
    daysThreshold: number = 60,
  ): Promise<void> {
    try {
      renewalAlerts.value =
        await poService.fetchRenewalAlerts(daysThreshold)
    } catch {
      renewalAlerts.value = []
    }
  }

  /** Load PO statistics. */
  async function loadStatistics(): Promise<void> {
    try {
      statistics.value = await poService.fetchPoStatistics()
    } catch {
      statistics.value = null
    }
  }

  /** Reset pagination and reload. */
  function resetAndReload(): void {
    currentPage.value = 1
    loadPurchaseOrders()
  }

  /** Clear messages. */
  function clearMessages(): void {
    error.value = null
    successMessage.value = null
  }

  /* ---------------------------------------------------------------- */
  /*  Helpers                                                          */
  /* ---------------------------------------------------------------- */

  function extractError(err: unknown): string {
    if (err && typeof err === 'object' && 'response' in err) {
      const axiosErr = err as { response?: { data?: { error?: string; detail?: string } } }
      return (
        axiosErr.response?.data?.error ||
        axiosErr.response?.data?.detail ||
        'purchaseOrders.errors.unexpected'
      )
    }
    return 'purchaseOrders.errors.unexpected'
  }

  return {
    /* State */
    purchaseOrders,
    totalCount,
    totalPages,
    currentPage,
    pageSize,
    searchQuery,
    statusFilter,
    tenantFilter,
    currentPo,
    renewalAlerts,
    statistics,
    isLoading,
    isSubmitting,
    error,
    successMessage,

    /* Getters */
    criticalAlerts,
    warningAlerts,
    alertsCount,
    hasAlerts,

    /* Actions */
    loadPurchaseOrders,
    loadPoDetail,
    createPurchaseOrder,
    changePoStatus,
    uploadDocument,
    loadRenewalAlerts,
    loadStatistics,
    resetAndReload,
    clearMessages,
  }
})
