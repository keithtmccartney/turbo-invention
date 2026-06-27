<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import type { OnboardingPlacement } from '../stores/onboarding'
import { useOnboardingStore } from '../stores/onboarding'
import { useDashboardInterstitialStore } from '../stores/dashboardInterstitial'

const onboarding = useOnboardingStore()
const dashboardInterstitial = useDashboardInterstitialStore()
const route = useRoute()
const router = useRouter()

const targetRect = ref<DOMRect | null>(null)
const tooltipRef = ref<HTMLElement | null>(null)
const tooltipStyle = ref<Record<string, string>>({})

const padding = 8
const tooltipGap = 12

const clickDemoStyle = computed(() => {
  const rect = targetRect.value
  if (!rect) return { display: 'none' }

  return {
    top: `${rect.top + rect.height * 0.55}px`,
    left: `${rect.left + rect.width * 0.72}px`,
  }
})

const isInterstitialStep = computed(() => onboarding.currentStep?.interstitialStep === true)

const spotlightStyle = computed(() => {
  const rect = targetRect.value
  if (!rect) return { display: 'none' }

  return {
    top: `${Math.max(rect.top - padding, 0)}px`,
    left: `${Math.max(rect.left - padding, 0)}px`,
    width: `${rect.width + padding * 2}px`,
    height: `${rect.height + padding * 2}px`,
  }
})

const backdropClipPath = computed(() => {
  const rect = targetRect.value
  if (!rect) {
    return 'none'
  }

  const t = Math.max(rect.top - padding, 0)
  const l = Math.max(rect.left - padding, 0)
  const r = rect.right + padding
  const b = rect.bottom + padding

  return `polygon(evenodd, 0 0, 100vw 0, 100vw 100vh, 0 100vh, 0 0, ${l}px ${t}px, ${r}px ${t}px, ${r}px ${b}px, ${l}px ${b}px, ${l}px ${t}px)`
})

function resolveTarget(): HTMLElement | null {
  const selector = onboarding.currentStep?.target
  if (!selector) return null
  return document.querySelector<HTMLElement>(selector)
}

function resolveSpotlightElements(): HTMLElement[] {
  const step = onboarding.currentStep
  if (!step) return []

  const elements: HTMLElement[] = []
  const primary = resolveTarget()
  if (primary) elements.push(primary)

  for (const selector of step.spotlightIncludes ?? []) {
    const element = document.querySelector<HTMLElement>(selector)
    if (element && !elements.includes(element)) {
      elements.push(element)
    }
  }

  return elements
}

function unionRect(elements: HTMLElement[]): DOMRect | null {
  if (!elements.length) return null

  const rects = elements.map(element => element.getBoundingClientRect())
  const top = Math.min(...rects.map(rect => rect.top))
  const left = Math.min(...rects.map(rect => rect.left))
  const right = Math.max(...rects.map(rect => rect.right))
  const bottom = Math.max(...rects.map(rect => rect.bottom))

  return new DOMRect(left, top, right - left, bottom - top)
}

function updateTargetRect() {
  targetRect.value = unionRect(resolveSpotlightElements())
  nextTick(positionTooltip)
}

function positionTooltip() {
  const rect = targetRect.value
  const tooltip = tooltipRef.value
  const step = onboarding.currentStep
  if (!rect || !tooltip || !step) {
    tooltipStyle.value = {}
    return
  }

  const placement = step.placement ?? choosePlacement(rect)
  const tooltipRect = tooltip.getBoundingClientRect()
  const margin = 16
  let top = 0
  let left = 0

  switch (placement) {
    case 'right':
      top = rect.top + rect.height / 2 - tooltipRect.height / 2
      left = rect.right + padding + tooltipGap
      break
    case 'left':
      top = rect.top + rect.height / 2 - tooltipRect.height / 2
      left = rect.left - padding - tooltipGap - tooltipRect.width
      break
    case 'bottom':
      top = rect.bottom + padding + tooltipGap
      left = rect.left + rect.width / 2 - tooltipRect.width / 2
      break
    default:
      top = rect.top - padding - tooltipGap - tooltipRect.height
      left = rect.left + rect.width / 2 - tooltipRect.width / 2
      break
  }

  top = clamp(top, margin, window.innerHeight - tooltipRect.height - margin)
  left = clamp(left, margin, window.innerWidth - tooltipRect.width - margin)

  tooltipStyle.value = {
    top: `${top}px`,
    left: `${left}px`,
  }
}

function choosePlacement(rect: DOMRect): OnboardingPlacement {
  if (rect.right + 320 < window.innerWidth) return 'right'
  if (rect.top > 220) return 'top'
  return 'bottom'
}

function clamp(value: number, min: number, max: number) {
  return Math.min(Math.max(value, min), max)
}

async function ensureStepRoute() {
  const step = onboarding.currentStep
  if (!step?.route || route.name === step.route) return

  await router.replace({ name: step.route })
  await nextTick()
  updateTargetRect()
}

async function goNext() {
  const isLastStep = onboarding.stepProgress.current === onboarding.stepProgress.total

  if (isLastStep) {
    onboarding.nextStep()
    return
  }

  const nextStep = onboarding.activeTour?.steps[onboarding.stepIndex + 1]
  if (nextStep?.route && route.name !== nextStep.route) {
    await router.replace({ name: nextStep.route })
    await nextTick()
  }

  onboarding.nextStep()
}

