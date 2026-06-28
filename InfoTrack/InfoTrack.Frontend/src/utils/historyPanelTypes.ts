import type { AssistantQueryEntry } from '../stores/assistant'
import type { ScrapeRunSummaryDto } from '../types/api'
import { formatDateTime } from './format'

export type HistoryPanelKind = 'scrape' | 'query'

export type HistoryPanelEntry = {
  id: string
  text: string
}

export type HistoryPanelModel = {
  kind: HistoryPanelKind
  lastAt?: string
  count: number
  entries: HistoryPanelEntry[]
}

export function buildScrapeHistoryPanel(
  lastScrapedAt: string | undefined,
  runs: ScrapeRunSummaryDto[],
): HistoryPanelModel | null {
  if (!lastScrapedAt && runs.length === 0) return null

  return {
    kind: 'scrape',
    lastAt: lastScrapedAt,
    count: runs.length,
    entries: runs.map((run, index) => ({
      id: `${run.scrapedAt}-${index}`,
      text: [
        run.locationNames.join(', ') || 'No locations recorded',
        formatDateTime(run.scrapedAt),
        `${run.totalFirms} firm${run.totalFirms === 1 ? '' : 's'}`,
      ].join(' · '),
    })),
  }
}

export function buildQueryHistoryPanel(
  lastQueryAt: string | undefined,
  queries: AssistantQueryEntry[],
): HistoryPanelModel | null {
  if (queries.length === 0) return null

  return {
    kind: 'query',
    lastAt: lastQueryAt,
    count: queries.length,
    entries: queries.map((entry, index) => ({
      id: `${entry.askedAt}-${index}`,
      text: `${entry.question} · ${formatDateTime(entry.askedAt)}`,
    })),
  }
}

export function formatHistoryPanelSummary(panel: HistoryPanelModel): string {
  if (panel.kind === 'scrape') {
    const base = panel.lastAt
      ? `Last scrape: ${formatDateTime(panel.lastAt)}`
      : 'Scrape history'
    const suffix =
      panel.count > 0
        ? ` · ${panel.count} scrape${panel.count === 1 ? '' : 's'}`
        : ''
    return base + suffix
  }

  const base = panel.lastAt
    ? `Last query: ${formatDateTime(panel.lastAt)}`
    : 'Query history'
  const suffix =
    panel.count > 0
      ? ` · ${panel.count} quer${panel.count === 1 ? 'y' : 'ies'}`
      : ''
  return base + suffix
}
