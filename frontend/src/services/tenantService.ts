/**
 * Tenant Management API Service.
 *
 * Provides typed HTTP methods for all tenant lifecycle operations.
 * All data is fetched dynamically from the backend — NO mock data.
 *
 * Endpoints mirror TenantEndpoints.cs (/api/v1/tenants).
 */
import { httpGet, httpPost, httpPut } from '@/services/http'
import type {
  TenantDto,
  TenantListItemDto,
  TenantResolveDto,
  PagedResult,
  TenantStatusOption,
  CreateTenantRequest,
  UpdateTenantRequest,
  ChangeTenantStatusRequest,
  UpdateTenantBrandingRequest,
  TenantListParams,
  TenantSelectorOption,
} from '@/types/tenant'

const BASE_URL = '/v1/tenants'

/* ------------------------------------------------------------------ */
/*  Tenant Resolution (Public / Anonymous)                             */
/* ------------------------------------------------------------------ */

/**
 * Resolves the current tenant by hostname.
 * This endpoint is public (no auth required) and is used on the login page
 * to automatically detect which tenant the user belongs to.
 */
export async function resolveTenantByHostname(
  hostname: string,
): Promise<TenantResolveDto> {
  return httpGet<TenantResolveDto>(`${BASE_URL}/resolve?hostname=${encodeURIComponent(hostname)}`)
}

/* ------------------------------------------------------------------ */
/*  Tenant CRUD                                                        */
/* ------------------------------------------------------------------ */

/**
 * Fetches a paginated list of tenants with optional search and status filter.
 */
export async function fetchTenantsList(
  params: TenantListParams = {},
): Promise<PagedResult<TenantListItemDto>> {
  const queryParams = new URLSearchParams()

  if (params.page) queryParams.set('page', String(params.page))
  if (params.pageSize) queryParams.set('pageSize', String(params.pageSize))
  if (params.search) queryParams.set('search', params.search)
  if (params.status !== undefined && params.status !== null)
    queryParams.set('status', String(params.status))

  const queryString = queryParams.toString()
  const url = queryString ? `${BASE_URL}?${queryString}` : BASE_URL

  return httpGet<PagedResult<TenantListItemDto>>(url)
}

/**
 * Fetches detailed information about a specific tenant.
 */
export async function fetchTenantById(id: string): Promise<TenantDto> {
  return httpGet<TenantDto>(`${BASE_URL}/${id}`)
}

/**
 * Creates a new government entity (tenant).
 */
export async function createTenant(
  request: CreateTenantRequest,
): Promise<TenantDto> {
  return httpPost<TenantDto>(BASE_URL, request)
}

/**
 * Updates basic information of an existing tenant.
 */
export async function updateTenant(
  id: string,
  request: UpdateTenantRequest,
): Promise<TenantDto> {
  return httpPut<TenantDto>(`${BASE_URL}/${id}`, request)
}

/**
 * Updates visual branding (logo, colors) for a tenant.
 */
export async function updateTenantBranding(
  id: string,
  request: UpdateTenantBrandingRequest,
): Promise<TenantDto> {
  return httpPut<TenantDto>(`${BASE_URL}/${id}/branding`, request)
}

/**
 * Changes the lifecycle status of a tenant.
 */
export async function changeTenantStatus(
  id: string,
  request: ChangeTenantStatusRequest,
): Promise<TenantDto> {
  return httpPost<TenantDto>(`${BASE_URL}/${id}/status`, request)
}

/**
 * Triggers automated database provisioning for a tenant.
 */
export async function provisionTenant(id: string): Promise<TenantDto> {
  return httpPost<TenantDto>(`${BASE_URL}/${id}/provision`)
}

/**
 * Fetches all available tenant lifecycle statuses.
 */
export async function fetchTenantStatuses(): Promise<TenantStatusOption[]> {
  return httpGet<TenantStatusOption[]>(`${BASE_URL}/statuses`)
}

/* ------------------------------------------------------------------ */
/*  Tenant Selector (for Super Admin context switching)                */
/* ------------------------------------------------------------------ */

/**
 * Fetches a lightweight list of all tenants for the Super Admin selector.
 * Uses the same list endpoint but with a large page size and minimal fields.
 */
export async function fetchTenantSelectorOptions(): Promise<
  TenantSelectorOption[]
> {
  const result = await fetchTenantsList({ page: 1, pageSize: 500 })
  return result.items.map((t) => ({
    id: t.id,
    nameAr: t.nameAr,
    nameEn: t.nameEn,
    identifier: t.identifier,
    status: t.status,
    statusName: t.statusName,
  }))
}
