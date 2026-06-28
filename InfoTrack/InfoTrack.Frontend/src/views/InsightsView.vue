<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { insightsApi } from '../api/client'
import type {
  FirmRankingDto,
  RegionalDeltaDto,
  SnapshotComparisonResponse,
  SolicitorDeltaDto,
} from '../types/api'
import { useAppStore } from '../stores/app'
import { formatSignalType } from '../utils/format'
import { filterBySearch } from '../utils/panelSearch'
import { useTableSort } from '../utils/tableSort'
import Panel from '../components/Panel.vue'
import PanelGroupControls from '../components/PanelGroupControls.vue'

const store = useAppStore()
const comparison = ref<SnapshotComparisonResponse | null>(null)

type RegionalSortKey =
  | 'locationName'
  | 'currentCount'
  | 'previousCount'
  | 'netChange'
  | 'newCount'
  | 'removedCount'
const {
  toggleSort: toggleRegionalSort,
  ariaSort: regionalAriaSort,
  sortedRows: regionalRows,
  bodyKey: regionalBodyKey,
} = useTableSort<
  RegionalSortKey,
  RegionalDeltaDto
>('locationName', () => comparison.value?.regionalDeltas ?? [])

type NewSolicitorSortKey = 'firmName' | 'locationName' | 'rating'
const {
  toggleSort: toggleNewSolicitorsSort,
  ariaSort: newSolicitorAriaSort,
  sortedRows: newSolicitorRows,
  bodyKey: newSolicitorsBodyKey,
} = useTableSort<
  NewSolicitorSortKey,
  SolicitorDeltaDto
>('firmName', () => comparison.value?.newSolicitors ?? [])

type RemovedSolicitorSortKey = 'firmName' | 'locationName' | 'rating'
const {
  toggleSort: toggleRemovedSolicitorsSort,
  ariaSort: removedSolicitorAriaSort,
  sortedRows: removedSolicitorRows,
  bodyKey: removedSolicitorsBodyKey,
} = useTableSort<
  RemovedSolicitorSortKey,
  SolicitorDeltaDto
>('firmName', () => comparison.value?.removedSolicitors ?? [])

type LeaderboardSortKey = 'rank' | 'firmName' | 'locationName' | 'rating' | 'reviewCount' | 'rankChange'
const {
  toggleSort: toggleLeaderboardSort,
  ariaSort: leaderboardAriaSort,
  sortedRows: leaderboardRows,
  bodyKey: leaderboardBodyKey,
} = useTableSort<
  LeaderboardSortKey,
  FirmRankingDto
>('rank', () => store.dashboard?.nationalLeaderboard.slice(0, 25) ?? [])

onMounted(async () => {
  comparison.value = await insightsApi.compare()
})
</script>

