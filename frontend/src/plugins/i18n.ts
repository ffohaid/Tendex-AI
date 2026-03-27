import { createI18n } from 'vue-i18n'
import ar from '@/locales/ar.json'
import en from '@/locales/en.json'

export type SupportedLocale = 'ar' | 'en'

export const DEFAULT_LOCALE: SupportedLocale = 'ar'

export const SUPPORTED_LOCALES: SupportedLocale[] = ['ar', 'en']

/**
 * Returns the text direction for a given locale.
 */
export function getLocaleDirection(locale: SupportedLocale): 'rtl' | 'ltr' {
  return locale === 'ar' ? 'rtl' : 'ltr'
}

/**
 * Updates the document's `dir` and `lang` attributes
 * based on the active locale.
 */
export function applyLocaleDirection(locale: SupportedLocale): void {
  const dir = getLocaleDirection(locale)
  document.documentElement.setAttribute('dir', dir)
  document.documentElement.setAttribute('lang', locale)
}

const i18n = createI18n({
  legacy: false,
  locale: DEFAULT_LOCALE,
  fallbackLocale: 'en',
  messages: {
    ar,
    en,
  },
})

export default i18n
