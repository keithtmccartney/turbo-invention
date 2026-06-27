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

    <Panel v-if="store.lastDiscoveryRun" title="Latest discovery result" :searchable="false">
      <p class="muted">
        {{ store.lastDiscoveryRun.statistics.totalDiscovered }} locations discovered from
        {{ store.lastDiscoveryRun.source }} in
        {{ formatDuration(store.lastDiscoveryRun.durationMilliseconds) }}.
      </p>
    </Panel>
  </section>
</template>
