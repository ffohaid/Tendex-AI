<script setup lang="ts">
/**
 * UsersManagementView — User Management Page.
 *
 * TASK-903: Displays a paginated list of users with the ability to
 * invite new users, edit profiles, toggle status, and manage roles.
 * Also includes an invitations tab for managing pending invitations.
 * Data is fetched dynamically from the API (no mock data).
 *
 * Features:
 * - Paginated user list
 * - Invite new user dialog
 * - Edit user profile dialog
 * - Toggle user active/inactive status
 * - Assign/Remove roles
 * - Invitations management (resend, revoke)
 * - RTL/LTR support with Tailwind logical properties
 * - English numerals exclusively
 */
import { ref, onMounted, computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  fetchUsers,
  fetchUserById,
  updateUser,
  toggleUserStatus,
  assignRole,
  removeRole,
  fetchRoles,
  fetchInvitations,
  sendInvitation,
  resendInvitation,
  revokeInvitation,
} from '@/services/userManagementService'
import type {
  UserDto,
  RoleDto,
  InvitationDto,
  SendInvitationRequest,
  UpdateUserRequest,
} from '@/types/userManagement'
import { useFormatters } from '@/composables/useFormatters'

const { t, locale } = useI18n()
const { formatDate, formatDateTime } = useFormatters()

/* ------------------------------------------------------------------ */
/*  State                                                              */
/* ------------------------------------------------------------------ */

/* Tabs */
const activeTab = ref<'users' | 'invitations'>('users')

/* Users */
const users = ref<UserDto[]>([])
const isLoading = ref(false)
const error = ref('')
const successMessage = ref('')
const currentPage = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)

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
const isSubmitting = ref(false)

/* Selected user for edit/role management */
const selectedUser = ref<UserDto | null>(null)

/* Invite form */
const inviteForm = ref<SendInvitationRequest>({
  email: '',
  firstNameAr: '',
  lastNameAr: '',
  firstNameEn: '',
  lastNameEn: '',
  roleId: null,
})

/* Edit form */
const editForm = ref<UpdateUserRequest>({
  firstName: '',
  lastName: '',
  phoneNumber: '',
})

/* Role assignment */
const selectedRoleId = ref('')

/* ------------------------------------------------------------------ */
/*  Computed                                                           */
/* ------------------------------------------------------------------ */
const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value))
const invTotalPages = computed(() => Math.ceil(invTotalCount.value / pageSize.value))

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */
function getStatusBadge(isActive: boolean) {
  return isActive
    ? { label: t('settings.users.statuses.active'), bgClass: 'bg-green-50', textClass: 'text-green-700' }
    : { label: t('settings.users.statuses.inactive'), bgClass: 'bg-red-50', textClass: 'text-red-700' }
}

function getInvitationStatusBadge(status: string) {
  const map: Record<string, { label: string; bgClass: string; textClass: string }> = {
    Pending: { label: t('settings.users.invStatuses.pending'), bgClass: 'bg-amber-50', textClass: 'text-amber-700' },
    Accepted: { label: t('settings.users.invStatuses.accepted'), bgClass: 'bg-green-50', textClass: 'text-green-700' },
    Expired: { label: t('settings.users.invStatuses.expired'), bgClass: 'bg-gray-50', textClass: 'text-gray-600' },
    Revoked: { label: t('settings.users.invStatuses.revoked'), bgClass: 'bg-red-50', textClass: 'text-red-700' },
  }
  return map[status] || map.Pending
}

function getUserRoleNames(user: UserDto): string {
  if (!user.roles || user.roles.length === 0) return '—'
  return user.roles.map(r => locale.value === 'ar' ? r.nameAr : r.nameEn).join(', ')
}

function getRoleName(role: RoleDto): string {
  return locale.value === 'ar' ? role.nameAr : role.nameEn
}

/* ------------------------------------------------------------------ */
/*  API Calls                                                          */
/* ------------------------------------------------------------------ */
async function loadUsers() {
  isLoading.value = true
  error.value = ''
  try {
    const result = await fetchUsers({ page: currentPage.value, pageSize: pageSize.value })
    users.value = result.items
    totalCount.value = result.totalCount
  } catch {
    error.value = t('settings.users.errors.loadFailed')
    users.value = []
  } finally {
    isLoading.value = false
  }
}

async function loadRoles() {
  try {
    roles.value = await fetchRoles()
  } catch {
    /* Roles load failure is non-critical for list display */
  }
}

