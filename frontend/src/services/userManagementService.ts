/**
 * User Management API Service.
 *
 * Provides typed HTTP methods for all user, role, invitation, and permission operations.
 * All data is fetched dynamically from the backend — NO mock data.
 *
 * Endpoints mirror UserManagementEndpoints.cs:
 * - /api/v1/users
 * - /api/v1/roles
 * - /api/v1/invitations
 * - /api/v1/permissions
 *
 * TASK-903: Settings & User Management frontend pages.
 */
import { httpGet, httpPost, httpPut, httpPatch, httpDelete } from '@/services/http'
import type {
  UserDto,
  RoleDto,
  RoleDetailDto,
  InvitationDto,
  PermissionGroupDto,
  PaginatedResult,
  UpdateUserRequest,
  ToggleUserStatusRequest,
  AssignRoleRequest,
  SendInvitationRequest,
  CreateRoleRequest,
  UpdateRoleRequest,
  ToggleRoleStatusRequest,
  UserListParams,
  InvitationListParams,
} from '@/types/userManagement'

/* ------------------------------------------------------------------ */
/*  User Endpoints (/api/v1/users)                                     */
/* ------------------------------------------------------------------ */

const USERS_URL = '/v1/users'

/**
 * Fetches a paginated list of users with search and filter support.
 */
export async function fetchUsers(
  params: UserListParams = {},
): Promise<PaginatedResult<UserDto>> {
  const queryParams = new URLSearchParams()
  queryParams.set('page', String(params.page ?? 1))
  queryParams.set('pageSize', String(params.pageSize ?? 20))

  if (params.search) {
    queryParams.set('search', params.search)
  }
  if (params.roleId) {
    queryParams.set('roleId', params.roleId)
  }
  if (params.isActive !== undefined) {
    queryParams.set('isActive', String(params.isActive))
  }

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

/**
 * Fetches a role with its permissions and users.
 */
export async function fetchRoleById(roleId: string): Promise<RoleDetailDto> {
  return httpGet<RoleDetailDto>(`${ROLES_URL}/${roleId}`)
}

/**
 * Creates a new custom role.
 */
export async function createRole(request: CreateRoleRequest): Promise<RoleDto> {
  return httpPost<RoleDto>(ROLES_URL, request)
}

/**
 * Updates a role's name, description, and permissions.
 */
export async function updateRole(
  roleId: string,
  request: UpdateRoleRequest,
): Promise<void> {
  await httpPut<void>(`${ROLES_URL}/${roleId}`, request)
}

/**
 * Activates or deactivates a role.
 */
export async function toggleRoleStatus(
  roleId: string,
  request: ToggleRoleStatusRequest,
): Promise<void> {
  await httpPatch<void>(`${ROLES_URL}/${roleId}/status`, request)
}

/* ------------------------------------------------------------------ */
/*  Permission Endpoints (/api/v1/permissions)                         */
/* ------------------------------------------------------------------ */

const PERMISSIONS_URL = '/v1/permissions'

/**
 * Fetches all available permissions grouped by module.
 */
export async function fetchPermissions(): Promise<PermissionGroupDto[]> {
  return httpGet<PermissionGroupDto[]>(PERMISSIONS_URL)
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
