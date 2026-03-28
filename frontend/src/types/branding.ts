/**
 * TypeScript type definitions for Tenant Branding.
 *
 * These types support the dynamic branding system that applies
 * tenant-specific visual identity (logo, colors) at runtime.
 */

/* ------------------------------------------------------------------ */
/*  Branding DTOs                                                      */
/* ------------------------------------------------------------------ */

/** Lightweight branding configuration fetched from the API. */
export interface TenantBrandingDto {
  tenantId: string
  nameAr: string
  nameEn: string
  logoUrl: string | null
  primaryColor: string | null
  secondaryColor: string | null
}

/* ------------------------------------------------------------------ */
/*  Branding State                                                     */
/* ------------------------------------------------------------------ */

/** Runtime branding state applied to the application shell. */
export interface BrandingState {
  tenantId: string | null
  nameAr: string
  nameEn: string
  logoUrl: string | null
  primaryColor: string
  secondaryColor: string
  isLoaded: boolean
}

/* ------------------------------------------------------------------ */
/*  Default Branding Constants                                         */
/* ------------------------------------------------------------------ */

/** Default platform branding values (Tendex AI brand identity). */
export const DEFAULT_BRANDING: Omit<BrandingState, 'isLoaded'> = {
  tenantId: null,
  nameAr: 'Tendex AI',
  nameEn: 'Tendex AI',
  logoUrl: null,
  primaryColor: '#2BB673',
  secondaryColor: '#0D2745',
}
