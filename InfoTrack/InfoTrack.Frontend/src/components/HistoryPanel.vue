<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, useId } from 'vue'
import {
  formatHistoryPanelSummary,
  type HistoryPanelModel,
} from '../utils/historyPanelTypes'
import {
  registerHistoryPanel,
  syncHistoryPanelGroupState,
} from '../utils/historyPanelRegistry'

const props = defineProps<{
  panel: HistoryPanelModel
}>()

const expanded = ref(false)
const historyId = useId()

const summary = computed(() => formatHistoryPanelSummary(props.panel))
const hasEntries = computed(() => props.panel.count > 0)

let unregisterPanel: (() => void) | null = null

function toggle() {
  if (!hasEntries.value) return
  expanded.value = !expanded.value
  syncHistoryPanelGroupState()
}

onMounted(() => {
  unregisterPanel = registerHistoryPanel({
    setExpanded: value => {
      expanded.value = value
      syncHistoryPanelGroupState()
    },
    isExpanded: () => expanded.value,
    hasEntries: () => hasEntries.value,
  })
})

onUnmounted(() => {
  unregisterPanel?.()
})
</script>

<template>
  <article
    class="history-panel"
    :class="{ 'history-panel--collapsed': !expanded }"
    :data-history-kind="panel.kind"
  >
    <button
      v-if="hasEntries"
      type="button"
      class="history-panel__header history-panel__header--interactive"
      :aria-expanded="expanded"
      :aria-controls="historyId"
      @click="toggle"
    >
      <span class="history-panel__heading">
        <span class="history-panel__summary">{{ summary }}</span>
      </span>
      <span class="history-panel__toggle" aria-hidden="true">
        <svg
          class="history-panel__toggle-icon"
          :class="{ 'history-panel__toggle-icon--open': expanded }"
          viewBox="0 0 24 24"
        >
          <path
            d="M6 9l6 6 6-6"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
          />
        </svg>
      </span>
    </button>

    <div v-else class="history-panel__header">
      <div class="history-panel__heading">
        <p class="history-panel__summary">{{ summary }}</p>
      </div>
    </div>

    <ul
      v-if="hasEntries && expanded"
      :id="historyId"
      class="history-panel__body"
    >
      <li v-for="entry in panel.entries" :key="entry.id">
        {{ entry.text }}
      </li>
    </ul>
  </article>
</template>
