<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, useTemplateRef, watch } from 'vue'
import { panelCount, registerPanel, syncPanelGroupState } from '../utils/panelRegistry'

const props = withDefaults(
  defineProps<{
    title?: string
    defaultExpanded?: boolean
    searchable?: boolean
    onboardingTarget?: string
  }>(),
  {
    defaultExpanded: true,
    searchable: true,
  },
)

const open = ref(props.defaultExpanded)
const searchQuery = ref('')
const searchInput = useTemplateRef<HTMLInputElement>('searchInput')
const shortcutIndex = ref(0)

let unregisterPanel: (() => void) | null = null

const searchPlaceholder = computed(() => {
  if (panelCount.value <= 1) return 'Search… (Press /, Esc to exit)'

  return `Search… (/ and ${shortcutIndex.value}, Esc to switch)`
})

function toggle() {
  open.value = !open.value
}

function focusSearch() {
  open.value = true
  nextTick(() => searchInput.value?.focus())
}

watch(open, syncPanelGroupState, { flush: 'sync' })

onMounted(() => {
  const registration = registerPanel({
    setExpanded: value => {
      open.value = value
      syncPanelGroupState()
    },
    isExpanded: () => open.value,
    ...(props.searchable ? { focusSearch } : {}),
  })

  if (props.searchable) {
    shortcutIndex.value = registration.searchIndex
  }

  unregisterPanel = registration.unregister
})

onUnmounted(() => {
  unregisterPanel?.()
})
</script>

<template>
  <article
    class="panel"
    :class="{ 'panel--collapsed': !open }"
    :data-onboarding="onboardingTarget || undefined"
  >
    <div class="panel__header">
      <div class="panel__heading">
        <slot name="header">
          <h2 v-if="title">{{ title }}</h2>
        </slot>
      </div>
      <button
        type="button"
        class="panel__toggle"
        :aria-expanded="open"
        :aria-label="open ? 'Collapse section' : 'Expand section'"
        @click="toggle"
      >
        <svg
          class="panel__toggle-icon"
          :class="{ 'panel__toggle-icon--open': open }"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <path d="M6 9l6 6 6-6" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
        </svg>
      </button>
    </div>

    <div v-show="open && searchable" class="panel__search-wrap">
      <input
        ref="searchInput"
        v-model="searchQuery"
        type="search"
        class="panel__search"
        :placeholder="searchPlaceholder"
        :aria-label="title ? `Search ${title}` : 'Search panel'"
      />
    </div>

    <div v-show="open" class="panel__body">
      <slot :search-query="searchQuery" />
    </div>
  </article>
</template>
