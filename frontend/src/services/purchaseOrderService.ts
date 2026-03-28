/**
 * Purchase Order (PO) Management API Service.
 *
 * Provides typed HTTP methods for PO lifecycle operations.
 * All data is fetched dynamically from the backend — NO mock data.
 *
 * These endpoints will be available once the PO backend module is built.
 * The service is ready to consume them immediately.
 */
import { httpGet, httpPost } from '@/services/http'
import type {
  PurchaseOrderDto,
  PurchaseOrderListItemDto,
  PagedResult,
  CreatePurchaseOrderRequest,
  ChangePurchaseOrderStatusRequest,
  PurchaseOrderListParams,
  RenewalAlertDto,
} from '@/types/tenant'

const BASE_URL = '/v1/purchase-orders'

/* ------------------------------------------------------------------ */
/*  PO CRUD                                                            */
/* ------------------------------------------------------------------ */

/**
 * Fetches a paginated list of purchase orders with optional filters.
 */
export async function fetchPurchaseOrdersList(
  params: PurchaseOrderListParams = {},
): Promise<PagedResult<PurchaseOrderListItemDto>> {
  const queryParams = new URLSearchParams()

  if (params.page) queryParams.set('page', String(params.page))
  if (params.pageSize) queryParams.set('pageSize', String(params.pageSize))
  if (params.search) queryParams.set('search', params.search)
  if (params.status !== undefined && params.status !== null)
    queryParams.set('status', String(params.status))
  if (params.tenantId) queryParams.set('tenantId', params.tenantId)

  const queryString = queryParams.toString()
  const url = queryString ? `${BASE_URL}?${queryString}` : BASE_URL

  return httpGet<PagedResult<PurchaseOrderListItemDto>>(url)
}

/**
 * Fetches detailed information about a specific purchase order.
 */
export async function fetchPurchaseOrderById(
  id: string,
): Promise<PurchaseOrderDto> {
  return httpGet<PurchaseOrderDto>(`${BASE_URL}/${id}`)
}

/**
 * Creates a new purchase order for a tenant.
 */
export async function createPurchaseOrder(
  request: CreatePurchaseOrderRequest,
): Promise<PurchaseOrderDto> {
  return httpPost<PurchaseOrderDto>(BASE_URL, request)
}

/**
 * Changes the lifecycle status of a purchase order.
 */
export async function changePurchaseOrderStatus(
  id: string,
  request: ChangePurchaseOrderStatusRequest,
): Promise<PurchaseOrderDto> {
  return httpPost<PurchaseOrderDto>(`${BASE_URL}/${id}/status`, request)
}

/**
 * Uploads a PO document (PDF) for a purchase order.
 * Uses multipart/form-data for file upload.
 */
export async function uploadPoDocument(
  id: string,
  file: File,
): Promise<{ documentUrl: string }> {
  const formData = new FormData()
  formData.append('file', file)

  return httpPost<{ documentUrl: string }>(
    `${BASE_URL}/${id}/document`,
    formData,
    {
      headers: { 'Content-Type': 'multipart/form-data' },
    },
  )
}

/* ------------------------------------------------------------------ */
/*  Renewal Alerts                                                     */
/* ------------------------------------------------------------------ */

/**
 * Fetches renewal alerts for subscriptions approaching expiry.
 * Returns tenants with subscriptions expiring within the specified days.
 */
export async function fetchRenewalAlerts(
  daysThreshold: number = 60,
): Promise<RenewalAlertDto[]> {
  return httpGet<RenewalAlertDto[]>(
    `${BASE_URL}/renewal-alerts?daysThreshold=${daysThreshold}`,
  )
}

/* ------------------------------------------------------------------ */
/*  PO Statistics                                                      */
/* ------------------------------------------------------------------ */

export interface PoStatistics {
  totalActive: number
  totalPending: number
  totalRenewing: number
  totalExpired: number
  totalRevenue: number
}

/**
 * Fetches aggregate PO statistics for the operator dashboard.
 */
export async function fetchPoStatistics(): Promise<PoStatistics> {
  return httpGet<PoStatistics>(`${BASE_URL}/statistics`)
}
