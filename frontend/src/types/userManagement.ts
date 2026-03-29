/**
 * User Management TypeScript types.
 *
 * Mirrors backend DTOs from UserManagementDtos.cs and request models
 * from UserManagementRequestModels.cs.
 *
 * TASK-903: Settings & User Management frontend pages.
 */

/* ------------------------------------------------------------------ */
/*  User DTOs                                                          */
/* ------------------------------------------------------------------ */

/**
 * Represents a role assigned to a user.
 */
export interface UserRoleDto {
  roleId: string
  nameAr: string
  nameEn: string
  assignedAt: string
  assignedBy: string | null
}

/**
 * Represents a user in list/detail views.
 */
export interface UserDto {
  id: string
  email: string
  firstName: string
  lastName: string
  phoneNumber: string | null
  isActive: boolean
  mfaEnabled: boolean
  emailConfirmed: boolean
  lastLoginAt: string | null
  createdAt: string
  roles: UserRoleDto[]
}

/* ------------------------------------------------------------------ */
/*  Role DTOs                                                          */
/* ------------------------------------------------------------------ */

/**
 * Represents a role in list views.
 */
export interface RoleDto {
  id: string
  nameAr: string
  nameEn: string
  description: string | null
  isSystemRole: boolean
  isActive: boolean
  userCount: number
  createdAt: string
}

/* ------------------------------------------------------------------ */
/*  Invitation DTOs                                                    */
/* ------------------------------------------------------------------ */

/**
 * Represents an invitation in list views.
 */
export interface InvitationDto {
  id: string
  email: string
  firstNameAr: string
  lastNameAr: string
  firstNameEn: string | null
  lastNameEn: string | null
  status: string
  roleName: string | null
  roleId: string | null
  invitedByName: string
  expiresAt: string
  acceptedAt: string | null
  resendCount: number
  createdAt: string
}

/* ------------------------------------------------------------------ */
/*  Paginated Result                                                   */
/* ------------------------------------------------------------------ */

/**
 * Paginated result wrapper matching backend PaginatedResult<T>.
 */
export interface PaginatedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

/* ------------------------------------------------------------------ */
/*  Request Models                                                     */
/* ------------------------------------------------------------------ */

/**
 * Request model for updating user profile information.
 */
export interface UpdateUserRequest {
  firstName: string
  lastName: string
  phoneNumber?: string | null
}

/**
 * Request model for activating/deactivating a user.
 */
export interface ToggleUserStatusRequest {
  activate: boolean
}

/**
 * Request model for assigning a role to a user.
 */
export interface AssignRoleRequest {
  roleId: string
}

/**
 * Request model for sending a registration invitation.
 */
export interface SendInvitationRequest {
  email: string
  firstNameAr: string
  lastNameAr: string
  firstNameEn?: string | null
  lastNameEn?: string | null
  roleId?: string | null
  tenantName?: string | null
  baseUrl?: string | null
}

/* ------------------------------------------------------------------ */
/*  Query Parameters                                                   */
/* ------------------------------------------------------------------ */

/**
 * Query parameters for fetching paginated users.
 */
export interface UserListParams {
  page?: number
  pageSize?: number
}

/**
 * Query parameters for fetching paginated invitations.
 */
export interface InvitationListParams {
  page?: number
  pageSize?: number
}
