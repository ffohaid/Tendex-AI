<script setup lang="ts">
/**
 * Tenant Feature Flags Management View — Super Admin Portal.
 *
 * Displays and manages feature flags for a specific government entity.
 * Features:
 * - Merged view combining global definitions with tenant-specific state
 * - Category-based grouping with collapsible sections
 * - Individual toggle and batch save operations
 * - Search and filter capabilities
 * - Real-time status indicators
 *
 * Data is fetched dynamically from the API — NO mock data.
 */
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter, useRoute } from 'vue-router'
import { storeToRefs } from 'pinia'
import { useFeatureFlagStore } from '@/stores/featureFlag'
import { useTenantStore } from '@/stores/tenant'
import type { MergedFeatureFlag, BatchToggleFeatureFlagsRequest } from '@/types/featureFlag'

const { t, locale } = useI18n()
const router = useRouter()
const route = useRoute()
const featureFlagStore = useFeatureFlagStore()
const tenantStore = useTenantStore()

const {
  mergedFlags,
  flagsByCategory,
  categories,
  enabledCount,
  totalCount,
  isLoading,
  isSubmitting,
  error,
  successMessage,
} = storeToRefs(featureFlagStore)

const { currentTenant } = storeToRefs(tenantStore)

/** Tenant ID from route */
const tenantId = computed(() => route.params.id as string)

/** Search query */
const searchQuery = ref('')

/** Category filter */
const selectedCategory = ref<string | null>(null)

/** Expanded categories */
const expandedCategories = ref<Set<string>>(new Set())

/** Track local changes for batch save */
const localChanges = ref<Map<string, boolean>>(new Map())

/** Whether there are unsaved changes */
const hasUnsavedChanges = computed(() => localChanges.value.size > 0)

/** Get tenant display name */
function getTenantName(): string {
  if (!currentTenant.value) return ''
  return locale.value === 'ar'
    ? currentTenant.value.nameAr
    : currentTenant.value.nameEn
}

/** Get feature display name */
function getFeatureName(flag: MergedFeatureFlag): string {
  return locale.value === 'ar' ? flag.nameAr : flag.nameEn
}

/** Get feature description */
function getFeatureDescription(flag: MergedFeatureFlag): string {
  const desc = locale.value === 'ar' ? flag.descriptionAr : flag.descriptionEn
  return desc || ''
}

/** Filtered flags based on search and category */
const filteredFlagsByCategory = computed(() => {
  const result: Record<string, MergedFeatureFlag[]> = {}
  const query = searchQuery.value.toLowerCase().trim()

  for (const [category, flags] of Object.entries(flagsByCategory.value)) {
    // Apply category filter
    if (selectedCategory.value && category !== selectedCategory.value) continue

    // Apply search filter
    const filtered = flags.filter((flag) => {
      if (!query) return true
      return (
        flag.nameAr.toLowerCase().includes(query) ||
        flag.nameEn.toLowerCase().includes(query) ||
        flag.featureKey.toLowerCase().includes(query) ||
        (flag.descriptionAr && flag.descriptionAr.toLowerCase().includes(query)) ||
        (flag.descriptionEn && flag.descriptionEn.toLowerCase().includes(query))
      )
    })

    if (filtered.length > 0) {
      result[category] = filtered
    }
  }

  return result
})

/** Total filtered count */
const filteredCount = computed(() => {
  return Object.values(filteredFlagsByCategory.value).reduce(
    (sum, flags) => sum + flags.length,
    0,
  )
})

/** Get the effective enabled state (local change or original) */
function getEffectiveState(flag: MergedFeatureFlag): boolean {
  if (localChanges.value.has(flag.featureKey)) {
    return localChanges.value.get(flag.featureKey)!
  }
  return flag.isEnabled
}

/** Toggle a feature flag locally */
function handleToggle(flag: MergedFeatureFlag): void {
  const currentState = getEffectiveState(flag)
  const newState = !currentState

  // If toggling back to original state, remove from changes
  if (newState === flag.isEnabled) {
    localChanges.value.delete(flag.featureKey)
  } else {
    localChanges.value.set(flag.featureKey, newState)
  }
  // Force reactivity
  localChanges.value = new Map(localChanges.value)
}

/** Toggle category expansion */
function toggleCategory(category: string): void {
  if (expandedCategories.value.has(category)) {
    expandedCategories.value.delete(category)
  } else {
    expandedCategories.value.add(category)
  }
  expandedCategories.value = new Set(expandedCategories.value)
}

/** Check if category is expanded */
function isCategoryExpanded(category: string): boolean {
  return expandedCategories.value.has(category)
}

