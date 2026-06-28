export interface ScrapeProgressDto {
  stage: string
  message?: string
  locationsTotal: number
  locationsCompleted: number
  firmsDiscovered: number
  newFirms: number
  removedFirms: number
  errorsEncountered: number
  percentComplete: number
}

export interface StartScrapeResponse {
  operationId: string
  correlationId: string
  status: string
}

export interface ScrapeRunStatusResponse {
  operationId: string
  correlationId: string
  status: string
  startedAt: string
  completedAt?: string
  durationMilliseconds?: number
  progress: ScrapeProgressDto
  result?: ScrapeResponseDto
  errorMessage?: string
}

export interface ScrapeResponseDto {
  snapshotId: string
  scrapedAt: string
  totalFirms: number
  locationsSearched: number
  newFirms: number
  removedFirms: number
}
