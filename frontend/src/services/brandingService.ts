/**
 * Tenant Branding API Service.
 *
 * Provides typed HTTP methods for fetching and updating
 * tenant branding configuration (logo, colors).
 * All data is fetched dynamically from the backend — NO mock data.
 *
 * Endpoints mirror TenantEndpoints.cs branding routes.
 */
import { httpGet, httpPut } from '@/services/http'
import httpClient from '@/services/http'
import type { TenantBrandingDto } from '@/types/branding'
import type { UpdateTenantBrandingRequest, TenantDto } from '@/types/tenant'

const BASE_URL = '/v1/tenants'

/**
 * Fetches the branding configuration for a specific tenant.
 * Used at login/session restore to apply dynamic branding.
 */
export async function fetchTenantBranding(
  tenantId: string,
): Promise<TenantBrandingDto> {
  return httpGet<TenantBrandingDto>(`${BASE_URL}/${tenantId}/branding`)
}

/**
 * Updates the branding configuration for a specific tenant.
 * Used by the Super Admin branding editor.
 */
export async function updateTenantBranding(
  tenantId: string,
  request: UpdateTenantBrandingRequest,
): Promise<TenantDto> {
  return httpPut<TenantDto>(`${BASE_URL}/${tenantId}/branding`, request)
}

/**
 * Uploads a logo file for a tenant and returns the uploaded file metadata.
 * Uses the file upload endpoint with query parameters as required by FileEndpoints.cs.
 * Category 8 = FileCategory.BrandingAsset
 */
export async function uploadTenantLogo(
  tenantId: string,
  file: File,
): Promise<{ fileId: string; objectKey: string; fileName: string }> {
  const formData = new FormData()
  formData.append('file', file)

  // Backend expects tenantId, category, and folderPath as query parameters
  const params = new URLSearchParams({
    tenantId,
    category: '8', // FileCategory.BrandingAsset = 8
    folderPath: 'branding/logos',
  })

  const response = await httpClient.post(
    `/v1/files/upload?${params.toString()}`,
    formData,
    { headers: { 'Content-Type': 'multipart/form-data' } },
  )

  return response.data
}

/**
 * Gets a presigned download URL for a file.
 * Used to display uploaded logos.
 */
export async function getFileDownloadUrl(
  fileId: string,
): Promise<string> {
  const response = await httpClient.get(`/v1/files/${fileId}/download-url`)
  return response.data.url
}
