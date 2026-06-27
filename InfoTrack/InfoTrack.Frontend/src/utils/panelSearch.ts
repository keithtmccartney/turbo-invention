export function matchesSearch(value: unknown, query: string): boolean {
  const trimmed = query.trim()
  if (!trimmed) return true
  const needle = trimmed.toLowerCase()

  if (value == null) return false

  if (typeof value === 'object') {
    return Object.values(value as Record<string, unknown>).some(entry => matchesSearch(entry, query))
  }

  return String(value).toLowerCase().includes(needle)
}

export function filterBySearch<T>(rows: readonly T[], query: string): T[] {
  if (!query.trim()) return [...rows]
  return rows.filter(row => matchesSearch(row, query))
}
