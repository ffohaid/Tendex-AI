/**
 * TypeScript type definitions for Feature Flag Management.
 *
 * These types mirror the backend DTOs from
 * TendexAI.Application.Features.FeatureFlags.Dtos.
 */

/* ------------------------------------------------------------------ */
/*  Feature Definition (Global Catalog)                                */
/* ------------------------------------------------------------------ */

/** Global feature definition available on the platform. */
export interface FeatureDefinitionDto {
  id: string
  featureKey: string
  nameAr: string
  nameEn: string
  descriptionAr: string | null
  descriptionEn: string | null
  category: string
  isEnabledByDefault: boolean
  isActive: boolean
  createdAt: string
  lastModifiedAt: string | null
}

/* ------------------------------------------------------------------ */
/*  Tenant Feature Flag (Per-Tenant Configuration)                     */
/* ------------------------------------------------------------------ */

/** Feature flag configuration for a specific tenant. */
export interface TenantFeatureFlagDto {
  id: string
  tenantId: string
  featureKey: string
  featureNameAr: string
  featureNameEn: string
  isEnabled: boolean
  configuration: string | null
  createdAt: string
  lastModifiedAt: string | null
}

/* ------------------------------------------------------------------ */
/*  Request DTOs                                                       */
/* ------------------------------------------------------------------ */

/** Request to toggle a single feature flag. */
export interface ToggleFeatureFlagRequest {
  featureKey: string
  isEnabled: boolean
  configuration?: string | null
}

/** Request to create a new feature definition. */
export interface CreateFeatureDefinitionRequest {
  featureKey: string
  nameAr: string
  nameEn: string
  descriptionAr?: string | null
  descriptionEn?: string | null
  category: string
  isEnabledByDefault: boolean
}

/** Request to batch-toggle multiple feature flags. */
export interface BatchToggleFeatureFlagsRequest {
  flags: FeatureFlagToggleItem[]
}

/** Individual flag toggle item within a batch request. */
export interface FeatureFlagToggleItem {
  featureKey: string
  isEnabled: boolean
  configuration?: string | null
}

/* ------------------------------------------------------------------ */
/*  Merged View Model (for UI rendering)                               */
/* ------------------------------------------------------------------ */

/**
 * Merged feature flag view combining global definition with tenant-specific state.
 * Used by the UI to display a complete feature flag management panel.
 */
export interface MergedFeatureFlag {
  /** Feature definition ID. */
  definitionId: string
  /** Unique feature key. */
  featureKey: string
  /** Arabic display name. */
  nameAr: string
  /** English display name. */
  nameEn: string
  /** Arabic description. */
  descriptionAr: string | null
  /** English description. */
  descriptionEn: string | null
  /** Category grouping (e.g., "AI", "Workflow", "Analytics"). */
  category: string
  /** Whether enabled by default for new tenants. */
  isEnabledByDefault: boolean
  /** Whether currently enabled for this specific tenant. */
  isEnabled: boolean
  /** Optional JSON configuration for the feature. */
  configuration: string | null
  /** Whether the tenant has a persisted flag record for this feature. */
  hasPersistedFlag: boolean
}

/* ------------------------------------------------------------------ */
/*  Feature Category Constants                                         */
/* ------------------------------------------------------------------ */

/** Known feature categories for grouping in the UI. */
export const FEATURE_CATEGORIES = [
  'AI',
  'Workflow',
  'Analytics',
  'Security',
  'Integration',
  'Communication',
  'Documents',
] as const

export type FeatureCategory = (typeof FEATURE_CATEGORIES)[number]
