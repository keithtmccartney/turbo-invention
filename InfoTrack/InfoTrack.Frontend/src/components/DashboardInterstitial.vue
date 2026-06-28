<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, useTemplateRef, watch } from 'vue'
import { useDashboardInterstitialStore } from '../stores/dashboardInterstitial'
import { useOnboardingStore } from '../stores/onboarding'

const interstitial = useDashboardInterstitialStore()
const onboarding = useOnboardingStore()
const continueButton = useTemplateRef<HTMLButtonElement>('continueButton')

const showContinueDemo = computed(() => interstitial.pendingConfirmation)

function onContinue() {
  if (interstitial.pendingConfirmation) {
    const shouldCompleteTour =
      onboarding.isActive && onboarding.currentStep?.interstitialStep === true

    if (shouldCompleteTour) {
      onboarding.nextStep()
    }

    interstitial.confirm()
    return
  }

  interstitial.dismiss()
}

function onKeydown(event: KeyboardEvent) {
  if (!interstitial.visible || interstitial.pendingConfirmation) return

  if (event.key === 'Escape') {
    event.preventDefault()
    interstitial.dismiss()
  }
}

onMounted(() => {
  window.addEventListener('keydown', onKeydown)
})

watch(
  () => interstitial.visible,
  async visible => {
    if (!visible) return
    await nextTick()
    continueButton.value?.focus()
  },
)

onUnmounted(() => {
  window.removeEventListener('keydown', onKeydown)
})
</script>

<template>
  <Teleport to="body">
    <div
      v-if="interstitial.visible"
      class="dashboard-interstitial"
      :class="{ 'dashboard-interstitial--access-locked': interstitial.pendingConfirmation }"
      role="dialog"
      aria-modal="true"
      aria-labelledby="dashboard-interstitial-title"
    >
      <div class="dashboard-interstitial__split">
        <div class="dashboard-interstitial__copy">
          <h2 id="dashboard-interstitial-title">Your market intelligence dashboard.</h2>
          <div class="dashboard-interstitial__body">
            <p><strong>Discover. Configure. Analyse. Zero friction.</strong></p>
            <p>
              Solicitor Intelligence brings discovery, location configuration, scraping, and
              analytics into one executive view — a full picture of conveyancing market activity
              across your chosen cities.
            </p>
            <p>No more switching tools. No more stale spreadsheets.</p>
            <p><strong>Ready to explore your dashboard?</strong></p>
          </div>
          <div class="dashboard-interstitial__actions">
            <button
              ref="continueButton"
              type="button"
              class="primary dashboard-interstitial__continue"
              :class="{ 'dashboard-interstitial__continue--demo': showContinueDemo }"
              data-onboarding="dashboard-interstitial-continue"
              @click="onContinue"
            >
              Continue to dashboard
            </button>
          </div>
        </div>

        <div class="dashboard-interstitial__visual">
          <svg
            class="dashboard-interstitial__divider"
            viewBox="0 0 100 100"
            preserveAspectRatio="none"
            xmlns="http://www.w3.org/2000/svg"
            aria-hidden="true"
          >
            <polygon
              fill="var(--dashboard-interstitial-copy-bg, #ffffff)"
              points="0,0 0,100 100,0"
            />
          </svg>
        </div>
      </div>
    </div>
  </Teleport>
</template>
