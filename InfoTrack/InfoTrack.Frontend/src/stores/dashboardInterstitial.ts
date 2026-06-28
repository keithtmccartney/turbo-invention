import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

const STORAGE_KEY = 'infotrack-dashboard-access-confirmed'

function readConfirmedAccess(): boolean {
  try {
    return localStorage.getItem(STORAGE_KEY) === '1'
  } catch {
    return false
  }
}

function writeConfirmedAccess() {
  localStorage.setItem(STORAGE_KEY, '1')
}

export const useDashboardInterstitialStore = defineStore('dashboardInterstitial', () => {
  const visible = ref(false)
  const pendingConfirmation = ref(false)
  const accessConfirmed = ref(readConfirmedAccess())

  const isAccessLocked = computed(() => pendingConfirmation.value)

  function showForOnboarding() {
    visible.value = true
    pendingConfirmation.value = true
  }

  function confirm() {
    visible.value = false
    pendingConfirmation.value = false
    accessConfirmed.value = true
    writeConfirmedAccess()
  }

  function dismiss() {
    if (pendingConfirmation.value) return
    visible.value = false
  }

  function hasConfirmedAccess() {
    return accessConfirmed.value
  }

  return {
    visible,
    pendingConfirmation,
    isAccessLocked,
    showForOnboarding,
    confirm,
    dismiss,
    hasConfirmedAccess,
  }
})
