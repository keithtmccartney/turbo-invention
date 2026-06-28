import axios from 'axios'
import type {
  DashboardResponse,
  DiscoveryRunResponse,
  DiscoveryRunStatusResponse,
  DiscoveryRunSummaryDto,
  DiscoverySummaryDto,
  LocationDto,
  ResultsResponse,
  ScrapeRunStatusResponse,
  SnapshotComparisonResponse,
  StartDiscoveryResponse,
  StartScrapeResponse,
} from '../types/api'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: { Accept: 'application/json' },
})

export const locationsApi = {
  get: () => api.get<{ locations: LocationDto[] }>('/locations').then(r => r.data.locations),
  update: (locations: string[]) =>
    api.post<{ locations: LocationDto[] }>('/locations', { locations }).then(r => r.data.locations),
}

export const discoveryApi = {
  start: () => api.post<StartDiscoveryResponse>('/discovery/run').then(r => r.data),
  status: (operationId: string) =>
    api.get<DiscoveryRunStatusResponse>(`/discovery/runs/${operationId}/status`).then(r => r.data),
  summary: () => api.get<DiscoverySummaryDto>('/discovery/summary').then(r => r.data),
  latestRun: () => api.get<DiscoveryRunSummaryDto>('/discovery/runs/latest').then(r => r.data),
  history: (take = 20) =>
    api.get<{ runs: DiscoveryRunSummaryDto[] }>('/discovery/runs', { params: { take } }).then(r => r.data.runs),
}

export const scrapeApi = {
  start: () => api.post<StartScrapeResponse>('/scrape').then(r => r.data),
  status: (operationId: string) =>
    api.get<ScrapeRunStatusResponse>(`/scrape/runs/${operationId}/status`).then(r => r.data),
  results: () => api.get<ResultsResponse>('/results').then(r => r.data),
}

export const insightsApi = {
  dashboard: () => api.get<DashboardResponse>('/insights').then(r => r.data),
  compare: (currentSnapshotId?: string, previousSnapshotId?: string) =>
    api
      .get<SnapshotComparisonResponse>('/insights/compare', {
        params: { currentSnapshotId, previousSnapshotId },
      })
      .then(r => r.data),
}

export type ChatMessageDto = {
  role: 'user' | 'assistant'
  content: string
}

export type ChatResponseDto = {
  reply: string
  toolsInvoked: string[]
}

export const chatApi = {
  send: (messages: ChatMessageDto[]) =>
    api.post<ChatResponseDto>('/chat', { messages }).then(r => r.data),
}
