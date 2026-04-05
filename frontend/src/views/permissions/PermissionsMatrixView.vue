<script setup lang="ts">
/**
 * PermissionsMatrixView — True Dynamic Grid/Table
 *
 * Redesigned permission matrix that displays as a proper grid:
 *   - Rows = Resources (grouped by scope: Global, Competition, Committee)
 *   - Columns = Roles (all tenant roles including committee roles)
 *   - Cells = Toggle switches for each action (Read, Create, Update, Delete, etc.)
 *
 * Features:
 *   - True matrix layout (all roles visible simultaneously as columns)
 *   - Scope-based accordion sections
 *   - Expandable action detail per cell (click to see individual action toggles)
 *   - Protected roles shown but not editable
 *   - Only primary admin (المسؤول الأول) can edit
 *   - Bulk operations: grant all / revoke all per role
 *   - Reset to defaults
 *   - Full RTL/LTR support with logical properties
 *   - Responsive horizontal scroll for many roles
 *
 * All data fetched dynamically from /api/permission-matrix/grid — NO mock data.
 */
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { httpGet, httpPut, httpPost } from '@/services/http'
import { usePermissions } from '@/composables/usePermissions'

const { t, locale } = useI18n()
const isArabic = computed(() => locale.value === 'ar')
const { isOwner, isAdmin } = usePermissions()

/* ── Types ── */
interface GridCell {
  ruleId: string | null
  allowedActions: number
  isCustomized: boolean
}

interface GridResourceRow {
  resourceType: number
  resourceTypeNameAr: string
  resourceTypeNameEn: string
  committeeRole: number | null
  committeeRoleNameAr: string | null
  committeeRoleNameEn: string | null
  competitionPhase: number | null
  competitionPhaseNameAr: string | null
  competitionPhaseNameEn: string | null
  cells: Record<string, GridCell>
}

interface GridScopeGroup {
  scope: number
  scopeNameAr: string
  scopeNameEn: string
  resources: GridResourceRow[]
}

interface GridRoleColumn {
  id: string
  nameAr: string
  nameEn: string
  normalizedName: string
  isProtected: boolean
  isSystemRole: boolean
}

interface GridActionMeta {
  key: string
  flag: number
  nameAr: string
  nameEn: string
}

interface GridResponse {
  roles: GridRoleColumn[]
  scopes: GridScopeGroup[]
  actions: GridActionMeta[]
}

/* ── Permission Action Flags ── */
const ACTION_FLAGS: Record<string, number> = {
  read: 1,
  create: 2,
  update: 4,
  delete: 8,
  approve: 16,
  reject: 32,
  submit: 64,
  upload: 128,
  score: 256,
  sign: 512,
}

/* ── Reactive State ── */
const loading = ref(false)
const saving = ref(false)
const seeding = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const gridData = ref<GridResponse | null>(null)
const expandedScopes = ref<Set<number>>(new Set([0, 1, 2]))
const expandedCell = ref<string | null>(null) // "scopeIdx-resourceIdx-roleId"

// Track pending changes: "scopeIdx-resourceIdx-roleId" -> new allowedActions
const pendingChanges = ref<Map<string, number>>(new Map())

/* ── Computed ── */
const hasData = computed(() => gridData.value !== null && gridData.value.scopes.length > 0)
const roles = computed(() => gridData.value?.roles ?? [])
const scopes = computed(() => gridData.value?.scopes ?? [])
const actions = computed(() => gridData.value?.actions ?? [])
const hasChanges = computed(() => pendingChanges.value.size > 0)
const changesCount = computed(() => pendingChanges.value.size)

const canEdit = computed(() => isOwner.value || isAdmin.value)

const totalRules = computed(() => {
  if (!gridData.value) return 0
  let count = 0
  for (const scope of gridData.value.scopes) {
    for (const resource of scope.resources) {
      for (const cell of Object.values(resource.cells)) {
        if (cell.ruleId) count++
      }
    }
  }
  return count
})

