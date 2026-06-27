import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

export type ThemeMode = 'light' | 'dark'

const storageKey = 'infotrack-theme'

export const useThemeStore = defineStore('theme', () => {
  const mode = ref<ThemeMode>('light')

  function applyTheme(theme: ThemeMode) {
    document.documentElement.dataset.theme = theme
  }

  function init() {
    const stored = localStorage.getItem(storageKey)
    mode.value = stored === 'dark' ? 'dark' : 'light'
    applyTheme(mode.value)
  }

  function toggle() {
    mode.value = mode.value === 'light' ? 'dark' : 'light'
  }

  function setTheme(theme: ThemeMode) {
    mode.value = theme
  }

  watch(mode, theme => {
    applyTheme(theme)
    localStorage.setItem(storageKey, theme)
  })

  return { mode, init, toggle, setTheme }
})
