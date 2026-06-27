import { computed, ref } from 'vue'

export type SortDirection = 'asc' | 'desc'

export function compareSortValues(a: unknown, b: unknown): number {
  const aEmpty = a == null || a === ''
  const bEmpty = b == null || b === ''
  if (aEmpty && bEmpty) return 0
  if (aEmpty) return 1
  if (bEmpty) return -1

  if (typeof a === 'number' && typeof b === 'number') {
    return a - b
  }

  return String(a).localeCompare(String(b), undefined, { numeric: true, sensitivity: 'base' })
}

export function useTableSort<K extends string, T extends Record<K, unknown>>(
  defaultKey: K,
  getRows: () => readonly T[],
) {
  const sortKey = ref(defaultKey) as { value: K }
  const sortDir = ref<SortDirection>('asc')

  const sortedRows = computed(() => {
    const key = sortKey.value
    const direction = sortDir.value
    const rows = getRows()

    return [...rows].sort((a, b) => {
      const cmp = compareSortValues(a[key], b[key])
      return direction === 'asc' ? cmp : -cmp
    })
  })

  const bodyKey = computed(() => `${String(sortKey.value)}:${sortDir.value}`)

  function toggleSort(key: K) {
    if (sortKey.value === key) {
      sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
    } else {
      sortKey.value = key
      sortDir.value = 'asc'
    }
  }

  function ariaSort(key: K): 'none' | 'ascending' | 'descending' {
    if (sortKey.value !== key) return 'none'
    return sortDir.value === 'asc' ? 'ascending' : 'descending'
  }

  return { sortKey, sortDir, sortedRows, bodyKey, toggleSort, ariaSort }
}
