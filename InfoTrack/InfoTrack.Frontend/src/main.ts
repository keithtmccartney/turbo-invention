import { watch } from 'vue'
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { useAppStore } from './stores/app'
import { useThemeStore } from './stores/theme'
import './styles/main.css'

const pinia = createPinia()
const app = createApp(App)

app.use(pinia)
app.use(router)

useThemeStore(pinia).init()

const store = useAppStore(pinia)

router.isReady().then(() => {
  store.refreshAll()
})

router.afterEach(to => {
  document.title = to.meta.title
    ? `${String(to.meta.title)} | InfoTrack Solicitor Intelligence`
    : 'InfoTrack Solicitor Intelligence'
})

watch(
  () => router.currentRoute.value.name,
  name => {
    if (name === 'discovery') {
      void store.loadDiscovery()
    }
    if (name === 'locations') {
      void store.loadLocations()
    }
    if (name === 'results' && !store.results) {
      void store.loadResults()
    }
    if (name === 'dashboard' && !store.dashboard) {
      void store.loadDashboard()
    }
    if (name === 'insights' && !store.dashboard) {
      void store.loadDashboard()
    }
  },
)

app.mount('#app')
