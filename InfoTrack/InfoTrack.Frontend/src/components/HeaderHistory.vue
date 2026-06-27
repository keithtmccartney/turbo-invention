<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAppStore } from '../stores/app'
import { useAssistantStore } from '../stores/assistant'
import {
  buildQueryHistoryPanel,
  buildScrapeHistoryPanel,
} from '../utils/historyPanelTypes'
import {
  collapsibleHistoryPanelCount,
  showExpandAllHistory,
  toggleAllHistoryPanels,
} from '../utils/historyPanelRegistry'
import HistoryPanel from './HistoryPanel.vue'

const route = useRoute()
const store = useAppStore()
const assistant = useAssistantStore()

const panels = computed(() => {
  const result = []

  const scrapePanel = buildScrapeHistoryPanel(
    store.dashboard?.lastScrapedAt,
    store.dashboard?.scrapeHistory ?? [],
  )
  if (scrapePanel) result.push(scrapePanel)

  if (route.name === 'assistant') {
    const queryPanel = buildQueryHistoryPanel(
      assistant.lastQueryAt,
      assistant.queryHistory,
    )
    if (queryPanel) result.push(queryPanel)
  }

  return result
})

const toggleAllLabel = computed(() =>
  showExpandAllHistory.value ? 'Expand all' : 'Collapse all',
)

const showToggleAll = computed(() => collapsibleHistoryPanelCount.value > 1)
</script>

<template>
  <div v-if="panels.length" class="header-history">
    <div v-if="showToggleAll" class="header-history__controls">
      <button
        type="button"
        class="secondary header-history__toggle-all"
        @click="toggleAllHistoryPanels"
      >
        {{ toggleAllLabel }}
      </button>
    </div>

    <HistoryPanel
      v-for="panel in panels"
      :key="panel.kind"
      :panel="panel"
    />
  </div>
</template>
