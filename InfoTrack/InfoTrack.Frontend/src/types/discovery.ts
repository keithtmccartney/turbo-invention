export interface DiscoveryProgressDto {
  stage: string
  message?: string
  sitemapsDownloaded: number
  urlsParsed: number
  locationsDiscovered: number
  newLocationsAdded: number
  existingLocationsUpdated: number
  errorsEncountered: number
  percentComplete: number
}

export interface StartDiscoveryResponse {
  operationId: string
  correlationId: string
  status: string
}

export interface DiscoveryRunStatusResponse {
  operationId: string
  correlationId: string
  source: string
  status: string
  startedAt: string
  completedAt?: string
  durationMilliseconds?: number
  progress: DiscoveryProgressDto
  statistics?: DiscoveryStatisticsDto
  errorMessage?: string
  locations?: DiscoveredLocationDto[]
}
