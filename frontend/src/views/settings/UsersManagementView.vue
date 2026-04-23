<script setup lang="ts">
/**
 * UsersManagementView — User Management Page.
 *
 * Features:
 * - Paginated user list with search and filters
 * - Search by name, email, phone
 * - Filter by role and active status
 * - Invite new user dialog
 * - Edit user profile dialog
 * - Toggle user active/inactive status
 * - Assign/Remove roles with search
 * - User detail dialog
 * - Invitations management (resend, revoke)
 * - Stats cards
 * - RTL/LTR support with Tailwind logical properties
 * - English numerals exclusively
 */
import { ref, onMounted, computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import AiUserManagementAssistant from '@/components/ai/AiUserManagementAssistant.vue'
import {
  fetchUsers, fetchUserById, updateUser, toggleUserStatus,
  assignRole, removeRole, fetchRoles, fetchInvitations,
  sendInvitation, resendInvitation, revokeInvitation,
  adminResetPassword,
} from '@/services/userManagementService'
import type { UserDto, RoleDto, InvitationDto, SendInvitationRequest, AdminResetPasswordRequest } from '@/types/userManagement'
import { useFormatters } from '@/composables/useFormatters'
import { useAuthStore } from '@/stores/auth'

const { t, locale } = useI18n()
const { formatDate, formatDateTime } = useFormatters()
const authStore = useAuthStore()

const canCreateUser = computed(() => authStore.hasPermission('users.create'))
const canEditUser = computed(() => authStore.hasPermission('users.edit'))
const canResetPassword = computed(() => authStore.hasPermission('users.reset_password'))
// const canDeleteUser = computed(() => authStore.hasPermission('users.delete'))

/* State */
const activeTab = ref<'users' | 'invitations'>('users')
const users = ref<UserDto[]>([])
const isLoading = ref(false)
const error = ref('')
const successMessage = ref('')
const currentPage = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)

/* Search & Filters */
const searchTerm = ref('')
const filterRoleId = ref('')
const filterStatus = ref<string>('')
let searchTimeout: ReturnType<typeof setTimeout> | null = null

/* Roles */
const roles = ref<RoleDto[]>([])

/* Invitations */
const invitations = ref<InvitationDto[]>([])
const invCurrentPage = ref(1)
const invTotalCount = ref(0)
const isLoadingInvitations = ref(false)

/* Dialogs */
const showInviteDialog = ref(false)
const showEditDialog = ref(false)
const showRoleDialog = ref(false)
const showUserDetailDialog = ref(false)
const showResetPasswordDialog = ref(false)
const isSubmitting = ref(false)
const selectedUser = ref<UserDto | null>(null)
const inviteDialogError = ref('')
const editDialogError = ref('')
const resetPasswordDialogError = ref('')

/* Forms */
const inviteForm = ref<SendInvitationRequest>({
  email: '', firstNameAr: '', lastNameAr: '', firstNameEn: '', lastNameEn: '', roleId: null,
})
const editForm = ref({ firstName: '', lastName: '', phoneNumber: '' })
const selectedRoleId = ref('')
const resetPasswordForm = ref<AdminResetPasswordRequest>({
  newPassword: '',
  confirmPassword: '',
  notifyUser: true,
  forceChangeOnLogin: true,
})
const showNewPassword = ref(false)
const showConfirmPassword = ref(false)
const passwordStrength = ref<{ level: string; label: string; color: string }>({ level: '', label: '', color: '' })

/* Computed */
const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value) || 1)
const invTotalPages = computed(() => Math.ceil(invTotalCount.value / pageSize.value) || 1)
const activeUsersCount = computed(() => users.value.filter(u => u.isActive).length)
const inactiveUsersCount = computed(() => users.value.filter(u => !u.isActive).length)
const availableRolesForAssignment = computed(() => {
  if (!selectedUser.value) return roles.value
  const assignedIds = new Set(selectedUser.value.roles.map(r => r.roleId))
  return roles.value.filter(r => !assignedIds.has(r.id))
})
const hasActiveFilters = computed(() =>
  searchTerm.value !== '' || filterRoleId.value !== '' || filterStatus.value !== ''
)

/* Data Loading */
async function loadUsers() {
  isLoading.value = true
  error.value = ''
  try {
    const isActiveFilter = filterStatus.value === '' ? undefined : filterStatus.value === 'true'
    const result = await fetchUsers({
      page: currentPage.value, pageSize: pageSize.value,
      search: searchTerm.value || undefined,
      roleId: filterRoleId.value || undefined,
      isActive: isActiveFilter,
    })
    users.value = result.items
    totalCount.value = result.totalCount
  } catch {
    error.value = t('settings.users.errors.loadFailed')
  } finally {
    isLoading.value = false
  }
}

async function loadRoles() {
  try { roles.value = await fetchRoles() } catch { /* silent */ }
}

async function loadInvitations() {
  isLoadingInvitations.value = true
  try {
    const result = await fetchInvitations({ page: invCurrentPage.value, pageSize: pageSize.value })
    invitations.value = result.items
    invTotalCount.value = result.totalCount
  } catch {
    error.value = t('settings.users.errors.loadInvitationsFailed')
  } finally {
    isLoadingInvitations.value = false
  }
}

/* Search & Filter Handlers */
function onSearchInput() {
  if (searchTimeout) clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => { currentPage.value = 1; loadUsers() }, 400)
}
function onFilterChange() { currentPage.value = 1; loadUsers() }
function clearFilters() {
  searchTerm.value = ''; filterRoleId.value = ''; filterStatus.value = ''
  currentPage.value = 1; loadUsers()
}

/* Helpers */
function getRoleName(role: RoleDto): string {
  return locale.value === 'ar' ? role.nameAr : role.nameEn
}
function getStatusBadge(isActive: boolean) {
  return isActive
    ? { bgClass: 'bg-green-50', textClass: 'text-green-700', label: t('settings.users.statuses.active') }
    : { bgClass: 'bg-red-50', textClass: 'text-red-700', label: t('settings.users.statuses.inactive') }
}
function getInvitationStatusBadge(status: string) {
  const map: Record<string, { bgClass: string; textClass: string; label: string }> = {
    Pending: { bgClass: 'bg-amber-50', textClass: 'text-amber-700', label: t('settings.users.invStatuses.pending') },
    Accepted: { bgClass: 'bg-green-50', textClass: 'text-green-700', label: t('settings.users.invStatuses.accepted') },
    Expired: { bgClass: 'bg-gray-100', textClass: 'text-gray-600', label: t('settings.users.invStatuses.expired') },
    Revoked: { bgClass: 'bg-red-50', textClass: 'text-red-700', label: t('settings.users.invStatuses.revoked') },
  }
  return map[status] || { bgClass: 'bg-gray-100', textClass: 'text-gray-600', label: status }
}

