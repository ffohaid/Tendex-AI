/**
 * Feature Flag Management Pinia Store.
 *
 * Manages state for the Super Admin feature flag management module including:
 * - Global feature definitions catalog
 * - Per-tenant feature flag configuration
 * - Merged view combining definitions with tenant-specific state
 * - Batch toggle operations
 */
import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import * as featureFlagService from '@/services/featureFlagService'
import type {
  FeatureDefinitionDto,
  TenantFeatureFlagDto,
  MergedFeatureFlag,
  BatchToggleFeatureFlagsRequest,
  CreateFeatureDefinitionRequest,
} from '@/types/featureFlag'

export const useFeatureFlagStore = defineStore('featureFlag', () => {
  /* ---------------------------------------------------------------- */
  /*  State                                                            */
  /* ---------------------------------------------------------------- */

  /** Global feature definitions catalog. */
  const definitions = ref<FeatureDefinitionDto[]>([])

  /** Per-tenant feature flags. */
  const tenantFlags = ref<TenantFeatureFlagDto[]>([])

  /** Currently selected tenant ID for flag management. */
  const currentTenantId = ref<string | null>(null)

  /** UI state. */
  const isLoading = ref(false)
  const isSubmitting = ref(false)
  const error = ref<string | null>(null)
  const successMessage = ref<string | null>(null)

  /* ---------------------------------------------------------------- */
  /*  Getters                                                          */
  /* ---------------------------------------------------------------- */

  /**
   * Merged view combining global definitions with tenant-specific flag state.
   * For each definition, checks if the tenant has a persisted flag record.
   * If not, uses the definition's default enabled state.
   */
  const mergedFlags = computed<MergedFeatureFlag[]>(() => {
    return definitions.value
      .filter((d) => d.isActive)
      .map((def) => {
        const tenantFlag = tenantFlags.value.find(
          (f) => f.featureKey === def.featureKey,
        )

        return {
          definitionId: def.id,
          featureKey: def.featureKey,
          nameAr: def.nameAr,
          nameEn: def.nameEn,
          descriptionAr: def.descriptionAr,
          descriptionEn: def.descriptionEn,
          category: def.category,
          isEnabledByDefault: def.isEnabledByDefault,
          isEnabled: tenantFlag ? tenantFlag.isEnabled : def.isEnabledByDefault,
          configuration: tenantFlag?.configuration ?? null,
          hasPersistedFlag: !!tenantFlag,
        }
      })
  })

  /** Feature flags grouped by category. */
  const flagsByCategory = computed(() => {
    const groups: Record<string, MergedFeatureFlag[]> = {}
    for (const flag of mergedFlags.value) {
      if (!groups[flag.category]) {
        groups[flag.category] = []
      }
      groups[flag.category].push(flag)
    }
    return groups
  })

  /** Available categories derived from definitions. */
  const categories = computed(() => {
    return [...new Set(definitions.value.map((d) => d.category))].sort()
  })

  /** Count of enabled features for the current tenant. */
  const enabledCount = computed(
    () => mergedFlags.value.filter((f) => f.isEnabled).length,
  )

  /** Total features count. */
  const totalCount = computed(() => mergedFlags.value.length)

  /* ---------------------------------------------------------------- */
  /*  Actions                                                          */
  /* ---------------------------------------------------------------- */

  /** Load all global feature definitions. */
  async function loadDefinitions(): Promise<void> {
    try {
      definitions.value = await featureFlagService.fetchFeatureDefinitions()
    } catch (err) {
      error.value = extractError(err)
    }
  }

  /** Load feature flags for a specific tenant. */
  async function loadTenantFlags(tenantId: string): Promise<void> {
    isLoading.value = true
    error.value = null
    currentTenantId.value = tenantId

    try {
      // Load both definitions and tenant flags in parallel
      const [defs, flags] = await Promise.all([
        featureFlagService.fetchFeatureDefinitions(),
        featureFlagService.fetchTenantFeatureFlags(tenantId),
      ])

      definitions.value = defs
      tenantFlags.value = flags
    } catch (err) {
      error.value = extractError(err)
      tenantFlags.value = []
    } finally {
      isLoading.value = false
    }
  }

  /** Toggle a single feature flag for the current tenant. */
  async function toggleFlag(
    featureKey: string,
    isEnabled: boolean,
    configuration?: string | null,
  ): Promise<boolean> {
    if (!currentTenantId.value) return false

    isSubmitting.value = true
    error.value = null

    try {
      const result = await featureFlagService.toggleFeatureFlag(
        currentTenantId.value,
        featureKey,
        { featureKey, isEnabled, configuration },
      )

      // Update local state
      const idx = tenantFlags.value.findIndex(
        (f) => f.featureKey === featureKey,
      )
      if (idx >= 0) {
        tenantFlags.value[idx] = result
      } else {
        tenantFlags.value.push(result)
      }

      successMessage.value = 'featureFlags.messages.toggleSuccess'
      return true
    } catch (err) {
      error.value = extractError(err)
      return false
    } finally {
      isSubmitting.value = false
    }
  }

  /** Batch-toggle multiple feature flags for the current tenant. */
  async function batchToggle(
    request: BatchToggleFeatureFlagsRequest,
  ): Promise<boolean> {
    if (!currentTenantId.value) return false

    isSubmitting.value = true
    error.value = null

    try {
      const results = await featureFlagService.batchToggleFeatureFlags(
        currentTenantId.value,
        request,
      )

      // Update local state with results
      for (const result of results) {
        const idx = tenantFlags.value.findIndex(
          (f) => f.featureKey === result.featureKey,
        )
        if (idx >= 0) {
          tenantFlags.value[idx] = result
        } else {
          tenantFlags.value.push(result)
        }
      }

      successMessage.value = 'featureFlags.messages.batchSuccess'
      return true
    } catch (err) {
      error.value = extractError(err)
      return false
    } finally {
      isSubmitting.value = false
    }
  }

  /** Create a new feature definition. */
  async function createDefinition(
    request: CreateFeatureDefinitionRequest,
  ): Promise<boolean> {
    isSubmitting.value = true
    error.value = null

    try {
      const def = await featureFlagService.createFeatureDefinition(request)
      definitions.value.push(def)
      successMessage.value = 'featureFlags.messages.definitionCreated'
      return true
    } catch (err) {
      error.value = extractError(err)
      return false
    } finally {
      isSubmitting.value = false
    }
  }

  /** Clear messages. */
  function clearMessages(): void {
    error.value = null
    successMessage.value = null
  }

  /** Reset store state. */
  function reset(): void {
    definitions.value = []
    tenantFlags.value = []
    currentTenantId.value = null
    error.value = null
    successMessage.value = null
  }

  /* ---------------------------------------------------------------- */
  /*  Helpers                                                          */
  /* ---------------------------------------------------------------- */

  function extractError(err: unknown): string {
    if (err && typeof err === 'object' && 'response' in err) {
      const axiosErr = err as {
        response?: { data?: { error?: string; detail?: string } }
      }
      return (
        axiosErr.response?.data?.error ||
        axiosErr.response?.data?.detail ||
        'featureFlags.errors.unexpected'
      )
    }
    return 'featureFlags.errors.unexpected'
  }

  return {
    /* State */
    definitions,
    tenantFlags,
    currentTenantId,
    isLoading,
    isSubmitting,
    error,
    successMessage,

    /* Getters */
    mergedFlags,
    flagsByCategory,
    categories,
    enabledCount,
    totalCount,

    /* Actions */
    loadDefinitions,
    loadTenantFlags,
    toggleFlag,
    batchToggle,
    createDefinition,
    clearMessages,
    reset,
  }
})
