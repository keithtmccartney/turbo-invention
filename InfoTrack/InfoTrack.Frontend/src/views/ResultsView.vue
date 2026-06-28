<script setup lang="ts">
import { computed, ref } from 'vue'
import Panel from '../components/Panel.vue'
import { useAppStore } from '../stores/app'
import { filterBySearch } from '../utils/panelSearch'
import { useTableSort } from '../utils/tableSort'

const store = useAppStore()
const locationFilter = ref('all')
const scrapeProgress = computed(() => store.activeScrapeStatus?.progress)

type ResultSortKey = 'firmName' | 'locationName' | 'phone' | 'rating' | 'reviewCount' | 'address'
const { toggleSort, ariaSort, sortedRows: rows, bodyKey } = useTableSort<ResultSortKey, Record<ResultSortKey, unknown>>(
  'firmName',
  () => {
    const all =
      store.results?.results.flatMap(group =>
        group.solicitors.map(s => ({ ...s, locationName: group.locationName })),
      ) ?? []

    return locationFilter.value === 'all'
      ? all
      : all.filter(x => x.locationName === locationFilter.value)
  },
)

const locations = computed(() => store.results?.results.map(x => x.locationName) ?? [])
</script>

<template>
  <section class="page">
    <Panel v-if="store.scraping && scrapeProgress" title="Scrape progress" :searchable="false">
      <div class="results-progress">
        <div class="results-progress-bar" role="progressbar" :aria-valuenow="scrapeProgress.percentComplete" aria-valuemin="0" aria-valuemax="100">
          <div class="results-progress-fill" :style="{ width: `${scrapeProgress.percentComplete}%` }" />
        </div>
        <p class="results-progress-stage">{{ scrapeProgress.stage }}</p>
        <p v-if="scrapeProgress.message" class="muted">{{ scrapeProgress.message }}</p>
        <p class="muted">
          {{ scrapeProgress.locationsCompleted }} of {{ scrapeProgress.locationsTotal }} locations ·
          {{ scrapeProgress.firmsDiscovered }} firms
        </p>
      </div>
    </Panel>

    <Panel onboarding-target="results-table">
      <template #header>
        <div class="toolbar">
          <h2>Solicitor results</h2>
          <select v-model="locationFilter">
            <option value="all">All locations</option>
            <option v-for="name in locations" :key="name" :value="name">{{ name }}</option>
          </select>
        </div>
      </template>

      <template #default="{ searchQuery }">
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th scope="col" :aria-sort="ariaSort('firmName')">
                <button type="button" class="sort-header" @click="toggleSort('firmName')">Firm</button>
              </th>
              <th scope="col" :aria-sort="ariaSort('locationName')">
                <button type="button" class="sort-header" @click="toggleSort('locationName')">Location</button>
              </th>
              <th scope="col" :aria-sort="ariaSort('phone')">
                <button type="button" class="sort-header" @click="toggleSort('phone')">Phone</button>
              </th>
              <th scope="col" :aria-sort="ariaSort('rating')">
                <button type="button" class="sort-header" @click="toggleSort('rating')">Rating</button>
              </th>
              <th scope="col" :aria-sort="ariaSort('reviewCount')">
                <button type="button" class="sort-header" @click="toggleSort('reviewCount')">Reviews</button>
              </th>
              <th scope="col" :aria-sort="ariaSort('address')">
                <button type="button" class="sort-header" @click="toggleSort('address')">Address</button>
              </th>
            </tr>
          </thead>
          <tbody :key="bodyKey">
            <tr v-for="(row, index) in filterBySearch(rows, searchQuery)" :key="index">
              <td>
                <strong>{{ row.firmName }}</strong>
                <div v-if="row.website" class="muted small">
                  <a :href="row.website" target="_blank" rel="noopener">Website</a>
                </div>
              </td>
              <td>{{ row.locationName }}</td>
              <td>{{ row.phone ?? '—' }}</td>
              <td>{{ row.rating?.toFixed(1) ?? '—' }}</td>
              <td>{{ row.reviewCount ?? '—' }}</td>
              <td>{{ row.address ?? '—' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <p v-if="searchQuery && !filterBySearch(rows, searchQuery).length" class="muted">
        No solicitors match your search.
      </p>
      </template>
    </Panel>
  </section>
</template>

<style scoped>
.results-progress-bar {
  height: 0.5rem;
  background: var(--surface-muted, #e5e7eb);
  border-radius: 999px;
  overflow: hidden;
  margin-bottom: 0.75rem;
}

.results-progress-fill {
  height: 100%;
  background: var(--accent, #f59e0b);
  transition: width 0.3s ease;
}

.results-progress-stage {
  font-weight: 600;
  margin: 0 0 0.25rem;
}
</style>