/* Action Handlers */
async function handleSendInvitation() {
  isSubmitting.value = true
  error.value = ''
  inviteDialogError.value = ''
  try {
    await sendInvitation(inviteForm.value)
    showInviteDialog.value = false
    successMessage.value = t('settings.users.inviteSentSuccess')
    await loadInvitations()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch (e: any) {
    const detail = e?.response?.data?.detail || e?.response?.data?.title || e?.message || ''
    const message = detail
      ? `${t('settings.users.errors.inviteFailed')}: ${detail}`
      : t('settings.users.errors.inviteFailed')
    inviteDialogError.value = message
    error.value = message
  } finally {
    isSubmitting.value = false
  }
}

async function handleUpdateUser() {
  if (!selectedUser.value) return
  isSubmitting.value = true
  error.value = ''
  editDialogError.value = ''
  try {
    await updateUser(selectedUser.value.id, editForm.value)
    showEditDialog.value = false
    successMessage.value = t('settings.users.updateSuccess')
    await loadUsers()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch (e: any) {
    const detail = e?.response?.data?.detail || e?.response?.data?.title || e?.message || ''
    const message = detail
      ? `${t('settings.users.errors.updateFailed')}: ${detail}`
      : t('settings.users.errors.updateFailed')
    editDialogError.value = message
    error.value = message
  } finally {
    isSubmitting.value = false
  }
}

async function handleToggleStatus(user: UserDto) {
  try {
    await toggleUserStatus(user.id, { activate: !user.isActive })
    successMessage.value = user.isActive ? t('settings.users.deactivateSuccess') : t('settings.users.activateSuccess')
    await loadUsers()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch (e: any) {
    const detail = e?.response?.data?.detail || e?.response?.data?.title || e?.message || ''
    error.value = detail ? `${t('settings.users.errors.toggleStatusFailed')}: ${detail}` : t('settings.users.errors.toggleStatusFailed')
  }
}

async function handleAssignRole() {
  if (!selectedUser.value || !selectedRoleId.value) return
  isSubmitting.value = true; error.value = ''
  try {
    await assignRole(selectedUser.value.id, { roleId: selectedRoleId.value })
    successMessage.value = t('settings.users.roleAssignSuccess')
    const updated = await fetchUserById(selectedUser.value.id)
    selectedUser.value = updated
    selectedRoleId.value = ''
    await loadUsers()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch (e: any) {
    const detail = e?.response?.data?.detail || e?.response?.data?.title || e?.message || ''
    error.value = detail ? `${t('settings.users.errors.roleAssignFailed')}: ${detail}` : t('settings.users.errors.roleAssignFailed')
  }
  finally { isSubmitting.value = false }
}

async function handleRemoveRole(userId: string, roleId: string) {
  try {
    await removeRole(userId, roleId)
    successMessage.value = t('settings.users.roleRemoveSuccess')
    if (selectedUser.value && selectedUser.value.id === userId) {
      const updated = await fetchUserById(userId)
      selectedUser.value = updated
    }
    await loadUsers()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch (e: any) {
    const detail = e?.response?.data?.detail || e?.response?.data?.title || e?.message || ''
    error.value = detail ? `${t('settings.users.errors.roleRemoveFailed')}: ${detail}` : t('settings.users.errors.roleRemoveFailed')
  }
}

async function handleResendInvitation(invitationId: string) {
  try {
    await resendInvitation(invitationId)
    successMessage.value = t('settings.users.inviteResendSuccess')
    await loadInvitations()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch (e: any) {
    const detail = e?.response?.data?.detail || e?.response?.data?.title || e?.message || ''
    error.value = detail ? `${t('settings.users.errors.resendFailed')}: ${detail}` : t('settings.users.errors.resendFailed')
  }
}

async function handleRevokeInvitation(invitationId: string) {
  try {
    await revokeInvitation(invitationId)
    successMessage.value = t('settings.users.inviteRevokeSuccess')
    await loadInvitations()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch (e: any) {
    const detail = e?.response?.data?.detail || e?.response?.data?.title || e?.message || ''
    error.value = detail ? `${t('settings.users.errors.revokeFailed')}: ${detail}` : t('settings.users.errors.revokeFailed')
  }
}

/* Password Reset Handlers */
function checkPasswordStrength(password: string) {
  if (!password) { passwordStrength.value = { level: '', label: '', color: '' }; return }
  let score = 0
  if (password.length >= 8) score++
  if (password.length >= 12) score++
  if (/[A-Z]/.test(password)) score++
  if (/[a-z]/.test(password)) score++
  if (/[0-9]/.test(password)) score++
  if (/[^A-Za-z0-9]/.test(password)) score++
  if (score <= 2) passwordStrength.value = { level: 'weak', label: t('settings.users.resetPassword.strengthWeak'), color: 'bg-red-500' }
  else if (score <= 4) passwordStrength.value = { level: 'medium', label: t('settings.users.resetPassword.strengthMedium'), color: 'bg-amber-500' }
  else passwordStrength.value = { level: 'strong', label: t('settings.users.resetPassword.strengthStrong'), color: 'bg-green-500' }
}

function openResetPasswordDialog(user: UserDto) {
  selectedUser.value = user
  resetPasswordForm.value = { newPassword: '', confirmPassword: '', notifyUser: true, forceChangeOnLogin: true }
  showNewPassword.value = false
  showConfirmPassword.value = false
  passwordStrength.value = { level: '', label: '', color: '' }
  resetPasswordDialogError.value = ''
  showResetPasswordDialog.value = true
}

function generateRandomPassword() {
  const upper = 'ABCDEFGHJKLMNPQRSTUVWXYZ'
  const lower = 'abcdefghjkmnpqrstuvwxyz'
  const digits = '23456789'
  const special = '@#$%&*!'
  const all = upper + lower + digits + special
  let pwd = ''
  pwd += upper[Math.floor(Math.random() * upper.length)]
  pwd += lower[Math.floor(Math.random() * lower.length)]
  pwd += digits[Math.floor(Math.random() * digits.length)]
  pwd += special[Math.floor(Math.random() * special.length)]
  for (let i = 0; i < 8; i++) pwd += all[Math.floor(Math.random() * all.length)]
  pwd = pwd.split('').sort(() => Math.random() - 0.5).join('')
  resetPasswordForm.value.newPassword = pwd
  resetPasswordForm.value.confirmPassword = pwd
  showNewPassword.value = true
  showConfirmPassword.value = true
  checkPasswordStrength(pwd)
}

async function handleResetPassword() {
  if (!selectedUser.value) return
  if (resetPasswordForm.value.newPassword !== resetPasswordForm.value.confirmPassword) {
    resetPasswordDialogError.value = t('settings.users.resetPassword.passwordMismatch')
    error.value = resetPasswordDialogError.value
    return
  }
  if (resetPasswordForm.value.newPassword.length < 8) {
    resetPasswordDialogError.value = t('settings.users.resetPassword.passwordTooShort')
    error.value = resetPasswordDialogError.value
    return
  }
  isSubmitting.value = true
  error.value = ''
  resetPasswordDialogError.value = ''
  try {
    await adminResetPassword(selectedUser.value.id, resetPasswordForm.value)
    showResetPasswordDialog.value = false
    successMessage.value = t('settings.users.resetPassword.success')
    setTimeout(() => { successMessage.value = '' }, 5000)
  } catch (e: any) {
    const detail = e?.response?.data?.detail || e?.response?.data?.title || e?.message || ''
    const message = detail
      ? `${t('settings.users.resetPassword.failed')}: ${detail}`
      : t('settings.users.resetPassword.failed')
    resetPasswordDialogError.value = message
    error.value = message
  } finally { isSubmitting.value = false }
}

/* Form Helpers */
function openInviteDialog() {
  inviteForm.value = { email: '', firstNameAr: '', lastNameAr: '', firstNameEn: '', lastNameEn: '', roleId: null }
  inviteDialogError.value = ''
  showInviteDialog.value = true
}
function openEditDialog(user: UserDto) {
  selectedUser.value = user
  editForm.value = { firstName: user.firstName, lastName: user.lastName, phoneNumber: user.phoneNumber || '' }
  editDialogError.value = ''
  showEditDialog.value = true
}
function openRoleDialog(user: UserDto) {
  selectedUser.value = user; selectedRoleId.value = ''; showRoleDialog.value = true
}
function openUserDetail(user: UserDto) {
  selectedUser.value = user; showUserDetailDialog.value = true
}

/* Pagination */
function goToPage(page: number) { if (page >= 1 && page <= totalPages.value) currentPage.value = page }
function goToInvPage(page: number) { if (page >= 1 && page <= invTotalPages.value) invCurrentPage.value = page }

/* Watchers */
watch(currentPage, () => loadUsers())
watch(invCurrentPage, () => loadInvitations())
watch(activeTab, (tab) => { if (tab === 'invitations') loadInvitations() })

/* Lifecycle */
onMounted(() => { loadUsers(); loadRoles() })
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-secondary">{{ t('settings.users.title') }}</h1>
        <p class="mt-1 text-sm text-tertiary">{{ t('settings.users.subtitle') }}</p>
      </div>
      <button
        v-if="canCreateUser"
        class="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-primary-dark"
        @click="openInviteDialog"
      >
        <i class="pi pi-user-plus text-sm"></i>
        {{ t('settings.users.inviteUser') }}
      </button>
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
      <div class="rounded-lg border border-surface-dim bg-white p-4">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
            <i class="pi pi-users text-primary"></i>
          </div>
          <div>
            <p class="text-2xl font-bold text-secondary">{{ totalCount }}</p>
            <p class="text-xs text-tertiary">{{ t('settings.users.stats.totalUsers') }}</p>
          </div>
        </div>
      </div>
      <div class="rounded-lg border border-surface-dim bg-white p-4">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-green-50">
            <i class="pi pi-check-circle text-green-600"></i>
          </div>
          <div>
            <p class="text-2xl font-bold text-secondary">{{ activeUsersCount }}</p>
            <p class="text-xs text-tertiary">{{ t('settings.users.stats.activeUsers') }}</p>
          </div>
        </div>
      </div>
      <div class="rounded-lg border border-surface-dim bg-white p-4">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-red-50">
            <i class="pi pi-ban text-red-600"></i>
          </div>
          <div>
            <p class="text-2xl font-bold text-secondary">{{ inactiveUsersCount }}</p>
            <p class="text-xs text-tertiary">{{ t('settings.users.stats.inactiveUsers') }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Success/Error Messages -->
    <div v-if="successMessage" class="flex items-center gap-2 rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700">
      <i class="pi pi-check-circle text-sm"></i>
      {{ successMessage }}
      <button class="ms-auto text-green-600 hover:text-green-800" @click="successMessage = ''"><i class="pi pi-times text-xs"></i></button>
    </div>
    <div v-if="error" class="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      <i class="pi pi-exclamation-circle text-sm"></i>
      {{ error }}
      <button class="ms-auto text-red-600 hover:text-red-800" @click="error = ''"><i class="pi pi-times text-xs"></i></button>
    </div>

    <!-- Tabs -->
    <div class="flex gap-1 rounded-lg border border-surface-dim bg-surface-ground p-1">
      <button
        :class="['flex-1 rounded-md px-4 py-2 text-sm font-medium transition-all', activeTab === 'users' ? 'bg-white text-primary shadow-sm' : 'text-tertiary hover:text-secondary']"
        @click="activeTab = 'users'"
      >
        <i class="pi pi-users me-2 text-xs"></i>{{ t('settings.users.tabs.users') }}
        <span class="ms-1 text-xs text-tertiary">({{ totalCount }})</span>
      </button>
      <button
        :class="['flex-1 rounded-md px-4 py-2 text-sm font-medium transition-all', activeTab === 'invitations' ? 'bg-white text-primary shadow-sm' : 'text-tertiary hover:text-secondary']"
        @click="activeTab = 'invitations'"
      >
        <i class="pi pi-envelope me-2 text-xs"></i>{{ t('settings.users.tabs.invitations') }}
      </button>
    </div>

    <!-- USERS TAB -->
    <template v-if="activeTab === 'users'">
      <!-- Search & Filters Bar -->
      <div class="flex flex-col gap-3 rounded-lg border border-surface-dim bg-white p-4 sm:flex-row sm:items-center">
        <div class="relative flex-1">
          <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-sm text-tertiary"></i>
          <input v-model="searchTerm" type="text" :placeholder="t('settings.users.searchPlaceholder')" class="w-full rounded-lg border border-surface-dim bg-surface-ground py-2.5 ps-10 pe-4 text-sm text-secondary placeholder:text-tertiary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" @input="onSearchInput" />
        </div>
        <select v-model="filterRoleId" class="rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" @change="onFilterChange">
          <option value="">{{ t('settings.users.allRoles') }}</option>
          <option v-for="role in roles" :key="role.id" :value="role.id">{{ getRoleName(role) }}</option>
        </select>
        <select v-model="filterStatus" class="rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" @change="onFilterChange">
          <option value="">{{ t('settings.users.allStatuses') }}</option>
          <option value="true">{{ t('settings.users.statuses.active') }}</option>
          <option value="false">{{ t('settings.users.statuses.inactive') }}</option>
        </select>
        <button v-if="hasActiveFilters" class="flex items-center gap-1 rounded-lg border border-surface-dim px-3 py-2.5 text-sm text-tertiary transition-colors hover:bg-surface-ground hover:text-secondary" @click="clearFilters">
          <i class="pi pi-filter-slash text-xs"></i>{{ t('settings.users.clearFilters') }}
        </button>
      </div>

      <!-- Loading -->
      <div v-if="isLoading" class="flex items-center justify-center py-16">
        <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
      </div>

      <!-- Empty State -->
      <div v-else-if="users.length === 0" class="flex flex-col items-center justify-center rounded-lg border border-surface-dim bg-white py-16">
        <i class="pi pi-users text-5xl text-surface-dim"></i>
        <p class="mt-4 text-sm text-tertiary">{{ hasActiveFilters ? t('settings.users.noSearchResults') : t('settings.users.emptyState') }}</p>
        <button v-if="hasActiveFilters" class="mt-4 rounded-lg border border-surface-dim px-4 py-2 text-sm font-medium text-secondary hover:bg-surface-ground" @click="clearFilters">{{ t('settings.users.clearFilters') }}</button>
        <button v-else-if="canCreateUser" class="mt-4 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary-dark" @click="openInviteDialog">{{ t('settings.users.inviteUser') }}</button>
      </div>

      <!-- Users Table -->
      <div v-else class="overflow-hidden rounded-lg border border-surface-dim bg-white">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-surface-dim bg-surface-ground">
                <th class="px-4 py-3 text-start font-semibold text-secondary">{{ t('settings.users.table.name') }}</th>
                <th class="px-4 py-3 text-start font-semibold text-secondary">{{ t('settings.users.table.email') }}</th>
                <th class="px-4 py-3 text-start font-semibold text-secondary">{{ t('settings.users.table.roles') }}</th>
                <th class="px-4 py-3 text-center font-semibold text-secondary">{{ t('settings.users.table.status') }}</th>
                <th class="px-4 py-3 text-start font-semibold text-secondary">{{ t('settings.users.table.lastLogin') }}</th>
                <th class="px-4 py-3 text-center font-semibold text-secondary">{{ t('settings.users.table.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="user in users" :key="user.id" class="cursor-pointer border-b border-surface-dim transition-colors hover:bg-surface-ground/50 last:border-b-0" @click="openUserDetail(user)">
                <td class="align-top px-4 py-3">
                  <div class="flex items-center gap-3">
                    <div class="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">{{ user.firstName?.charAt(0) || '' }}{{ user.lastName?.charAt(0) || '' }}</div>
                    <div>
                      <p class="font-medium text-secondary">{{ user.firstName }} {{ user.lastName }}</p>
                      <p v-if="user.phoneNumber" class="text-xs text-tertiary" dir="ltr">{{ user.phoneNumber }}</p>
                    </div>
                  </div>
                </td>
                <td class="align-top px-4 py-3 text-tertiary" dir="ltr">{{ user.email }}</td>
                <td class="align-top px-4 py-3">
                  <div class="flex flex-wrap gap-1">
                    <span v-for="role in user.roles" :key="role.roleId" class="inline-block rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">{{ locale === 'ar' ? role.nameAr : role.nameEn }}</span>
                    <span v-if="!user.roles || user.roles.length === 0" class="text-tertiary">&mdash;</span>
                  </div>
                </td>
                <td class="align-top px-4 py-3 text-center">
                  <span :class="[getStatusBadge(user.isActive).bgClass, getStatusBadge(user.isActive).textClass]" class="inline-block rounded-full px-2.5 py-0.5 text-xs font-medium">{{ getStatusBadge(user.isActive).label }}</span>
                </td>
                <td class="align-top px-4 py-3 text-tertiary">{{ formatDateTime(user.lastLoginAt) }}</td>
                <td class="align-top px-4 py-3 text-center" @click.stop>
                  <div class="flex items-center justify-center gap-1">
                    <button v-if="canEditUser" class="rounded-lg p-1.5 text-tertiary transition-colors hover:bg-surface-ground hover:text-primary" :title="t('settings.users.editDialogTitle')" @click="openEditDialog(user)"><i class="pi pi-pencil text-sm"></i></button>
                    <button v-if="canEditUser" class="rounded-lg p-1.5 text-tertiary transition-colors hover:bg-surface-ground hover:text-primary" :title="t('settings.users.manageRoles')" @click="openRoleDialog(user)"><i class="pi pi-shield text-sm"></i></button>
                    <button v-if="canResetPassword" class="rounded-lg p-1.5 text-tertiary transition-colors hover:bg-amber-50 hover:text-amber-600" :title="t('settings.users.resetPassword.title')" @click="openResetPasswordDialog(user)"><i class="pi pi-key text-sm"></i></button>
                    <button v-if="canEditUser" :class="['rounded-lg p-1.5 transition-colors', user.isActive ? 'text-tertiary hover:bg-red-50 hover:text-red-600' : 'text-tertiary hover:bg-green-50 hover:text-green-600']" :title="user.isActive ? t('settings.users.deactivate') : t('settings.users.activate')" @click="handleToggleStatus(user)">
                      <i :class="['pi text-sm', user.isActive ? 'pi-ban' : 'pi-check-circle']"></i>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <!-- Pagination -->
        <div v-if="totalPages > 1" class="flex items-center justify-between border-t border-surface-dim px-4 py-3">
          <p class="text-xs text-tertiary">{{ t('settings.users.pagination.showing', { from: (currentPage - 1) * pageSize + 1, to: Math.min(currentPage * pageSize, totalCount), total: totalCount }) }}</p>
          <div class="flex items-center gap-1">
            <button :disabled="currentPage <= 1" class="rounded-lg px-3 py-1.5 text-xs font-medium text-tertiary transition-colors hover:bg-surface-ground disabled:opacity-40" @click="goToPage(currentPage - 1)">{{ t('common.previous') }}</button>
            <template v-for="page in totalPages" :key="page">
              <button v-if="page === 1 || page === totalPages || (page >= currentPage - 1 && page <= currentPage + 1)" :class="['rounded-lg px-3 py-1.5 text-xs font-medium transition-colors', page === currentPage ? 'bg-primary text-white' : 'text-tertiary hover:bg-surface-ground']" @click="goToPage(page)">{{ page }}</button>
              <span v-else-if="page === currentPage - 2 || page === currentPage + 2" class="px-1 text-tertiary">...</span>
            </template>
            <button :disabled="currentPage >= totalPages" class="rounded-lg px-3 py-1.5 text-xs font-medium text-tertiary transition-colors hover:bg-surface-ground disabled:opacity-40" @click="goToPage(currentPage + 1)">{{ t('common.next') }}</button>
          </div>
        </div>
      </div>
    </template>

    <!-- INVITATIONS TAB -->
    <template v-if="activeTab === 'invitations'">
      <div v-if="isLoadingInvitations" class="flex items-center justify-center py-16"><i class="pi pi-spin pi-spinner text-3xl text-primary"></i></div>
      <div v-else-if="invitations.length === 0" class="flex flex-col items-center justify-center rounded-lg border border-surface-dim bg-white py-16">
        <i class="pi pi-envelope text-5xl text-surface-dim"></i>
        <p class="mt-4 text-sm text-tertiary">{{ t('settings.users.emptyInvitations') }}</p>
        <button class="mt-4 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary-dark" @click="openInviteDialog">{{ t('settings.users.inviteUser') }}</button>
      </div>
      <div v-else class="overflow-hidden rounded-lg border border-surface-dim bg-white">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-surface-dim bg-surface-ground">
                <th class="px-4 py-3 text-start font-semibold text-secondary">{{ t('settings.users.invTable.name') }}</th>
                <th class="px-4 py-3 text-start font-semibold text-secondary">{{ t('settings.users.invTable.email') }}</th>
                <th class="px-4 py-3 text-start font-semibold text-secondary">{{ t('settings.users.invTable.role') }}</th>
                <th class="px-4 py-3 text-center font-semibold text-secondary">{{ t('settings.users.invTable.status') }}</th>
                <th class="px-4 py-3 text-start font-semibold text-secondary">{{ t('settings.users.invTable.expiresAt') }}</th>
                <th class="px-4 py-3 text-center font-semibold text-secondary">{{ t('settings.users.invTable.actions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="inv in invitations" :key="inv.id" class="border-b border-surface-dim transition-colors hover:bg-surface-ground/50 last:border-b-0">
                <td class="align-top px-4 py-3 font-medium text-secondary">{{ locale === 'ar' ? `${inv.firstNameAr} ${inv.lastNameAr}` : `${inv.firstNameEn || inv.firstNameAr} ${inv.lastNameEn || inv.lastNameAr}` }}</td>
                <td class="align-top px-4 py-3 text-tertiary" dir="ltr">{{ inv.email }}</td>
                <td class="align-top px-4 py-3 text-tertiary">{{ inv.roleName || '—' }}</td>
                <td class="align-top px-4 py-3 text-center">
                  <span :class="[getInvitationStatusBadge(inv.status).bgClass, getInvitationStatusBadge(inv.status).textClass]" class="inline-block rounded-full px-2.5 py-0.5 text-xs font-medium">{{ getInvitationStatusBadge(inv.status).label }}</span>
                </td>
                       <td class="align-top px-4 py-3 text-tertiary">{{ formatDate(inv.expiresAt) }}</td>
                <td class="align-top px-4 py-3 text-center">
">
                  <div class="flex items-center justify-center gap-1">
                    <button v-if="inv.status === 'Pending'" class="rounded-lg p-1.5 text-tertiary transition-colors hover:bg-surface-ground hover:text-primary" :title="t('settings.users.resendInvitation')" @click="handleResendInvitation(inv.id)"><i class="pi pi-refresh text-sm"></i></button>
                    <button v-if="inv.status === 'Pending'" class="rounded-lg p-1.5 text-tertiary transition-colors hover:bg-red-50 hover:text-red-600" :title="t('settings.users.revokeInvitation')" @click="handleRevokeInvitation(inv.id)"><i class="pi pi-times text-sm"></i></button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div v-if="invTotalPages > 1" class="flex items-center justify-between border-t border-surface-dim px-4 py-3">
          <p class="text-xs text-tertiary">{{ t('settings.users.pagination.showing', { from: (invCurrentPage - 1) * pageSize + 1, to: Math.min(invCurrentPage * pageSize, invTotalCount), total: invTotalCount }) }}</p>
          <div class="flex items-center gap-1">
            <button :disabled="invCurrentPage <= 1" class="rounded-lg px-3 py-1.5 text-xs font-medium text-tertiary transition-colors hover:bg-surface-ground disabled:opacity-40" @click="goToInvPage(invCurrentPage - 1)">{{ t('common.previous') }}</button>
            <button :disabled="invCurrentPage >= invTotalPages" class="rounded-lg px-3 py-1.5 text-xs font-medium text-tertiary transition-colors hover:bg-surface-ground disabled:opacity-40" @click="goToInvPage(invCurrentPage + 1)">{{ t('common.next') }}</button>
          </div>
        </div>
      </div>
    </template>

    <!-- USER DETAIL DIALOG -->
    <Teleport to="body">
      <div v-if="showUserDetailDialog && selectedUser" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40" @click.self="showUserDetailDialog = false">
        <div class="w-full max-w-lg rounded-xl bg-white shadow-xl">
          <div class="flex items-center justify-between border-b border-surface-dim px-6 py-4">
            <h3 class="text-lg font-semibold text-secondary">{{ t('settings.users.userDetailTitle') }}</h3>
            <button class="rounded-lg p-1 text-tertiary hover:bg-surface-ground" @click="showUserDetailDialog = false"><i class="pi pi-times"></i></button>
          </div>
          <div class="p-6 space-y-4">
            <div class="flex items-center gap-4">
              <div class="flex h-14 w-14 items-center justify-center rounded-full bg-primary/10 text-lg font-bold text-primary">{{ selectedUser.firstName?.charAt(0) || '' }}{{ selectedUser.lastName?.charAt(0) || '' }}</div>
              <div>
                <h4 class="text-lg font-semibold text-secondary">{{ selectedUser.firstName }} {{ selectedUser.lastName }}</h4>
                <p class="text-sm text-tertiary" dir="ltr">{{ selectedUser.email }}</p>
              </div>
            </div>
            <div class="grid grid-cols-2 gap-4 rounded-lg border border-surface-dim bg-surface-ground p-4">
              <div>
                <p class="text-xs text-tertiary">{{ t('settings.users.fields.phone') }}</p>
                <p class="text-sm font-medium text-secondary" dir="ltr">{{ selectedUser.phoneNumber || '—' }}</p>
              </div>
              <div>
                <p class="text-xs text-tertiary">{{ t('settings.users.table.status') }}</p>
                <span :class="[getStatusBadge(selectedUser.isActive).bgClass, getStatusBadge(selectedUser.isActive).textClass]" class="inline-block rounded-full px-2.5 py-0.5 text-xs font-medium">{{ getStatusBadge(selectedUser.isActive).label }}</span>
              </div>
              <div>
                <p class="text-xs text-tertiary">{{ t('settings.users.table.lastLogin') }}</p>
                <p class="text-sm font-medium text-secondary">{{ formatDateTime(selectedUser.lastLoginAt) }}</p>
              </div>
              <div>
                <p class="text-xs text-tertiary">{{ t('settings.users.mfaStatus') }}</p>
                <span :class="selectedUser.mfaEnabled ? 'bg-green-50 text-green-700' : 'bg-gray-100 text-gray-600'" class="inline-block rounded-full px-2.5 py-0.5 text-xs font-medium">{{ selectedUser.mfaEnabled ? t('settings.users.mfaEnabled') : t('settings.users.mfaDisabled') }}</span>
              </div>
            </div>
            <div>
              <h5 class="mb-2 text-sm font-medium text-secondary">{{ t('settings.users.table.roles') }}</h5>
              <div class="flex flex-wrap gap-2">
                <span v-for="role in selectedUser.roles" :key="role.roleId" class="inline-flex items-center gap-1 rounded-full bg-primary/10 px-3 py-1 text-xs font-medium text-primary"><i class="pi pi-shield text-xs"></i>{{ locale === 'ar' ? role.nameAr : role.nameEn }}</span>
                <span v-if="!selectedUser.roles || selectedUser.roles.length === 0" class="text-sm text-tertiary">{{ t('settings.users.noRolesAssigned') }}</span>
              </div>
            </div>
            <div class="flex gap-2 border-t border-surface-dim pt-4">
              <button v-if="canEditUser" class="flex items-center gap-2 rounded-lg border border-surface-dim px-4 py-2 text-sm font-medium text-secondary transition-colors hover:bg-surface-ground" @click="showUserDetailDialog = false; openEditDialog(selectedUser!)"><i class="pi pi-pencil text-xs"></i>{{ t('settings.users.editDialogTitle') }}</button>
              <button v-if="canEditUser" class="flex items-center gap-2 rounded-lg border border-surface-dim px-4 py-2 text-sm font-medium text-secondary transition-colors hover:bg-surface-ground" @click="showUserDetailDialog = false; openRoleDialog(selectedUser!)"><i class="pi pi-shield text-xs"></i>{{ t('settings.users.manageRoles') }}</button>
              <button v-if="canResetPassword" class="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-4 py-2 text-sm font-medium text-amber-700 transition-colors hover:bg-amber-100" @click="showUserDetailDialog = false; openResetPasswordDialog(selectedUser!)"><i class="pi pi-key text-xs"></i>{{ t('settings.users.resetPassword.title') }}</button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- INVITE USER DIALOG -->
    <Teleport to="body">
      <div v-if="showInviteDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40" @click.self="showInviteDialog = false">
        <div class="w-full max-w-lg rounded-xl bg-white shadow-xl">
          <div class="flex items-center justify-between border-b border-surface-dim px-6 py-4">
            <h3 class="text-lg font-semibold text-secondary">{{ t('settings.users.inviteDialogTitle') }}</h3>
            <button class="rounded-lg p-1 text-tertiary hover:bg-surface-ground" @click="showInviteDialog = false"><i class="pi pi-times"></i></button>
          </div>
          <form class="p-6" @submit.prevent="handleSendInvitation">
            <div v-if="inviteDialogError" class="mb-4 flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              <i class="pi pi-exclamation-circle mt-0.5 text-sm"></i>
              <p>{{ inviteDialogError }}</p>
            </div>
            <div class="space-y-4">
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.users.fields.email') }} *</label>
                <input v-model="inviteForm.email" type="email" required dir="ltr" class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
              </div>
              <div class="grid grid-cols-2 gap-4">
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.users.fields.firstNameAr') }} *</label>
                  <input v-model="inviteForm.firstNameAr" type="text" required class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
                </div>
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.users.fields.lastNameAr') }} *</label>
                  <input v-model="inviteForm.lastNameAr" type="text" required class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
                </div>
              </div>
              <div class="grid grid-cols-2 gap-4">
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.users.fields.firstNameEn') }}</label>
                  <input v-model="inviteForm.firstNameEn" type="text" dir="ltr" class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
                </div>
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.users.fields.lastNameEn') }}</label>
                  <input v-model="inviteForm.lastNameEn" type="text" dir="ltr" class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
                </div>
              </div>
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.users.fields.role') }}</label>
                <select v-model="inviteForm.roleId" class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
                  <option :value="null">{{ t('settings.users.selectRole') }}</option>
                  <option v-for="role in roles" :key="role.id" :value="role.id">{{ getRoleName(role) }}</option>
                </select>
              </div>
            </div>
            <div class="mt-6 flex items-center justify-end gap-3">
              <button type="button" class="rounded-lg border border-surface-dim px-5 py-2.5 text-sm font-medium text-secondary transition-colors hover:bg-surface-ground" @click="showInviteDialog = false">{{ t('common.cancel') }}</button>
              <button type="submit" :disabled="isSubmitting" class="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-primary-dark disabled:opacity-50">
                <i v-if="isSubmitting" class="pi pi-spin pi-spinner text-xs"></i>{{ t('settings.users.sendInvitation') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- EDIT USER DIALOG -->
    <Teleport to="body">
      <div v-if="showEditDialog && selectedUser" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40" @click.self="showEditDialog = false">
        <div class="w-full max-w-lg rounded-xl bg-white shadow-xl">
          <div class="flex items-center justify-between border-b border-surface-dim px-6 py-4">
            <h3 class="text-lg font-semibold text-secondary">{{ t('settings.users.editDialogTitle') }}</h3>
            <button class="rounded-lg p-1 text-tertiary hover:bg-surface-ground" @click="showEditDialog = false"><i class="pi pi-times"></i></button>
          </div>
          <form class="p-6" @submit.prevent="handleUpdateUser">
            <div v-if="editDialogError" class="mb-4 flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              <i class="pi pi-exclamation-circle mt-0.5 text-sm"></i>
              <p>{{ editDialogError }}</p>
            </div>
            <!-- User info header -->
            <div class="mb-5 flex items-center gap-3 rounded-lg border border-surface-dim bg-surface-ground p-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">{{ selectedUser.firstName?.charAt(0) || '' }}{{ selectedUser.lastName?.charAt(0) || '' }}</div>
              <div>
                <p class="text-sm font-medium text-secondary" dir="ltr">{{ selectedUser.email }}</p>
                <div class="flex flex-wrap gap-1 mt-1">
                  <span v-for="role in selectedUser.roles" :key="role.roleId" class="inline-block rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">{{ locale === 'ar' ? role.nameAr : role.nameEn }}</span>
                </div>
              </div>
            </div>
            <div class="space-y-4">
              <div class="grid grid-cols-2 gap-4">
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.users.fields.firstName') }} *</label>
                  <input v-model="editForm.firstName" type="text" required class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
                </div>
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.users.fields.lastName') }} *</label>
                  <input v-model="editForm.lastName" type="text" required class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
                </div>
              </div>
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.users.fields.phone') }}</label>
                <input v-model="editForm.phoneNumber" type="tel" dir="ltr" class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" :placeholder="'+966 5xx xxx xxxx'" />
              </div>
              <div class="grid grid-cols-2 gap-4 rounded-lg border border-surface-dim bg-surface-ground/50 p-3">
                <div>
                  <p class="text-xs text-tertiary">{{ t('settings.users.table.status') }}</p>
                  <span :class="[getStatusBadge(selectedUser.isActive).bgClass, getStatusBadge(selectedUser.isActive).textClass]" class="mt-1 inline-block rounded-full px-2.5 py-0.5 text-xs font-medium">{{ getStatusBadge(selectedUser.isActive).label }}</span>
                </div>
                <div>
                  <p class="text-xs text-tertiary">{{ t('settings.users.table.lastLogin') }}</p>
                  <p class="mt-1 text-sm font-medium text-secondary">{{ formatDateTime(selectedUser.lastLoginAt) }}</p>
                </div>
              </div>
            </div>
            <div class="mt-6 flex items-center justify-end gap-3">
              <button type="button" class="rounded-lg border border-surface-dim px-5 py-2.5 text-sm font-medium text-secondary transition-colors hover:bg-surface-ground" @click="showEditDialog = false">{{ t('common.cancel') }}</button>
              <button type="submit" :disabled="isSubmitting" class="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-primary-dark disabled:opacity-50">
                <i v-if="isSubmitting" class="pi pi-spin pi-spinner text-xs"></i>{{ t('common.save') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- MANAGE ROLES DIALOG -->
    <Teleport to="body">
      <div v-if="showRoleDialog && selectedUser" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40" @click.self="showRoleDialog = false">
        <div class="w-full max-w-md rounded-xl bg-white shadow-xl">
          <div class="flex items-center justify-between border-b border-surface-dim px-6 py-4">
            <h3 class="text-lg font-semibold text-secondary">{{ t('settings.users.rolesDialogTitle') }}</h3>
            <button class="rounded-lg p-1 text-tertiary hover:bg-surface-ground" @click="showRoleDialog = false"><i class="pi pi-times"></i></button>
          </div>
          <div class="p-6">
            <!-- User info -->
            <div class="mb-4 flex items-center gap-3 rounded-lg border border-surface-dim bg-surface-ground p-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">{{ selectedUser.firstName?.charAt(0) || '' }}{{ selectedUser.lastName?.charAt(0) || '' }}</div>
              <div>
                <p class="text-sm font-medium text-secondary">{{ selectedUser.firstName }} {{ selectedUser.lastName }}</p>
                <p class="text-xs text-tertiary" dir="ltr">{{ selectedUser.email }}</p>
              </div>
            </div>
            <!-- Current roles -->
            <div class="mb-4">
              <h4 class="mb-2 text-sm font-medium text-secondary">{{ t('settings.users.currentRoles') }} ({{ selectedUser.roles.length }})</h4>
              <div v-if="selectedUser.roles.length === 0" class="rounded-lg border border-dashed border-surface-dim bg-surface-ground/50 p-4 text-center text-sm text-tertiary">{{ t('settings.users.noRolesAssigned') }}</div>
              <div v-else class="space-y-2">
                <div v-for="role in selectedUser.roles" :key="role.roleId" class="flex items-center justify-between rounded-lg border border-surface-dim bg-surface-ground px-3 py-2">
                  <div class="flex items-center gap-2">
                    <i class="pi pi-shield text-sm text-primary"></i>
                    <div>
                      <p class="text-sm font-medium text-secondary">{{ locale === 'ar' ? role.nameAr : role.nameEn }}</p>
                      <p class="text-xs text-tertiary">{{ t('settings.users.assignedAt') }}: {{ formatDate(role.assignedAt) }}</p>
                    </div>
                  </div>
                  <button class="rounded-lg p-1.5 text-tertiary transition-colors hover:bg-red-50 hover:text-red-600" :title="t('settings.users.removeRole')" @click="handleRemoveRole(selectedUser!.id, role.roleId)"><i class="pi pi-trash text-sm"></i></button>
                </div>
              </div>
            </div>
            <!-- Assign new role -->
            <div class="border-t border-surface-dim pt-4">
              <h4 class="mb-2 text-sm font-medium text-secondary">{{ t('settings.users.assignNewRole') }}</h4>
              <div v-if="availableRolesForAssignment.length === 0" class="text-sm text-tertiary">{{ t('settings.users.allRolesAssigned') }}</div>
              <div v-else class="flex items-center gap-2">
                <select v-model="selectedRoleId" class="flex-1 rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary">
                  <option value="">{{ t('settings.users.selectRole') }}</option>
                  <option v-for="role in availableRolesForAssignment" :key="role.id" :value="role.id">{{ getRoleName(role) }}</option>
                </select>
                <button :disabled="!selectedRoleId || isSubmitting" class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-primary-dark disabled:opacity-50" @click="handleAssignRole">
                  <i v-if="isSubmitting" class="pi pi-spin pi-spinner text-xs"></i>
                  <i v-else class="pi pi-plus text-xs"></i>{{ t('settings.users.assign') }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- RESET PASSWORD DIALOG -->
    <Teleport to="body">
      <div v-if="showResetPasswordDialog && selectedUser" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40" @click.self="showResetPasswordDialog = false">
        <div class="w-full max-w-lg rounded-xl bg-white shadow-xl">
          <div class="flex items-center justify-between border-b border-surface-dim px-6 py-4">
            <div class="flex items-center gap-2">
              <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-amber-50">
                <i class="pi pi-key text-amber-600"></i>
              </div>
              <h3 class="text-lg font-semibold text-secondary">{{ t('settings.users.resetPassword.title') }}</h3>
            </div>
            <button class="rounded-lg p-1 text-tertiary hover:bg-surface-ground" @click="showResetPasswordDialog = false"><i class="pi pi-times"></i></button>
          </div>
          <form class="p-6" @submit.prevent="handleResetPassword">
            <div v-if="resetPasswordDialogError" class="mb-4 flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              <i class="pi pi-exclamation-circle mt-0.5 text-sm"></i>
              <p>{{ resetPasswordDialogError }}</p>
            </div>
            <!-- User info header -->
            <div class="mb-5 flex items-center gap-3 rounded-lg border border-surface-dim bg-surface-ground p-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">{{ selectedUser.firstName?.charAt(0) || '' }}{{ selectedUser.lastName?.charAt(0) || '' }}</div>
              <div>
                <p class="text-sm font-medium text-secondary">{{ selectedUser.firstName }} {{ selectedUser.lastName }}</p>
                <p class="text-xs text-tertiary" dir="ltr">{{ selectedUser.email }}</p>
              </div>
            </div>

            <!-- Warning -->
            <div class="mb-5 flex items-start gap-2 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-700">
              <i class="pi pi-exclamation-triangle mt-0.5 text-sm"></i>
              <p>{{ t('settings.users.resetPassword.warning') }}</p>
            </div>

            <div class="space-y-4">
              <!-- New Password -->
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.users.resetPassword.newPassword') }} *</label>
                <div class="relative">
                  <input v-model="resetPasswordForm.newPassword" :type="showNewPassword ? 'text' : 'password'" required minlength="8" dir="ltr" class="w-full rounded-lg border border-surface-dim bg-surface-ground py-2.5 ps-4 pe-10 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" :placeholder="t('settings.users.resetPassword.newPasswordPlaceholder')" @input="checkPasswordStrength(resetPasswordForm.newPassword)" />
                  <button type="button" class="absolute end-3 top-1/2 -translate-y-1/2 text-tertiary hover:text-secondary" @click="showNewPassword = !showNewPassword">
                    <i :class="['pi text-sm', showNewPassword ? 'pi-eye-slash' : 'pi-eye']"></i>
                  </button>
                </div>
                <!-- Password Strength -->
                <div v-if="passwordStrength.level" class="mt-2">
                  <div class="flex items-center gap-2">
                    <div class="h-1.5 flex-1 overflow-hidden rounded-full bg-gray-200">
                      <div :class="[passwordStrength.color, 'h-full rounded-full transition-all']" :style="{ width: passwordStrength.level === 'weak' ? '33%' : passwordStrength.level === 'medium' ? '66%' : '100%' }"></div>
                    </div>
                    <span class="text-xs font-medium" :class="passwordStrength.level === 'weak' ? 'text-red-600' : passwordStrength.level === 'medium' ? 'text-amber-600' : 'text-green-600'">{{ passwordStrength.label }}</span>
                  </div>
                </div>
              </div>

              <!-- Confirm Password -->
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.users.resetPassword.confirmPassword') }} *</label>
                <div class="relative">
                  <input v-model="resetPasswordForm.confirmPassword" :type="showConfirmPassword ? 'text' : 'password'" required minlength="8" dir="ltr" class="w-full rounded-lg border border-surface-dim bg-surface-ground py-2.5 ps-4 pe-10 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" :placeholder="t('settings.users.resetPassword.confirmPasswordPlaceholder')" />
                  <button type="button" class="absolute end-3 top-1/2 -translate-y-1/2 text-tertiary hover:text-secondary" @click="showConfirmPassword = !showConfirmPassword">
                    <i :class="['pi text-sm', showConfirmPassword ? 'pi-eye-slash' : 'pi-eye']"></i>
                  </button>
                </div>
                <p v-if="resetPasswordForm.confirmPassword && resetPasswordForm.newPassword !== resetPasswordForm.confirmPassword" class="mt-1 text-xs text-red-600">{{ t('settings.users.resetPassword.passwordMismatch') }}</p>
              </div>

              <!-- Generate Random Password -->
              <button type="button" class="flex items-center gap-2 rounded-lg border border-dashed border-primary/30 bg-primary/5 px-4 py-2.5 text-sm font-medium text-primary transition-colors hover:bg-primary/10" @click="generateRandomPassword">
                <i class="pi pi-refresh text-sm"></i>
                {{ t('settings.users.resetPassword.generateRandom') }}
              </button>

              <!-- Options -->
              <div class="rounded-lg border border-surface-dim bg-surface-ground/50 p-4 space-y-3">
                <label class="flex items-center gap-3 cursor-pointer">
                  <input v-model="resetPasswordForm.notifyUser" type="checkbox" class="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary" />
                  <div>
                    <p class="text-sm font-medium text-secondary">{{ t('settings.users.resetPassword.notifyUser') }}</p>
                    <p class="text-xs text-tertiary">{{ t('settings.users.resetPassword.notifyUserDesc') }}</p>
                  </div>
                </label>
                <label class="flex items-center gap-3 cursor-pointer">
                  <input v-model="resetPasswordForm.forceChangeOnLogin" type="checkbox" class="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary" />
                  <div>
                    <p class="text-sm font-medium text-secondary">{{ t('settings.users.resetPassword.forceChange') }}</p>
                    <p class="text-xs text-tertiary">{{ t('settings.users.resetPassword.forceChangeDesc') }}</p>
                  </div>
                </label>
              </div>

              <!-- Password Policy -->
              <div class="rounded-lg border border-blue-100 bg-blue-50 px-4 py-3">
                <p class="mb-1 text-xs font-medium text-blue-700">{{ t('settings.users.resetPassword.policyTitle') }}</p>
                <ul class="space-y-0.5 text-xs text-blue-600">
                  <li class="flex items-center gap-1"><i class="pi pi-check text-xs"></i>{{ t('settings.users.resetPassword.policyMinLength') }}</li>
                  <li class="flex items-center gap-1"><i class="pi pi-check text-xs"></i>{{ t('settings.users.resetPassword.policyUppercase') }}</li>
                  <li class="flex items-center gap-1"><i class="pi pi-check text-xs"></i>{{ t('settings.users.resetPassword.policyLowercase') }}</li>
                  <li class="flex items-center gap-1"><i class="pi pi-check text-xs"></i>{{ t('settings.users.resetPassword.policyDigit') }}</li>
                  <li class="flex items-center gap-1"><i class="pi pi-check text-xs"></i>{{ t('settings.users.resetPassword.policySpecial') }}</li>
                </ul>
              </div>
            </div>

            <div class="mt-6 flex items-center justify-end gap-3">
              <button type="button" class="rounded-lg border border-surface-dim px-5 py-2.5 text-sm font-medium text-secondary transition-colors hover:bg-surface-ground" @click="showResetPasswordDialog = false">{{ t('common.cancel') }}</button>
              <button type="submit" :disabled="isSubmitting || !resetPasswordForm.newPassword || resetPasswordForm.newPassword !== resetPasswordForm.confirmPassword" class="flex items-center gap-2 rounded-lg bg-amber-600 px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-amber-700 disabled:opacity-50">
                <i v-if="isSubmitting" class="pi pi-spin pi-spinner text-xs"></i>
                <i v-else class="pi pi-key text-xs"></i>
                {{ t('settings.users.resetPassword.submit') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- AI Assistant -->
    <AiUserManagementAssistant />
  </div>
</template>
