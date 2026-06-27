import { ref } from 'vue'

export type PanelHandle = {
  setExpanded: (expanded: boolean) => void
  isExpanded: () => boolean
  focusSearch?: () => void
}

const panels: PanelHandle[] = []

export const panelCount = ref(0)
export const collapsiblePanelCount = ref(0)
export const showExpandAll = ref(false)

function syncCounts() {
  collapsiblePanelCount.value = panels.length
  panelCount.value = panels.filter(p => p.focusSearch).length
}

export function syncPanelGroupState(): void {
  const total = panels.length

  if (total === 0) {
    showExpandAll.value = false
    return
  }

  const expandedCount = panels.filter(p => p.isExpanded()).length
  const collapsedCount = total - expandedCount

  if (expandedCount === total) {
    showExpandAll.value = false
    return
  }

  if (collapsedCount === total) {
    showExpandAll.value = true
    return
  }

  const median = total / 2

  if (expandedCount > median) {
    showExpandAll.value = true
  } else if (collapsedCount > median) {
    showExpandAll.value = false
  } else {
    showExpandAll.value = expandedCount >= collapsedCount
  }
}

export function registerPanel(handle: PanelHandle): { searchIndex: number; unregister: () => void } {
  panels.push(handle)
  syncCounts()
  syncPanelGroupState()

  const searchIndex = handle.focusSearch ? panels.filter(p => p.focusSearch).length : 0

  return {
    searchIndex,
    unregister: () => {
      const slot = panels.indexOf(handle)
      if (slot >= 0) panels.splice(slot, 1)
      syncCounts()
      syncPanelGroupState()
    },
  }
}

export function clearPanelRegistry(): void {
  panels.length = 0
  syncCounts()
  syncPanelGroupState()
}

export function getPanelCount(): number {
  return panelCount.value
}

export function focusPanel(index: number): void {
  const searchable = panels.filter(p => p.focusSearch)
  searchable[index - 1]?.focusSearch?.()
}

export function expandAllPanels(): void {
  panels.forEach(p => p.setExpanded(true))
  syncPanelGroupState()
}

export function collapseAllPanels(): void {
  panels.forEach(p => p.setExpanded(false))
  syncPanelGroupState()
}

export function toggleAllPanels(): void {
  if (showExpandAll.value) expandAllPanels()
  else collapseAllPanels()
}
