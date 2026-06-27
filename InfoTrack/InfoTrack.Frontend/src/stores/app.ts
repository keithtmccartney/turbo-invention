import axios from 'axios'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { discoveryApi, insightsApi, locationsApi, scrapeApi } from '../api/client'
import type {
  DashboardResponse,
  DiscoveryRunResponse,
  DiscoveryRunSummaryDto,
  DiscoverySummaryDto,
  LocationDto,
  ResultsResponse,
} from '../types/api'

export const useAppStore = defineStore('app', () => {
  const locations = ref<LocationDto[]>([])
  const dashboard = ref<DashboardResponse | null>(null)
  const results = ref<ResultsResponse | null>(null)
  const discoverySummary = ref<DiscoverySummaryDto | null>(null)
  const discoveryHistory = ref<DiscoveryRunSummaryDto[]>([])
  const lastDiscoveryRun = ref<DiscoveryRunResponse | null>(null)
  const loading = ref(false)
  const scraping = ref(false)
  const discovering = ref(false)
  const error = ref<string | null>(null)
  const activeLocationCount = computed(() => locations.value.filter(location => location.isActive).length)
  const canRunScrape = computed(() => activeLocationCount.value > 0)

  function readApiErrorMessage(e: unknown, fallback: string) {
    if (axios.isAxiosError(e)) {
      const data = e.response?.data as {
        title?: string
        detail?: string
        errors?: Record<string, string[]>
      } | undefined

      const validationMessage = data?.errors?.['']?.[0]
      if (validationMessage) return validationMessage
      if (data?.detail) return data.detail
      if (data?.title && data.title !== 'One or more validation errors occurred.') return data.title
    }

    return e instanceof Error ? e.message : fallback
  }

  async function loadLocations() {
    locations.value = await locationsApi.get()
  }

  async function saveLocations(names: string[]) {
    locations.value = await locationsApi.update(names)
  }

  async function loadDashboard() {
    dashboard.value = await insightsApi.dashboard()
    discoverySummary.value = dashboard.value.discovery ?? null
  }

  async function loadDiscoverySummary() {
    discoverySummary.value = await discoveryApi.summary()
  }

  async function loadDiscoveryHistory() {
    discoveryHistory.value = await discoveryApi.history()
  }

  async function loadDiscovery() {
    await Promise.all([loadDiscoverySummary(), loadDiscoveryHistory(), loadLocations()])
  }

  async function runDiscovery() {
    discovering.value = true
    error.value = null
    try {
      lastDiscoveryRun.value = await discoveryApi.run()
      await loadDiscovery()
      if (dashboard.value) {
        dashboard.value = {
          ...dashboard.value,
          discovery: discoverySummary.value ?? undefined,
        }
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Discovery failed'
      throw e
    } finally {
      discovering.value = false
    }
  }

  async function loadResults() {
    results.value = await scrapeApi.results()
  }

  async function runScrape() {
    if (!canRunScrape.value) {
      error.value = 'No active locations configured. Add locations on the Locations page before running a scrape.'
      return
    }

    scraping.value = true
    error.value = null
    try {
      await scrapeApi.run()
      await Promise.all([loadDashboard(), loadResults()])
    } catch (e) {
      error.value = readApiErrorMessage(e, 'Scrape failed')
    } finally {
      scraping.value = false
    }
  }

  async function refreshAll() {
    loading.value = true
    error.value = null
    try {
      await Promise.all([loadLocations(), loadDashboard(), loadResults(), loadDiscoverySummary()])
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load data'
    } finally {
      loading.value = false
    }
  }

  return {
    locations,
    dashboard,
    results,
    discoverySummary,
    discoveryHistory,
    lastDiscoveryRun,
    loading,
    scraping,
    discovering,
    error,
    activeLocationCount,
    canRunScrape,
    loadLocations,
    saveLocations,
    loadDashboard,
    loadResults,
    loadDiscovery,
    loadDiscoverySummary,
    loadDiscoveryHistory,
    runDiscovery,
    runScrape,
    refreshAll,
  }
})
