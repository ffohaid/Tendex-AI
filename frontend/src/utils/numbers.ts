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
export function formatCurrency(amount: number): string {
  const formatted = new Intl.NumberFormat('en-SA', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount)

  return `${formatted} \uFDFC`
}