async function skipTour() {
  onboarding.dismissTour()

  if (!dashboardInterstitial.hasConfirmedAccess()) {
    dashboardInterstitial.showForOnboarding()
    if (route.name !== 'dashboard') {
      await router.replace({ name: 'dashboard' })
    }
  }
}

function onBackdropClick() {
  if (isInterstitialStep.value) return
  void skipTour()
}

async function goBack() {
  const previousStep = onboarding.activeTour?.steps[onboarding.stepIndex - 1]
  onboarding.previousStep()

  if (previousStep?.route && route.name !== previousStep.route) {
    await router.replace({ name: previousStep.route })
    await nextTick()
  }
}

function onTargetClick() {
  if (onboarding.currentStep?.interstitialStep) return

  if (onboarding.currentStep?.advanceOnTargetClick) {
    void goNext()
  }
}

function onKeydown(event: KeyboardEvent) {
  if (!onboarding.isActive) return

  if (event.key === 'Escape') {
    if (isInterstitialStep.value) return
    event.preventDefault()
    void skipTour()
    return
  }

  if (event.key === 'Enter' && isInterstitialStep.value) {
    event.preventDefault()
    document
      .querySelector<HTMLButtonElement>('[data-onboarding="dashboard-interstitial-continue"]')
      ?.click()
  }
}

let targetElement: HTMLElement | null = null
let spotlightElements: HTMLElement[] = []
let resizeObserver: ResizeObserver | null = null
let bindTargetAttempts = 0

function bindTarget() {
  unbindTarget()

  targetElement = resolveTarget()
  spotlightElements = resolveSpotlightElements()

  if (!targetElement) {
    if (bindTargetAttempts < 20) {
      bindTargetAttempts += 1
      window.setTimeout(bindTarget, 50)
    }
    targetRect.value = null
    return
  }

  bindTargetAttempts = 0

  for (const element of spotlightElements) {
    element.classList.add('onboarding-target--highlighted')
  }

  if (!onboarding.currentStep?.interstitialStep) {
    targetElement.addEventListener('click', onTargetClick)
  }

  updateTargetRect()

  resizeObserver = new ResizeObserver(updateTargetRect)
  for (const element of spotlightElements) {
    resizeObserver.observe(element)
  }
  window.addEventListener('resize', updateTargetRect)
  window.addEventListener('scroll', updateTargetRect, true)
}

function unbindTarget() {
  for (const element of spotlightElements) {
    element.classList.remove('onboarding-target--highlighted')
  }
  spotlightElements = []

  if (targetElement) {
    targetElement.removeEventListener('click', onTargetClick)
    targetElement = null
  }

  resizeObserver?.disconnect()
  resizeObserver = null
  window.removeEventListener('resize', updateTargetRect)
  window.removeEventListener('scroll', updateTargetRect, true)
}

watch(
  () => [onboarding.isActive, onboarding.currentStep?.id, route.name] as const,
  async ([active]) => {
    unbindTarget()
    bindTargetAttempts = 0

    if (!active) {
      targetRect.value = null
      return
    }

    if (onboarding.currentStep?.interstitialStep) {
      dashboardInterstitial.showForOnboarding()
    }

    await ensureStepRoute()
    await nextTick()
    bindTarget()
  },
  { immediate: true },
)

onMounted(() => {
  window.addEventListener('keydown', onKeydown)
})

onUnmounted(() => {
  window.removeEventListener('keydown', onKeydown)
  unbindTarget()
})
</script>

<template>
  <Teleport to="body">
    <div
      v-if="onboarding.isActive && onboarding.currentStep"
      class="onboarding"
      role="presentation"
    >
      <div
        class="onboarding__backdrop"
        :class="{ 'onboarding__backdrop--pass-through': isInterstitialStep }"
        :style="{ clipPath: backdropClipPath }"
        @click="onBackdropClick()"
      />

      <div
        v-if="targetRect"
        class="onboarding__spotlight"
        :style="spotlightStyle"
        aria-hidden="true"
      />

      <div
        v-if="isInterstitialStep && targetRect"
        class="onboarding__click-demo"
        :style="clickDemoStyle"
        aria-hidden="true"
      />

      <div
        ref="tooltipRef"
        class="onboarding__tooltip"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="`onboarding-title-${onboarding.currentStep.id}`"
        :style="tooltipStyle"
        @click.stop
      >
        <p class="onboarding__progress muted">
          Step {{ onboarding.stepProgress.current }} of {{ onboarding.stepProgress.total }}
        </p>
        <h2 :id="`onboarding-title-${onboarding.currentStep.id}`" class="onboarding__title">
          {{ onboarding.currentStep.title }}
        </h2>
        <p class="onboarding__body">{{ onboarding.currentStep.body }}</p>

        <p v-if="isInterstitialStep" class="onboarding__hint muted">
          Click Continue to dashboard to finish the tour and unlock the app.
        </p>

        <div v-if="!isInterstitialStep" class="onboarding__actions">
          <button type="button" class="secondary" @click="skipTour()">
            Skip tour
          </button>
          <div class="onboarding__actions-primary">
            <button
              v-if="onboarding.stepIndex > 0"
              type="button"
              class="secondary"
              @click="goBack()"
            >
              Back
            </button>
            <button type="button" class="primary" @click="goNext()">
              {{
                onboarding.stepProgress.current === onboarding.stepProgress.total
                  ? 'Finish'
                  : 'Next'
              }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>
