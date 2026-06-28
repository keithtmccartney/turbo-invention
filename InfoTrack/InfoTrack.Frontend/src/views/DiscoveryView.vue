<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { Line } from 'vue-chartjs'
import {
  CategoryScale,
  Chart as ChartJS,
  Legend,
  LinearScale,
  LineElement,
  PointElement,
  Title,
  Tooltip,
} from 'chart.js'
import Panel from '../components/Panel.vue'
import PanelGroupControls from '../components/PanelGroupControls.vue'
import StatCard from '../components/StatCard.vue'
import { useAppStore } from '../stores/app'
import { useChartTheme } from '../utils/chartTheme'
import { formatDateTime, formatDuration } from '../utils/format'
import { filterBySearch } from '../utils/panelSearch'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Title, Tooltip, Legend)

const store = useAppStore()
const { lineOptions, brandPalette } = useChartTheme()

const summary = computed(() => store.discoverySummary)
const progress = computed(() => store.activeDiscoveryStatus?.progress)

const trendChart = computed(() => {
  const trend = summary.value?.historicalTrend ?? []
  return {
    labels: trend.map(x => formatDateTime(x.completedAt)),
    datasets: [
      {
        label: 'Locations found',
        data: trend.map(x => x.locationsFound),
        borderColor: brandPalette.value[0],
        backgroundColor: brandPalette.value[0],
        tension: 0.3,
      },
      {
        label: 'Added',
        data: trend.map(x => x.added),
        borderColor: brandPalette.value[1],
        backgroundColor: brandPalette.value[1],
        tension: 0.3,
      },
    ],
  }
})

onMounted(() => {
  void store.loadDiscovery()
})

async function discover() {
  await store.runDiscovery()
}
</script>

<template>
  <section class="page">
    <div class="toolbar discovery-toolbar" data-onboarding="discover-locations">
      <button class="primary" :disabled="store.discovering" @click="discover">
        {{ store.discovering ? 'Discovering…' : 'Discover locations' }}
      </button>
    </div>

    <Panel v-if="store.discovering && progress" title="Discovery progress" :searchable="false">
      <div class="discovery-progress">
        <div class="discovery-progress-bar" role="progressbar" :aria-valuenow="progress.percentComplete" aria-valuemin="0" aria-valuemax="100">
          <div class="discovery-progress-fill" :style="{ width: `${progress.percentComplete}%` }" />
        </div>
        <p class="discovery-progress-stage">{{ progress.stage }}</p>
        <p v-if="progress.message" class="muted">{{ progress.message }}</p>
        <dl class="detail-list compact">
          <div>
            <dt>Sitemaps</dt>
            <dd>{{ progress.sitemapsDownloaded }}</dd>
          </div>
          <div>
            <dt>URLs parsed</dt>
            <dd>{{ progress.urlsParsed }}</dd>
          </div>
          <div>
            <dt>Locations</dt>
            <dd>{{ progress.locationsDiscovered }}</dd>
          </div>
          <div>
            <dt>Added</dt>
            <dd>{{ progress.newLocationsAdded }}</dd>
          </div>
          <div>
            <dt>Updated</dt>
            <dd>{{ progress.existingLocationsUpdated }}</dd>
          </div>
          <div v-if="progress.errorsEncountered">
            <dt>Errors</dt>
            <dd>{{ progress.errorsEncountered }}</dd>
          </div>
        </dl>
      </div>
    </Panel>

    <div class="cards">
      <StatCard label="Active locations" :value="summary?.activeLocationCount ?? 0" />
      <StatCard
        label="Last run found"
        :value="summary?.lastRunLocationsFound ?? '—'"
        accent="amber"
      />
      <StatCard label="Added" :value="summary?.lastRunAdded ?? '—'" accent="orange" />
      <StatCard label="Removed" :value="summary?.lastRunRemoved ?? '—'" />
      <StatCard
        label="Duration"
        :value="formatDuration(summary?.lastRunDurationMilliseconds)"
        accent="amber"
      />
    </div>

    <PanelGroupControls />

    <div class="grid two">
      <Panel title="Last discovery run" :searchable="false">
        <dl class="detail-list">
          <div>
            <dt>Source</dt>
            <dd>{{ summary?.lastRunSource ?? '—' }}</dd>
          </div>
          <div>
            <dt>Completed</dt>
            <dd>{{ summary?.lastRunCompletedAt ? formatDateTime(summary.lastRunCompletedAt) : '—' }}</dd>
          </div>
          <div>
            <dt>Duration</dt>
            <dd>{{ formatDuration(summary?.lastRunDurationMilliseconds) }}</dd>
          </div>
          <div>
            <dt>Locations found</dt>
            <dd>{{ summary?.lastRunLocationsFound ?? '—' }}</dd>
          </div>
          <div>
            <dt>New</dt>
            <dd>{{ summary?.lastRunAdded ?? '—' }}</dd>
          </div>
          <div>
            <dt>Updated</dt>
            <dd>{{ summary?.lastRunUpdated ?? '—' }}</dd>
          </div>
          <div>
            <dt>Removed</dt>
            <dd>{{ summary?.lastRunRemoved ?? '—' }}</dd>
          </div>
          <div>
            <dt>Skipped</dt>
            <dd>{{ summary?.lastRunSkipped ?? '—' }}</dd>
          </div>
        </dl>
      </Panel>

      <Panel title="Discovery trend" :searchable="false">
        <Line v-if="trendChart.labels.length" :data="trendChart" :options="lineOptions" />
        <p v-else class="muted">Run discovery to populate historical trend data.</p>
      </Panel>
    </div>

    <Panel title="Discovery history" v-slot="{ searchQuery }">
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th scope="col">Completed</th>
              <th scope="col">Source</th>
              <th scope="col">Status</th>
              <th scope="col">Found</th>
              <th scope="col">Added</th>
              <th scope="col">Updated</th>
              <th scope="col">Removed</th>
              <th scope="col">Duration</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="run in filterBySearch(store.discoveryHistory, searchQuery)" :key="run.id">
              <td>{{ run.completedAt ? formatDateTime(run.completedAt) : '—' }}</td>
              <td>{{ run.source }}</td>
              <td>{{ run.status }}</td>
              <td>{{ run.locationsFound }}</td>
              <td>{{ run.newLocations }}</td>
              <td>{{ run.updatedLocations }}</td>
              <td>{{ run.removedLocations }}</td>
              <td>{{ formatDuration(run.durationMilliseconds) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <p v-if="searchQuery && !filterBySearch(store.discoveryHistory, searchQuery).length" class="muted">
        No discovery runs match your search.
      </p>
      <p v-else-if="!store.discoveryHistory.length" class="muted">No discovery runs recorded yet.</p>
    </Panel>
  </section>
</template>

<style scoped>
.discovery-progress-bar {
  height: 0.5rem;
  background: var(--surface-muted, #e5e7eb);
  border-radius: 999px;
  overflow: hidden;
  margin-bottom: 0.75rem;
}

.discovery-progress-fill {
  height: 100%;
  background: var(--accent, #f59e0b);
  transition: width 0.3s ease;
}

.discovery-progress-stage {
  font-weight: 600;
  margin: 0 0 0.25rem;
}

.detail-list.compact {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(8rem, 1fr));
  gap: 0.5rem 1rem;
  margin-top: 0.75rem;
}
</style>
