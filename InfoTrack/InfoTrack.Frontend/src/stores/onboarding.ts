import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

export type OnboardingPlacement = 'top' | 'bottom' | 'left' | 'right'

export interface OnboardingStep {
  id: string
  target: string
  title: string
  body: string
  placement?: OnboardingPlacement
  route?: string
  advanceOnTargetClick?: boolean
  /** Additional elements included in the spotlight cut-out alongside the primary target. */
  spotlightIncludes?: string[]
  /** Shows the dashboard interstitial and requires Continue before the tour can finish. */
  interstitialStep?: boolean
}

export interface OnboardingTour {
  id: string
  steps: OnboardingStep[]
}

const STORAGE_KEY = 'infotrack-onboarding-completed'
const INITIAL_TOUR_ID = 'getting-started'

const tours: Record<string, OnboardingTour> = {
  [INITIAL_TOUR_ID]: {
    id: INITIAL_TOUR_ID,
    steps: [
      {
        id: 'welcome',
        target: '[data-onboarding="brand"]',
        spotlightIncludes: ['[data-onboarding="brand-breadcrumb"]'],
        title: 'Welcome to InfoTrack',
        body: 'You are in Products → Conveyancing → Solicitor Intelligence. This workspace helps you discover firms from solicitors.com, configure scrape targets, and analyse market changes over time.',
        placement: 'right',
        route: 'dashboard',
      },
      {
        id: 'sidebar-nav',
        target: '[data-onboarding="sidebar-nav"]',
        title: 'Move around the app',
        body: 'Use these links to switch between Dashboard, Discovery, Locations, Results, Insights, and Assistant. Each section covers one part of the workflow.',
        placement: 'right',
        route: 'dashboard',
      },
      {
        id: 'dashboard-nav',
        target: '[data-onboarding="nav-dashboard"]',
        title: 'Executive dashboard',
        body: 'The Dashboard summarises firm counts, ratings, and discovery activity. Each section also has a title and description in the main content area.',
        placement: 'right',
        route: 'dashboard',
        advanceOnTargetClick: true,
      },
      {
        id: 'dashboard-summary',
        target: '[data-onboarding="dashboard-summary"]',
        title: 'Key metrics at a glance',
        body: 'Stat cards surface totals, recent changes, and discovery status. They populate after you configure locations and run a scrape.',
        placement: 'top',
        route: 'dashboard',
      },
      {
        id: 'discovery-nav',
        target: '[data-onboarding="nav-discovery"]',
        title: 'Discover new locations',
        body: 'Start in Discovery to pull the latest city catalogue from the solicitors.com sitemap and sync it locally.',
        placement: 'right',
        route: 'dashboard',
        advanceOnTargetClick: true,
      },
      {
        id: 'discover-locations',
        target: '[data-onboarding="discover-locations"]',
        title: 'Run discovery',
        body: 'Click Discover locations to fetch and synchronise the canonical location list. Review the run summary and history below.',
        placement: 'bottom',
        route: 'discovery',
      },
      {
        id: 'locations-nav',
        target: '[data-onboarding="nav-locations"]',
        title: 'Choose scrape targets',
        body: 'Open Locations to review the discovered catalogue and decide which cities to include in each scrape.',
        placement: 'right',
        advanceOnTargetClick: true,
      },
      {
        id: 'current-config',
        target: '[data-onboarding="locations-config"]',
        title: 'Select locations',
        body: 'Click chips to select or deselect cities. Search to narrow the list, then pick the locations you want included.',
        placement: 'top',
        route: 'locations',
      },
      {
        id: 'scrape-list',
        target: '[data-onboarding="locations-scrape"]',
        title: 'Confirm and save',
        body: 'Selected locations appear here as a list. Click Save locations when you are ready to persist your configuration.',
        placement: 'top',
        route: 'locations',
      },
      {
        id: 'run-scrape',
        target: '[data-onboarding="run-scrape"]',
        title: 'Collect solicitor data',
        body: 'Run Scrape fetches listings from solicitors.com for your saved locations. This may take a while depending on how many cities you selected.',
        placement: 'right',
      },
      {
        id: 'results-nav',
        target: '[data-onboarding="nav-results"]',
        title: 'Browse firm listings',
        body: 'After scraping, open Results to explore firms by location, sort columns, and search within the table.',
        placement: 'right',
        advanceOnTargetClick: true,
      },
      {
        id: 'results-table',
        target: '[data-onboarding="results-table"]',
        title: 'Solicitor results',
        body: 'Filter by location, sort any column, and use panel search to find specific firms, ratings, or addresses.',
        placement: 'top',
        route: 'results',
      },
      {
        id: 'insights-nav',
        target: '[data-onboarding="nav-insights"]',
        title: 'Market analytics',
        body: 'Insights compares snapshots over time — regional deltas, new and removed firms, growth signals, and the national leaderboard.',
        placement: 'right',
        advanceOnTargetClick: true,
      },
      {
        id: 'insights-overview',
        target: '[data-onboarding="insights-overview"]',
        title: 'Track market movement',
        body: 'Review growth signals and regional deltas here after at least two scrapes. Use search and sort to focus on the changes that matter.',
        placement: 'top',
        route: 'insights',
      },
      {
        id: 'assistant-nav',
        target: '[data-onboarding="nav-assistant"]',
        title: 'Ask in natural language',
        body: 'Open Assistant to query solicitor data in plain English. Your local LM Studio model chooses MCP tools to fetch live counts, searches, and reports from InfoTrack.',
        placement: 'right',
        route: 'insights',
        advanceOnTargetClick: true,
      },
      {
        id: 'assistant-chat',
        target: '[data-onboarding="assistant-composer"]',
        title: 'Chat with your data',
        body: 'Type a question and press Send. The assistant calls InfoTrack tools against your scrape and analytics data before answering. Keep LM Studio running with the configured model loaded.',
        placement: 'top',
        route: 'assistant',
      },
      {
        id: 'dashboard-interstitial',
        target: '[data-onboarding="dashboard-interstitial-continue"]',
        title: 'Your executive dashboard',
        body: 'When you are ready, click Continue to dashboard to unlock Solicitor Intelligence and start exploring.',
        placement: 'left',
        route: 'dashboard',
        interstitialStep: true,
      },
    ],
  },
}

