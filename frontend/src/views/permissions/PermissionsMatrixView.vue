<script setup lang="ts">
/**
 * PermissionsMatrixView - 4D Permissions Matrix (TASK-1001)
 *
 * Four-dimensional dynamic permissions matrix:
 * 1. Competition (which competition)
 * 2. Stage (which stage in the competition lifecycle)
 * 3. Committee/Team Role (role within the stage's committee)
 * 4. User Role (specific user role within the committee)
 *
 * Features:
 * - Interactive matrix grid with checkboxes
 * - Dimension selectors (dropdowns)
 * - Permission templates (save/load)
 * - Effective permissions preview
 * - Bulk operations (grant/revoke)
 * - Export to Excel
 * - RTL/LTR support
 *
 * All data fetched dynamically from APIs.
 */
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { httpGet, httpPut } from '@/services/http'

const { t } = useI18n()

/* ── Types ── */
interface Competition {
  id: string
  nameAr: string
  nameEn: string
}

interface Stage {
  id: string
  nameAr: string
  nameEn: string
  order: number
}

interface CommitteeRole {
  id: string
  nameAr: string
  nameEn: string
}

interface UserRole {
  id: string
  nameAr: string
  nameEn: string
}

interface PermissionAction {
  key: string
  labelKey: string
}

interface PermissionEntry {
  roleId: string
  actionKey: string
  granted: boolean
}

/* ── State ── */
const isLoading = ref(false)
const isSaving = ref(false)

/* Dimension 1: Competition */
const competitions = ref<Competition[]>([])
const selectedCompetitionId = ref<string>('')

/* Dimension 2: Stage */
const stages = ref<Stage[]>([])
const selectedStageId = ref<string>('')

/* Dimension 3: Committee Role */
const committeeRoles = ref<CommitteeRole[]>([])

/* Dimension 4: User Roles (dynamic based on stage) */
const userRoles = ref<UserRole[]>([])

/* Permission actions */
const permissionActions: PermissionAction[] = [
  { key: 'view', labelKey: 'permissions.actions.view' },
  { key: 'create', labelKey: 'permissions.actions.create' },
  { key: 'edit', labelKey: 'permissions.actions.edit' },
  { key: 'delete', labelKey: 'permissions.actions.delete' },
  { key: 'approve', labelKey: 'permissions.actions.approve' },
  { key: 'export', labelKey: 'permissions.actions.export' },
]

/* Matrix data: roleId -> actionKey -> granted */
const permissionMatrix = reactive<Record<string, Record<string, boolean>>>({})

/* Templates */
// eslint-disable-next-line @typescript-eslint/no-unused-vars
const showTemplateDialog = ref(false)

/* Active tab for dimension view */
const activeView = ref<'matrix' | 'effective'>('matrix')

/* ── Computed ── */
const currentStageRoles = computed(() => {
  /* User roles change dynamically with the selected stage */
  return userRoles.value
})



/* ── Methods ── */
async function loadCompetitions(): Promise<void> {
  try {
    const data = await httpGet<{ items: Competition[] }>('/v1/competitions', {
      params: { page: 1, pageSize: 100 },
    })
    competitions.value = data.items
  } catch (err) {
    console.error('Failed to load competitions:', err)
  }
}

async function loadStages(): Promise<void> {
  if (!selectedCompetitionId.value) return
  try {
    const data = await httpGet<{ items: Stage[] }>(
      `/v1/competitions/${selectedCompetitionId.value}/stages`
    )
    stages.value = data.items
  } catch (err) {
    console.error('Failed to load stages:', err)
  }
}

async function loadRoles(): Promise<void> {
  if (!selectedStageId.value) return
  try {
    const [committeeData, userRoleData] = await Promise.all([
      httpGet<{ items: CommitteeRole[] }>(`/v1/stages/${selectedStageId.value}/committee-roles`),
      httpGet<{ items: UserRole[] }>(`/v1/stages/${selectedStageId.value}/user-roles`),
    ])
    committeeRoles.value = committeeData.items
    userRoles.value = userRoleData.items
  } catch (err) {
    console.error('Failed to load roles:', err)
  }
}

async function loadPermissions(): Promise<void> {
  if (!selectedCompetitionId.value || !selectedStageId.value) return
  isLoading.value = true
  try {
    const data = await httpGet<{ permissions: PermissionEntry[] }>(
      `/v1/permissions/matrix`,
      {
        params: {
          competitionId: selectedCompetitionId.value,
          stageId: selectedStageId.value,
        },
      }
    )
    /* Build matrix from API response */
    const matrix: Record<string, Record<string, boolean>> = {}
    for (const role of userRoles.value) {
      matrix[role.id] = {}
      for (const action of permissionActions) {
        const entry = data.permissions.find(
          p => p.roleId === role.id && p.actionKey === action.key
        )
        matrix[role.id][action.key] = entry?.granted ?? false
      }
    }
    Object.assign(permissionMatrix, matrix)
  } catch (err) {
    console.error('Failed to load permissions:', err)
    /* Initialize empty matrix */
    const matrix: Record<string, Record<string, boolean>> = {}
    for (const role of userRoles.value) {
      matrix[role.id] = {}
      for (const action of permissionActions) {
        matrix[role.id][action.key] = false
      }
    }
    Object.assign(permissionMatrix, matrix)
  } finally {
    isLoading.value = false
  }
}

