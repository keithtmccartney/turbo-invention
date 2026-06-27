<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import Panel from '../components/Panel.vue'
import PanelGroupControls from '../components/PanelGroupControls.vue'
import SelectableChipList from '../components/SelectableChipList.vue'
import { filterBySearch } from '../utils/panelSearch'
import { useAppStore } from '../stores/app'

const store = useAppStore()
const draft = ref('')
const selectedLocationIds = ref(new Set<string>())

let syncing = false

const activeLocations = computed(() =>
  store.locations
    .filter(x => x.isActive)
    .map(x => ({ id: x.id, label: x.name })),
)

const locationById = computed(() => new Map(store.locations.map(location => [location.id, location])))

function parseDraftNames() {
  return draft.value
    .split('\n')
    .map(line => line.trim())
    .filter(Boolean)
}

function namesMatch(a: string, b: string) {
  return a.localeCompare(b, undefined, { sensitivity: 'accent' }) === 0
}

function draftContainsName(name: string) {
  return parseDraftNames().some(line => namesMatch(line, name))
}

function removeNameFromDraft(name: string) {
  const names = parseDraftNames().filter(line => !namesMatch(line, name))
  draft.value = names.join('\n')
}

function addNameToDraft(name: string) {
  if (draftContainsName(name)) return

  const names = parseDraftNames()
  draft.value = names.length ? `${names.join('\n')}\n${name}` : name
}

function setsEqual(a: Set<string>, b: Set<string>) {
  return a.size === b.size && [...a].every(id => b.has(id))
}

function syncSelectionFromDraft() {
  if (syncing) return

  syncing = true

  const next = new Set<string>()
  for (const item of activeLocations.value) {
    const location = locationById.value.get(item.id)
    if (location && draftContainsName(location.name)) {
      next.add(item.id)
    }
  }

  if (!setsEqual(selectedLocationIds.value, next)) {
    selectedLocationIds.value = next
  }

  syncing = false
}

function syncDraftFromSelection() {
  if (syncing) return

  syncing = true

  for (const item of activeLocations.value) {
    const location = locationById.value.get(item.id)
    if (!location) continue

    const selected = selectedLocationIds.value.has(item.id)
    const inDraft = draftContainsName(location.name)

    if (selected && !inDraft) {
      addNameToDraft(location.name)
    } else if (!selected && inDraft) {
      removeNameFromDraft(location.name)
    }
  }

  syncing = false
}

function applyStoreLocations() {
  syncing = true

  draft.value = store.locations
    .filter(x => x.isActive)
    .map(x => x.name)
    .join('\n')

  selectedLocationIds.value = new Set(
    store.locations.filter(x => x.isActive).map(x => x.id),
  )

  syncing = false
}

const selectedLocationKey = computed(() => [...selectedLocationIds.value].sort().join('\0'))

watch(() => store.locations, applyStoreLocations, { immediate: true, deep: true })
watch(selectedLocationKey, syncDraftFromSelection)
watch(draft, syncSelectionFromDraft)

async function save() {
  syncDraftFromSelection()
  await store.saveLocations(parseDraftNames())
}
</script>

<template>
  <section class="page">
    <PanelGroupControls />

    <Panel title="Scrape locations" :searchable="false" onboarding-target="locations-scrape">
      <p class="muted">
        Edit the list of cities searched during each scrape. One location per line. Changes in Current configuration are reflected here automatically.
      </p>
      <textarea v-model="draft" rows="12" class="location-editor" />
      <div class="actions">
        <button class="primary" @click="save">Save locations</button>
      </div>
    </Panel>

    <Panel title="Current configuration" onboarding-target="locations-config" v-slot="{ searchQuery }">
      <SelectableChipList
        v-model:selected-ids="selectedLocationIds"
        :items="filterBySearch(activeLocations, searchQuery)"
        :exclusive-click="Boolean(searchQuery.trim())"
      />
      <p v-if="searchQuery.trim() && !filterBySearch(activeLocations, searchQuery).length" class="muted">
        No locations match your search.
      </p>
    </Panel>
  </section>
</template>