function readCompletedTours(): Set<string> {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return new Set()
    const parsed = JSON.parse(raw) as unknown
    const completed = new Set(Array.isArray(parsed) ? parsed.filter(x => typeof x === 'string') : [])

    // Treat the earlier post-discovery tour as completed for this fuller walkthrough.
    if (completed.has('post-discovery')) {
      completed.add(INITIAL_TOUR_ID)
    }

    return completed
  } catch {
    return new Set()
  }
}

function writeCompletedTours(completed: Set<string>) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify([...completed]))
}

export const useOnboardingStore = defineStore('onboarding', () => {
  const activeTourId = ref<string | null>(null)
  const stepIndex = ref(0)
  const completedTours = ref(readCompletedTours())

  const activeTour = computed(() => (activeTourId.value ? tours[activeTourId.value] ?? null : null))

  const currentStep = computed(() => {
    const tour = activeTour.value
    if (!tour) return null
    return tour.steps[stepIndex.value] ?? null
  })

  const isActive = computed(() => activeTour.value != null && currentStep.value != null)

  const stepProgress = computed(() => {
    const tour = activeTour.value
    if (!tour) return { current: 0, total: 0 }
    return { current: stepIndex.value + 1, total: tour.steps.length }
  })

  function isTourCompleted(tourId: string) {
    return completedTours.value.has(tourId)
  }

  function startTour(tourId: string) {
    if (!tours[tourId] || isTourCompleted(tourId)) return false

    activeTourId.value = tourId
    stepIndex.value = 0
    return true
  }

  function tryStartInitialTour() {
    return startTour(INITIAL_TOUR_ID)
  }

  function completeTour() {
    if (activeTourId.value) {
      completedTours.value = new Set([...completedTours.value, activeTourId.value])
      writeCompletedTours(completedTours.value)
    }
    activeTourId.value = null
    stepIndex.value = 0
  }

  function dismissTour() {
    completeTour()
  }

  function nextStep() {
    const tour = activeTour.value
    if (!tour) return

    if (stepIndex.value >= tour.steps.length - 1) {
      completeTour()
      return
    }

    stepIndex.value += 1
  }

  function previousStep() {
    if (stepIndex.value > 0) {
      stepIndex.value -= 1
    }
  }

  return {
    activeTourId,
    stepIndex,
    activeTour,
    currentStep,
    isActive,
    stepProgress,
    isTourCompleted,
    startTour,
    tryStartInitialTour,
    completeTour,
    dismissTour,
    nextStep,
    previousStep,
  }
})