function togglePermission(roleId: string, actionKey: string): void {
  if (!permissionMatrix[roleId]) {
    permissionMatrix[roleId] = {}
  }
  permissionMatrix[roleId][actionKey] = !permissionMatrix[roleId][actionKey]
}

function grantAll(): void {
  for (const roleId of Object.keys(permissionMatrix)) {
    for (const action of permissionActions) {
      permissionMatrix[roleId][action.key] = true
    }
  }
}

function revokeAll(): void {
  for (const roleId of Object.keys(permissionMatrix)) {
    for (const action of permissionActions) {
      permissionMatrix[roleId][action.key] = false
    }
  }
}

async function savePermissions(): Promise<void> {
  isSaving.value = true
  try {
    const entries: PermissionEntry[] = []
    for (const [roleId, actions] of Object.entries(permissionMatrix)) {
      for (const [actionKey, granted] of Object.entries(actions)) {
        entries.push({ roleId, actionKey, granted })
      }
    }
    await httpPut('/v1/permissions/matrix', {
      competitionId: selectedCompetitionId.value,
      stageId: selectedStageId.value,
      permissions: entries,
    })
  } catch (err) {
    console.error('Failed to save permissions:', err)
  } finally {
    isSaving.value = false
  }
}

/* Watch dimension changes to reload data */
watch(selectedCompetitionId, () => {
  selectedStageId.value = ''
  stages.value = []
  loadStages()
})

watch(selectedStageId, () => {
  loadRoles()
  loadPermissions()
})

