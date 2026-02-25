/**
 * Parse decimal string with Icelandic convention (comma as decimal separator).
 * Accepts both "0,1" and "0.1". Returns null if empty or invalid.
 */
export function parseDecimal(value: string | number | null | undefined): number | null {
  if (value === '' || value === null || value === undefined) return null
  if (typeof value === 'number') return Number.isNaN(value) ? null : value
  const s = String(value).trim().replace(',', '.')
  if (s === '') return null
  const n = parseFloat(s)
  return Number.isNaN(n) ? null : n
}

/**
 * Format number for display in an input using Icelandic decimal separator (comma).
 */
export function formatDecimalForInput(value: number | null | undefined): string {
  if (value === null || value === undefined) return ''
  const s = String(value)
  return s.replace('.', ',')
}

export function useDecimalInput() {
  return { parseDecimal, formatDecimalForInput }
}
