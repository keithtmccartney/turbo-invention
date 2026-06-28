<script setup lang="ts">
import { computed } from 'vue'
import { useAppStore } from '../stores/app'

const store = useAppStore()
const progress = computed(() => store.activeScrapeStatus?.progress)
</script>

<template>
  <div v-if="store.scraping && progress" class="operation-progress operation-progress--scrape">
    <div class="operation-progress__header">
      <strong>Scrape in progress</strong>
      <span class="muted">{{ progress.stage }}</span>
    </div>
    <div
      class="operation-progress__bar"
      role="progressbar"
      :aria-valuenow="progress.percentComplete"
      aria-valuemin="0"
      aria-valuemax="100"
    >
      <div class="operation-progress__fill" :style="{ width: `${progress.percentComplete}%` }" />
    </div>
    <p v-if="progress.message" class="operation-progress__message muted">{{ progress.message }}</p>
    <dl class="operation-progress__stats">
      <div>
        <dt>Locations</dt>
        <dd>{{ progress.locationsCompleted }} / {{ progress.locationsTotal }}</dd>
      </div>
      <div>
        <dt>Firms</dt>
        <dd>{{ progress.firmsDiscovered }}</dd>
      </div>
      <div v-if="progress.errorsEncountered">
        <dt>Errors</dt>
        <dd>{{ progress.errorsEncountered }}</dd>
      </div>
    </dl>
  </div>
</template>

<style scoped>
.operation-progress {
  margin-bottom: 1rem;
  padding: 1rem 1.25rem;
  border-radius: 0.75rem;
  background: var(--surface-elevated, #fff);
  border: 1px solid var(--border, #e5e7eb);
}

.operation-progress__header {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 0.75rem;
}

.operation-progress__bar {
  height: 0.5rem;
  background: var(--surface-muted, #e5e7eb);
  border-radius: 999px;
  overflow: hidden;
}

.operation-progress__fill {
  height: 100%;
  background: var(--accent, #f59e0b);
  transition: width 0.3s ease;
}

.operation-progress__message {
  margin: 0.5rem 0 0;
}

.operation-progress__stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(6rem, 1fr));
  gap: 0.5rem 1rem;
  margin: 0.75rem 0 0;
}
</style>
