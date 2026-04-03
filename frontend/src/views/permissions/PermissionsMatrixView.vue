<script setup lang="ts">
/**
 * PermissionsMatrixView - Flexible Multi-Dimensional Permission Matrix
 *
 * Provides a comprehensive UI for managing the permission matrix per tenant.
 * Dimensions:
 *   1. Role (system role or custom role)
 *   2. Scope (Global / Competition / Committee)
 *   3. ResourceType (Organization, Users, Competition, Offers, etc.)
 *   4. CompetitionPhase (optional, for competition-scoped resources)
 *   5. CommitteeRole (optional, for committee-scoped resources)
 *   6. PermissionAction flags (Read, Create, Update, Delete, Approve, Reject, Submit, Upload, Score, Sign)
 *
 * Features:
 *   - Scope-based tabs (Global, Competition, Committee)
 *   - Role selector dropdown
 *   - Phase & committee role filters for scoped resources
 *   - Interactive checkbox grid for each action per resource
 *   - Bulk grant/revoke per role
 *   - Customization indicator (shows which rules differ from defaults)
 *   - Reset to defaults
 *   - Seed default rules for new tenants
 *   - RTL/LTR support
 *
 * All data fetched dynamically from APIs - NO mock data.
 */
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { httpGet, httpPut, httpPost } from '@/services/http'

const { t, locale } = useI18n()
const isArabic = computed(() => locale.value === 'ar')

/* ── Types ── */
interface PermissionRule {
  id: string
  roleId: string
  roleName: string
  roleNameEn: string
  scope: number
  scopeNameAr: string
  scopeNameEn: string
  resourceType: number
  resourceTypeNameAr: string
  resourceTypeNameEn: string
  committeeRole: number | null
  competitionPhase: number | null
  allowedActions: number
  isCustomized: boolean
  isActive: boolean
}

interface RoleOption {
  id: string
  nameAr: string
  nameEn: string
}

/* ── Permission Action Flags (must match backend PermissionAction enum) ── */
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

/* ── Scope enum values ── */
const SCOPES = {
  Global: 0,
  Competition: 1,
  Committee: 2,
}

/* ── Competition Phase enum values ── */
const COMPETITION_PHASES: Record<number, string> = {
  0: 'bookletPreparation',
  1: 'bookletApproval',
  2: 'bookletPublishing',
  3: 'offerReception',
  4: 'technicalAnalysis',
  5: 'financialAnalysis',
  6: 'awardNotification',
  7: 'contractApproval',
  8: 'contractSigning',
}

/* ── Committee Role enum values ── */
const COMMITTEE_ROLES: Record<number, string> = {
  0: 'none',
  1: 'preparationChair',
  2: 'preparationMember',
  3: 'technicalChair',
  4: 'technicalMember',
  5: 'financialChair',
  6: 'financialMember',
  7: 'inquiryChair',
  8: 'inquiryMember',
  9: 'secretary',
}

/* ── Reactive State ── */
const loading = ref(false)
const saving = ref(false)
const seeding = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const allRules = ref<PermissionRule[]>([])
const roles = ref<RoleOption[]>([])

const selectedRoleId = ref<string>('')
const selectedScope = ref<number | null>(null)
const selectedPhase = ref<number | null>(null)
const selectedCommitteeRole = ref<number | null>(null)

// Track pending changes: ruleId -> new allowedActions
const pendingChanges = ref<Map<string, number>>(new Map())

/* ── Scope/Phase/Committee change handlers (fix string-to-number conversion) ── */
function onScopeChange(event: Event) {
  const val = (event.target as HTMLSelectElement).value
  selectedScope.value = val === '' ? null : Number(val)
}

function onPhaseChange(event: Event) {
  const val = (event.target as HTMLSelectElement).value
  selectedPhase.value = val === '' ? null : Number(val)
}

function onCommitteeRoleChange(event: Event) {
  const val = (event.target as HTMLSelectElement).value
  selectedCommitteeRole.value = val === '' ? null : Number(val)
}

/* ── Computed ── */
const hasRules = computed(() => allRules.value.length > 0)
const hasChanges = computed(() => pendingChanges.value.size > 0)
const changesCount = computed(() => pendingChanges.value.size)

// Extract unique roles from rules
const availableRoles = computed(() => {
  if (roles.value.length > 0) return roles.value
  const roleMap = new Map<string, RoleOption>()
  allRules.value.forEach(r => {
    if (!roleMap.has(r.roleId)) {
      roleMap.set(r.roleId, {
        id: r.roleId,
        nameAr: r.roleName,
        nameEn: r.roleNameEn,
      })
    }
  })
  return Array.from(roleMap.values())
})