async function loadInvitations() {
  isLoadingInvitations.value = true
  try {
    const result = await fetchInvitations({ page: invCurrentPage.value, pageSize: pageSize.value })
    invitations.value = result.items
    invTotalCount.value = result.totalCount
  } catch {
    error.value = t('settings.users.errors.loadInvitationsFailed')
    invitations.value = []
  } finally {
    isLoadingInvitations.value = false
  }
}

async function handleInviteUser() {
  isSubmitting.value = true
  error.value = ''
  try {
    await sendInvitation(inviteForm.value)
    showInviteDialog.value = false
    resetInviteForm()
    successMessage.value = t('settings.users.inviteSentSuccess')
    await loadInvitations()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch {
    error.value = t('settings.users.errors.inviteFailed')
  } finally {
    isSubmitting.value = false
  }
}

async function handleUpdateUser() {
  if (!selectedUser.value) return
  isSubmitting.value = true
  error.value = ''
  try {
    await updateUser(selectedUser.value.id, editForm.value)
    showEditDialog.value = false
    successMessage.value = t('settings.users.updateSuccess')
    await loadUsers()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch {
    error.value = t('settings.users.errors.updateFailed')
  } finally {
    isSubmitting.value = false
  }
}

async function handleToggleStatus(user: UserDto) {
  try {
    await toggleUserStatus(user.id, { activate: !user.isActive })
    successMessage.value = user.isActive
      ? t('settings.users.deactivateSuccess')
      : t('settings.users.activateSuccess')
    await loadUsers()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch {
    error.value = t('settings.users.errors.toggleStatusFailed')
  }
}

async function handleAssignRole() {
  if (!selectedUser.value || !selectedRoleId.value) return
  isSubmitting.value = true
  error.value = ''
  try {
    await assignRole(selectedUser.value.id, { roleId: selectedRoleId.value })
    successMessage.value = t('settings.users.roleAssignSuccess')
    /* Reload user detail */
    const updated = await fetchUserById(selectedUser.value.id)
    selectedUser.value = updated
    selectedRoleId.value = ''
    await loadUsers()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch {
    error.value = t('settings.users.errors.roleAssignFailed')
  } finally {
    isSubmitting.value = false
  }
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
  } catch {
    error.value = t('settings.users.errors.roleRemoveFailed')
  }
}

async function handleResendInvitation(invitationId: string) {
  try {
    await resendInvitation(invitationId)
    successMessage.value = t('settings.users.inviteResendSuccess')
    await loadInvitations()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch {
    error.value = t('settings.users.errors.resendFailed')
  }
}

async function handleRevokeInvitation(invitationId: string) {
  try {
    await revokeInvitation(invitationId)
    successMessage.value = t('settings.users.inviteRevokeSuccess')
    await loadInvitations()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch {
    error.value = t('settings.users.errors.revokeFailed')
  }
}

/* ------------------------------------------------------------------ */
/*  Form Helpers                                                       */
/* ------------------------------------------------------------------ */
function openInviteDialog() {
  resetInviteForm()
  showInviteDialog.value = true
}

function openEditDialog(user: UserDto) {
  selectedUser.value = user
  editForm.value = {
    firstName: user.firstName,
    lastName: user.lastName,
    phoneNumber: user.phoneNumber || '',
  }
  showEditDialog.value = true
}

function openRoleDialog(user: UserDto) {
  selectedUser.value = user
  selectedRoleId.value = ''
  showRoleDialog.value = true
}

function resetInviteForm() {
  inviteForm.value = {
    email: '',
    firstNameAr: '',
    lastNameAr: '',
    firstNameEn: '',
    lastNameEn: '',
    roleId: null,
  }
}

/* ------------------------------------------------------------------ */
/*  Pagination                                                         */
/* ------------------------------------------------------------------ */
function goToPage(page: number) {
  if (page >= 1 && page <= totalPages.value) {
    currentPage.value = page
  }
}

function goToInvPage(page: number) {
  if (page >= 1 && page <= invTotalPages.value) {
    invCurrentPage.value = page
  }
}

/* ------------------------------------------------------------------ */
/*  Watchers                                                           */
/* ------------------------------------------------------------------ */
watch(currentPage, () => loadUsers())
watch(invCurrentPage, () => loadInvitations())
watch(activeTab, (tab) => {
  if (tab === 'invitations' && invitations.value.length === 0) {
    loadInvitations()
  }
})

/* ------------------------------------------------------------------ */
/*  Lifecycle                                                          */
/* ------------------------------------------------------------------ */
onMounted(() => {
  loadUsers()
  loadRoles()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-secondary">
          {{ t('settings.users.title') }}
        </h1>
        <p class="mt-1 text-sm text-tertiary">
          {{ t('settings.users.subtitle') }}
        </p>
      </div>
      <button
        class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-primary-dark"
        @click="openInviteDialog"
      >
        <i class="pi pi-user-plus text-xs"></i>
        {{ t('settings.users.inviteUser') }}
      </button>
    </div>

    <!-- Success Banner -->
    <div
      v-if="successMessage"
      class="flex items-center gap-3 rounded-lg border border-green-200 bg-green-50 p-4"
    >
      <i class="pi pi-check-circle text-lg text-green-600"></i>
      <p class="flex-1 text-sm text-green-700">{{ successMessage }}</p>
      <button class="text-xs font-medium text-green-600 hover:underline" @click="successMessage = ''">
        {{ t('common.close') }}
      </button>
    </div>

    <!-- Error Banner -->
    <div
      v-if="error"
      class="flex items-center gap-3 rounded-lg border border-danger/20 bg-danger/5 p-4"
    >
      <i class="pi pi-exclamation-triangle text-lg text-danger"></i>
      <p class="flex-1 text-sm text-danger">{{ error }}</p>
      <button class="text-xs font-medium text-danger hover:underline" @click="error = ''">
        {{ t('common.close') }}
      </button>
    </div>

    <!-- Tabs -->
    <div class="flex gap-1 rounded-lg border border-surface-dim bg-surface-ground p-1">
      <button
        :class="[
          'flex-1 rounded-md px-4 py-2 text-sm font-medium transition-all',
          activeTab === 'users'
            ? 'bg-white text-primary shadow-sm'
            : 'text-tertiary hover:text-secondary',
        ]"
        @click="activeTab = 'users'"
      >
        <i class="pi pi-users me-2 text-xs"></i>
        {{ t('settings.users.tabs.users') }}
      </button>
      <button
        :class="[
          'flex-1 rounded-md px-4 py-2 text-sm font-medium transition-all',
          activeTab === 'invitations'
            ? 'bg-white text-primary shadow-sm'
            : 'text-tertiary hover:text-secondary',
        ]"
        @click="activeTab = 'invitations'"
      >
        <i class="pi pi-envelope me-2 text-xs"></i>
        {{ t('settings.users.tabs.invitations') }}
      </button>
    </div>

    <!-- ============================================================ -->
    <!-- USERS TAB                                                     -->
    <!-- ============================================================ -->
    <template v-if="activeTab === 'users'">
      <!-- Loading -->
      <div v-if="isLoading" class="flex items-center justify-center py-16">
        <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
      </div>

      <!-- Empty State -->
      <div
        v-else-if="users.length === 0"
        class="flex flex-col items-center justify-center rounded-lg border border-surface-dim bg-white py-16"
      >
        <i class="pi pi-users text-5xl text-surface-dim"></i>
        <p class="mt-4 text-sm text-tertiary">{{ t('settings.users.emptyState') }}</p>
        <button
          class="mt-4 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary-dark"
          @click="openInviteDialog"
        >
          {{ t('settings.users.inviteUser') }}
        </button>
      </div>

      <!-- Users Table -->
      <div v-else class="overflow-hidden rounded-lg border border-surface-dim bg-white">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-surface-dim bg-surface-ground">
                <th class="px-4 py-3 text-start font-semibold text-secondary">
                  {{ t('settings.users.table.name') }}
                </th>
                <th class="px-4 py-3 text-start font-semibold text-secondary">
                  {{ t('settings.users.table.email') }}
                </th>
                <th class="px-4 py-3 text-start font-semibold text-secondary">
                  {{ t('settings.users.table.roles') }}
                </th>
                <th class="px-4 py-3 text-center font-semibold text-secondary">
                  {{ t('settings.users.table.status') }}
                </th>
                <th class="px-4 py-3 text-start font-semibold text-secondary">
                  {{ t('settings.users.table.lastLogin') }}
                </th>
                <th class="px-4 py-3 text-center font-semibold text-secondary">
                  {{ t('settings.users.table.actions') }}
                </th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="user in users"
                :key="user.id"
                class="border-b border-surface-dim last:border-b-0 transition-colors hover:bg-surface-ground/50"
              >
                <td class="px-4 py-3">
                  <div class="flex items-center gap-3">
                    <div class="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">
                      {{ user.firstName?.charAt(0) || '' }}{{ user.lastName?.charAt(0) || '' }}
                    </div>
                    <div>
                      <p class="font-medium text-secondary">{{ user.firstName }} {{ user.lastName }}</p>
                      <p v-if="user.phoneNumber" class="text-xs text-tertiary" dir="ltr">{{ user.phoneNumber }}</p>
                    </div>
                  </div>
                </td>
                <td class="px-4 py-3 text-tertiary" dir="ltr">{{ user.email }}</td>
                <td class="px-4 py-3 text-tertiary">{{ getUserRoleNames(user) }}</td>
                <td class="px-4 py-3 text-center">
                  <span
                    :class="[getStatusBadge(user.isActive).bgClass, getStatusBadge(user.isActive).textClass]"
                    class="inline-block rounded-full px-2.5 py-0.5 text-xs font-medium"
                  >
                    {{ getStatusBadge(user.isActive).label }}
                  </span>
                </td>
                <td class="px-4 py-3 text-tertiary">{{ formatDateTime(user.lastLoginAt) }}</td>
                <td class="px-4 py-3 text-center">
                  <div class="flex items-center justify-center gap-1">
                    <button
                      class="rounded-lg p-1.5 text-tertiary transition-colors hover:bg-surface-ground hover:text-primary"
                      :title="t('common.edit')"
                      @click="openEditDialog(user)"
                    >
                      <i class="pi pi-pencil text-sm"></i>
                    </button>
                    <button
                      class="rounded-lg p-1.5 text-tertiary transition-colors hover:bg-surface-ground hover:text-primary"
                      :title="t('settings.users.manageRoles')"
                      @click="openRoleDialog(user)"
                    >
                      <i class="pi pi-shield text-sm"></i>
                    </button>
                    <button
                      :class="[
                        'rounded-lg p-1.5 transition-colors',
                        user.isActive
                          ? 'text-tertiary hover:bg-red-50 hover:text-red-600'
                          : 'text-tertiary hover:bg-green-50 hover:text-green-600',
                      ]"
                      :title="user.isActive ? t('settings.users.deactivate') : t('settings.users.activate')"
                      @click="handleToggleStatus(user)"
                    >
                      <i :class="['pi text-sm', user.isActive ? 'pi-ban' : 'pi-check-circle']"></i>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div
          v-if="totalPages > 1"
          class="flex items-center justify-between border-t border-surface-dim px-4 py-3"
        >
          <p class="text-xs text-tertiary">
            {{ t('settings.users.pagination.showing', { from: (currentPage - 1) * pageSize + 1, to: Math.min(currentPage * pageSize, totalCount), total: totalCount }) }}
          </p>
          <div class="flex items-center gap-1">
            <button
              :disabled="currentPage <= 1"
              class="rounded-lg px-3 py-1.5 text-xs font-medium text-tertiary transition-colors hover:bg-surface-ground disabled:opacity-40"
              @click="goToPage(currentPage - 1)"
            >
              {{ t('common.previous') }}
            </button>
            <template v-for="page in totalPages" :key="page">
              <button
                v-if="page <= 5 || page === totalPages || Math.abs(page - currentPage) <= 1"
                :class="[
                  'rounded-lg px-3 py-1.5 text-xs font-medium transition-colors',
                  page === currentPage
                    ? 'bg-primary text-white'
                    : 'text-tertiary hover:bg-surface-ground',
                ]"
                @click="goToPage(page)"
              >
                {{ page }}
              </button>
              <span
                v-else-if="page === 6 || (page === totalPages - 1 && totalPages > 7)"
                class="px-1 text-xs text-tertiary"
              >...</span>
            </template>
            <button
              :disabled="currentPage >= totalPages"
              class="rounded-lg px-3 py-1.5 text-xs font-medium text-tertiary transition-colors hover:bg-surface-ground disabled:opacity-40"
              @click="goToPage(currentPage + 1)"
            >
              {{ t('common.next') }}
            </button>
          </div>
        </div>
      </div>
    </template>

    <!-- ============================================================ -->
    <!-- INVITATIONS TAB                                               -->
    <!-- ============================================================ -->
    <template v-if="activeTab === 'invitations'">
      <!-- Loading -->
      <div v-if="isLoadingInvitations" class="flex items-center justify-center py-16">
        <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
      </div>

      <!-- Empty State -->
      <div
        v-else-if="invitations.length === 0"
        class="flex flex-col items-center justify-center rounded-lg border border-surface-dim bg-white py-16"
      >
        <i class="pi pi-envelope text-5xl text-surface-dim"></i>
        <p class="mt-4 text-sm text-tertiary">{{ t('settings.users.emptyInvitations') }}</p>
        <button
          class="mt-4 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary-dark"
          @click="openInviteDialog"
        >
          {{ t('settings.users.inviteUser') }}
        </button>
      </div>

      <!-- Invitations Table -->
      <div v-else class="overflow-hidden rounded-lg border border-surface-dim bg-white">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-surface-dim bg-surface-ground">
                <th class="px-4 py-3 text-start font-semibold text-secondary">
                  {{ t('settings.users.invTable.name') }}
                </th>
                <th class="px-4 py-3 text-start font-semibold text-secondary">
                  {{ t('settings.users.invTable.email') }}
                </th>
                <th class="px-4 py-3 text-start font-semibold text-secondary">
                  {{ t('settings.users.invTable.role') }}
                </th>
                <th class="px-4 py-3 text-center font-semibold text-secondary">
                  {{ t('settings.users.invTable.status') }}
                </th>
                <th class="px-4 py-3 text-start font-semibold text-secondary">
                  {{ t('settings.users.invTable.expiresAt') }}
                </th>
                <th class="px-4 py-3 text-center font-semibold text-secondary">
                  {{ t('settings.users.invTable.actions') }}
                </th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="inv in invitations"
                :key="inv.id"
                class="border-b border-surface-dim last:border-b-0 transition-colors hover:bg-surface-ground/50"
              >
                <td class="px-4 py-3 font-medium text-secondary">
                  {{ locale === 'ar' ? `${inv.firstNameAr} ${inv.lastNameAr}` : `${inv.firstNameEn || inv.firstNameAr} ${inv.lastNameEn || inv.lastNameAr}` }}
                </td>
                <td class="px-4 py-3 text-tertiary" dir="ltr">{{ inv.email }}</td>
                <td class="px-4 py-3 text-tertiary">{{ inv.roleName || '—' }}</td>
                <td class="px-4 py-3 text-center">
                  <span
                    :class="[getInvitationStatusBadge(inv.status).bgClass, getInvitationStatusBadge(inv.status).textClass]"
                    class="inline-block rounded-full px-2.5 py-0.5 text-xs font-medium"
                  >
                    {{ getInvitationStatusBadge(inv.status).label }}
                  </span>
                </td>
                <td class="px-4 py-3 text-tertiary">{{ formatDate(inv.expiresAt) }}</td>
                <td class="px-4 py-3 text-center">
                  <div class="flex items-center justify-center gap-1">
                    <button
                      v-if="inv.status === 'Pending'"
                      class="rounded-lg p-1.5 text-tertiary transition-colors hover:bg-surface-ground hover:text-primary"
                      :title="t('settings.users.resendInvitation')"
                      @click="handleResendInvitation(inv.id)"
                    >
                      <i class="pi pi-refresh text-sm"></i>
                    </button>
                    <button
                      v-if="inv.status === 'Pending'"
                      class="rounded-lg p-1.5 text-tertiary transition-colors hover:bg-red-50 hover:text-red-600"
                      :title="t('settings.users.revokeInvitation')"
                      @click="handleRevokeInvitation(inv.id)"
                    >
                      <i class="pi pi-times text-sm"></i>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div
          v-if="invTotalPages > 1"
          class="flex items-center justify-between border-t border-surface-dim px-4 py-3"
        >
          <p class="text-xs text-tertiary">
            {{ t('settings.users.pagination.showing', { from: (invCurrentPage - 1) * pageSize + 1, to: Math.min(invCurrentPage * pageSize, invTotalCount), total: invTotalCount }) }}
          </p>
          <div class="flex items-center gap-1">
            <button
              :disabled="invCurrentPage <= 1"
              class="rounded-lg px-3 py-1.5 text-xs font-medium text-tertiary transition-colors hover:bg-surface-ground disabled:opacity-40"
              @click="goToInvPage(invCurrentPage - 1)"
            >
              {{ t('common.previous') }}
            </button>
            <button
              :disabled="invCurrentPage >= invTotalPages"
              class="rounded-lg px-3 py-1.5 text-xs font-medium text-tertiary transition-colors hover:bg-surface-ground disabled:opacity-40"
              @click="goToInvPage(invCurrentPage + 1)"
            >
              {{ t('common.next') }}
            </button>
          </div>
        </div>
      </div>
    </template>

    <!-- ============================================================ -->
    <!-- INVITE USER DIALOG                                            -->
    <!-- ============================================================ -->
    <Teleport to="body">
      <div
        v-if="showInviteDialog"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
        @click.self="showInviteDialog = false"
      >
        <div class="w-full max-w-lg rounded-xl bg-white shadow-xl">
          <div class="flex items-center justify-between border-b border-surface-dim px-6 py-4">
            <h3 class="text-lg font-semibold text-secondary">
              {{ t('settings.users.inviteDialogTitle') }}
            </h3>
            <button
              class="rounded-lg p-1 text-tertiary hover:bg-surface-ground"
              @click="showInviteDialog = false"
            >
              <i class="pi pi-times"></i>
            </button>
          </div>
          <form class="space-y-4 p-6" @submit.prevent="handleInviteUser">
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('settings.users.fields.email') }} <span class="text-danger">*</span>
              </label>
              <input
                v-model="inviteForm.email"
                type="email"
                required
                dir="ltr"
                class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>
            <div class="grid gap-4 md:grid-cols-2">
              <div>
                <label class="mb-1.5 block text-sm font-medium text-secondary">
                  {{ t('settings.users.fields.firstNameAr') }} <span class="text-danger">*</span>
                </label>
                <input
                  v-model="inviteForm.firstNameAr"
                  type="text"
                  required
                  dir="rtl"
                  class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>
              <div>
                <label class="mb-1.5 block text-sm font-medium text-secondary">
                  {{ t('settings.users.fields.lastNameAr') }} <span class="text-danger">*</span>
                </label>
                <input
                  v-model="inviteForm.lastNameAr"
                  type="text"
                  required
                  dir="rtl"
                  class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>
            </div>
            <div class="grid gap-4 md:grid-cols-2">
              <div>
                <label class="mb-1.5 block text-sm font-medium text-secondary">
                  {{ t('settings.users.fields.firstNameEn') }}
                </label>
                <input
                  v-model="inviteForm.firstNameEn"
                  type="text"
                  dir="ltr"
                  class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>
              <div>
                <label class="mb-1.5 block text-sm font-medium text-secondary">
                  {{ t('settings.users.fields.lastNameEn') }}
                </label>
                <input
                  v-model="inviteForm.lastNameEn"
                  type="text"
                  dir="ltr"
                  class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>
            </div>
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('settings.users.fields.role') }}
              </label>
              <select
                v-model="inviteForm.roleId"
                class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              >
                <option :value="null">{{ t('settings.users.selectRole') }}</option>
                <option v-for="role in roles" :key="role.id" :value="role.id">
                  {{ getRoleName(role) }}
                </option>
              </select>
            </div>
            <div class="flex items-center justify-end gap-3 pt-2">
              <button
                type="button"
                class="rounded-lg border border-surface-dim px-5 py-2.5 text-sm font-medium text-secondary transition-colors hover:bg-surface-ground"
                @click="showInviteDialog = false"
              >
                {{ t('common.cancel') }}
              </button>
              <button
                type="submit"
                :disabled="isSubmitting"
                class="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-primary-dark disabled:opacity-50"
              >
                <i v-if="isSubmitting" class="pi pi-spin pi-spinner text-xs"></i>
                {{ t('settings.users.sendInvitation') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- ============================================================ -->
    <!-- EDIT USER DIALOG                                              -->
    <!-- ============================================================ -->
    <Teleport to="body">
      <div
        v-if="showEditDialog"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
        @click.self="showEditDialog = false"
      >
        <div class="w-full max-w-md rounded-xl bg-white shadow-xl">
          <div class="flex items-center justify-between border-b border-surface-dim px-6 py-4">
            <h3 class="text-lg font-semibold text-secondary">
              {{ t('settings.users.editDialogTitle') }}
            </h3>
            <button
              class="rounded-lg p-1 text-tertiary hover:bg-surface-ground"
              @click="showEditDialog = false"
            >
              <i class="pi pi-times"></i>
            </button>
          </div>
          <form class="space-y-4 p-6" @submit.prevent="handleUpdateUser">
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('settings.users.fields.firstName') }} <span class="text-danger">*</span>
              </label>
              <input
                v-model="editForm.firstName"
                type="text"
                required
                class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('settings.users.fields.lastName') }} <span class="text-danger">*</span>
              </label>
              <input
                v-model="editForm.lastName"
                type="text"
                required
                class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>
            <div>
              <label class="mb-1.5 block text-sm font-medium text-secondary">
                {{ t('settings.users.fields.phone') }}
              </label>
              <input
                v-model="editForm.phoneNumber"
                type="tel"
                dir="ltr"
                class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>
            <div class="flex items-center justify-end gap-3 pt-2">
              <button
                type="button"
                class="rounded-lg border border-surface-dim px-5 py-2.5 text-sm font-medium text-secondary transition-colors hover:bg-surface-ground"
                @click="showEditDialog = false"
              >
                {{ t('common.cancel') }}
              </button>
              <button
                type="submit"
                :disabled="isSubmitting"
                class="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-primary-dark disabled:opacity-50"
              >
                <i v-if="isSubmitting" class="pi pi-spin pi-spinner text-xs"></i>
                {{ t('common.save') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- ============================================================ -->
    <!-- MANAGE ROLES DIALOG                                           -->
    <!-- ============================================================ -->
    <Teleport to="body">
      <div
        v-if="showRoleDialog && selectedUser"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
        @click.self="showRoleDialog = false"
      >
        <div class="w-full max-w-md rounded-xl bg-white shadow-xl">
          <div class="flex items-center justify-between border-b border-surface-dim px-6 py-4">
            <h3 class="text-lg font-semibold text-secondary">
              {{ t('settings.users.rolesDialogTitle') }}
            </h3>
            <button
              class="rounded-lg p-1 text-tertiary hover:bg-surface-ground"
              @click="showRoleDialog = false"
            >
              <i class="pi pi-times"></i>
            </button>
          </div>
          <div class="p-6">
            <!-- Current Roles -->
            <div class="mb-4">
              <h4 class="mb-2 text-sm font-medium text-secondary">
                {{ t('settings.users.currentRoles') }}
              </h4>
              <div v-if="selectedUser.roles.length === 0" class="text-sm text-tertiary">
                {{ t('settings.users.noRolesAssigned') }}
              </div>
              <div v-else class="space-y-2">
                <div
                  v-for="role in selectedUser.roles"
                  :key="role.roleId"
                  class="flex items-center justify-between rounded-lg border border-surface-dim bg-surface-ground px-3 py-2"
                >
                  <div>
                    <p class="text-sm font-medium text-secondary">
                      {{ locale === 'ar' ? role.nameAr : role.nameEn }}
                    </p>
                    <p class="text-xs text-tertiary">
                      {{ t('settings.users.assignedAt') }}: {{ formatDate(role.assignedAt) }}
                    </p>
                  </div>
                  <button
                    class="rounded-lg p-1 text-tertiary transition-colors hover:bg-red-50 hover:text-red-600"
                    :title="t('settings.users.removeRole')"
                    @click="handleRemoveRole(selectedUser!.id, role.roleId)"
                  >
                    <i class="pi pi-trash text-sm"></i>
                  </button>
                </div>
              </div>
            </div>

            <!-- Assign New Role -->
            <div class="border-t border-surface-dim pt-4">
              <h4 class="mb-2 text-sm font-medium text-secondary">
                {{ t('settings.users.assignNewRole') }}
              </h4>
              <div class="flex items-center gap-2">
                <select
                  v-model="selectedRoleId"
                  class="flex-1 rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                >
                  <option value="">{{ t('settings.users.selectRole') }}</option>
                  <option
                    v-for="role in roles.filter(r => !selectedUser!.roles.some(ur => ur.roleId === r.id))"
                    :key="role.id"
                    :value="role.id"
                  >
                    {{ getRoleName(role) }}
                  </option>
                </select>
                <button
                  :disabled="!selectedRoleId || isSubmitting"
                  class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-primary-dark disabled:opacity-50"
                  @click="handleAssignRole"
                >
                  <i v-if="isSubmitting" class="pi pi-spin pi-spinner text-xs"></i>
                  <i v-else class="pi pi-plus text-xs"></i>
                  {{ t('settings.users.assign') }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
