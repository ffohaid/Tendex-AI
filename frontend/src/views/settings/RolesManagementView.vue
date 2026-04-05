<script setup lang="ts">
/**
 * RolesManagementView — Roles & Permissions Management Page.
 *
 * Features:
 * - List all roles with user counts
 * - Create custom roles with permissions
 * - Edit role name, description, and permissions
 * - Toggle role active/inactive status
 * - View role details with assigned users and permissions
 * - Permissions grouped by module with select all
 * - Stats cards
 * - RTL/LTR support with Tailwind logical properties
 * - English numerals exclusively
 */
import { ref, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import AiUserManagementAssistant from '@/components/ai/AiUserManagementAssistant.vue'
import {
  fetchRoles, fetchRoleById, createRole, updateRole,
  toggleRoleStatus, fetchPermissions,
} from '@/services/userManagementService'
import type {
  RoleDto, RoleDetailDto, PermissionGroupDto, PermissionDto,
  CreateRoleRequest, UpdateRoleRequest,
} from '@/types/userManagement'
import { useFormatters } from '@/composables/useFormatters'

const { t, locale } = useI18n()
const { formatDate } = useFormatters()
const authStore = useAuthStore()

const canCreateRole = computed(() => authStore.hasPermission('roles.create'))
const canEditRole = computed(() => authStore.hasPermission('roles.edit'))

/* State */
const rolesData = ref<RoleDto[]>([])
const permissionGroups = ref<PermissionGroupDto[]>([])
const isLoading = ref(false)
const error = ref('')
const successMessage = ref('')

/* Dialogs */
const showCreateDialog = ref(false)
const showEditDialog = ref(false)
const showDetailDialog = ref(false)
const isSubmitting = ref(false)
const selectedRole = ref<RoleDetailDto | null>(null)

/* Create Form */
const createForm = ref<CreateRoleRequest>({
  nameAr: '', nameEn: '', descriptionAr: '', descriptionEn: '', permissionIds: [],
})

/* Edit Form */
const editForm = ref<UpdateRoleRequest>({
  nameAr: '', nameEn: '', description: '', permissionIds: [],
})

/* Computed */
const totalRoles = computed(() => rolesData.value.length)
const systemRolesCount = computed(() => rolesData.value.filter(r => r.isSystemRole).length)
const customRolesCount = computed(() => rolesData.value.filter(r => !r.isSystemRole).length)
const totalUsersInRoles = computed(() => rolesData.value.reduce((sum, r) => sum + r.userCount, 0))

const allPermissions = computed(() => {
  const result: PermissionDto[] = []
  for (const group of permissionGroups.value) {
    result.push(...group.permissions)
  }
  return result
})

/* Data Loading */
async function loadRoles() {
  isLoading.value = true
  error.value = ''
  try {
    rolesData.value = await fetchRoles()
  } catch (e: any) {
    const detail = e?.response?.data?.detail || e?.response?.data?.title || e?.message || ''
    error.value = detail ? `${t('settings.roles.errors.loadFailed')}: ${detail}` : t('settings.roles.errors.loadFailed')
  } finally {
    isLoading.value = false
  }
}

async function loadPermissions() {
  try {
    permissionGroups.value = await fetchPermissions()
  } catch { /* silent */ }
}

/* Helpers */
function getRoleName(role: RoleDto | RoleDetailDto): string {
  return locale.value === 'ar' ? role.nameAr : role.nameEn
}

function getPermissionName(perm: PermissionDto): string {
  return locale.value === 'ar' ? perm.nameAr : perm.nameEn
}

function getModuleName(module: string): string {
  const key = `settings.roles.modules.${module}`
  const translated = t(key)
  return translated === key ? module : translated
}

function getRoleTypeBadge(isSystem: boolean) {
  return isSystem
    ? { label: t('settings.roles.systemRole'), bgClass: 'bg-blue-50', textClass: 'text-blue-700' }
    : { label: t('settings.roles.customRole'), bgClass: 'bg-amber-50', textClass: 'text-amber-700' }
}

function getStatusBadge(isActive: boolean) {
  return isActive
    ? { label: t('settings.roles.active'), bgClass: 'bg-green-50', textClass: 'text-green-700' }
    : { label: t('settings.roles.inactive'), bgClass: 'bg-red-50', textClass: 'text-red-700' }
}

/* Create Role */
function openCreateDialog() {
  createForm.value = { nameAr: '', nameEn: '', descriptionAr: '', descriptionEn: '', permissionIds: [] }
  showCreateDialog.value = true
}

async function handleCreateRole() {
  isSubmitting.value = true
  error.value = ''
  try {
    await createRole(createForm.value)
    showCreateDialog.value = false
    successMessage.value = t('settings.roles.createSuccess')
    await loadRoles()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch (e: any) {
    const detail = e?.response?.data?.detail || e?.response?.data?.title || e?.message || ''
    error.value = detail ? `${t('settings.roles.errors.createFailed')}: ${detail}` : t('settings.roles.errors.createFailed')
  } finally {
    isSubmitting.value = false
  }
}

/* Edit Role */
async function openEditDialog(role: RoleDto) {
  try {
    const detail = await fetchRoleById(role.id)
    selectedRole.value = detail
    editForm.value = {
      nameAr: detail.nameAr,
      nameEn: detail.nameEn,
      description: detail.description || '',
      permissionIds: detail.permissions.map(p => p.id),
    }
    showEditDialog.value = true
  } catch (e: any) {
    const detail = e?.response?.data?.detail || e?.response?.data?.title || e?.message || ''
    error.value = detail ? `${t('settings.roles.errors.loadFailed')}: ${detail}` : t('settings.roles.errors.loadFailed')
  }
}

async function handleUpdateRole() {
  if (!selectedRole.value) return
  isSubmitting.value = true
  error.value = ''
  try {
    await updateRole(selectedRole.value.id, editForm.value)
    showEditDialog.value = false
    successMessage.value = t('settings.roles.updateSuccess')
    await loadRoles()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch (e: any) {
    const detail = e?.response?.data?.detail || e?.response?.data?.title || e?.message || ''
    error.value = detail ? `${t('settings.roles.errors.updateFailed')}: ${detail}` : t('settings.roles.errors.updateFailed')
  } finally {
    isSubmitting.value = false
  }
}

/* Toggle Role Status */
async function handleToggleRoleStatus(role: RoleDto) {
  try {
    await toggleRoleStatus(role.id, { activate: !role.isActive })
    successMessage.value = role.isActive
      ? t('settings.roles.deactivateSuccess')
      : t('settings.roles.activateSuccess')
    await loadRoles()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch (e: any) {
    const detail = e?.response?.data?.detail || e?.response?.data?.title || e?.message || ''
    error.value = detail ? `${t('settings.roles.errors.toggleFailed')}: ${detail}` : t('settings.roles.errors.toggleFailed')
  }
}

/* View Role Details */
async function openDetailDialog(role: RoleDto) {
  try {
    selectedRole.value = await fetchRoleById(role.id)
    showDetailDialog.value = true
  } catch (e: any) {
    const detail = e?.response?.data?.detail || e?.response?.data?.title || e?.message || ''
    error.value = detail ? `${t('settings.roles.errors.loadFailed')}: ${detail}` : t('settings.roles.errors.loadFailed')
  }
}

/* Permission Toggle Helpers */
function isPermissionSelected(permId: string, form: { permissionIds?: string[] | null }): boolean {
  return form.permissionIds?.includes(permId) ?? false
}

function togglePermission(permId: string, form: { permissionIds?: string[] | null }) {
  if (!form.permissionIds) form.permissionIds = []
  const idx = form.permissionIds.indexOf(permId)
  if (idx >= 0) form.permissionIds.splice(idx, 1)
  else form.permissionIds.push(permId)
}

function isGroupAllSelected(group: PermissionGroupDto, form: { permissionIds?: string[] | null }): boolean {
  if (!form.permissionIds) return false
  return group.permissions.every(p => form.permissionIds!.includes(p.id))
}

function toggleGroupAll(group: PermissionGroupDto, form: { permissionIds?: string[] | null }) {
  if (!form.permissionIds) form.permissionIds = []
  if (isGroupAllSelected(group, form)) {
    const groupIds = new Set(group.permissions.map(p => p.id))
    form.permissionIds = form.permissionIds.filter(id => !groupIds.has(id))
  } else {
    const existing = new Set(form.permissionIds)
    for (const p of group.permissions) {
      if (!existing.has(p.id)) form.permissionIds.push(p.id)
    }
  }
}

function selectAllPermissions(form: { permissionIds?: string[] | null }) {
  form.permissionIds = allPermissions.value.map(p => p.id)
}

function deselectAllPermissions(form: { permissionIds?: string[] | null }) {
  form.permissionIds = []
}

/* Lifecycle */
onMounted(() => {
  loadRoles()
  loadPermissions()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-secondary">{{ t('settings.roles.title') }}</h1>
        <p class="mt-1 text-sm text-tertiary">{{ t('settings.roles.subtitle') }}</p>
      </div>
      <button v-if="canCreateRole" class="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-primary-dark" @click="openCreateDialog">
        <i class="pi pi-plus text-sm"></i>
        {{ t('settings.roles.createRole') }}
      </button>
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-1 gap-4 sm:grid-cols-4">
      <div class="rounded-lg border border-surface-dim bg-white p-4">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10"><i class="pi pi-shield text-primary"></i></div>
          <div>
            <p class="text-2xl font-bold text-secondary">{{ totalRoles }}</p>
            <p class="text-xs text-tertiary">{{ t('settings.roles.stats.totalRoles') }}</p>
          </div>
        </div>
      </div>
      <div class="rounded-lg border border-surface-dim bg-white p-4">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-50"><i class="pi pi-lock text-blue-600"></i></div>
          <div>
            <p class="text-2xl font-bold text-secondary">{{ systemRolesCount }}</p>
            <p class="text-xs text-tertiary">{{ t('settings.roles.stats.systemRoles') }}</p>
          </div>
        </div>
      </div>
      <div class="rounded-lg border border-surface-dim bg-white p-4">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-amber-50"><i class="pi pi-cog text-amber-600"></i></div>
          <div>
            <p class="text-2xl font-bold text-secondary">{{ customRolesCount }}</p>
            <p class="text-xs text-tertiary">{{ t('settings.roles.stats.customRoles') }}</p>
          </div>
        </div>
      </div>
      <div class="rounded-lg border border-surface-dim bg-white p-4">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-green-50"><i class="pi pi-users text-green-600"></i></div>
          <div>
            <p class="text-2xl font-bold text-secondary">{{ totalUsersInRoles }}</p>
            <p class="text-xs text-tertiary">{{ t('settings.roles.stats.totalUsers') }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Messages -->
    <div v-if="successMessage" class="flex items-center gap-2 rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700">
      <i class="pi pi-check-circle text-sm"></i>{{ successMessage }}
      <button class="ms-auto text-green-600 hover:text-green-800" @click="successMessage = ''"><i class="pi pi-times text-xs"></i></button>
    </div>
    <div v-if="error" class="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      <i class="pi pi-exclamation-circle text-sm"></i>{{ error }}
      <button class="ms-auto text-red-600 hover:text-red-800" @click="error = ''"><i class="pi pi-times text-xs"></i></button>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="flex items-center justify-center py-16"><i class="pi pi-spin pi-spinner text-3xl text-primary"></i></div>

    <!-- Empty State -->
    <div v-else-if="rolesData.length === 0" class="flex flex-col items-center justify-center rounded-lg border border-surface-dim bg-white py-16">
      <i class="pi pi-shield text-5xl text-surface-dim"></i>
      <p class="mt-4 text-sm text-tertiary">{{ t('settings.roles.emptyState') }}</p>
      <button v-if="canCreateRole" class="mt-4 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary-dark" @click="openCreateDialog">{{ t('settings.roles.createRole') }}</button>
    </div>

    <!-- Roles Grid -->
    <div v-else class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <div v-for="role in rolesData" :key="role.id" class="group rounded-lg border border-surface-dim bg-white p-5 transition-all hover:border-primary/30 hover:shadow-md cursor-pointer" @click="openDetailDialog(role)">
        <div class="flex items-start justify-between">
          <div class="flex items-center gap-3">
            <div :class="['flex h-10 w-10 items-center justify-center rounded-lg', role.isSystemRole ? 'bg-blue-50' : 'bg-amber-50']">
              <i :class="['pi', role.isSystemRole ? 'pi-lock text-blue-600' : 'pi-cog text-amber-600']"></i>
            </div>
            <div>
              <h3 class="font-semibold text-secondary group-hover:text-primary">{{ getRoleName(role) }}</h3>
              <span :class="[getRoleTypeBadge(role.isSystemRole).bgClass, getRoleTypeBadge(role.isSystemRole).textClass]" class="mt-1 inline-block rounded-full px-2 py-0.5 text-xs font-medium">{{ getRoleTypeBadge(role.isSystemRole).label }}</span>
            </div>
          </div>
          <span :class="[getStatusBadge(role.isActive).bgClass, getStatusBadge(role.isActive).textClass]" class="rounded-full px-2.5 py-0.5 text-xs font-medium">{{ getStatusBadge(role.isActive).label }}</span>
        </div>
        <p v-if="role.description" class="mt-3 text-sm text-tertiary line-clamp-2">{{ role.description }}</p>
        <div class="mt-4 flex items-center justify-between border-t border-surface-dim pt-3">
          <div class="flex items-center gap-1 text-sm text-tertiary">
            <i class="pi pi-users text-xs"></i>
            <span>{{ role.userCount }} {{ t('settings.roles.usersCount') }}</span>
          </div>
          <div class="flex items-center gap-1 opacity-0 transition-opacity group-hover:opacity-100" @click.stop>
            <button v-if="!role.isSystemRole && canEditRole" class="rounded-lg p-1.5 text-tertiary transition-colors hover:bg-surface-ground hover:text-primary" :title="t('common.edit')" @click="openEditDialog(role)"><i class="pi pi-pencil text-sm"></i></button>
            <button v-if="!role.isSystemRole" :class="['rounded-lg p-1.5 transition-colors', role.isActive ? 'text-tertiary hover:bg-red-50 hover:text-red-600' : 'text-tertiary hover:bg-green-50 hover:text-green-600']" :title="role.isActive ? t('settings.roles.deactivateRole') : t('settings.roles.activateRole')" @click="handleToggleRoleStatus(role)">
              <i :class="['pi text-sm', role.isActive ? 'pi-ban' : 'pi-check-circle']"></i>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ROLE DETAIL DIALOG -->
    <Teleport to="body">
      <div v-if="showDetailDialog && selectedRole" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40" @click.self="showDetailDialog = false">
        <div class="w-full max-w-2xl max-h-[80vh] overflow-y-auto rounded-xl bg-white shadow-xl">
          <div class="sticky top-0 z-10 flex items-center justify-between border-b border-surface-dim bg-white px-6 py-4">
            <h3 class="text-lg font-semibold text-secondary">{{ t('settings.roles.detailTitle') }}</h3>
            <button class="rounded-lg p-1 text-tertiary hover:bg-surface-ground" @click="showDetailDialog = false"><i class="pi pi-times"></i></button>
          </div>
          <div class="p-6 space-y-6">
            <!-- Role Info -->
            <div class="flex items-center gap-4">
              <div :class="['flex h-14 w-14 items-center justify-center rounded-lg', selectedRole.isSystemRole ? 'bg-blue-50' : 'bg-amber-50']">
                <i :class="['pi text-xl', selectedRole.isSystemRole ? 'pi-lock text-blue-600' : 'pi-cog text-amber-600']"></i>
              </div>
              <div>
                <h4 class="text-lg font-semibold text-secondary">{{ getRoleName(selectedRole) }}</h4>
                <div class="flex items-center gap-2 mt-1">
                  <span :class="[getRoleTypeBadge(selectedRole.isSystemRole).bgClass, getRoleTypeBadge(selectedRole.isSystemRole).textClass]" class="rounded-full px-2 py-0.5 text-xs font-medium">{{ getRoleTypeBadge(selectedRole.isSystemRole).label }}</span>
                  <span :class="[getStatusBadge(selectedRole.isActive).bgClass, getStatusBadge(selectedRole.isActive).textClass]" class="rounded-full px-2 py-0.5 text-xs font-medium">{{ getStatusBadge(selectedRole.isActive).label }}</span>
                </div>
              </div>
            </div>
            <div v-if="selectedRole.description" class="rounded-lg border border-surface-dim bg-surface-ground p-4">
              <p class="text-sm text-tertiary">{{ selectedRole.description }}</p>
            </div>
            <!-- Role Names -->
            <div class="grid grid-cols-2 gap-4">
              <div>
                <p class="text-xs text-tertiary">{{ t('settings.roles.fields.nameAr') }}</p>
                <p class="text-sm font-medium text-secondary" dir="rtl">{{ selectedRole.nameAr }}</p>
              </div>
              <div>
                <p class="text-xs text-tertiary">{{ t('settings.roles.fields.nameEn') }}</p>
                <p class="text-sm font-medium text-secondary" dir="ltr">{{ selectedRole.nameEn }}</p>
              </div>
            </div>
            <!-- Permissions -->
            <div>
              <h5 class="mb-3 text-sm font-semibold text-secondary">{{ t('settings.roles.permissionsTitle') }} ({{ selectedRole.permissions.length }})</h5>
              <div v-if="selectedRole.permissions.length === 0" class="text-sm text-tertiary">{{ t('settings.roles.noPermissions') }}</div>
              <div v-else class="flex flex-wrap gap-2">
                <span v-for="perm in selectedRole.permissions" :key="perm.id" class="inline-block rounded-full bg-primary/10 px-3 py-1 text-xs font-medium text-primary">{{ getPermissionName(perm) }}</span>
              </div>
            </div>
            <!-- Users -->
            <div>
              <h5 class="mb-3 text-sm font-semibold text-secondary">{{ t('settings.roles.assignedUsers') }} ({{ selectedRole.userCount }})</h5>
              <div v-if="selectedRole.users.length === 0" class="text-sm text-tertiary">{{ t('settings.roles.noUsers') }}</div>
              <div v-else class="space-y-2">
                <div v-for="u in selectedRole.users" :key="u.userId" class="flex items-center justify-between rounded-lg border border-surface-dim bg-surface-ground px-3 py-2">
                  <p class="text-sm text-secondary">{{ u.userId }}</p>
                  <p class="text-xs text-tertiary">{{ formatDate(u.assignedAt) }}</p>
                </div>
              </div>
            </div>
            <!-- Meta -->
            <div class="grid grid-cols-2 gap-4 rounded-lg border border-surface-dim bg-surface-ground p-4">
              <div>
                <p class="text-xs text-tertiary">{{ t('settings.roles.fields.createdAt') }}</p>
                <p class="text-sm font-medium text-secondary">{{ formatDate(selectedRole.createdAt) }}</p>
              </div>
              <div>
                <p class="text-xs text-tertiary">{{ t('settings.roles.fields.userCount') }}</p>
                <p class="text-sm font-medium text-secondary">{{ selectedRole.userCount }}</p>
              </div>
            </div>
            <!-- Actions -->
            <div v-if="!selectedRole.isSystemRole" class="flex gap-2 border-t border-surface-dim pt-4">
              <button v-if="canEditRole" class="flex items-center gap-2 rounded-lg border border-surface-dim px-4 py-2 text-sm font-medium text-secondary transition-colors hover:bg-surface-ground" @click="showDetailDialog = false; openEditDialog(selectedRole as any)"><i class="pi pi-pencil text-xs"></i>{{ t('common.edit') }}</button>
              <button :class="['flex items-center gap-2 rounded-lg border px-4 py-2 text-sm font-medium transition-colors', selectedRole.isActive ? 'border-red-200 text-red-600 hover:bg-red-50' : 'border-green-200 text-green-600 hover:bg-green-50']" @click="handleToggleRoleStatus(selectedRole as any); showDetailDialog = false">
                <i :class="['pi text-xs', selectedRole.isActive ? 'pi-ban' : 'pi-check-circle']"></i>
                {{ selectedRole.isActive ? t('settings.roles.deactivateRole') : t('settings.roles.activateRole') }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- CREATE ROLE DIALOG -->
    <Teleport to="body">
      <div v-if="showCreateDialog" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40" @click.self="showCreateDialog = false">
        <div class="w-full max-w-2xl max-h-[85vh] overflow-y-auto rounded-xl bg-white shadow-xl">
          <div class="sticky top-0 z-10 flex items-center justify-between border-b border-surface-dim bg-white px-6 py-4">
            <h3 class="text-lg font-semibold text-secondary">{{ t('settings.roles.createRole') }}</h3>
            <button class="rounded-lg p-1 text-tertiary hover:bg-surface-ground" @click="showCreateDialog = false"><i class="pi pi-times"></i></button>
          </div>
          <form class="p-6" @submit.prevent="handleCreateRole">
            <div class="space-y-4">
              <div class="grid grid-cols-2 gap-4">
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.roles.fields.nameAr') }} *</label>
                  <input v-model="createForm.nameAr" type="text" required class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
                </div>
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.roles.fields.nameEn') }} *</label>
                  <input v-model="createForm.nameEn" type="text" required dir="ltr" class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
                </div>
              </div>
              <div class="grid grid-cols-2 gap-4">
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.roles.fields.descriptionAr') }}</label>
                  <textarea v-model="createForm.descriptionAr" rows="2" class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"></textarea>
                </div>
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.roles.fields.descriptionEn') }}</label>
                  <textarea v-model="createForm.descriptionEn" rows="2" dir="ltr" class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"></textarea>
                </div>
              </div>
              <!-- Permissions Section -->
              <div>
                <div class="flex items-center justify-between mb-3">
                  <h4 class="text-sm font-semibold text-secondary">{{ t('settings.roles.permissionsTitle') }}</h4>
                  <div class="flex items-center gap-2">
                    <button type="button" class="text-xs text-primary hover:underline" @click="selectAllPermissions(createForm)">{{ t('settings.roles.selectAll') }}</button>
                    <span class="text-xs text-tertiary">|</span>
                    <button type="button" class="text-xs text-tertiary hover:underline" @click="deselectAllPermissions(createForm)">{{ t('settings.roles.deselectAll') }}</button>
                  </div>
                </div>
                <div v-if="permissionGroups.length === 0" class="text-sm text-tertiary">{{ t('settings.roles.noPermissionsAvailable') }}</div>
                <div v-else class="space-y-4">
                  <div v-for="group in permissionGroups" :key="group.module" class="rounded-lg border border-surface-dim p-4">
                    <div class="flex items-center gap-2 mb-3">
                      <input type="checkbox" :checked="isGroupAllSelected(group, createForm)" class="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary" @change="toggleGroupAll(group, createForm)" />
                      <h5 class="text-sm font-medium text-secondary">{{ getModuleName(group.module) }}</h5>
                      <span class="text-xs text-tertiary">({{ group.permissions.length }})</span>
                    </div>
                    <div class="grid grid-cols-2 gap-2 ps-6">
                      <label v-for="perm in group.permissions" :key="perm.id" class="flex items-center gap-2 cursor-pointer rounded-lg px-2 py-1.5 transition-colors hover:bg-surface-ground">
                        <input type="checkbox" :checked="isPermissionSelected(perm.id, createForm)" class="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary" @change="togglePermission(perm.id, createForm)" />
                        <span class="text-sm text-secondary">{{ getPermissionName(perm) }}</span>
                      </label>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <div class="mt-6 flex items-center justify-end gap-3">
              <button type="button" class="rounded-lg border border-surface-dim px-5 py-2.5 text-sm font-medium text-secondary transition-colors hover:bg-surface-ground" @click="showCreateDialog = false">{{ t('common.cancel') }}</button>
              <button type="submit" :disabled="isSubmitting" class="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-primary-dark disabled:opacity-50">
                <i v-if="isSubmitting" class="pi pi-spin pi-spinner text-xs"></i>{{ t('common.save') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- EDIT ROLE DIALOG -->
    <Teleport to="body">
      <div v-if="showEditDialog && selectedRole" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40" @click.self="showEditDialog = false">
        <div class="w-full max-w-2xl max-h-[85vh] overflow-y-auto rounded-xl bg-white shadow-xl">
          <div class="sticky top-0 z-10 flex items-center justify-between border-b border-surface-dim bg-white px-6 py-4">
            <h3 class="text-lg font-semibold text-secondary">{{ t('settings.roles.editRole') }}</h3>
            <button class="rounded-lg p-1 text-tertiary hover:bg-surface-ground" @click="showEditDialog = false"><i class="pi pi-times"></i></button>
          </div>
          <form class="p-6" @submit.prevent="handleUpdateRole">
            <div class="space-y-4">
              <div class="grid grid-cols-2 gap-4">
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.roles.fields.nameAr') }} *</label>
                  <input v-model="editForm.nameAr" type="text" required class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
                </div>
                <div>
                  <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.roles.fields.nameEn') }} *</label>
                  <input v-model="editForm.nameEn" type="text" required dir="ltr" class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary" />
                </div>
              </div>
              <div>
                <label class="mb-1 block text-sm font-medium text-secondary">{{ t('settings.roles.fields.description') }}</label>
                <textarea v-model="editForm.description" rows="2" class="w-full rounded-lg border border-surface-dim bg-surface-ground px-4 py-2.5 text-sm text-secondary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"></textarea>
              </div>
              <!-- Permissions Section -->
              <div>
                <div class="flex items-center justify-between mb-3">
                  <h4 class="text-sm font-semibold text-secondary">{{ t('settings.roles.permissionsTitle') }}</h4>
                  <div class="flex items-center gap-2">
                    <button type="button" class="text-xs text-primary hover:underline" @click="selectAllPermissions(editForm)">{{ t('settings.roles.selectAll') }}</button>
                    <span class="text-xs text-tertiary">|</span>
                    <button type="button" class="text-xs text-tertiary hover:underline" @click="deselectAllPermissions(editForm)">{{ t('settings.roles.deselectAll') }}</button>
                  </div>
                </div>
                <div v-if="permissionGroups.length === 0" class="text-sm text-tertiary">{{ t('settings.roles.noPermissionsAvailable') }}</div>
                <div v-else class="space-y-4">
                  <div v-for="group in permissionGroups" :key="group.module" class="rounded-lg border border-surface-dim p-4">
                    <div class="flex items-center gap-2 mb-3">
                      <input type="checkbox" :checked="isGroupAllSelected(group, editForm)" class="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary" @change="toggleGroupAll(group, editForm)" />
                      <h5 class="text-sm font-medium text-secondary">{{ getModuleName(group.module) }}</h5>
                      <span class="text-xs text-tertiary">({{ group.permissions.length }})</span>
                    </div>
                    <div class="grid grid-cols-2 gap-2 ps-6">
                      <label v-for="perm in group.permissions" :key="perm.id" class="flex items-center gap-2 cursor-pointer rounded-lg px-2 py-1.5 transition-colors hover:bg-surface-ground">
                        <input type="checkbox" :checked="isPermissionSelected(perm.id, editForm)" class="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary" @change="togglePermission(perm.id, editForm)" />
                        <span class="text-sm text-secondary">{{ getPermissionName(perm) }}</span>
                      </label>
                    </div>
                  </div>
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

    <!-- AI Assistant -->
    <AiUserManagementAssistant />
  </div>
</template>