/** Get category icon */
function getCategoryIcon(category: string): string {
  const icons: Record<string, string> = {
    AI: 'pi pi-microchip-ai',
    Workflow: 'pi pi-sitemap',
    Analytics: 'pi pi-chart-bar',
    Security: 'pi pi-shield',
    Integration: 'pi pi-link',
    Communication: 'pi pi-comments',
    Documents: 'pi pi-file',
  }
  return icons[category] || 'pi pi-cog'
}

/** Get category display name */
function getCategoryName(category: string): string {
  const key = `featureFlags.categories.${category.toLowerCase()}`
  const translated = t(key)
  // If no translation found, return the original category name
  return translated === key ? category : translated
}

/** Count enabled flags in a category */
function getCategoryEnabledCount(flags: MergedFeatureFlag[]): number {
  return flags.filter((f) => getEffectiveState(f)).length
}

/** Save all changes via batch toggle */
async function saveAllChanges(): Promise<void> {
  if (!hasUnsavedChanges.value) return

  const request: BatchToggleFeatureFlagsRequest = {
    flags: Array.from(localChanges.value.entries()).map(([featureKey, isEnabled]) => ({
      featureKey,
      isEnabled,
      configuration: null,
    })),
  }

  const success = await featureFlagStore.batchToggle(request)
  if (success) {
    localChanges.value = new Map()
  }
}

/** Discard all local changes */
function discardChanges(): void {
  localChanges.value = new Map()
}

/** Enable all features */
function enableAll(): void {
  for (const flag of mergedFlags.value) {
    if (!flag.isEnabled) {
      localChanges.value.set(flag.featureKey, true)
    } else {
      localChanges.value.delete(flag.featureKey)
    }
  }
  localChanges.value = new Map(localChanges.value)
}

/** Disable all features */
function disableAll(): void {
  for (const flag of mergedFlags.value) {
    if (flag.isEnabled) {
      localChanges.value.set(flag.featureKey, false)
    } else {
      localChanges.value.delete(flag.featureKey)
    }
  }
  localChanges.value = new Map(localChanges.value)
}

/** Reset to defaults */
function resetToDefaults(): void {
  for (const flag of mergedFlags.value) {
    if (flag.isEnabled !== flag.isEnabledByDefault) {
      localChanges.value.set(flag.featureKey, flag.isEnabledByDefault)
    } else {
      localChanges.value.delete(flag.featureKey)
    }
  }
  localChanges.value = new Map(localChanges.value)
}

/** Go back to tenant detail */
function goBack(): void {
  router.push({ name: 'TenantDetail', params: { id: tenantId.value } })
}

/** Load data on mount */
onMounted(async () => {
  // Load tenant detail if not already loaded
  if (!currentTenant.value || currentTenant.value.id !== tenantId.value) {
    await tenantStore.loadTenantDetail(tenantId.value)
  }
  // Load feature flags
  await featureFlagStore.loadTenantFlags(tenantId.value)
  // Expand all categories by default
  for (const cat of categories.value) {
    expandedCategories.value.add(cat)
  }
  expandedCategories.value = new Set(expandedCategories.value)
})

/** Clear messages after timeout */
watch(successMessage, (val) => {
  if (val) {
    setTimeout(() => featureFlagStore.clearMessages(), 4000)
  }
})
</script>