// Filter rules based on selected filters
const filteredRules = computed(() => {
  let rules = allRules.value

  if (selectedRoleId.value) {
    rules = rules.filter(r => r.roleId === selectedRoleId.value)
  }

  if (selectedScope.value !== null) {
    rules = rules.filter(r => r.scope === selectedScope.value)
  }

  if (selectedPhase.value !== null) {
    rules = rules.filter(r => r.competitionPhase === selectedPhase.value || r.competitionPhase === null)
  }

  if (selectedCommitteeRole.value !== null) {
    rules = rules.filter(r => r.committeeRole === selectedCommitteeRole.value || r.committeeRole === null)
  }

  return rules
})

// Group filtered rules by scope for display
const groupedByScope = computed(() => {
  const groups: Record<number, PermissionRule[]> = {}
  filteredRules.value.forEach(r => {
    if (!groups[r.scope]) groups[r.scope] = []
    groups[r.scope].push(r)
  })
  return groups
})

// Stats
const totalRules = computed(() => allRules.value.length)
const customizedRulesCount = computed(() => allRules.value.filter(r => r.isCustomized).length)

// Determine which action columns are relevant for a scope
function getActionsForScope(scope: number): string[] {
  if (scope === SCOPES.Global) {
    return ['read', 'create', 'update', 'delete']
  } else if (scope === SCOPES.Competition) {
    return ['read', 'create', 'update', 'delete', 'approve', 'reject', 'submit', 'upload', 'score', 'sign']
  } else {
    return ['read', 'create', 'update', 'delete', 'approve']
  }
}

/* ── Methods ── */
function hasAction(rule: PermissionRule, actionKey: string): boolean {
  const flag = ACTION_FLAGS[actionKey]
  const currentActions = pendingChanges.value.has(rule.id)
    ? pendingChanges.value.get(rule.id)!
    : rule.allowedActions
  return (currentActions & flag) !== 0
}

function toggleAction(rule: PermissionRule, actionKey: string) {
  const flag = ACTION_FLAGS[actionKey]
  const currentActions = pendingChanges.value.has(rule.id)
    ? pendingChanges.value.get(rule.id)!
    : rule.allowedActions

  const newActions = (currentActions & flag) !== 0
    ? currentActions & ~flag  // Remove flag
    : currentActions | flag   // Add flag

  if (newActions === rule.allowedActions) {
    // No change from original - remove from pending
    pendingChanges.value.delete(rule.id)
  } else {
    pendingChanges.value.set(rule.id, newActions)
  }
}

function grantAllForRole() {
  if (!selectedRoleId.value) return
  const roleRules = allRules.value.filter(r => r.roleId === selectedRoleId.value)
  const fullAccess = 1023 // All 10 flags set
  roleRules.forEach(r => {
    if (r.allowedActions !== fullAccess) {
      pendingChanges.value.set(r.id, fullAccess)
    }
  })
}

function revokeAllForRole() {
  if (!selectedRoleId.value) return
  const roleRules = allRules.value.filter(r => r.roleId === selectedRoleId.value)
  roleRules.forEach(r => {
    if (r.allowedActions !== 0) {
      pendingChanges.value.set(r.id, 0)
    }
  })
}

function getResourceName(rule: PermissionRule): string {
  return isArabic.value ? rule.resourceTypeNameAr : rule.resourceTypeNameEn
}

function getScopeName(scope: number): string {
  const key = scope === 0 ? 'global' : scope === 1 ? 'competition' : 'committee'
  return t(`permissions.scopes.${key}`)
}

function getPhaseName(phase: number | null): string {
  if (phase === null) return ''
  const key = COMPETITION_PHASES[phase]
  return key ? t(`permissions.phases.${key}`) : ''
}

function getCommitteeRoleName(role: number | null): string {
  if (role === null) return ''
  const key = COMMITTEE_ROLES[role]
  return key ? t(`permissions.committeeRoles.${key}`) : ''
}

function getRoleName(role: RoleOption): string {
  return isArabic.value ? role.nameAr : role.nameEn
}

/* ── API Calls ── */
async function loadMatrix() {
  loading.value = true
  errorMessage.value = ''
  try {
    const data = await httpGet<PermissionRule[]>('/permission-matrix')
    allRules.value = data || []
    pendingChanges.value.clear()
  } catch (err: any) {
    errorMessage.value = t('permissions.loadError')
    console.error('Failed to load permission matrix:', err)
  } finally {
    loading.value = false
  }
}