onMounted(() => {
  loadCompetitions()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="page-title">{{ t('permissions.title') }}</h1>
        <p class="page-description">{{ t('permissions.subtitle') }}</p>
      </div>
      <div class="flex items-center gap-2">
        <button class="btn-ghost btn-sm" @click="showTemplateDialog = true">
          <i class="pi pi-bookmark"></i>
          {{ t('permissions.templates') }}
        </button>
        <button class="btn-primary" :disabled="isSaving" @click="savePermissions">
          <i class="pi pi-save" :class="{ 'animate-spin': isSaving }"></i>
          {{ t('common.save') }}
        </button>
      </div>
    </div>

    <!-- Dimension Selectors -->
    <div class="card !p-4">
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <!-- D1: Competition -->
        <div>
          <label class="mb-1 block text-xs font-semibold text-secondary-600">
            <i class="pi pi-briefcase me-1 text-primary"></i>
            {{ t('permissions.dimensions.competition') }}
          </label>
          <select v-model="selectedCompetitionId" class="input text-sm">
            <option value="">-- {{ t('common.select') }} --</option>
            <option v-for="c in competitions" :key="c.id" :value="c.id">
              {{ c.nameAr }}
            </option>
          </select>
        </div>

        <!-- D2: Stage -->
        <div>
          <label class="mb-1 block text-xs font-semibold text-secondary-600">
            <i class="pi pi-flag me-1 text-info"></i>
            {{ t('permissions.dimensions.stage') }}
          </label>
          <select v-model="selectedStageId" class="input text-sm" :disabled="!selectedCompetitionId">
            <option value="">-- {{ t('common.select') }} --</option>
            <option v-for="s in stages" :key="s.id" :value="s.id">
              {{ s.nameAr }}
            </option>
          </select>
        </div>

        <!-- D3: Committee Role (info display) -->
        <div>
          <label class="mb-1 block text-xs font-semibold text-secondary-600">
            <i class="pi pi-users me-1 text-warning"></i>
            {{ t('permissions.dimensions.committeeRole') }}
          </label>
          <div class="flex flex-wrap gap-1 rounded-xl border border-secondary-200 bg-surface-subtle p-2 min-h-[42px]">
            <span
              v-for="cr in committeeRoles"
              :key="cr.id"
              class="badge badge-info text-[10px]"
            >
              {{ cr.nameAr }}
            </span>
            <span v-if="committeeRoles.length === 0" class="text-xs text-secondary-400 p-1">
              {{ t('common.noData') }}
            </span>
          </div>
        </div>

        <!-- D4: User Role (info display) -->
        <div>
          <label class="mb-1 block text-xs font-semibold text-secondary-600">
            <i class="pi pi-user me-1 text-success"></i>
            {{ t('permissions.dimensions.userRole') }}
          </label>
          <div class="flex flex-wrap gap-1 rounded-xl border border-secondary-200 bg-surface-subtle p-2 min-h-[42px]">
            <span
              v-for="ur in userRoles"
              :key="ur.id"
              class="badge badge-success text-[10px]"
            >
              {{ ur.nameAr }}
            </span>
            <span v-if="userRoles.length === 0" class="text-xs text-secondary-400 p-1">
              {{ t('common.noData') }}
            </span>
          </div>
        </div>
      </div>
    </div>

    <!-- View Tabs -->
    <div class="flex items-center gap-4 border-b border-secondary-100 pb-0">
      <button
        class="tab-item"
        :class="{ 'tab-item-active': activeView === 'matrix' }"
        @click="activeView = 'matrix'"
      >
        {{ t('permissions.title') }}
      </button>
      <button
        class="tab-item"
        :class="{ 'tab-item-active': activeView === 'effective' }"
        @click="activeView = 'effective'"
      >
        {{ t('permissions.effectivePermissions') }}
      </button>
    </div>

    <!-- Permission Matrix Grid -->
    <div v-if="activeView === 'matrix'" class="card overflow-hidden !p-0">
      <!-- Bulk actions bar -->
      <div class="flex items-center justify-between border-b border-secondary-100 bg-surface-subtle px-4 py-2">
        <span class="text-xs font-medium text-secondary-500">
          {{ userRoles.length }} {{ t('permissions.dimensions.userRole') }} x {{ permissionActions.length }} {{ t('permissions.actions.view') }}
        </span>
        <div class="flex gap-2">
          <button class="text-xs font-medium text-success hover:underline" @click="grantAll">
            Grant All
          </button>
          <button class="text-xs font-medium text-danger hover:underline" @click="revokeAll">
            Revoke All
          </button>
        </div>
      </div>

      <!-- Loading state -->
      <div v-if="isLoading" class="p-8">
        <div class="space-y-3">
          <div v-for="i in 5" :key="i" class="skeleton-text"></div>
        </div>
      </div>

      <!-- Empty state -->
      <div v-else-if="userRoles.length === 0" class="empty-state py-12">
        <div class="empty-state-icon">
          <i class="pi pi-lock"></i>
        </div>
        <h3 class="empty-state-title">{{ t('common.noData') }}</h3>
        <p class="empty-state-text">
          {{ selectedCompetitionId && selectedStageId
            ? 'No roles found for this stage'
            : 'Select a competition and stage to view permissions' }}
        </p>
      </div>

      <!-- Matrix table -->
      <div v-else class="overflow-x-auto">
        <table class="table-modern">
          <thead>
            <tr>
              <th class="sticky start-0 z-10 bg-surface-subtle min-w-[200px]">
                {{ t('permissions.dimensions.userRole') }}
              </th>
              <th
                v-for="action in permissionActions"
                :key="action.key"
                class="text-center min-w-[100px]"
              >
                {{ t(action.labelKey) }}
              </th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="role in currentStageRoles" :key="role.id">
              <td class="sticky start-0 z-10 bg-white font-medium">
                <div class="flex items-center gap-2">
                  <div class="flex h-7 w-7 items-center justify-center rounded-lg bg-primary-50">
                    <i class="pi pi-user text-xs text-primary"></i>
                  </div>
                  {{ role.nameAr }}
                </div>
              </td>
              <td
                v-for="action in permissionActions"
                :key="`${role.id}-${action.key}`"
                class="text-center"
              >
                <button
                  class="inline-flex h-8 w-8 items-center justify-center rounded-lg transition-all duration-200"
                  :class="permissionMatrix[role.id]?.[action.key]
                    ? 'bg-success-50 text-success hover:bg-success-50/80'
                    : 'bg-secondary-50 text-secondary-300 hover:bg-secondary-100'"
                  @click="togglePermission(role.id, action.key)"
                >
                  <i
                    class="pi text-sm"
                    :class="permissionMatrix[role.id]?.[action.key] ? 'pi-check' : 'pi-minus'"
                  ></i>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Effective Permissions View -->
    <div v-if="activeView === 'effective'" class="card">
      <div class="space-y-4">
        <div v-for="role in currentStageRoles" :key="role.id" class="rounded-xl border border-secondary-100 p-4">
          <div class="flex items-center gap-3 mb-3">
            <div class="flex h-9 w-9 items-center justify-center rounded-xl bg-primary-50">
              <i class="pi pi-user text-primary"></i>
            </div>
            <div>
              <h4 class="text-sm font-bold text-secondary">{{ role.nameAr }}</h4>
              <p class="text-xs text-tertiary">{{ role.nameEn }}</p>
            </div>
          </div>
          <div class="flex flex-wrap gap-2">
            <span
              v-for="action in permissionActions"
              :key="`eff-${role.id}-${action.key}`"
              class="badge"
              :class="permissionMatrix[role.id]?.[action.key] ? 'badge-success' : 'badge-secondary'"
            >
              <i
                class="pi text-[10px]"
                :class="permissionMatrix[role.id]?.[action.key] ? 'pi-check' : 'pi-times'"
              ></i>
              {{ t(action.labelKey) }}
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