const customizedCount = computed(() => {
  if (!gridData.value) return 0
  let count = 0
  for (const scope of gridData.value.scopes) {
    for (const resource of scope.resources) {
      for (const cell of Object.values(resource.cells)) {
        if (cell.isCustomized) count++
      }
    }
  }
  return count
})

/* ── Scope Icons ── */
const scopeIcons: Record<number, string> = {
  0: 'M12 6.042A8.967 8.967 0 006 3.75c-1.052 0-2.062.18-3 .512v14.25A8.987 8.987 0 016 18c2.305 0 4.408.867 6 2.292m0-14.25a8.966 8.966 0 016-2.292c1.052 0 2.062.18 3 .512v14.25A8.987 8.987 0 0018 18a8.967 8.967 0 00-6 2.292m0-14.25v14.25',
  1: 'M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z',
  2: 'M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z',
}

/* ── Scope Colors ── */
const scopeColors: Record<number, { bg: string; border: string; text: string; badge: string }> = {
  0: { bg: 'bg-blue-50 dark:bg-blue-900/20', border: 'border-blue-200 dark:border-blue-800', text: 'text-blue-700 dark:text-blue-300', badge: 'bg-blue-100 text-blue-700 dark:bg-blue-800 dark:text-blue-200' },
  1: { bg: 'bg-emerald-50 dark:bg-emerald-900/20', border: 'border-emerald-200 dark:border-emerald-800', text: 'text-emerald-700 dark:text-emerald-300', badge: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-800 dark:text-emerald-200' },
  2: { bg: 'bg-purple-50 dark:bg-purple-900/20', border: 'border-purple-200 dark:border-purple-800', text: 'text-purple-700 dark:text-purple-300', badge: 'bg-purple-100 text-purple-700 dark:bg-purple-800 dark:text-purple-200' },
}

/* ── Helper Methods ── */
function getResourceName(resource: GridResourceRow): string {
  return isArabic.value ? resource.resourceTypeNameAr : resource.resourceTypeNameEn
}

function getResourceSubtext(resource: GridResourceRow): string {
  const parts: string[] = []
  if (resource.competitionPhase !== null) {
    parts.push(isArabic.value ? (resource.competitionPhaseNameAr || '') : (resource.competitionPhaseNameEn || ''))
  }
  if (resource.committeeRole !== null) {
    parts.push(isArabic.value ? (resource.committeeRoleNameAr || '') : (resource.committeeRoleNameEn || ''))
  }
  return parts.join(' — ')
}

function getRoleName(role: GridRoleColumn): string {
  return isArabic.value ? role.nameAr : role.nameEn
}

function getScopeName(scope: GridScopeGroup): string {
  return isArabic.value ? scope.scopeNameAr : scope.scopeNameEn
}

function getActionName(action: GridActionMeta): string {
  return isArabic.value ? action.nameAr : action.nameEn
}

function getCellKey(scopeIdx: number, resourceIdx: number, roleId: string): string {
  return `${scopeIdx}-${resourceIdx}-${roleId}`
}

function getCellActions(scopeIdx: number, resourceIdx: number, roleId: string): number {
  const key = getCellKey(scopeIdx, resourceIdx, roleId)
  if (pendingChanges.value.has(key)) {
    return pendingChanges.value.get(key)!
  }
  const scope = scopes.value[scopeIdx]
  if (!scope) return 0
  const resource = scope.resources[resourceIdx]
  if (!resource) return 0
  const cell = resource.cells[roleId]
  return cell ? cell.allowedActions : 0
}

function isCellModified(scopeIdx: number, resourceIdx: number, roleId: string): boolean {
  return pendingChanges.value.has(getCellKey(scopeIdx, resourceIdx, roleId))
}

function hasAction(scopeIdx: number, resourceIdx: number, roleId: string, actionFlag: number): boolean {
  const cellActions = getCellActions(scopeIdx, resourceIdx, roleId)
  return (cellActions & actionFlag) !== 0
}

function getActiveActionsCount(scopeIdx: number, resourceIdx: number, roleId: string): number {
  const cellActions = getCellActions(scopeIdx, resourceIdx, roleId)
  let count = 0
  for (const flag of Object.values(ACTION_FLAGS)) {
    if ((cellActions & flag) !== 0) count++
  }
  return count
}

function getRelevantActions(scope: number): GridActionMeta[] {
  if (scope === 0) {
    return actions.value.filter(a => ['read', 'create', 'update', 'delete'].includes(a.key))
  }
  return actions.value
}

function toggleAction(scopeIdx: number, resourceIdx: number, roleId: string, actionFlag: number) {
  if (!canEdit.value) return
  const role = roles.value.find(r => r.id === roleId)
  if (role?.isProtected) return

  const currentActions = getCellActions(scopeIdx, resourceIdx, roleId)
  const newActions = (currentActions & actionFlag) !== 0
    ? currentActions & ~actionFlag
    : currentActions | actionFlag

  const key = getCellKey(scopeIdx, resourceIdx, roleId)
  const scope = scopes.value[scopeIdx]
  const resource = scope.resources[resourceIdx]
  const originalCell = resource.cells[roleId]
  const originalActions = originalCell ? originalCell.allowedActions : 0

  if (newActions === originalActions) {
    pendingChanges.value.delete(key)
  } else {
    pendingChanges.value.set(key, newActions)
  }
}

function toggleAllActionsForCell(scopeIdx: number, resourceIdx: number, roleId: string) {
  if (!canEdit.value) return
  const role = roles.value.find(r => r.id === roleId)
  if (role?.isProtected) return

  const currentActions = getCellActions(scopeIdx, resourceIdx, roleId)
  const relevantActions = getRelevantActions(scopes.value[scopeIdx].scope)
  const allFlags = relevantActions.reduce((acc, a) => acc | a.flag, 0)
  const hasAll = (currentActions & allFlags) === allFlags

  const newActions = hasAll ? (currentActions & ~allFlags) : (currentActions | allFlags)

  const key = getCellKey(scopeIdx, resourceIdx, roleId)
  const scope = scopes.value[scopeIdx]
  const resource = scope.resources[resourceIdx]
  const originalCell = resource.cells[roleId]
  const originalActions = originalCell ? originalCell.allowedActions : 0

  if (newActions === originalActions) {
    pendingChanges.value.delete(key)
  } else {
    pendingChanges.value.set(key, newActions)
  }
}

function toggleExpandedCell(scopeIdx: number, resourceIdx: number, roleId: string) {
  const key = getCellKey(scopeIdx, resourceIdx, roleId)
  expandedCell.value = expandedCell.value === key ? null : key
}

function toggleScope(scope: number) {
  if (expandedScopes.value.has(scope)) {
    expandedScopes.value.delete(scope)
  } else {
    expandedScopes.value.add(scope)
  }
}

function grantAllForRole(roleId: string) {
  if (!canEdit.value) return
  const role = roles.value.find(r => r.id === roleId)
  if (role?.isProtected) return

  scopes.value.forEach((scope, scopeIdx) => {
    scope.resources.forEach((resource, resourceIdx) => {
      const key = getCellKey(scopeIdx, resourceIdx, roleId)
      const originalCell = resource.cells[roleId]
      const originalActions = originalCell ? originalCell.allowedActions : 0
      const fullAccess = 1023
      if (originalActions !== fullAccess) {
        pendingChanges.value.set(key, fullAccess)
      }
    })
  })
}

function revokeAllForRole(roleId: string) {
  if (!canEdit.value) return
  const role = roles.value.find(r => r.id === roleId)
  if (role?.isProtected) return

  scopes.value.forEach((scope, scopeIdx) => {
    scope.resources.forEach((resource, resourceIdx) => {
      const key = getCellKey(scopeIdx, resourceIdx, roleId)
      const originalCell = resource.cells[roleId]
      const originalActions = originalCell ? originalCell.allowedActions : 0
      if (originalActions !== 0) {
        pendingChanges.value.set(key, 0)
      }
    })
  })
}

function discardChanges() {
  pendingChanges.value.clear()
}

/* ── API Calls ── */
async function loadGrid() {
  loading.value = true
  errorMessage.value = ''
  try {
    const data = await httpGet<GridResponse>('/permission-matrix/grid')
    gridData.value = data
    pendingChanges.value.clear()
    expandedCell.value = null
  } catch (err: any) {
    errorMessage.value = t('permissions.loadError')
    console.error('Failed to load permission matrix grid:', err)
  } finally {
    loading.value = false
  }
}

async function saveChanges() {
  if (!hasChanges.value || !canEdit.value) return
  saving.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const cells: Array<{
      ruleId: string | null
      roleId: string
      scope: number
      resourceType: number
      committeeRole: number | null
      competitionPhase: number | null
      allowedActions: number
    }> = []

    for (const [key, newActions] of pendingChanges.value.entries()) {
      const parts = key.split('-')
      const scopeIdx = parseInt(parts[0])
      const resourceIdx = parseInt(parts[1])
      const roleId = parts.slice(2).join('-') // roleId may contain dashes (GUID)
      const scope = scopes.value[scopeIdx]
      const resource = scope.resources[resourceIdx]
      const originalCell = resource.cells[roleId]

      cells.push({
        ruleId: originalCell?.ruleId || null,
        roleId,
        scope: scope.scope,
        resourceType: resource.resourceType,
        committeeRole: resource.committeeRole,
        competitionPhase: resource.competitionPhase,
        allowedActions: newActions,
      })
    }

    await httpPut('/permission-matrix/grid/bulk-update', { cells })

    successMessage.value = t('permissions.saveSuccess')
    await loadGrid()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch (err: any) {
    errorMessage.value = t('permissions.saveError')
    console.error('Failed to save changes:', err)
  } finally {
    saving.value = false
  }
}

async function seedDefaultRules() {
  seeding.value = true
  errorMessage.value = ''
  try {
    await httpPost('/permission-matrix/seed', {})
    successMessage.value = t('permissions.seedSuccess')
    await loadGrid()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch (err: any) {
    errorMessage.value = err?.response?.data?.message || 'Failed to seed default rules'
    console.error('Failed to seed default rules:', err)
  } finally {
    seeding.value = false
  }
}

async function resetToDefaults() {
  if (!confirm(t('permissions.resetConfirm'))) return
  loading.value = true
  errorMessage.value = ''
  try {
    await httpPost('/permission-matrix/reset', {})
    successMessage.value = t('permissions.seedSuccess')
    await loadGrid()
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch (err: any) {
    errorMessage.value = err?.response?.data?.message || 'Failed to reset matrix'
    console.error('Failed to reset matrix:', err)
  } finally {
    loading.value = false
  }
}

/* ── Lifecycle ── */
onMounted(async () => {
  await loadGrid()
})
</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <!-- ═══════════════════ Header ═══════════════════ -->
    <div class="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 px-6 py-5">
      <div class="flex items-center justify-between flex-wrap gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-3">
            <svg class="h-7 w-7 text-primary" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" />
            </svg>
            {{ t('permissions.title') }}
          </h1>
          <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
            {{ t('permissions.subtitle') }}
          </p>
        </div>

        <div class="flex items-center gap-3 flex-wrap">
          <!-- Stats -->
          <div v-if="hasData" class="flex items-center gap-4 me-4">
            <div class="text-center">
              <div class="text-lg font-semibold text-blue-600">{{ totalRules }}</div>
              <div class="text-xs text-gray-500">{{ t('permissions.totalRules') }}</div>
            </div>
            <div class="text-center">
              <div class="text-lg font-semibold text-amber-600">{{ customizedCount }}</div>
              <div class="text-xs text-gray-500">{{ t('permissions.customizedRules') }}</div>
            </div>
            <div class="text-center">
              <div class="text-lg font-semibold text-emerald-600">{{ roles.length }}</div>
              <div class="text-xs text-gray-500">{{ isArabic ? 'الأدوار' : 'Roles' }}</div>
            </div>
          </div>

          <!-- Action Buttons -->
          <template v-if="canEdit">
            <button
              v-if="!hasData"
              @click="seedDefaultRules"
              :disabled="seeding"
              class="px-4 py-2 bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 disabled:opacity-50 text-sm font-medium flex items-center gap-2 transition-colors"
            >
              <svg v-if="seeding" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
              <span>{{ seeding ? t('permissions.seeding') : t('permissions.seedRules') }}</span>
            </button>

            <button
              v-if="hasData"
              @click="resetToDefaults"
              class="px-3 py-2 text-gray-600 dark:text-gray-300 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 text-sm font-medium transition-colors"
            >
              {{ t('permissions.resetToDefaults') }}
            </button>

            <button
              v-if="hasChanges"
              @click="discardChanges"
              class="px-3 py-2 text-red-600 border border-red-300 dark:border-red-700 rounded-lg hover:bg-red-50 dark:hover:bg-red-900/20 text-sm font-medium transition-colors"
            >
              {{ isArabic ? 'تجاهل التغييرات' : 'Discard' }}
            </button>

            <button
              @click="saveChanges"
              :disabled="!hasChanges || saving"
              class="px-4 py-2 rounded-lg text-sm font-medium flex items-center gap-2 transition-colors"
              :class="hasChanges
                ? 'bg-primary text-white hover:bg-primary-dark shadow-sm'
                : 'bg-gray-200 dark:bg-gray-700 text-gray-400 dark:text-gray-500 cursor-not-allowed'"
            >
              <svg v-if="saving" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
              <svg v-else class="h-4 w-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
              </svg>
              <span v-if="saving">{{ t('permissions.saving') }}</span>
              <span v-else-if="hasChanges">{{ t('permissions.saveChanges') }} ({{ changesCount }})</span>
              <span v-else>{{ t('permissions.noChanges') }}</span>
            </button>
          </template>
        </div>
      </div>
    </div>

    <!-- ═══════════════════ Messages ═══════════════════ -->
    <div v-if="errorMessage" class="mx-6 mt-4 p-3 bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-300 text-sm flex items-center gap-2">
      <svg class="h-5 w-5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
      </svg>
      {{ errorMessage }}
    </div>
    <div v-if="successMessage" class="mx-6 mt-4 p-3 bg-green-50 dark:bg-green-900/30 border border-green-200 dark:border-green-800 rounded-lg text-green-700 dark:text-green-300 text-sm flex items-center gap-2">
      <svg class="h-5 w-5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
      </svg>
      {{ successMessage }}
    </div>

    <!-- Not editable notice for non-admins -->
    <div v-if="!canEdit && hasData" class="mx-6 mt-4 p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg text-amber-700 dark:text-amber-300 text-sm flex items-center gap-2">
      <svg class="h-5 w-5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
        <path fill-rule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clip-rule="evenodd" />
      </svg>
      {{ t('permissions.adminOnly') }}
    </div>

    <!-- ═══════════════════ Loading ═══════════════════ -->
    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="text-center">
        <svg class="animate-spin h-10 w-10 text-primary mx-auto mb-4" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
        </svg>
        <p class="text-gray-500 dark:text-gray-400">{{ t('common.loading') || 'Loading...' }}</p>
      </div>
    </div>

    <!-- ═══════════════════ Empty State ═══════════════════ -->
    <div v-else-if="!hasData" class="flex items-center justify-center py-20">
      <div class="text-center max-w-md">
        <svg class="h-16 w-16 text-gray-300 dark:text-gray-600 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
        </svg>
        <h3 class="text-lg font-medium text-gray-900 dark:text-white mb-2">{{ t('permissions.noRules') }}</h3>
        <p class="text-sm text-gray-500 dark:text-gray-400">{{ t('permissions.noRulesDesc') }}</p>
      </div>
    </div>

    <!-- ═══════════════════ Matrix Grid ═══════════════════ -->
    <div v-else class="p-6 space-y-4">

      <!-- ── Scope Accordion Sections ── -->
      <template v-for="(scope, scopeIdx) in scopes" :key="scope.scope">
        <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden shadow-sm">

          <!-- Scope Header (Accordion Toggle) -->
          <button
            @click="toggleScope(scope.scope)"
            class="w-full flex items-center justify-between px-5 py-4 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
          >
            <div class="flex items-center gap-3">
              <div :class="[scopeColors[scope.scope]?.bg, scopeColors[scope.scope]?.border, 'p-2 rounded-lg border']">
                <svg class="h-5 w-5" :class="scopeColors[scope.scope]?.text" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" :d="scopeIcons[scope.scope]" />
                </svg>
              </div>
              <div class="text-start">
                <h3 class="text-base font-semibold text-gray-900 dark:text-white">
                  {{ getScopeName(scope) }}
                </h3>
                <p class="text-xs text-gray-500 dark:text-gray-400">
                  {{ scope.resources.length }} {{ isArabic ? 'مورد' : 'resources' }}
                </p>
              </div>
            </div>
            <div class="flex items-center gap-3">
              <span :class="[scopeColors[scope.scope]?.badge, 'text-xs px-2.5 py-1 rounded-full font-medium']">
                {{ scope.resources.length }}
              </span>
              <svg
                class="h-5 w-5 text-gray-400 transition-transform duration-200"
                :class="{ 'rotate-180': expandedScopes.has(scope.scope) }"
                fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"
              >
                <path stroke-linecap="round" stroke-linejoin="round" d="M19 9l-7 7-7-7" />
              </svg>
            </div>
          </button>

          <!-- Scope Content (Grid Table) -->
          <div v-if="expandedScopes.has(scope.scope)" class="border-t border-gray-200 dark:border-gray-700">
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead>
                  <tr class="bg-gray-50 dark:bg-gray-900/50">
                    <!-- Resource Name Column (sticky) -->
                    <th class="text-start px-4 py-3 font-semibold text-gray-700 dark:text-gray-200 min-w-[220px] sticky start-0 bg-gray-50 dark:bg-gray-900/50 z-20 border-e border-gray-200 dark:border-gray-700">
                      {{ t('permissions.resource') }}
                    </th>
                    <!-- Role Columns -->
                    <th
                      v-for="role in roles"
                      :key="role.id"
                      class="text-center px-2 py-3 min-w-[120px] relative group"
                    >
                      <div class="flex flex-col items-center gap-1">
                        <span
                          class="text-xs font-semibold leading-tight"
                          :class="role.isProtected ? 'text-amber-600 dark:text-amber-400' : 'text-gray-700 dark:text-gray-200'"
                        >
                          {{ getRoleName(role) }}
                        </span>
                        <span v-if="role.isProtected" class="text-[10px] text-amber-500 dark:text-amber-400 font-medium">
                          {{ isArabic ? 'محمي' : 'Protected' }}
                        </span>
                        <!-- Role actions dropdown -->
                        <div v-if="canEdit && !role.isProtected" class="flex gap-1 mt-1 opacity-0 group-hover:opacity-100 transition-opacity">
                          <button
                            @click.stop="grantAllForRole(role.id)"
                            class="text-[9px] px-1.5 py-0.5 rounded bg-emerald-100 text-emerald-700 dark:bg-emerald-800 dark:text-emerald-200 hover:bg-emerald-200 dark:hover:bg-emerald-700 transition-colors"
                            :title="t('permissions.grantAll')"
                          >
                            {{ isArabic ? 'منح' : 'All' }}
                          </button>
                          <button
                            @click.stop="revokeAllForRole(role.id)"
                            class="text-[9px] px-1.5 py-0.5 rounded bg-red-100 text-red-700 dark:bg-red-800 dark:text-red-200 hover:bg-red-200 dark:hover:bg-red-700 transition-colors"
                            :title="t('permissions.revokeAll')"
                          >
                            {{ isArabic ? 'سحب' : 'None' }}
                          </button>
                        </div>
                      </div>
                    </th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-100 dark:divide-gray-700/50">
                  <tr
                    v-for="(resource, resourceIdx) in scope.resources"
                    :key="`${resource.resourceType}-${resource.competitionPhase}-${resource.committeeRole}`"
                    class="hover:bg-gray-50/50 dark:hover:bg-gray-800/50 transition-colors"
                  >
                    <!-- Resource Name (sticky) -->
                    <td class="px-4 py-3 sticky start-0 bg-white dark:bg-gray-800 z-10 border-e border-gray-200 dark:border-gray-700">
                      <div>
                        <span class="font-medium text-gray-900 dark:text-white text-sm">
                          {{ getResourceName(resource) }}
                        </span>
                        <span
                          v-if="getResourceSubtext(resource)"
                          class="block text-[11px] text-gray-400 dark:text-gray-500 mt-0.5"
                        >
                          {{ getResourceSubtext(resource) }}
                        </span>
                      </div>
                    </td>

                    <!-- Role Cells -->
                    <td
                      v-for="role in roles"
                      :key="role.id"
                      class="text-center px-2 py-2 relative"
                      :class="{
                        'bg-amber-50/60 dark:bg-amber-900/10': isCellModified(scopeIdx, resourceIdx, role.id),
                      }"
                    >
                      <!-- Compact Cell: Show action count badge + click to expand -->
                      <div class="flex flex-col items-center gap-1">
                        <!-- Action count indicator -->
                        <button
                          @click="toggleExpandedCell(scopeIdx, resourceIdx, role.id)"
                          class="relative inline-flex items-center justify-center w-9 h-9 rounded-lg transition-all duration-150"
                          :class="[
                            getActiveActionsCount(scopeIdx, resourceIdx, role.id) > 0
                              ? 'bg-emerald-100 dark:bg-emerald-900/40 text-emerald-700 dark:text-emerald-300 hover:bg-emerald-200 dark:hover:bg-emerald-800/60'
                              : 'bg-gray-100 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 hover:bg-gray-200 dark:hover:bg-gray-600/50',
                            role.isProtected ? 'cursor-default' : 'cursor-pointer',
                            isCellModified(scopeIdx, resourceIdx, role.id) ? 'ring-2 ring-amber-400 dark:ring-amber-500' : ''
                          ]"
                        >
                          <span class="text-xs font-bold">
                            {{ getActiveActionsCount(scopeIdx, resourceIdx, role.id) }}
                          </span>
                        </button>

                        <!-- Expanded: Individual action toggles (popup) -->
                        <div
                          v-if="expandedCell === getCellKey(scopeIdx, resourceIdx, role.id)"
                          class="absolute top-full start-1/2 -translate-x-1/2 z-30 mt-1 bg-white dark:bg-gray-800 rounded-xl shadow-xl border border-gray-200 dark:border-gray-700 p-3 min-w-[180px]"
                          @click.stop
                        >
                          <div class="flex items-center justify-between mb-2 pb-2 border-b border-gray-100 dark:border-gray-700">
                            <span class="text-xs font-semibold text-gray-700 dark:text-gray-200">
                              {{ isArabic ? 'الإجراءات' : 'Actions' }}
                            </span>
                            <button
                              v-if="canEdit && !role.isProtected"
                              @click="toggleAllActionsForCell(scopeIdx, resourceIdx, role.id)"
                              class="text-[10px] px-2 py-0.5 rounded bg-blue-100 text-blue-700 dark:bg-blue-800 dark:text-blue-200 hover:bg-blue-200 transition-colors"
                            >
                              {{ isArabic ? 'تبديل الكل' : 'Toggle All' }}
                            </button>
                          </div>
                          <div class="space-y-1.5">
                            <label
                              v-for="action in getRelevantActions(scope.scope)"
                              :key="action.key"
                              class="flex items-center justify-between gap-3 py-1 px-1 rounded hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
                              :class="canEdit && !role.isProtected ? 'cursor-pointer' : 'cursor-default opacity-70'"
                            >
                              <span class="text-xs text-gray-700 dark:text-gray-300">{{ getActionName(action) }}</span>
                              <!-- Toggle Switch -->
                              <button
                                @click.prevent="toggleAction(scopeIdx, resourceIdx, role.id, action.flag)"
                                :disabled="!canEdit || role.isProtected"
                                class="relative inline-flex h-5 w-9 flex-shrink-0 rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none"
                                :class="[
                                  hasAction(scopeIdx, resourceIdx, role.id, action.flag)
                                    ? 'bg-emerald-500'
                                    : 'bg-gray-300 dark:bg-gray-600',
                                  canEdit && !role.isProtected ? 'cursor-pointer' : 'cursor-not-allowed opacity-60'
                                ]"
                              >
                                <span
                                  class="pointer-events-none inline-block h-4 w-4 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out"
                                  :class="hasAction(scopeIdx, resourceIdx, role.id, action.flag) ? 'translate-x-4 rtl:-translate-x-4' : 'translate-x-0'"
                                />
                              </button>
                            </label>
                          </div>
                          <!-- Close button -->
                          <button
                            @click="expandedCell = null"
                            class="mt-2 w-full text-center text-[10px] text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 py-1 transition-colors"
                          >
                            {{ isArabic ? 'إغلاق' : 'Close' }}
                          </button>
                        </div>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </template>

      <!-- Legend -->
      <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
        <h4 class="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">
          {{ isArabic ? 'دليل الألوان' : 'Legend' }}
        </h4>
        <div class="flex flex-wrap gap-4 text-xs">
          <div class="flex items-center gap-2">
            <div class="w-8 h-8 rounded-lg bg-emerald-100 dark:bg-emerald-900/40 flex items-center justify-center text-emerald-700 dark:text-emerald-300 font-bold text-xs">5</div>
            <span class="text-gray-600 dark:text-gray-400">{{ isArabic ? 'صلاحيات مفعلة' : 'Active permissions' }}</span>
          </div>
          <div class="flex items-center gap-2">
            <div class="w-8 h-8 rounded-lg bg-gray-100 dark:bg-gray-700/50 flex items-center justify-center text-gray-400 dark:text-gray-500 font-bold text-xs">0</div>
            <span class="text-gray-600 dark:text-gray-400">{{ isArabic ? 'بدون صلاحيات' : 'No permissions' }}</span>
          </div>
          <div class="flex items-center gap-2">
            <div class="w-8 h-8 rounded-lg bg-emerald-100 dark:bg-emerald-900/40 ring-2 ring-amber-400 flex items-center justify-center text-emerald-700 dark:text-emerald-300 font-bold text-xs">3</div>
            <span class="text-gray-600 dark:text-gray-400">{{ isArabic ? 'تم التعديل (غير محفوظ)' : 'Modified (unsaved)' }}</span>
          </div>
          <div class="flex items-center gap-2">
            <span class="text-amber-600 dark:text-amber-400 font-semibold">{{ isArabic ? 'محمي' : 'Protected' }}</span>
            <span class="text-gray-600 dark:text-gray-400">{{ isArabic ? 'دور محمي لا يمكن تعديله' : 'Protected role (read-only)' }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Click-away overlay for expanded cells -->
    <div
      v-if="expandedCell"
      class="fixed inset-0 z-20"
      @click="expandedCell = null"
    />
  </div>
</template>
