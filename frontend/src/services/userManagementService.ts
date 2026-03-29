/**
 * User Management API Service.
 *
 * Provides typed HTTP methods for all user, role, and invitation operations.
 * All data is fetched dynamically from the backend — NO mock data.
 *
 * Endpoints mirror UserManagementEndpoints.cs:
 * - /api/v1/users
 * - /api/v1/roles
 * - /api/v1/invitations
 *
 * TASK-903: Settings & User Management frontend pages.
 */
import { httpGet, httpPost, httpPut, httpPatch, httpDelete } from '@/services/http'
import type {
  UserDto,
  RoleDto,
  InvitationDto,
  PaginatedResult,
  UpdateUserRequest,
  ToggleUserStatusRequest,
  AssignRoleRequest,
  SendInvitationRequest,
  UserListParams,
  InvitationListParams,
} from '@/types/userManagement'

/* ------------------------------------------------------------------ */
/*  User Endpoints (/api/v1/users)                                     */
/* ------------------------------------------------------------------ */

const USERS_URL = '/v1/users'

/**
 * Fetches a paginated list of users for the current tenant.
 */
export async function fetchUsers(
  params: UserListParams = {},
): Promise<PaginatedResult<UserDto>> {
  const queryParams = new URLSearchParams()
  queryParams.set('page', String(params.page ?? 1))
  queryParams.set('pageSize', String(params.pageSize ?? 20))

  return httpGet<PaginatedResult<UserDto>>(`${USERS_URL}?${queryParams.toString()}`)
}

/**
 * Fetches a specific user by ID.
 */
export async function fetchUserById(userId: string): Promise<UserDto> {
  return httpGet<UserDto>(`${USERS_URL}/${userId}`)
}

/**
 * Updates user profile information.
 */
export async function updateUser(
  userId: string,
  request: UpdateUserRequest,
): Promise<void> {
  await httpPut<void>(`${USERS_URL}/${userId}`, request)
}

/**
 * Activates or deactivates a user account.
 */
export async function toggleUserStatus(
  userId: string,
  request: ToggleUserStatusRequest,
): Promise<void> {
  await httpPatch<void>(`${USERS_URL}/${userId}/status`, request)
}

/**
 * Assigns a role to a user.
 */
export async function assignRole(
  userId: string,
  request: AssignRoleRequest,
): Promise<void> {
  await httpPost<void>(`${USERS_URL}/${userId}/roles`, request)
}

/**
 * Removes a role from a user.
 */
export async function removeRole(
  userId: string,
  roleId: string,
): Promise<void> {
  await httpDelete<void>(`${USERS_URL}/${userId}/roles/${roleId}`)
}

/* ------------------------------------------------------------------ */
/*  Role Endpoints (/api/v1/roles)                                     */
/* ------------------------------------------------------------------ */

const ROLES_URL = '/v1/roles'

/**
 * Fetches all roles for the current tenant.
 */
export async function fetchRoles(): Promise<RoleDto[]> {
  return httpGet<RoleDto[]>(ROLES_URL)
}

/* ------------------------------------------------------------------ */
/*  Invitation Endpoints (/api/v1/invitations)                         */
/* ------------------------------------------------------------------ */

const INVITATIONS_URL = '/v1/invitations'

/**
 * Fetches a paginated list of invitations for the current tenant.
 */
export async function fetchInvitations(
  params: InvitationListParams = {},
): Promise<PaginatedResult<InvitationDto>> {
  const queryParams = new URLSearchParams()
  queryParams.set('page', String(params.page ?? 1))
  queryParams.set('pageSize', String(params.pageSize ?? 20))

  return httpGet<PaginatedResult<InvitationDto>>(
    `${INVITATIONS_URL}?${queryParams.toString()}`,
  )
}

/**
 * Sends a registration invitation to a new user.
 */
export async function sendInvitation(
  request: SendInvitationRequest,
): Promise<{ id: string }> {
  return httpPost<{ id: string }>(INVITATIONS_URL, request)
}

/**
 * Resends an invitation email.
 */
export async function resendInvitation(invitationId: string): Promise<void> {
  await httpPost<void>(`${INVITATIONS_URL}/${invitationId}/resend`)
}

/**
 * Revokes a pending invitation.
 */
export async function revokeInvitation(invitationId: string): Promise<void> {
  await httpDelete<void>(`${INVITATIONS_URL}/${invitationId}`)
}
