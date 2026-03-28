/**
 * Mapping of Arabic-Indic numerals to Western Arabic (English) numerals.
 */
const ARABIC_INDIC_MAP: Record<string, string> = {
  '\u0660': '0',
  '\u0661': '1',
  '\u0662': '2',
  '\u0663': '3',
  '\u0664': '4',
  '\u0665': '5',
  '\u0666': '6',
  '\u0667': '7',
  '\u0668': '8',
  '\u0669': '9',
}

/**
 * Converts any Arabic-Indic numerals in the input string
 * to Western Arabic (English) numerals.
 *
 * Per project standards, all displayed numbers must use
 * English numerals (0-9) exclusively.
 */
export function toEnglishNumerals(value: string): string {
  return value.replace(/[\u0660-\u0669]/g, (char) => ARABIC_INDIC_MAP[char] ?? char)
}

/**
 * Formats a number as currency using the Saudi Riyal symbol.
 * Always uses English numerals.
 */
export function formatCurrency(
  amount: number | null | undefined,
  decimals = 2,
): string {
  if (amount === null || amount === undefined) return '0.00 \uFDFC'

  const formatted = new Intl.NumberFormat('en-SA', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(amount)

  return `${formatted} \uFDFC`
}

/**
 * Formats a number with thousand separators.
 * Always uses English numerals.
 */
export function formatNumber(
  value: number | null | undefined,
  decimals = 0,
): string {
  if (value === null || value === undefined) return '0'

  return new Intl.NumberFormat('en-SA', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(value)
}

/**
 * Formats a percentage value.
 * Always uses English numerals.
 */
export function formatPercentage(
  value: number | null | undefined,
  decimals = 1,
): string {
  if (value === null || value === undefined) return '0%'
  return `${value.toFixed(decimals)}%`
}
