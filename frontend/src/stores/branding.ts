/**
 * Tenant Branding Pinia Store.
 *
 * Manages the runtime branding state for the application.
 * Responsible for:
 * - Fetching tenant branding from the API
 * - Applying CSS custom properties to the document root
 * - Persisting branding in sessionStorage for fast restore
 * - Resetting to default platform branding
 */
import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import * as brandingService from '@/services/brandingService'
import * as organizationService from '@/services/organizationService'
import { DEFAULT_BRANDING, type BrandingState } from '@/types/branding'

const STORAGE_KEY = 'tenant_branding'

export const useBrandingStore = defineStore('branding', () => {
  /* ---------------------------------------------------------------- */
  /*  State                                                            */
  /* ---------------------------------------------------------------- */

  const tenantId = ref<string | null>(null)
  const nameAr = ref(DEFAULT_BRANDING.nameAr)
  const nameEn = ref(DEFAULT_BRANDING.nameEn)
  const logoUrl = ref<string | null>(DEFAULT_BRANDING.logoUrl)
  const primaryColor = ref(DEFAULT_BRANDING.primaryColor)
  const secondaryColor = ref(DEFAULT_BRANDING.secondaryColor)
  const isLoaded = ref(false)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  /* ---------------------------------------------------------------- */
  /*  Getters                                                          */
  /* ---------------------------------------------------------------- */

  const hasCustomBranding = computed(
    () =>
      tenantId.value !== null &&
      (logoUrl.value !== null ||
        primaryColor.value !== DEFAULT_BRANDING.primaryColor ||
        secondaryColor.value !== DEFAULT_BRANDING.secondaryColor),
  )

  const currentBranding = computed<BrandingState>(() => ({
    tenantId: tenantId.value,
    nameAr: nameAr.value,
    nameEn: nameEn.value,
    logoUrl: logoUrl.value,
    primaryColor: primaryColor.value,
    secondaryColor: secondaryColor.value,
    isLoaded: isLoaded.value,
  }))

  /** Active branding object for use in components (e.g., AppHeader). */
  const activeBranding = computed(() => ({
    tenantNameAr: nameAr.value !== DEFAULT_BRANDING.nameAr ? nameAr.value : null,
    tenantNameEn: nameEn.value !== DEFAULT_BRANDING.nameEn ? nameEn.value : null,
    logoUrl: logoUrl.value,
    primaryColor: primaryColor.value,
    secondaryColor: secondaryColor.value,
  }))

  /* ---------------------------------------------------------------- */
  /*  Actions                                                          */
  /* ---------------------------------------------------------------- */

  /**
   * Fetches branding from the API and applies it to the document.
   * Called after successful login or session restore.
   */
  async function loadAndApplyBranding(tid?: string): Promise<void> {
    isLoading.value = true
    error.value = null

    // Resolve tenant ID: explicit param > localStorage
    const resolvedTid = tid || localStorage.getItem('tenant_id') || ''
    if (!resolvedTid) {
      resetToDefaults()
      isLoading.value = false
      return
    }

    try {
      const hasAuthenticatedSession = !!localStorage.getItem('access_token')
      const branding = hasAuthenticatedSession
        ? await organizationService.fetchCurrentOrganizationBranding().catch(() =>
            brandingService.fetchTenantBranding(resolvedTid),
          )
        : await brandingService.fetchTenantBranding(resolvedTid)

      tenantId.value = branding.tenantId
      nameAr.value = branding.nameAr
      nameEn.value = branding.nameEn
      logoUrl.value = branding.logoUrl
      primaryColor.value = branding.primaryColor || DEFAULT_BRANDING.primaryColor
      secondaryColor.value =
        branding.secondaryColor || DEFAULT_BRANDING.secondaryColor
      isLoaded.value = true

      applyBrandingToDocument()
      persistBranding()
    } catch {
      error.value = 'تعذر تحميل إعدادات العلامة التجارية'
      // Apply defaults on error
      resetToDefaults()
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Restores branding from sessionStorage without an API call.
   * Used on page refresh to avoid flash of default branding.
   */
  function restoreFromStorage(): boolean {
    try {
      const stored = sessionStorage.getItem(STORAGE_KEY)
      if (!stored) return false

      const data = JSON.parse(stored) as BrandingState
      tenantId.value = data.tenantId
      nameAr.value = data.nameAr
      nameEn.value = data.nameEn
      logoUrl.value = data.logoUrl
      primaryColor.value = data.primaryColor
      secondaryColor.value = data.secondaryColor
      isLoaded.value = true

      applyBrandingToDocument()
      return true
    } catch {
      return false
    }
  }

  /**
   * Applies branding values directly (e.g., from a preview in the admin panel).
   */
  function applyPreview(
    previewPrimary: string,
    previewSecondary: string,
    previewLogoUrl: string | null,
  ): void {
    primaryColor.value = previewPrimary || DEFAULT_BRANDING.primaryColor
    secondaryColor.value = previewSecondary || DEFAULT_BRANDING.secondaryColor
    logoUrl.value = previewLogoUrl
    applyBrandingToDocument()
  }

  /**
   * Resets branding to platform defaults and clears storage.
   */
  function resetToDefaults(): void {
    tenantId.value = DEFAULT_BRANDING.tenantId
    nameAr.value = DEFAULT_BRANDING.nameAr
    nameEn.value = DEFAULT_BRANDING.nameEn
    logoUrl.value = DEFAULT_BRANDING.logoUrl
    primaryColor.value = DEFAULT_BRANDING.primaryColor
    secondaryColor.value = DEFAULT_BRANDING.secondaryColor
    isLoaded.value = false

    applyBrandingToDocument()
    sessionStorage.removeItem(STORAGE_KEY)
  }

  /* ---------------------------------------------------------------- */
  /*  Internal Helpers                                                 */
  /* ---------------------------------------------------------------- */

  /**
   * Applies the current branding colors as CSS custom properties
   * on the document root element. This enables Tailwind and all
   * components to use the tenant's brand colors immediately.
   */
  function applyBrandingToDocument(): void {
    const root = document.documentElement

    // Primary color and computed variants
    root.style.setProperty('--color-primary', primaryColor.value)
    root.style.setProperty(
      '--color-primary-light',
      lightenColor(primaryColor.value, 15),
    )
    root.style.setProperty(
      '--color-primary-dark',
      darkenColor(primaryColor.value, 15),
    )

    // Secondary color and computed variants
    root.style.setProperty('--color-secondary', secondaryColor.value)
    root.style.setProperty(
      '--color-secondary-light',
      lightenColor(secondaryColor.value, 15),
    )
    root.style.setProperty(
      '--color-secondary-dark',
      darkenColor(secondaryColor.value, 15),
    )

    // Success color matches primary for brand consistency
    root.style.setProperty('--color-success', primaryColor.value)
  }

  /**
   * Persists current branding state to sessionStorage.
   */
  function persistBranding(): void {
    const state: BrandingState = {
      tenantId: tenantId.value,
      nameAr: nameAr.value,
      nameEn: nameEn.value,
      logoUrl: logoUrl.value,
      primaryColor: primaryColor.value,
      secondaryColor: secondaryColor.value,
      isLoaded: true,
    }
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(state))
  }

  /**
   * Lightens a hex color by a given percentage.
   */
  function lightenColor(hex: string, percent: number): string {
    return adjustColor(hex, percent)
  }

  /**
   * Darkens a hex color by a given percentage.
   */
  function darkenColor(hex: string, percent: number): string {
    return adjustColor(hex, -percent)
  }

  /**
   * Adjusts a hex color brightness by a percentage.
   * Positive values lighten, negative values darken.
   */
  function adjustColor(hex: string, percent: number): string {
    const cleanHex = hex.replace('#', '')
    const num = parseInt(cleanHex, 16)

    let r = (num >> 16) & 0xff
    let g = (num >> 8) & 0xff
    let b = num & 0xff

    r = Math.min(255, Math.max(0, Math.round(r + (r * percent) / 100)))
    g = Math.min(255, Math.max(0, Math.round(g + (g * percent) / 100)))
    b = Math.min(255, Math.max(0, Math.round(b + (b * percent) / 100)))

    return `#${((1 << 24) + (r << 16) + (g << 8) + b).toString(16).slice(1).toUpperCase()}`
  }

  return {
    /* State */
    tenantId,
    nameAr,
    nameEn,
    logoUrl,
    primaryColor,
    secondaryColor,
    isLoaded,
    isLoading,
    error,

    /* Getters */
    hasCustomBranding,
    currentBranding,
    activeBranding,

    /* Actions */
    loadAndApplyBranding,
    restoreFromStorage,
    applyPreview,
    resetToDefaults,
    resetBranding: resetToDefaults,
  }
})
