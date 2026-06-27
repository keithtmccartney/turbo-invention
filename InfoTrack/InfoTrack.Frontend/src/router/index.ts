import { createRouter, createMemoryHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import DashboardView from '../views/DashboardView.vue'
import DiscoveryView from '../views/DiscoveryView.vue'
import LocationsView from '../views/LocationsView.vue'
import ResultsView from '../views/ResultsView.vue'
import InsightsView from '../views/InsightsView.vue'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'dashboard',
    component: DashboardView,
    meta: {
      title: 'Dashboard',
      subtitle: 'Executive summary of solicitor market activity across configured locations.',
    },
  },
  {
    path: '/discovery',
    name: 'discovery',
    component: DiscoveryView,
    meta: {
      title: 'Discovery',
      subtitle: 'Discover scrape targets from the solicitors.com sitemap and synchronise the canonical location catalogue.',
    },
  },
  {
    path: '/locations',
    name: 'locations',
    component: LocationsView,
    meta: {
      title: 'Locations',
      subtitle: 'Configure the cities searched during each scrape.',
    },
  },
  {
    path: '/results',
    name: 'results',
    component: ResultsView,
    meta: {
      title: 'Results',
      subtitle: 'Latest solicitor listings grouped and sortable by location.',
    },
  },
  {
    path: '/insights',
    name: 'insights',
    component: InsightsView,
    meta: {
      title: 'Insights',
      subtitle: 'Analytics engine output including deltas, rankings, and growth signals.',
    },
  },
]

export default createRouter({
  history: createMemoryHistory(),
  scrollBehavior() {
    return { top: 0 }
  },
  routes,
})
