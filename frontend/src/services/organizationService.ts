/**
 * Organization API Service.
 *
 * Provides typed HTTP methods for the current tenant's organization settings.
 * Uses /api/v1/organization endpoints which require organization.view/edit permissions
 * instead of the operator-level tenants.view permission.
 *
 * All data is fetched dynamically from the backend — NO mock data.
 */
import { httpGet, httpPut } from '@/services/http'
import type { TenantDto, UpdateTenantRequest, UpdateTenantBrandingRequest } from '@/types/tenant'

const BASE_URL = '/v1/organization'

/**
 * Fetches the current tenant's organization details.
 * The tenant ID is determined server-side from the JWT claims.
 */
export async function fetchCurrentOrganization(): Promise<TenantDto> {
  return httpGet<TenantDto>(`${BASE_URL}/current`)
}

/**
 * Updates the current tenant's organization information.
 */
export async function updateCurrentOrganization(
  request: UpdateTenantRequest,
): Promise<TenantDto> {
  return httpPut<TenantDto>(`${BASE_URL}/current`, request)
}

/**
 * Fetches the current tenant's branding configuration.
 */
export async function fetchCurrentOrganizationBranding(): Promise<any> {
  return httpGet<any>(`${BASE_URL}/current/branding`)
}

/**
 * Updates the current tenant's visual branding.
 */
export async function updateCurrentOrganizationBranding(
  request: UpdateTenantBrandingRequest,
): Promise<TenantDto> {
  return httpPut<TenantDto>(`${BASE_URL}/current/branding`, request)
}