async function loadRoles() {
  try {
    const data = await httpGet<any[]>('/user-management/roles')
    if (Array.isArray(data)) {
      roles.value = data.map((r: any) => ({
        id: r.id,
        nameAr: r.nameAr || r.name || '',
        nameEn: r.nameEn || r.name || '',
      }))
    }
  } catch (err) {
    console.error('Failed to load roles:', err)
  }
}

async function saveChanges() {
  if (!hasChanges.value) return
  saving.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    // Save each changed rule individually
    const promises = Array.from(pendingChanges.value.entries()).map(([ruleId, actions]) =>
      httpPut(`/permission-matrix/rules/${ruleId}`, { allowedActions: actions })
    )
    await Promise.all(promises)

    // Update local state
    pendingChanges.value.forEach((actions, ruleId) => {
      const rule = allRules.value.find(r => r.id === ruleId)
      if (rule) {
        rule.allowedActions = actions
        rule.isCustomized = true
      }
    })
    pendingChanges.value.clear()
    successMessage.value = t('permissions.saveSuccess')
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
    await loadMatrix()
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
    await loadMatrix()
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
  await Promise.all([loadMatrix(), loadRoles()])
  // Auto-select first role if available
  if (availableRoles.value.length > 0 && !selectedRoleId.value) {
    selectedRoleId.value = availableRoles.value[0].id
  }
})

