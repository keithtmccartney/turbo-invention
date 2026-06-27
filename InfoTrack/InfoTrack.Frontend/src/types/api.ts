export interface LocationDto {
  id: string
  name: string
  slug: string
  displayOrder: number
  isActive: boolean
  firstDiscoveredAt?: string
  lastDiscoveredAt?: string
}

export interface DiscoverySummaryDto {
  activeLocationCount: number
  lastRunId?: string
  lastRunSource?: string
  lastRunCompletedAt?: string
  lastRunDurationMilliseconds?: number
  lastRunLocationsFound?: number
  lastRunAdded?: number
  lastRunUpdated?: number
  lastRunRemoved?: number
  lastRunSkipped?: number
  historicalTrend: DiscoveryRunTrendPointDto[]
}

export interface DiscoveryRunTrendPointDto {
  completedAt: string
  locationsFound: number
  added: number
  removed: number
}

export interface DiscoveryStatisticsDto {
  totalDiscovered: number
  added: number
  updated: number
  removed: number
  skipped: number
  existing: number
  durationMilliseconds: number
}

export interface DiscoveredLocationDto {
  slug: string
  name: string
}

export interface DiscoveryRunResponse {
  runId: string
  source: string
  startedAt: string
  completedAt: string
  durationMilliseconds: number
  statistics: DiscoveryStatisticsDto
  locations: DiscoveredLocationDto[]
}

export interface DiscoveryRunSummaryDto {
  id: string
  source: string
  startedAt: string
  completedAt?: string
  durationMilliseconds?: number
  status: string
  locationsFound: number
  newLocations: number
  updatedLocations: number
  removedLocations: number
  skippedLocations: number
  errorMessage?: string
}

export interface SolicitorDto {
  id: string
  firmName: string
  locationName: string
  phone?: string
  address?: string
  website?: string
  emailEnquiryUrl?: string
  description?: string
  rating?: number
  reviewCount?: number
}

export interface ScrapeRunSummaryDto {
  scrapedAt: string
  locationNames: string[]
  totalFirms: number
}

export interface DashboardResponse {
  totalFirms: number
  locationsSearched: number
  newFirms: number
  removedFirms: number
  averageRating?: number
  regionalBreakdown: RegionalStatisticDto[]
  topFirms: FirmRankingDto[]
  nationalLeaderboard: FirmRankingDto[]
  growthSignals: GrowthSignalDto[]
  lastScrapedAt?: string
  currentSnapshotId?: string
  previousSnapshotId?: string
  scrapeHistory?: ScrapeRunSummaryDto[]
  discovery?: DiscoverySummaryDto
}

export interface RegionalStatisticDto {
  locationName: string
  firmCount: number
  averageRating?: number
  totalReviews: number
}

export interface FirmRankingDto {
  rank: number
  firmName: string
  locationName: string
  rating?: number
  reviewCount?: number
  rankChange: number
}

export interface GrowthSignalDto {
  firmName: string
  locationName: string
  signalType: string
  description: string
}

export interface ScrapeResponse {
  snapshotId: string
  scrapedAt: string
  totalFirms: number
  locationsSearched: number
  newFirms: number
  removedFirms: number
}

export interface ResultsResponse {
  lastScrapedAt?: string
  results: { locationName: string; solicitors: SolicitorDto[] }[]
}

export interface SnapshotComparisonResponse {
  currentSnapshotId: string
  previousSnapshotId?: string
  newSolicitors: SolicitorDeltaDto[]
  removedSolicitors: SolicitorDeltaDto[]
  regionalDeltas: RegionalDeltaDto[]
}

export interface SolicitorDeltaDto {
  firmName: string
  locationName: string
  phone?: string
  rating?: number
  rank: number
}

export interface RegionalDeltaDto {
  locationName: string
  previousCount: number
  currentCount: number
  netChange: number
  newCount: number
  removedCount: number
  averageRating?: number
}
