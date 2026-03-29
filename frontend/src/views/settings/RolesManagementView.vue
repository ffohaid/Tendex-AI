<script setup lang="ts">
/**
 * RolesManagementView — Roles & Permissions Management Page.
 *
 * TASK-903: Displays the list of roles available in the current tenant.
 * Data is fetched dynamically from the API (no mock data).
 *
 * Features:
 * - List all roles with user count
 * - System vs custom role distinction
 * - Role detail view
 * - RTL/LTR support with Tailwind logical properties
 * - English numerals exclusively
 */
import { ref, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { fetchRoles } from '@/services/userManagementService'
import type { RoleDto } from '@/types/userManagement'
import { useFormatters } from '@/composables/useFormatters'

const { t, locale } = useI18n()
const { formatDate, formatNumber } = useFormatters()

/* ------------------------------------------------------------------ */
/*  State                                                              */
/* ------------------------------------------------------------------ */
const roles = ref<RoleDto[]>([])
const isLoading = ref(false)
const error = ref('')
const selectedRole = ref<RoleDto | null>(null)
const showDetailDialog = ref(false)

/* ------------------------------------------------------------------ */
/*  Computed                                                           */
/* ------------------------------------------------------------------ */
const totalUsers = computed(() =>
  roles.value.reduce((sum, r) => sum + r.userCount, 0),
)

const systemRolesCount = computed(() =>
  roles.value.filter(r => r.isSystemRole).length,
)

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */
function getRoleName(role: RoleDto): string {
  return locale.value === 'ar' ? role.nameAr : role.nameEn
}

function getRoleTypeBadge(isSystem: boolean) {
  return isSystem
    ? { label: t('settings.roles.systemRole'), bgClass: 'bg-blue-50', textClass: 'text-blue-700' }
    : { label: t('settings.roles.customRole'), bgClass: 'bg-purple-50', textClass: 'text-purple-700' }
}

function getStatusBadge(isActive: boolean) {
  return isActive
    ? { label: t('settings.roles.active'), bgClass: 'bg-green-50', textClass: 'text-green-700' }
    : { label: t('settings.roles.inactive'), bgClass: 'bg-red-50', textClass: 'text-red-700' }
}

/* ------------------------------------------------------------------ */
/*  API Calls                                                          */
/* ------------------------------------------------------------------ */
async function loadRoles() {
  isLoading.value = true
  error.value = ''
  try {
    roles.value = await fetchRoles()
  } catch {
    error.value = t('settings.roles.errors.loadFailed')
    roles.value = []
  } finally {
    isLoading.value = false
  }
}

function openDetailDialog(role: RoleDto) {
  selectedRole.value = role
  showDetailDialog.value = true
}

/* ------------------------------------------------------------------ */
/*  Lifecycle                                                          */
/* ------------------------------------------------------------------ */
onMounted(() => {
  loadRoles()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-secondary">
          {{ t('settings.roles.title') }}
        </h1>
        <p class="mt-1 text-sm text-tertiary">
          {{ t('settings.roles.subtitle') }}
        </p>
      </div>
    </div>

    <!-- Error Banner -->
    <div
      v-if="error"
      class="flex items-center gap-3 rounded-lg border border-danger/20 bg-danger/5 p-4"
    >
      <i class="pi pi-exclamation-triangle text-lg text-danger"></i>
      <p class="flex-1 text-sm text-danger">{{ error }}</p>
      <button
        class="text-xs font-medium text-danger hover:underline"
        @click="error = ''"
      >
        {{ t('common.close') }}
      </button>
    </div>

    <!-- Stats Cards -->
    <div class="grid gap-4 sm:grid-cols-3">
      <div class="rounded-lg border border-surface-dim bg-white p-5">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
            <i class="pi pi-shield text-lg text-primary"></i>
          </div>
          <div>
            <p class="text-2xl font-bold text-secondary">{{ formatNumber(roles.length) }}</p>
            <p class="text-xs text-tertiary">{{ t('settings.roles.stats.totalRoles') }}</p>
          </div>
        </div>
      </div>
      <div class="rounded-lg border border-surface-dim bg-white p-5">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-50">
            <i class="pi pi-lock text-lg text-blue-600"></i>
          </div>
          <div>
            <p class="text-2xl font-bold text-secondary">{{ formatNumber(systemRolesCount) }}</p>
            <p class="text-xs text-tertiary">{{ t('settings.roles.stats.systemRoles') }}</p>
          </div>
        </div>
      </div>
      <div class="rounded-lg border border-surface-dim bg-white p-5">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-green-50">
            <i class="pi pi-users text-lg text-green-600"></i>
          </div>
          <div>
            <p class="text-2xl font-bold text-secondary">{{ formatNumber(totalUsers) }}</p>
            <p class="text-xs text-tertiary">{{ t('settings.roles.stats.totalUsers') }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-16">
      <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
    </div>

    <!-- Empty State -->
    <div
      v-else-if="roles.length === 0"
      class="flex flex-col items-center justify-center rounded-lg border border-surface-dim bg-white py-16"
    >
      <i class="pi pi-shield text-5xl text-surface-dim"></i>
      <p class="mt-4 text-sm text-tertiary">{{ t('settings.roles.emptyState') }}</p>
    </div>

    <!-- Roles Grid -->
    <div v-else class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <div
        v-for="role in roles"
        :key="role.id"
        class="group cursor-pointer rounded-lg border border-surface-dim bg-white p-5 transition-all hover:border-primary/30 hover:shadow-md"
        @click="openDetailDialog(role)"
      >
        <div class="flex items-start justify-between">
          <div class="flex items-center gap-3">
            <div
              :class="[
                'flex h-10 w-10 items-center justify-center rounded-lg',
                role.isSystemRole ? 'bg-blue-50' : 'bg-purple-50',
              ]"
            >
              <i
                :class="[
                  'pi text-lg',
                  role.isSystemRole ? 'pi-lock text-blue-600' : 'pi-shield text-purple-600',
                ]"
              ></i>
            </div>
            <div>
              <h3 class="font-semibold text-secondary group-hover:text-primary">
                {{ getRoleName(role) }}
              </h3>
              <span
                :class="[getRoleTypeBadge(role.isSystemRole).bgClass, getRoleTypeBadge(role.isSystemRole).textClass]"
                class="mt-1 inline-block rounded-full px-2 py-0.5 text-xs font-medium"
              >
                {{ getRoleTypeBadge(role.isSystemRole).label }}
              </span>
            </div>
          </div>
          <span
            :class="[getStatusBadge(role.isActive).bgClass, getStatusBadge(role.isActive).textClass]"
            class="rounded-full px-2 py-0.5 text-xs font-medium"
          >
            {{ getStatusBadge(role.isActive).label }}
          </span>
        </div>

        <p v-if="role.description" class="mt-3 text-sm text-tertiary line-clamp-2">
          {{ role.description }}
        </p>

        <div class="mt-4 flex items-center justify-between border-t border-surface-dim pt-3">
          <div class="flex items-center gap-1.5 text-xs text-tertiary">
            <i class="pi pi-users text-xs"></i>
            <span>{{ formatNumber(role.userCount) }} {{ t('settings.roles.usersCount') }}</span>
          </div>
          <span class="text-xs text-tertiary">{{ formatDate(role.createdAt) }}</span>
        </div>
      </div>
    </div>

    <!-- ============================================================ -->
    <!-- ROLE DETAIL DIALOG                                            -->
    <!-- ============================================================ -->
    <Teleport to="body">
      <div
        v-if="showDetailDialog && selectedRole"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
        @click.self="showDetailDialog = false"
      >
        <div class="w-full max-w-md rounded-xl bg-white shadow-xl">
          <div class="flex items-center justify-between border-b border-surface-dim px-6 py-4">
            <h3 class="text-lg font-semibold text-secondary">
              {{ t('settings.roles.detailTitle') }}
            </h3>
            <button
              class="rounded-lg p-1 text-tertiary hover:bg-surface-ground"
              @click="showDetailDialog = false"
            >
              <i class="pi pi-times"></i>
            </button>
          </div>
          <div class="space-y-4 p-6">
            <div class="flex items-center gap-3">
              <div
                :class="[
                  'flex h-12 w-12 items-center justify-center rounded-lg',
                  selectedRole.isSystemRole ? 'bg-blue-50' : 'bg-purple-50',
                ]"
              >
                <i
                  :class="[
                    'pi text-xl',
                    selectedRole.isSystemRole ? 'pi-lock text-blue-600' : 'pi-shield text-purple-600',
                  ]"
                ></i>
              </div>
              <div>
                <h4 class="text-lg font-semibold text-secondary">
                  {{ getRoleName(selectedRole) }}
                </h4>
                <div class="flex items-center gap-2 mt-1">
                  <span
                    :class="[getRoleTypeBadge(selectedRole.isSystemRole).bgClass, getRoleTypeBadge(selectedRole.isSystemRole).textClass]"
                    class="rounded-full px-2 py-0.5 text-xs font-medium"
                  >
                    {{ getRoleTypeBadge(selectedRole.isSystemRole).label }}
                  </span>
                  <span
                    :class="[getStatusBadge(selectedRole.isActive).bgClass, getStatusBadge(selectedRole.isActive).textClass]"
                    class="rounded-full px-2 py-0.5 text-xs font-medium"
                  >
                    {{ getStatusBadge(selectedRole.isActive).label }}
                  </span>
                </div>
              </div>
            </div>

            <div class="grid gap-4 sm:grid-cols-2">
              <div>
                <label class="mb-1 block text-xs font-medium text-tertiary">
                  {{ t('settings.roles.fields.nameAr') }}
                </label>
                <p class="text-sm text-secondary" dir="rtl">{{ selectedRole.nameAr }}</p>
              </div>
              <div>
                <label class="mb-1 block text-xs font-medium text-tertiary">
                  {{ t('settings.roles.fields.nameEn') }}
                </label>
                <p class="text-sm text-secondary" dir="ltr">{{ selectedRole.nameEn }}</p>
              </div>
              <div>
                <label class="mb-1 block text-xs font-medium text-tertiary">
                  {{ t('settings.roles.fields.userCount') }}
                </label>
                <p class="text-sm font-medium text-secondary">{{ formatNumber(selectedRole.userCount) }}</p>
              </div>
              <div>
                <label class="mb-1 block text-xs font-medium text-tertiary">
                  {{ t('settings.roles.fields.createdAt') }}
                </label>
                <p class="text-sm text-secondary">{{ formatDate(selectedRole.createdAt) }}</p>
              </div>
            </div>

            <div v-if="selectedRole.description">
              <label class="mb-1 block text-xs font-medium text-tertiary">
                {{ t('settings.roles.fields.description') }}
              </label>
              <p class="text-sm text-secondary">{{ selectedRole.description }}</p>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
