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
    const resolvedPrimary = normalizeHex(primaryColor.value, DEFAULT_BRANDING.primaryColor)
    const resolvedSecondary = normalizeHex(secondaryColor.value, DEFAULT_BRANDING.secondaryColor)

    setPaletteProperties(root, 'primary', resolvedPrimary, {
      50: 92,
      100: 82,
      200: 64,
      300: 46,
      400: 24,
      500: 0,
      600: -10,
      700: -22,
      800: -34,
      900: -46,
    })

    setPaletteProperties(root, 'secondary', resolvedSecondary, {
      50: 92,
      100: 84,
      200: 70,
      300: 52,
      400: 28,
      500: 0,
      600: -10,
      700: -22,
      800: -34,
      900: -46,
    })

    root.style.setProperty('--color-success', resolvedPrimary)
    root.style.setProperty('--color-success-light', lightenColor(resolvedPrimary, 18))
    root.style.setProperty('--color-success-dark', darkenColor(resolvedPrimary, 18))
    root.style.setProperty('--color-success-50', lightenColor(resolvedPrimary, 90))
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

  function setPaletteProperties(
    root: HTMLElement,
    token: 'primary' | 'secondary',
    baseColor: string,
    shadeMap: Record<number, number>,
  ): void {
    root.style.setProperty(`--color-${token}`, baseColor)
    root.style.setProperty(`--color-${token}-light`, lightenColor(baseColor, 18))
    root.style.setProperty(`--color-${token}-dark`, darkenColor(baseColor, 18))

    Object.entries(shadeMap).forEach(([shade, amount]) => {
      const value = amount >= 0
        ? lightenColor(baseColor, amount)
        : darkenColor(baseColor, Math.abs(amount))
      root.style.setProperty(`--color-${token}-${shade}`, value)
    })
  }

  function normalizeHex(input: string | null | undefined, fallback: string): string {
    const candidate = (input || '').trim()
    return /^#?[0-9A-Fa-f]{6}$/.test(candidate)
      ? `#${candidate.replace('#', '').toUpperCase()}`
      : fallback
  }

  /**
   * Lightens a hex color by a given percentage.
   */
  function lightenColor(hex: string, percent: number): string {
    return mixColor(hex, '#FFFFFF', percent)
  }

  /**
   * Darkens a hex color by a given percentage.
   */
  function darkenColor(hex: string, percent: number): string {
    return mixColor(hex, '#000000', percent)
  }

  function mixColor(baseHex: string, targetHex: string, percent: number): string {
    const base = normalizeHex(baseHex, DEFAULT_BRANDING.primaryColor)
    const target = normalizeHex(targetHex, '#FFFFFF')
    const ratio = Math.min(100, Math.max(0, percent)) / 100

    const [r, g, b] = hexToRgb(base)
    const [tr, tg, tb] = hexToRgb(target)

    return rgbToHex(
      Math.round(r + (tr - r) * ratio),
      Math.round(g + (tg - g) * ratio),
      Math.round(b + (tb - b) * ratio),
    )
  }

  function hexToRgb(hex: string): [number, number, number] {
    const cleanHex = normalizeHex(hex, '#000000').replace('#', '')
    const value = parseInt(cleanHex, 16)
    return [
      (value >> 16) & 0xff,
      (value >> 8) & 0xff,
      value & 0xff,
    ]
  }

  function rgbToHex(r: number, g: number, b: number): string {
    return `#${[r, g, b]
      .map(channel => Math.min(255, Math.max(0, channel)).toString(16).padStart(2, '0'))
      .join('')
      .toUpperCase()}`
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