// Auto-select first role when roles change
watch(availableRoles, (newRoles) => {
  if (newRoles.length > 0 && !selectedRoleId.value) {
    selectedRoleId.value = newRoles[0].id
  }
})
</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <!-- Header -->
    <div class="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 px-6 py-5">
      <div class="flex items-center justify-between flex-wrap gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">
            {{ t('permissions.title') }}
          </h1>
          <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
            {{ t('permissions.subtitle') }}
          </p>
        </div>
        <div class="flex items-center gap-3 flex-wrap">
          <!-- Stats badges -->
          <div v-if="hasRules" class="flex items-center gap-4 me-4">
            <div class="text-center">
              <div class="text-lg font-semibold text-blue-600">{{ totalRules }}</div>
              <div class="text-xs text-gray-500">{{ t('permissions.totalRules') }}</div>
            </div>
            <div class="text-center">
              <div class="text-lg font-semibold text-amber-600">{{ customizedRulesCount }}</div>
              <div class="text-xs text-gray-500">{{ t('permissions.customizedRules') }}</div>
            </div>
          </div>

          <!-- Seed button (only when no rules) -->
          <button
            v-if="!hasRules && !loading"
            @click="seedDefaultRules"
            :disabled="seeding"
            class="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors"
          >
            <svg v-if="seeding" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
            </svg>
            <svg v-else class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
            </svg>
            {{ seeding ? t('permissions.seeding') : t('permissions.seedRules') }}
          </button>

          <!-- Reset button -->
          <button
            v-if="hasRules"
            @click="resetToDefaults"
            class="inline-flex items-center gap-2 px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
          >
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            {{ t('permissions.resetToDefaults') }}
          </button>

          <!-- Save button -->
          <button
            v-if="hasRules"
            @click="saveChanges"
            :disabled="!hasChanges || saving"
            class="inline-flex items-center gap-2 px-4 py-2 rounded-lg transition-colors"
            :class="hasChanges
              ? 'bg-green-600 text-white hover:bg-green-700'
              : 'bg-gray-200 text-gray-500 cursor-not-allowed dark:bg-gray-700 dark:text-gray-400'"
          >
            <svg v-if="saving" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
            </svg>
            <svg v-else class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
            </svg>
            <span v-if="saving">{{ t('permissions.saving') }}</span>
            <span v-else-if="hasChanges">{{ t('permissions.saveChanges') }} ({{ changesCount }})</span>
            <span v-else>{{ t('permissions.noChanges') }}</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Messages -->
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

    <!-- Loading state -->
    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="text-center">
        <svg class="animate-spin h-10 w-10 text-blue-600 mx-auto mb-4" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
        </svg>
        <p class="text-gray-500 dark:text-gray-400">{{ t('common.loading') || 'Loading...' }}</p>
      </div>
    </div>

    <!-- Empty state -->
    <div v-else-if="!hasRules" class="flex items-center justify-center py-20">
      <div class="text-center max-w-md">
        <svg class="h-16 w-16 text-gray-300 dark:text-gray-600 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
        </svg>
        <h3 class="text-lg font-medium text-gray-900 dark:text-white mb-2">{{ t('permissions.noRules') }}</h3>
        <p class="text-sm text-gray-500 dark:text-gray-400">{{ t('permissions.noRulesDesc') }}</p>
      </div>
    </div>

    <!-- Main Content -->
    <div v-else class="p-6 space-y-6">
      <!-- Filters Bar -->
      <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
        <div class="flex flex-wrap items-center gap-4">
          <!-- Role selector -->
          <div class="flex-1 min-w-[200px]">
            <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">
              {{ t('permissions.role') }}
            </label>
            <select
              v-model="selectedRoleId"
              class="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="">{{ t('permissions.selectRole') }}</option>
              <option v-for="role in availableRoles" :key="role.id" :value="role.id">
                {{ getRoleName(role) }}
              </option>
            </select>
          </div>

          <!-- Scope selector (using @change handler for proper number conversion) -->
          <div class="flex-1 min-w-[180px]">
            <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">
              {{ t('permissions.scope') }}
            </label>
            <select
              :value="selectedScope === null ? '' : selectedScope"
              @change="onScopeChange"
              class="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="">{{ t('permissions.allScopes') }}</option>
              <option value="0">{{ t('permissions.scopes.global') }}</option>
              <option value="1">{{ t('permissions.scopes.competition') }}</option>
              <option value="2">{{ t('permissions.scopes.committee') }}</option>
            </select>
          </div>

          <!-- Phase filter (only for competition scope) -->
          <div v-if="selectedScope === 1 || selectedScope === null" class="flex-1 min-w-[180px]">
            <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">
              {{ t('permissions.phase') }}
            </label>
            <select
              :value="selectedPhase === null ? '' : selectedPhase"
              @change="onPhaseChange"
              class="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="">{{ t('permissions.allPhases') }}</option>
              <option v-for="(key, val) in COMPETITION_PHASES" :key="val" :value="val">
                {{ t(`permissions.phases.${key}`) }}
              </option>
            </select>
          </div>

          <!-- Committee Role filter (only for committee scope) -->
          <div v-if="selectedScope === 2 || selectedScope === null" class="flex-1 min-w-[180px]">
            <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">
              {{ t('permissions.committee') }}
            </label>
            <select
              :value="selectedCommitteeRole === null ? '' : selectedCommitteeRole"
              @change="onCommitteeRoleChange"
              class="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="">{{ t('permissions.allCommitteeRoles') }}</option>
              <option v-for="(key, val) in COMMITTEE_ROLES" :key="val" :value="val">
                {{ t(`permissions.committeeRoles.${key}`) }}
              </option>
            </select>
          </div>

          <!-- Bulk actions -->
          <div v-if="selectedRoleId" class="flex items-end gap-2 pt-5">
            <button
              @click="grantAllForRole"
              class="px-3 py-2 text-xs font-medium bg-green-50 dark:bg-green-900/30 text-green-700 dark:text-green-300 border border-green-200 dark:border-green-800 rounded-lg hover:bg-green-100 dark:hover:bg-green-900/50 transition-colors"
            >
              {{ t('permissions.grantAll') }}
            </button>
            <button
              @click="revokeAllForRole"
              class="px-3 py-2 text-xs font-medium bg-red-50 dark:bg-red-900/30 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-800 rounded-lg hover:bg-red-100 dark:hover:bg-red-900/50 transition-colors"
            >
              {{ t('permissions.revokeAll') }}
            </button>
          </div>
        </div>
      </div>

      <!-- Permission Matrix Tables - one per scope -->
      <template v-for="scope in [0, 1, 2]" :key="scope">
        <div
          v-if="groupedByScope[scope] && groupedByScope[scope].length > 0"
          class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden"
        >
          <!-- Scope Header -->
          <div class="px-5 py-3 border-b border-gray-200 dark:border-gray-700 flex items-center gap-3"
               :class="{
                 'bg-blue-50 dark:bg-blue-900/20': scope === 0,
                 'bg-amber-50 dark:bg-amber-900/20': scope === 1,
                 'bg-purple-50 dark:bg-purple-900/20': scope === 2,
               }">
            <div class="w-2 h-2 rounded-full"
                 :class="{
                   'bg-blue-500': scope === 0,
                   'bg-amber-500': scope === 1,
                   'bg-purple-500': scope === 2,
                 }"></div>
            <h2 class="text-base font-semibold"
                :class="{
                  'text-blue-800 dark:text-blue-300': scope === 0,
                  'text-amber-800 dark:text-amber-300': scope === 1,
                  'text-purple-800 dark:text-purple-300': scope === 2,
                }">
              {{ getScopeName(scope) }}
            </h2>
            <span class="text-xs px-2 py-0.5 rounded-full"
                  :class="{
                    'bg-blue-100 text-blue-700 dark:bg-blue-800 dark:text-blue-200': scope === 0,
                    'bg-amber-100 text-amber-700 dark:bg-amber-800 dark:text-amber-200': scope === 1,
                    'bg-purple-100 text-purple-700 dark:bg-purple-800 dark:text-purple-200': scope === 2,
                  }">
              {{ (groupedByScope[scope] || []).length }}
            </span>
          </div>

          <!-- Matrix Table -->
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="bg-gray-50 dark:bg-gray-900/50">
                  <th class="text-start px-4 py-3 font-medium text-gray-600 dark:text-gray-300 min-w-[200px] sticky start-0 bg-gray-50 dark:bg-gray-900/50 z-10">
                    {{ t('permissions.resource') }}
                  </th>
                  <th v-if="scope === 1" class="text-start px-3 py-3 font-medium text-gray-600 dark:text-gray-300 min-w-[120px]">
                    {{ t('permissions.phase') }}
                  </th>
                  <th v-if="scope === 2" class="text-start px-3 py-3 font-medium text-gray-600 dark:text-gray-300 min-w-[140px]">
                    {{ t('permissions.committee') }}
                  </th>
                  <th
                    v-for="action in getActionsForScope(scope)"
                    :key="action"
                    class="text-center px-2 py-3 font-medium text-gray-600 dark:text-gray-300 min-w-[70px]"
                  >
                    {{ t(`permissions.actions.${action}`) }}
                  </th>
                  <th class="text-center px-3 py-3 font-medium text-gray-600 dark:text-gray-300 min-w-[80px]">
                    {{ t('permissions.customized') }}
                  </th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100 dark:divide-gray-700">
                <tr
                  v-for="rule in (groupedByScope[scope] || [])"
                  :key="rule.id"
                  class="hover:bg-gray-50 dark:hover:bg-gray-900/30 transition-colors"
                  :class="{ 'bg-amber-50/50 dark:bg-amber-900/10': pendingChanges.has(rule.id) }"
                >
                  <!-- Resource Name -->
                  <td class="px-4 py-3 sticky start-0 bg-white dark:bg-gray-800 z-10"
                      :class="{ '!bg-amber-50/50 dark:!bg-amber-900/10': pendingChanges.has(rule.id) }">
                    <span class="font-medium text-gray-900 dark:text-white">{{ getResourceName(rule) }}</span>
                  </td>

                  <!-- Phase (for competition scope) -->
                  <td v-if="scope === 1" class="px-3 py-3 text-xs text-gray-500 dark:text-gray-400">
                    {{ getPhaseName(rule.competitionPhase) || '—' }}
                  </td>

                  <!-- Committee Role (for committee scope) -->
                  <td v-if="scope === 2" class="px-3 py-3 text-xs text-gray-500 dark:text-gray-400">
                    {{ getCommitteeRoleName(rule.committeeRole) || '—' }}
                  </td>

                  <!-- Action checkboxes -->
                  <td
                    v-for="action in getActionsForScope(scope)"
                    :key="action"
                    class="text-center px-2 py-3"
                  >
                    <label class="inline-flex items-center justify-center cursor-pointer">
                      <input
                        type="checkbox"
                        :checked="hasAction(rule, action)"
                        @change="toggleAction(rule, action)"
                        class="w-4 h-4 rounded border-gray-300 dark:border-gray-600 text-blue-600 focus:ring-blue-500 dark:focus:ring-blue-400 dark:bg-gray-700 cursor-pointer"
                      />
                    </label>
                  </td>

                  <!-- Customized indicator -->
                  <td class="text-center px-3 py-3">
                    <span
                      v-if="rule.isCustomized"
                      class="text-[10px] px-2 py-1 rounded-full font-medium bg-amber-100 text-amber-700 dark:bg-amber-800 dark:text-amber-200"
                    >
                      {{ t('permissions.customized') }}
                    </span>
                    <span
                      v-else
                      class="text-[10px] px-2 py-1 rounded-full font-medium bg-gray-100 text-gray-500 dark:bg-gray-700 dark:text-gray-400"
                    >
                      {{ t('permissions.default') }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </template>

      <!-- No results for current filters -->
      <div v-if="filteredRules.length === 0 && hasRules" class="text-center py-12">
        <svg class="h-12 w-12 text-gray-300 dark:text-gray-600 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <p class="text-gray-500 dark:text-gray-400">{{ t('permissions.noRolesForStage') }}</p>
      </div>
    </div>
  </div>
</template>
