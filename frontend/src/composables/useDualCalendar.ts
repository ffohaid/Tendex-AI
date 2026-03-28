/**
 * Composable for dual calendar support (Hijri / Gregorian).
 * Uses the Umm al-Qura calendar via Intl.DateTimeFormat.
 * All dates are displayed in English numerals per project standards.
 */
import { computed, ref, type Ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { DualDate } from '@/types/evaluation'

/**
 * Hijri month names in Arabic.
 */
const HIJRI_MONTHS_AR: Record<number, string> = {
  1: 'محرم',
  2: 'صفر',
  3: 'ربيع الأول',
  4: 'ربيع الثاني',
  5: 'جمادى الأولى',
  6: 'جمادى الآخرة',
  7: 'رجب',
  8: 'شعبان',
  9: 'رمضان',
  10: 'شوال',
  11: 'ذو القعدة',
  12: 'ذو الحجة',
}

const HIJRI_MONTHS_EN: Record<number, string> = {
  1: 'Muharram',
  2: 'Safar',
  3: 'Rabi al-Awwal',
  4: 'Rabi al-Thani',
  5: 'Jumada al-Ula',
  6: 'Jumada al-Akhirah',
  7: 'Rajab',
  8: 'Shaban',
  9: 'Ramadan',
  10: 'Shawwal',
  11: 'Dhul Qadah',
  12: 'Dhul Hijjah',
}

/**
 * Format a Date object to Hijri date string using Intl API.
 */
function toHijriDate(date: Date, locale: string): string {
  try {
    const formatter = new Intl.DateTimeFormat(`${locale}-u-ca-islamic-umalqura-nu-latn`, {
      day: 'numeric',
      month: 'numeric',
      year: 'numeric',
    })
    const parts = formatter.formatToParts(date)
    const day = parts.find(p => p.type === 'day')?.value ?? ''
    const month = parts.find(p => p.type === 'month')?.value ?? ''
    const year = parts.find(p => p.type === 'year')?.value ?? ''

    const monthNum = parseInt(month, 10)
    const monthNames = locale === 'ar' ? HIJRI_MONTHS_AR : HIJRI_MONTHS_EN
    const monthName = monthNames[monthNum] ?? month

    return `${day} ${monthName} ${year}`
  } catch {
    return ''
  }
}

/**
 * Format a Date object to Gregorian date string.
 */
function toGregorianDate(date: Date, locale: string): string {
  try {
    const formatter = new Intl.DateTimeFormat(`${locale}-u-nu-latn`, {
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    })
    return formatter.format(date)
  } catch {
    return ''
  }
}

/**
 * Format a Date object to short Gregorian date string.
 */
function toGregorianShort(date: Date): string {
  try {
    const formatter = new Intl.DateTimeFormat('en-u-nu-latn', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    })
    return formatter.format(date)
  } catch {
    return ''
  }
}

/**
 * Format a Date object to short Hijri date string.
 */
function toHijriShort(date: Date): string {
  try {
    const formatter = new Intl.DateTimeFormat('en-u-ca-islamic-umalqura-nu-latn', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    })
    return formatter.format(date)
  } catch {
    return ''
  }
}

/**
 * Composable providing dual calendar formatting utilities.
 */
export function useDualCalendar() {
  const { locale } = useI18n()
  const showHijri: Ref<boolean> = ref(true)

  /**
   * Format a date string or Date object into dual calendar display.
   */
  function formatDualDate(dateInput: string | Date): DualDate {
    const date = typeof dateInput === 'string' ? new Date(dateInput) : dateInput
    if (isNaN(date.getTime())) {
      return { gregorian: '', hijri: '' }
    }

    return {
      gregorian: toGregorianDate(date, locale.value),
      hijri: toHijriDate(date, locale.value),
    }
  }

  /**
   * Format a date for compact display (both calendars on one line).
   */
  function formatCompactDual(dateInput: string | Date): string {
    const date = typeof dateInput === 'string' ? new Date(dateInput) : dateInput
    if (isNaN(date.getTime())) return ''

    const g = toGregorianShort(date)
    const h = toHijriShort(date)
    return `${g} | ${h}`
  }

  /**
   * Get the primary date based on user preference.
   */
  function formatPrimaryDate(dateInput: string | Date): string {
    const dual = formatDualDate(dateInput)
    return showHijri.value ? dual.hijri : dual.gregorian
  }

  /**
   * Get the secondary date based on user preference.
   */
  function formatSecondaryDate(dateInput: string | Date): string {
    const dual = formatDualDate(dateInput)
    return showHijri.value ? dual.gregorian : dual.hijri
  }

  /**
   * Toggle between Hijri and Gregorian as primary display.
   */
  function toggleCalendar() {
    showHijri.value = !showHijri.value
  }

  const calendarLabel = computed(() =>
    showHijri.value
      ? locale.value === 'ar' ? 'هجري' : 'Hijri'
      : locale.value === 'ar' ? 'ميلادي' : 'Gregorian'
  )

  return {
    showHijri,
    formatDualDate,
    formatCompactDual,
    formatPrimaryDate,
    formatSecondaryDate,
    toggleCalendar,
    calendarLabel,
  }
}
