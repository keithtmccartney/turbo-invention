import { nextTick, onMounted, useTemplateRef } from 'vue'
import { useRouter } from 'vue-router'

export function useMainContentFocus() {
  const router = useRouter()
  const mainContent = useTemplateRef<HTMLElement>('mainContent')

  function focusMainContent() {
    void nextTick(() => {
      void nextTick(() => {
        mainContent.value?.focus({ preventScroll: true })
      })
    })
  }

  function shouldFocusMainContent() {
    return router.currentRoute.value.name !== 'assistant'
  }

  onMounted(() => {
    void router.isReady().then(() => {
      if (shouldFocusMainContent()) {
        focusMainContent()
      }
    })
  })

  router.afterEach(() => {
    if (shouldFocusMainContent()) {
      focusMainContent()
    }
  })
}