<template>
  <div class="min-h-screen bg-surface-ground">
    <div class="mx-auto max-w-5xl px-4 py-8 sm:px-6">
      <!-- Back button -->
      <button
        type="button"
        class="mb-4 flex items-center gap-1 text-sm text-tertiary hover:text-primary"
        @click="goBack"
      >
        <i class="pi pi-arrow-right rotate-180 text-xs rtl:rotate-0"></i>
        {{ t('featureFlags.actions.backToTenant') }}
      </button>

      <!-- Loading -->
      <div v-if="isLoading" class="flex items-center justify-center py-20">
        <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
      </div>

      <!-- Error -->
      <div
        v-else-if="error && !mergedFlags.length"
        class="rounded-lg border border-danger/20 bg-danger/5 p-8 text-center"
      >
        <i class="pi pi-exclamation-triangle mb-3 text-3xl text-danger"></i>
        <p class="text-sm text-danger">{{ error }}</p>
        <button
          type="button"
          class="mt-4 rounded-lg bg-primary px-4 py-2 text-sm text-white hover:bg-primary-dark"
          @click="featureFlagStore.loadTenantFlags(tenantId)"
        >
          {{ t('common.retry') }}
        </button>
      </div>

      <!-- Content -->
      <template v-else>
        <!-- Header -->
        <div class="mb-6 flex items-start justify-between">
          <div>
            <h1 class="text-2xl font-bold text-secondary">
              {{ t('featureFlags.title') }}
            </h1>
            <p class="mt-1 text-sm text-tertiary">
              {{ t('featureFlags.subtitle', { tenant: getTenantName() }) }}
            </p>
          </div>
        </div>

        <!-- Stats bar -->
        <div class="mb-6 grid grid-cols-2 gap-4 sm:grid-cols-4">
          <div class="rounded-lg border border-surface-dim bg-white p-4 text-center">
            <p class="text-2xl font-bold text-secondary">{{ totalCount }}</p>
            <p class="text-xs text-tertiary">{{ t('featureFlags.stats.total') }}</p>
          </div>
          <div class="rounded-lg border border-surface-dim bg-white p-4 text-center">
            <p class="text-2xl font-bold text-emerald-600">{{ enabledCount }}</p>
            <p class="text-xs text-tertiary">{{ t('featureFlags.stats.enabled') }}</p>
          </div>
          <div class="rounded-lg border border-surface-dim bg-white p-4 text-center">
            <p class="text-2xl font-bold text-slate-500">{{ totalCount - enabledCount }}</p>
            <p class="text-xs text-tertiary">{{ t('featureFlags.stats.disabled') }}</p>
          </div>
          <div class="rounded-lg border border-surface-dim bg-white p-4 text-center">
            <p class="text-2xl font-bold text-primary">{{ categories.length }}</p>
            <p class="text-xs text-tertiary">{{ t('featureFlags.stats.categories') }}</p>
          </div>
        </div>

        <!-- Success message -->
        <div
          v-if="successMessage"
          class="mb-6 rounded-lg border border-emerald-200 bg-emerald-50 p-4"
        >
          <div class="flex items-center gap-2">
            <i class="pi pi-check-circle text-emerald-600"></i>
            <p class="text-sm text-emerald-700">{{ t(successMessage) }}</p>
          </div>
        </div>

        <!-- Error message -->
        <div
          v-if="error"
          class="mb-6 rounded-lg border border-danger/20 bg-danger/5 p-4"
        >
          <div class="flex items-center gap-2">
            <i class="pi pi-exclamation-triangle text-danger"></i>
            <p class="text-sm text-danger">{{ error }}</p>
          </div>
        </div>

        <!-- Toolbar -->
        <div class="mb-6 rounded-lg border border-surface-dim bg-white p-4">
          <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <!-- Search -->
            <div class="relative flex-1">
              <i class="pi pi-search absolute start-3 top-1/2 -translate-y-1/2 text-xs text-tertiary"></i>
              <input
                v-model="searchQuery"
                type="text"
                :placeholder="t('featureFlags.searchPlaceholder')"
                class="w-full rounded-lg border border-surface-dim py-2 pe-3 ps-9 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
              />
            </div>

            <!-- Category filter -->
            <div class="flex items-center gap-2">
              <select
                v-model="selectedCategory"
                class="rounded-lg border border-surface-dim px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
              >
                <option :value="null">{{ t('featureFlags.allCategories') }}</option>
                <option v-for="cat in categories" :key="cat" :value="cat">
                  {{ getCategoryName(cat) }}
                </option>
              </select>
            </div>

            <!-- Bulk actions -->
            <div class="flex items-center gap-2">
              <button
                type="button"
                class="rounded-lg border border-surface-dim px-3 py-2 text-xs font-medium text-secondary hover:bg-surface-muted"
                @click="enableAll"
              >
                {{ t('featureFlags.actions.enableAll') }}
              </button>
              <button
                type="button"
                class="rounded-lg border border-surface-dim px-3 py-2 text-xs font-medium text-secondary hover:bg-surface-muted"
                @click="disableAll"
              >
                {{ t('featureFlags.actions.disableAll') }}
              </button>
              <button
                type="button"
                class="rounded-lg border border-surface-dim px-3 py-2 text-xs font-medium text-secondary hover:bg-surface-muted"
                @click="resetToDefaults"
              >
                {{ t('featureFlags.actions.resetDefaults') }}
              </button>
            </div>
          </div>

          <!-- Showing count -->
          <div class="mt-3 text-xs text-tertiary">
            {{ t('featureFlags.showing', { count: filteredCount, total: totalCount }) }}
          </div>
        </div>

        <!-- Unsaved changes bar -->
        <div
          v-if="hasUnsavedChanges"
          class="mb-6 flex items-center justify-between rounded-lg border border-amber-200 bg-amber-50 p-4"
        >
          <div class="flex items-center gap-2">
            <i class="pi pi-exclamation-circle text-amber-600"></i>
            <p class="text-sm font-medium text-amber-700">
              {{ t('featureFlags.unsavedChanges', { count: localChanges.size }) }}
            </p>
          </div>
          <div class="flex items-center gap-2">
            <button
              type="button"
              class="rounded-lg border border-amber-300 px-4 py-1.5 text-sm font-medium text-amber-700 hover:bg-amber-100"
              @click="discardChanges"
            >
              {{ t('featureFlags.actions.discard') }}
            </button>
            <button
              type="button"
              class="flex items-center gap-2 rounded-lg bg-primary px-4 py-1.5 text-sm font-bold text-white hover:bg-primary-dark disabled:opacity-50"
              :disabled="isSubmitting"
              @click="saveAllChanges"
            >
              <i v-if="isSubmitting" class="pi pi-spin pi-spinner text-xs"></i>
              {{ t('featureFlags.actions.saveChanges') }}
            </button>
          </div>
        </div>

        <!-- Feature flag categories -->
        <div class="space-y-4">
          <div
            v-for="(flags, category) in filteredFlagsByCategory"
            :key="category"
            class="overflow-hidden rounded-lg border border-surface-dim bg-white"
          >
            <!-- Category header -->
            <button
              type="button"
              class="flex w-full items-center justify-between px-6 py-4 text-start hover:bg-surface-muted/50"
              @click="toggleCategory(category as string)"
            >
              <div class="flex items-center gap-3">
                <div class="flex h-9 w-9 items-center justify-center rounded-lg bg-primary/10">
                  <i :class="getCategoryIcon(category as string)" class="text-sm text-primary"></i>
                </div>
                <div>
                  <h3 class="text-sm font-semibold text-secondary">
                    {{ getCategoryName(category as string) }}
                  </h3>
                  <p class="text-xs text-tertiary">
                    {{ getCategoryEnabledCount(flags) }} / {{ flags.length }}
                    {{ t('featureFlags.enabledOf') }}
                  </p>
                </div>
              </div>
              <div class="flex items-center gap-3">
                <!-- Mini progress bar -->
                <div class="hidden h-2 w-24 overflow-hidden rounded-full bg-slate-100 sm:block">
                  <div
                    class="h-full rounded-full bg-primary transition-all duration-300"
                    :style="{ width: `${(getCategoryEnabledCount(flags) / flags.length) * 100}%` }"
                  ></div>
                </div>
                <i
                  class="pi text-xs text-tertiary transition-transform duration-200"
                  :class="isCategoryExpanded(category as string) ? 'pi-chevron-up' : 'pi-chevron-down'"
                ></i>
              </div>
            </button>

            <!-- Category flags -->
            <div
              v-show="isCategoryExpanded(category as string)"
              class="border-t border-surface-dim"
            >
              <div
                v-for="flag in flags"
                :key="flag.featureKey"
                class="flex items-center justify-between border-b border-surface-dim/50 px-6 py-4 last:border-b-0"
                :class="{ 'bg-amber-50/30': localChanges.has(flag.featureKey) }"
              >
                <div class="flex-1 pe-4">
                  <div class="flex items-center gap-2">
                    <h4 class="text-sm font-medium text-secondary">
                      {{ getFeatureName(flag) }}
                    </h4>
                    <span
                      v-if="!flag.hasPersistedFlag"
                      class="rounded bg-slate-100 px-1.5 py-0.5 text-[10px] font-medium text-slate-500"
                    >
                      {{ t('featureFlags.labels.default') }}
                    </span>
                    <span
                      v-if="localChanges.has(flag.featureKey)"
                      class="rounded bg-amber-100 px-1.5 py-0.5 text-[10px] font-medium text-amber-600"
                    >
                      {{ t('featureFlags.labels.modified') }}
                    </span>
                  </div>
                  <p v-if="getFeatureDescription(flag)" class="mt-1 text-xs text-tertiary">
                    {{ getFeatureDescription(flag) }}
                  </p>
                  <p class="mt-1 font-mono text-[10px] text-slate-400" dir="ltr">
                    {{ flag.featureKey }}
                  </p>
                </div>

                <!-- Toggle switch -->
                <button
                  type="button"
                  class="relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-primary/20"
                  :class="getEffectiveState(flag) ? 'bg-primary' : 'bg-slate-300'"
                  role="switch"
                  :aria-checked="getEffectiveState(flag)"
                  @click="handleToggle(flag)"
                >
                  <span
                    class="pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200"
                    :class="getEffectiveState(flag) ? 'ltr:translate-x-5 rtl:-translate-x-5' : 'translate-x-0'"
                  ></span>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Empty state -->
        <div
          v-if="Object.keys(filteredFlagsByCategory).length === 0 && !isLoading"
          class="rounded-lg border border-surface-dim bg-white p-12 text-center"
        >
          <i class="pi pi-flag mb-3 text-4xl text-slate-300"></i>
          <p class="text-sm text-tertiary">
            {{ searchQuery ? t('featureFlags.noResults') : t('featureFlags.noFeatures') }}
          </p>
        </div>
      </template>
    </div>
  </div>
</template>
