/**
 * Composable for consistent formatting across the dashboard.
 *
 * Ensures:
 * - English numerals (1, 2, 3) exclusively per PRD requirement
 * - SAR currency symbol (﷼) for financial values
 * - Consistent date formatting
 * - Relative time display for recent activities
 */
import { useI18n } from 'vue-i18n'
import { computed } from 'vue'

export function useFormatters() {
  const { locale } = useI18n()

  const isArabic = computed(() => locale.value === 'ar')

  /**
   * Formats a number with English numerals and locale-aware separators.
   * Always uses English (Western) numerals per PRD requirement.
   */
  function formatNumber(value: number, decimals = 0): string {
    return new Intl.NumberFormat('en-US', {
      minimumFractionDigits: decimals,
      maximumFractionDigits: decimals,
    }).format(value)
  }

  /**
   * Formats a monetary value with SAR symbol.
   * Uses the new Saudi Riyal symbol (﷼).
   */
  function formatCurrency(value: number): string {
    const formatted = formatNumber(value, 2)
    return isArabic.value ? `${formatted} ﷼` : `SAR ${formatted}`
  }

  /**
   * Formats a percentage value.
   */
  function formatPercentage(value: number, decimals = 1): string {
    return `${formatNumber(value, decimals)}%`
  }

  /**
   * Formats a date string to a localized display format.
   * Always uses English numerals.
   */
  function formatDate(dateStr: string | null): string {
    if (!dateStr) return '—'

    const date = new Date(dateStr)
    if (isNaN(date.getTime())) return '—'

    return new Intl.DateTimeFormat('en-GB', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    }).format(date)
  }

  /**
   * Formats a date with time.
   */
  function formatDateTime(dateStr: string | null): string {
    if (!dateStr) return '—'

    const date = new Date(dateStr)
    if (isNaN(date.getTime())) return '—'

    return new Intl.DateTimeFormat('en-GB', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
    }).format(date)
  }

  /**
   * Returns a relative time string (e.g., "5 minutes ago").
   */
  function formatRelativeTime(dateStr: string): string {
    const date = new Date(dateStr)
    const now = new Date()
    const diffMs = now.getTime() - date.getTime()
    const diffMinutes = Math.floor(diffMs / 60_000)
    const diffHours = Math.floor(diffMs / 3_600_000)
    const diffDays = Math.floor(diffMs / 86_400_000)

    if (diffMinutes < 1) {
      return isArabic.value ? 'الآن' : 'Just now'
    }
    if (diffMinutes < 60) {
      return isArabic.value
        ? `منذ ${formatNumber(diffMinutes)} دقيقة`
        : `${formatNumber(diffMinutes)} min ago`
    }
    if (diffHours < 24) {
      return isArabic.value
        ? `منذ ${formatNumber(diffHours)} ساعة`
        : `${formatNumber(diffHours)} hr ago`
    }
    if (diffDays < 7) {
      return isArabic.value
        ? `منذ ${formatNumber(diffDays)} يوم`
        : `${formatNumber(diffDays)} days ago`
    }

    return formatDate(dateStr)
  }

  /**
   * Formats remaining time from seconds to human-readable string.
   * Used for SLA countdown timers.
   */
  function formatRemainingTime(totalSeconds: number): string {
    if (totalSeconds <= 0) {
      return isArabic.value ? 'منتهي' : 'Expired'
    }

    const days = Math.floor(totalSeconds / 86_400)
    const hours = Math.floor((totalSeconds % 86_400) / 3_600)
    const minutes = Math.floor((totalSeconds % 3_600) / 60)

    if (days > 0) {
      return isArabic.value
        ? `${formatNumber(days)} يوم ${formatNumber(hours)} ساعة`
        : `${formatNumber(days)}d ${formatNumber(hours)}h`
    }
    if (hours > 0) {
      return isArabic.value
        ? `${formatNumber(hours)} ساعة ${formatNumber(minutes)} دقيقة`
        : `${formatNumber(hours)}h ${formatNumber(minutes)}m`
    }
    return isArabic.value
      ? `${formatNumber(minutes)} دقيقة`
      : `${formatNumber(minutes)}m`
  }

  return {
    isArabic,
    formatNumber,
    formatCurrency,
    formatPercentage,
    formatDate,
    formatDateTime,
    formatRelativeTime,
    formatRemainingTime,
  }
}
