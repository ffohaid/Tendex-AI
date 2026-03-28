/**
 * Feature Flag Management API Service.
 *
 * Provides typed HTTP methods for feature definition catalog
 * and per-tenant feature flag configuration operations.
 * All data is fetched dynamically from the backend — NO mock data.
 *
 * Endpoints mirror FeatureFlagEndpoints.cs.
 */
import { httpGet, httpPost, httpPut } from '@/services/http'
import type {
  FeatureDefinitionDto,
  TenantFeatureFlagDto,
  CreateFeatureDefinitionRequest,
  ToggleFeatureFlagRequest,
  BatchToggleFeatureFlagsRequest,
} from '@/types/featureFlag'

/* ------------------------------------------------------------------ */
/*  Feature Definitions (Global Catalog)                               */
/* ------------------------------------------------------------------ */

const DEFINITIONS_BASE_URL = '/v1/feature-definitions'

/**
 * Fetches all active feature definitions available on the platform.
 */
export async function fetchFeatureDefinitions(): Promise<FeatureDefinitionDto[]> {
  return httpGet<FeatureDefinitionDto[]>(DEFINITIONS_BASE_URL)
}

/**
 * Creates a new global feature definition.
 */
export async function createFeatureDefinition(
  request: CreateFeatureDefinitionRequest,
): Promise<FeatureDefinitionDto> {
  return httpPost<FeatureDefinitionDto>(DEFINITIONS_BASE_URL, request)
}

/* ------------------------------------------------------------------ */
/*  Tenant Feature Flags (Per-Tenant Configuration)                    */
/* ------------------------------------------------------------------ */

/**
 * Fetches all feature flags configured for a specific tenant.
 */
export async function fetchTenantFeatureFlags(
  tenantId: string,
): Promise<TenantFeatureFlagDto[]> {
  return httpGet<TenantFeatureFlagDto[]>(
    `/v1/tenants/${tenantId}/feature-flags`,
  )
}

/**
 * Toggles a single feature flag for a specific tenant.
 */
export async function toggleFeatureFlag(
  tenantId: string,
  featureKey: string,
  request: ToggleFeatureFlagRequest,
): Promise<TenantFeatureFlagDto> {
  return httpPut<TenantFeatureFlagDto>(
    `/v1/tenants/${tenantId}/feature-flags/${featureKey}`,
    request,
  )
}

/**
 * Batch-toggles multiple feature flags for a specific tenant.
 */
export async function batchToggleFeatureFlags(
  tenantId: string,
  request: BatchToggleFeatureFlagsRequest,
): Promise<TenantFeatureFlagDto[]> {
  return httpPut<TenantFeatureFlagDto[]>(
    `/v1/tenants/${tenantId}/feature-flags`,
    request,
  )
}
