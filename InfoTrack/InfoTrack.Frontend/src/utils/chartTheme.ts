import { computed } from 'vue'
import { useThemeStore } from '../stores/theme'

function cssVar(name: string): string {
  return getComputedStyle(document.documentElement).getPropertyValue(name).trim()
}

export function useChartTheme() {
  const theme = useThemeStore()

  const barOptions = computed(() => ({
    responsive: true,
    plugins: {
      legend: { display: false },
    },
    scales: {
      x: {
        ticks: { color: cssVar('--chart-text') },
        grid: { color: cssVar('--chart-grid') },
      },
      y: {
        ticks: { color: cssVar('--chart-text') },
        grid: { color: cssVar('--chart-grid') },
      },
    },
  }))

  const doughnutOptions = computed(() => ({
    responsive: true,
    plugins: {
      legend: {
        labels: { color: cssVar('--chart-text') },
      },
    },
  }))

  const lineOptions = computed(() => ({
    responsive: true,
    plugins: {
      legend: {
        labels: { color: cssVar('--chart-text') },
      },
    },
    scales: {
      x: {
        ticks: { color: cssVar('--chart-text') },
        grid: { color: cssVar('--chart-grid') },
      },
      y: {
        ticks: { color: cssVar('--chart-text') },
        grid: { color: cssVar('--chart-grid') },
      },
    },
  }))

  // Recompute when theme changes.
  const palette = computed(() => {
    void theme.mode
    return cssVar('--primary')
  })

  const brandPalette = computed(() => {
    void theme.mode
    return [
      cssVar('--it-teal'),
      cssVar('--it-orange'),
      cssVar('--it-amber'),
      '#057a96',
      '#d95424',
      '#2eb3d4',
      '#6b6b6b',
      '#3a3a3a',
    ]
  })

  return { barOptions, doughnutOptions, lineOptions, palette, brandPalette }
}
