import { ref } from 'vue'

export type HistoryPanelHandle = {
  setExpanded: (expanded: boolean) => void
  isExpanded: () => boolean
  hasEntries: () => boolean
}

const panels: HistoryPanelHandle[] = []

export const historyPanelCount = ref(0)
export const collapsibleHistoryPanelCount = ref(0)
export const showExpandAllHistory = ref(false)

function syncCounts() {
  collapsibleHistoryPanelCount.value = panels.filter(p => p.hasEntries()).length
  historyPanelCount.value = panels.length
}

export function syncHistoryPanelGroupState(): void {
  const collapsible = panels.filter(p => p.hasEntries())

  if (collapsible.length === 0) {
    showExpandAllHistory.value = false
    return
  }

  const expandedCount = collapsible.filter(p => p.isExpanded()).length
  const collapsedCount = collapsible.length - expandedCount

  if (expandedCount === collapsible.length) {
    showExpandAllHistory.value = false
    return
  }

  if (collapsedCount === collapsible.length) {
    showExpandAllHistory.value = true
    return
  }

  const median = collapsible.length / 2

  if (expandedCount > median) {
    showExpandAllHistory.value = true
  } else if (collapsedCount > median) {
    showExpandAllHistory.value = false
  } else {
    showExpandAllHistory.value = expandedCount >= collapsedCount
  }
}

export function registerHistoryPanel(handle: HistoryPanelHandle): () => void {
  panels.push(handle)
  syncCounts()
  syncHistoryPanelGroupState()

  return () => {
    const slot = panels.indexOf(handle)
    if (slot >= 0) panels.splice(slot, 1)
    syncCounts()
    syncHistoryPanelGroupState()
  }
}

export function expandAllHistoryPanels(): void {
  panels.filter(p => p.hasEntries()).forEach(p => p.setExpanded(true))
  syncHistoryPanelGroupState()
}

export function collapseAllHistoryPanels(): void {
  panels.filter(p => p.hasEntries()).forEach(p => p.setExpanded(false))
  syncHistoryPanelGroupState()
}

export function toggleAllHistoryPanels(): void {
  if (showExpandAllHistory.value) expandAllHistoryPanels()
  else collapseAllHistoryPanels()
}
