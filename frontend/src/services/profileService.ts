/**
 * Profile API service.
 *
 * Wraps all profile-related HTTP calls to the backend.
 * Endpoints aligned with ProfileEndpoints.cs (/api/v1/profile/*).
 */
import { httpGet, httpPut, httpPost, httpDelete } from './http'
import httpClient from './http'

/* ------------------------------------------------------------------ */
/*  Types                                                              */
/* ------------------------------------------------------------------ */

export interface ProfileDto {
  id: string
  email: string
  firstName: string
  lastName: string
  phoneNumber: string | null
  avatarUrl: string | null
  roles: string[]
  permissions: string[]
  tenantId: string
  tenantName: string | null
  isActive: boolean
  lastLoginAt: string | null
  createdAt: string
}

export interface UpdateProfileRequest {
  firstName: string
  lastName: string
  phoneNumber: string | null
  email: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
  confirmNewPassword: string
}

/* ------------------------------------------------------------------ */
/*  API Calls                                                          */
/* ------------------------------------------------------------------ */

const PROFILE_BASE = '/v1/profile'

/**
 * Get the current user's profile.
 */
export async function getProfile(): Promise<ProfileDto> {
  return httpGet<ProfileDto>(PROFILE_BASE)
}

/**
 * Update the current user's profile information.
 */
export async function updateProfile(request: UpdateProfileRequest): Promise<void> {
  await httpPut(PROFILE_BASE, request)
}

/**
 * Change the current user's password.
 */
export async function changePassword(request: ChangePasswordRequest): Promise<void> {
  await httpPost(`${PROFILE_BASE}/change-password`, request)
}

/**
 * Upload or update the current user's avatar.
 * Returns the new avatar URL.
 */
export async function uploadAvatar(file: File): Promise<{ avatarUrl: string }> {
  const formData = new FormData()
  formData.append('file', file)

  const { data } = await httpClient.post<{ avatarUrl: string }>(
    `/api${PROFILE_BASE}/avatar`,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    },
  )
  return data
}

/**
 * Remove the current user's avatar.
 */
export async function deleteAvatar(): Promise<void> {
  await httpDelete(`${PROFILE_BASE}/avatar`)
}
