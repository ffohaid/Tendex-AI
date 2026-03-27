import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { SupportedLocale } from '@/plugins/i18n'
import { DEFAULT_LOCALE, applyLocaleDirection } from '@/plugins/i18n'

/**
 * Global application store.
 * Manages locale, direction, and sidebar state.
 */
export const useAppStore = defineStore('app', () => {
  const locale = ref<SupportedLocale>(DEFAULT_LOCALE)
  const sidebarCollapsed = ref(false)

  const direction = computed(() => (locale.value === 'ar' ? 'rtl' : 'ltr'))
  const isRtl = computed(() => direction.value === 'rtl')

  /**
   * Switches the active locale and updates the document direction.
   */
  function setLocale(newLocale: SupportedLocale): void {
    locale.value = newLocale
    applyLocaleDirection(newLocale)
  }

  function toggleSidebar(): void {
    sidebarCollapsed.value = !sidebarCollapsed.value
  }

  return {
    locale,
    sidebarCollapsed,
    direction,
    isRtl,
    setLocale,
    toggleSidebar,
  }
})