<template>
  <section class="page">
    <PanelGroupControls />

    <div class="grid two">
      <Panel title="Growth signals" onboarding-target="insights-overview" v-slot="{ searchQuery }">
        <ul v-if="filterBySearch(store.dashboard?.growthSignals ?? [], searchQuery).length" class="signal-list">
          <li
            v-for="(signal, index) in filterBySearch(store.dashboard?.growthSignals ?? [], searchQuery)"
            :key="index"
          >
            <span class="chip">{{ formatSignalType(signal.signalType) }}</span>
            <div>
              <strong>{{ signal.firmName }}</strong>
              <p class="muted">{{ signal.description }}</p>
            </div>
          </li>
        </ul>
        <p v-else-if="searchQuery" class="muted">No growth signals match your search.</p>
      </Panel>

      <Panel title="Regional deltas" v-slot="{ searchQuery }">
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th scope="col" :aria-sort="regionalAriaSort('locationName')">
                  <button type="button" class="sort-header" @click="toggleRegionalSort('locationName')">
                    Location
                  </button>
                </th>
                <th scope="col" :aria-sort="regionalAriaSort('currentCount')">
                  <button type="button" class="sort-header" @click="toggleRegionalSort('currentCount')">
                    Current
                  </button>
                </th>
                <th scope="col" :aria-sort="regionalAriaSort('previousCount')">
                  <button type="button" class="sort-header" @click="toggleRegionalSort('previousCount')">
                    Previous
                  </button>
                </th>
                <th scope="col" :aria-sort="regionalAriaSort('netChange')">
                  <button type="button" class="sort-header" @click="toggleRegionalSort('netChange')">Net</button>
                </th>
                <th scope="col" :aria-sort="regionalAriaSort('newCount')">
                  <button type="button" class="sort-header" @click="toggleRegionalSort('newCount')">New</button>
                </th>
                <th scope="col" :aria-sort="regionalAriaSort('removedCount')">
                  <button type="button" class="sort-header" @click="toggleRegionalSort('removedCount')">
                    Removed
                  </button>
                </th>
              </tr>
            </thead>
            <tbody :key="regionalBodyKey">
              <tr v-for="(row, index) in filterBySearch(regionalRows, searchQuery)" :key="index">
                <td>{{ row.locationName }}</td>
                <td>{{ row.currentCount }}</td>
                <td>{{ row.previousCount }}</td>
                <td :class="{ up: row.netChange > 0, down: row.netChange < 0 }">{{ row.netChange }}</td>
                <td>{{ row.newCount }}</td>
                <td>{{ row.removedCount }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        <p v-if="searchQuery && !filterBySearch(regionalRows, searchQuery).length" class="muted">
          No regional deltas match your search.
        </p>
      </Panel>

      <Panel title="New solicitors" v-slot="{ searchQuery }">
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th scope="col" :aria-sort="newSolicitorAriaSort('firmName')">
                  <button type="button" class="sort-header" @click="toggleNewSolicitorsSort('firmName')">
                    Firm
                  </button>
                </th>
                <th scope="col" :aria-sort="newSolicitorAriaSort('locationName')">
                  <button type="button" class="sort-header" @click="toggleNewSolicitorsSort('locationName')">
                    Location
                  </button>
                </th>
                <th scope="col" :aria-sort="newSolicitorAriaSort('rating')">
                  <button type="button" class="sort-header" @click="toggleNewSolicitorsSort('rating')">
                    Rating
                  </button>
                </th>
              </tr>
            </thead>
            <tbody :key="newSolicitorsBodyKey">
              <tr v-for="(row, index) in filterBySearch(newSolicitorRows, searchQuery)" :key="index">
                <td>{{ row.firmName }}</td>
                <td>{{ row.locationName }}</td>
                <td>{{ row.rating?.toFixed(1) ?? '—' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        <p v-if="searchQuery && !filterBySearch(newSolicitorRows, searchQuery).length" class="muted">
          No solicitors match your search.
        </p>
      </Panel>

      <Panel title="Removed solicitors" v-slot="{ searchQuery }">
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th scope="col" :aria-sort="removedSolicitorAriaSort('firmName')">
                  <button type="button" class="sort-header" @click="toggleRemovedSolicitorsSort('firmName')">
                    Firm
                  </button>
                </th>
                <th scope="col" :aria-sort="removedSolicitorAriaSort('locationName')">
                  <button type="button" class="sort-header" @click="toggleRemovedSolicitorsSort('locationName')">
                    Location
                  </button>
                </th>
                <th scope="col" :aria-sort="removedSolicitorAriaSort('rating')">
                  <button type="button" class="sort-header" @click="toggleRemovedSolicitorsSort('rating')">
                    Rating
                  </button>
                </th>
              </tr>
            </thead>
            <tbody :key="removedSolicitorsBodyKey">
              <tr v-for="(row, index) in filterBySearch(removedSolicitorRows, searchQuery)" :key="index">
                <td>{{ row.firmName }}</td>
                <td>{{ row.locationName }}</td>
                <td>{{ row.rating?.toFixed(1) ?? '—' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        <p v-if="searchQuery && !filterBySearch(removedSolicitorRows, searchQuery).length" class="muted">
          No solicitors match your search.
        </p>
      </Panel>
    </div>

    <Panel title="National leaderboard" v-slot="{ searchQuery }">
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th scope="col" :aria-sort="leaderboardAriaSort('rank')">
                <button type="button" class="sort-header" @click="toggleLeaderboardSort('rank')">Rank</button>
              </th>
              <th scope="col" :aria-sort="leaderboardAriaSort('firmName')">
                <button type="button" class="sort-header" @click="toggleLeaderboardSort('firmName')">Firm</button>
              </th>
              <th scope="col" :aria-sort="leaderboardAriaSort('locationName')">
                <button type="button" class="sort-header" @click="toggleLeaderboardSort('locationName')">
                  Location
                </button>
              </th>
              <th scope="col" :aria-sort="leaderboardAriaSort('rating')">
                <button type="button" class="sort-header" @click="toggleLeaderboardSort('rating')">Rating</button>
              </th>
              <th scope="col" :aria-sort="leaderboardAriaSort('reviewCount')">
                <button type="button" class="sort-header" @click="toggleLeaderboardSort('reviewCount')">
                  Reviews
                </button>
              </th>
              <th scope="col" :aria-sort="leaderboardAriaSort('rankChange')">
                <button type="button" class="sort-header" @click="toggleLeaderboardSort('rankChange')">
                  Rank change
                </button>
              </th>
            </tr>
          </thead>
          <tbody :key="leaderboardBodyKey">
            <tr v-for="(firm, index) in filterBySearch(leaderboardRows, searchQuery)" :key="index">
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
      <p v-if="searchQuery && !filterBySearch(leaderboardRows, searchQuery).length" class="muted">
        No firms match your search.
      </p>
    </Panel>
  </section>
</template>
