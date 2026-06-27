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

const locationItems = computed(() =>
  [...store.locations]
    .sort((a, b) => a.name.localeCompare(b.name, undefined, { sensitivity: 'accent' }))
    .map(x => ({ id: x.id, label: x.name })),
)

function sortNames(names: string[]) {
  return [...names].sort((a, b) => a.localeCompare(b, undefined, { sensitivity: 'accent' }))
}

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

function setsEqual(a: Set<string>, b: Set<string>) {
  return a.size === b.size && [...a].every(id => b.has(id))
}

function syncSelectionFromDraft() {
  if (syncing) return

  syncing = true

  const next = new Set<string>()
  for (const location of store.locations) {
    if (draftContainsName(location.name)) {
      next.add(location.id)
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

  let names = parseDraftNames()

  for (const location of store.locations) {
    const selected = selectedLocationIds.value.has(location.id)
    const inDraft = names.some(line => namesMatch(line, location.name))

    if (selected && !inDraft) {
      names.push(location.name)
    } else if (!selected && inDraft) {
      names = names.filter(line => !namesMatch(line, location.name))
    }
  }

  const next = sortNames(names).join('\n')
  if (next !== draft.value) {
    draft.value = next
  }

  syncing = false
}

function applyStoreLocations() {
  syncing = true

  draft.value = sortNames(
    store.locations
      .filter(x => x.isActive)
      .map(x => x.name),
  ).join('\n')

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

    <div class="grid two locations-grid">
      <div class="grid-two__col">
        <Panel title="Scrape locations" :searchable="false" onboarding-target="locations-scrape">
          <p class="muted">
            Edit the list of cities searched during each scrape. One location per line. Changes in Current configuration are reflected here automatically.
          </p>
          <textarea v-model="draft" rows="12" class="location-editor" />
          <div class="actions">
            <button class="primary" @click="save">Save locations</button>
          </div>
        </Panel>
      </div>

      <div class="grid-two__col">
        <Panel title="Current configuration" onboarding-target="locations-config" v-slot="{ searchQuery }">
          <SelectableChipList
            v-model:selected-ids="selectedLocationIds"
            :items="filterBySearch(locationItems, searchQuery)"
            :exclusive-click="Boolean(searchQuery.trim())"
          />
          <p v-if="searchQuery.trim() && !filterBySearch(locationItems, searchQuery).length" class="muted">
            No locations match your search.
          </p>
        </Panel>
      </div>
    </div>
  </section>
</template>
