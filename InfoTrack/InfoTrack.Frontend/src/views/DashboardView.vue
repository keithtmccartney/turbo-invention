<script setup lang="ts">
import { computed } from 'vue'
import { Bar, Doughnut } from 'vue-chartjs'
import {
  ArcElement,
  BarElement,
  CategoryScale,
  Chart as ChartJS,
  Legend,
  LinearScale,
  Title,
  Tooltip,
} from 'chart.js'
import Panel from '../components/Panel.vue'
import PanelGroupControls from '../components/PanelGroupControls.vue'
import StatCard from '../components/StatCard.vue'
import { useAppStore } from '../stores/app'
import { useChartTheme } from '../utils/chartTheme'
import { filterBySearch } from '../utils/panelSearch'
import { formatDateTime, formatDuration } from '../utils/format'
import { useTableSort } from '../utils/tableSort'
import type { FirmRankingDto, RegionalStatisticDto } from '../types/api'

ChartJS.register(CategoryScale, LinearScale, BarElement, ArcElement, Title, Tooltip, Legend)

const store = useAppStore()
const { barOptions, doughnutOptions, palette, brandPalette } = useChartTheme()
const dashboard = computed(() => store.dashboard)

function filteredRegionalBreakdown(query: string): RegionalStatisticDto[] {
  return filterBySearch(dashboard.value?.regionalBreakdown ?? [], query)
}

function regionalChartFor(query: string) {
  const data = filteredRegionalBreakdown(query)
  return {
    labels: data.map(x => x.locationName),
    datasets: [
      {
        label: 'Firms',
        backgroundColor: palette.value,
        borderRadius: 8,
        data: data.map(x => x.firmCount),
      },
    ],
  }
}

function ratingChartFor(query: string) {
  const data = filteredRegionalBreakdown(query).filter(x => x.averageRating)
  return {
    labels: data.map(x => x.locationName),
    datasets: [
      {
        label: 'Average rating',
        data: data.map(x => x.averageRating ?? 0),
        backgroundColor: brandPalette.value,
      },
    ],
  }
}

type TopFirmsSortKey = 'rank' | 'firmName' | 'locationName' | 'rating' | 'reviewCount' | 'rankChange'
const {
  toggleSort: toggleTopFirmsSort,
  ariaSort: topFirmsAriaSort,
  sortedRows: topFirmRows,
  bodyKey: topFirmsBodyKey,
} = useTableSort<TopFirmsSortKey, FirmRankingDto>('rank', () => dashboard.value?.topFirms ?? [])
</script>

<template>
  <section v-if="dashboard" class="page" data-onboarding="dashboard-summary">
    <div class="cards">
      <StatCard label="Total firms" :value="dashboard.totalFirms" />
      <StatCard label="Locations searched" :value="dashboard.locationsSearched" accent="amber" />
      <StatCard label="New firms" :value="dashboard.newFirms" accent="orange" />
      <StatCard label="Removed firms" :value="dashboard.removedFirms" />
      <StatCard
        label="Average rating"
        :value="dashboard.averageRating?.toFixed(1) ?? '—'"
        accent="amber"
      />
    </div>

    <div v-if="dashboard.discovery" class="cards">
      <StatCard
        label="Discovered locations"
        :value="dashboard.discovery.activeLocationCount"
      />
      <StatCard
        label="Last discovery added"
        :value="dashboard.discovery.lastRunAdded ?? '—'"
        accent="amber"
      />
      <StatCard
        label="Last discovery removed"
        :value="dashboard.discovery.lastRunRemoved ?? '—'"
        accent="orange"
      />
      <StatCard
        label="Discovery duration"
        :value="formatDuration(dashboard.discovery.lastRunDurationMilliseconds)"
      />
      <StatCard
        label="Last discovery"
        :value="dashboard.discovery.lastRunCompletedAt ? formatDateTime(dashboard.discovery.lastRunCompletedAt) : '—'"
        accent="amber"
      />
    </div>

    <PanelGroupControls />

    <div class="grid two">
      <div class="grid-two__col">
        <Panel title="Regional breakdown" v-slot="{ searchQuery }">
          <Bar
            v-if="regionalChartFor(searchQuery).labels.length"
            :data="regionalChartFor(searchQuery)"
            :options="barOptions"
          />
          <p v-else-if="searchQuery" class="muted">No regions match your search.</p>
        </Panel>
      </div>
      <div class="grid-two__col">
        <Panel title="Average rating by region" v-slot="{ searchQuery }">
          <Doughnut
            v-if="ratingChartFor(searchQuery).labels.length"
            :data="ratingChartFor(searchQuery)"
            :options="doughnutOptions"
          />
          <p v-else-if="searchQuery" class="muted">No regions match your search.</p>
        </Panel>
      </div>
    </div>

    <Panel title="Top firms nationally" v-slot="{ searchQuery }">
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th scope="col" :aria-sort="topFirmsAriaSort('rank')">
                <button type="button" class="sort-header" @click="toggleTopFirmsSort('rank')">Rank</button>
              </th>
              <th scope="col" :aria-sort="topFirmsAriaSort('firmName')">
                <button type="button" class="sort-header" @click="toggleTopFirmsSort('firmName')">Firm</button>
              </th>
              <th scope="col" :aria-sort="topFirmsAriaSort('locationName')">
                <button type="button" class="sort-header" @click="toggleTopFirmsSort('locationName')">
                  Location
                </button>
              </th>
              <th scope="col" :aria-sort="topFirmsAriaSort('rating')">
                <button type="button" class="sort-header" @click="toggleTopFirmsSort('rating')">Rating</button>
              </th>
              <th scope="col" :aria-sort="topFirmsAriaSort('reviewCount')">
                <button type="button" class="sort-header" @click="toggleTopFirmsSort('reviewCount')">Reviews</button>
              </th>
              <th scope="col" :aria-sort="topFirmsAriaSort('rankChange')">
                <button type="button" class="sort-header" @click="toggleTopFirmsSort('rankChange')">Change</button>
              </th>
            </tr>
          </thead>
          <tbody :key="topFirmsBodyKey">
            <tr v-for="(firm, index) in filterBySearch(topFirmRows, searchQuery)" :key="index">
              <td>{{ firm.rank }}</td>
              <td>{{ firm.firmName }}</td>
              <td>{{ firm.locationName }}</td>
              <td>{{ firm.rating?.toFixed(1) ?? '—' }}</td>
              <td>{{ firm.reviewCount ?? '—' }}</td>
              <td :class="{ up: firm.rankChange > 0, down: firm.rankChange < 0 }">
                {{ firm.rankChange > 0 ? `+${firm.rankChange}` : firm.rankChange }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <p v-if="searchQuery && !filterBySearch(topFirmRows, searchQuery).length" class="muted">
        No firms match your search.
      </p>
    </Panel>
  </section>

  <section v-else class="empty-state panel" data-onboarding="dashboard-summary">
    <h2>No scrape data yet</h2>
    <p>Configure locations and run a scrape to populate the executive dashboard.</p>
  </section>
</template>
