<script setup lang="ts">
import { computed, onUnmounted, ref, useTemplateRef } from 'vue'

export type SelectableChipItem = {
  id: string
  label: string
}

const props = defineProps<{
  items: SelectableChipItem[]
  exclusiveClick?: boolean
}>()

const selectedIds = defineModel<Set<string>>('selectedIds', { default: () => new Set<string>() })

const surfaceRef = useTemplateRef<HTMLDivElement>('surface')
const chipElements = ref(new Map<string, HTMLElement>())

const marquee = ref<{ left: number; top: number; width: number; height: number } | null>(null)
const previewIds = ref<Set<string>>(new Set())
const marqueeActive = ref(false)

const anchorIndex = ref(-1)
const dragThresholdPx = 4
const autoScrollEdgePx = 80
const autoScrollMaxSpeedPx = 16

let pointerOriginClient: { x: number; y: number } | null = null
let originPage: { x: number; y: number } | null = null
let currentPage: { x: number; y: number } | null = null
let pendingChipId: string | null = null
let pointerMoved = false
let activePointerId: number | null = null
let marqueeBaseSelection = new Set<string>()
let documentListenersAttached = false
let lastPointerClientX = 0
let lastPointerClientY = 0
let autoScrollRafId: number | null = null

const displaySelectedIds = computed(() => (marqueeActive.value ? previewIds.value : selectedIds.value))

const fixedMarqueeStyle = computed(() => {
  if (!marquee.value) {
    return {}
  }

  return {
    left: `${marquee.value.left}px`,
    top: `${marquee.value.top}px`,
    width: `${marquee.value.width}px`,
    height: `${marquee.value.height}px`,
  }
})

function setChipElement(id: string, element: HTMLElement | null) {
  if (element) {
    chipElements.value.set(id, element)
    return
  }

  chipElements.value.delete(id)
}

function itemIndex(id: string) {
  return props.items.findIndex(item => item.id === id)
}

function isSelected(id: string) {
  return displaySelectedIds.value.has(id)
}

function idsInRange(fromIndex: number, toIndex: number) {
  const start = Math.min(fromIndex, toIndex)
  const end = Math.max(fromIndex, toIndex)

  return new Set(props.items.slice(start, end + 1).map(item => item.id))
}

function toggleChip(id: string) {
  const next = new Set(selectedIds.value)

  if (next.has(id)) {
    next.delete(id)
  } else {
    next.add(id)
  }

  selectedIds.value = next
}

function handleChipSelection(id: string, shiftKey: boolean, ctrlKey: boolean) {
  const index = itemIndex(id)
  if (index < 0) return

  if (shiftKey) {
    if (anchorIndex.value < 0) {
      anchorIndex.value = index
    }

    selectedIds.value = idsInRange(anchorIndex.value, index)
    return
  }

  if (ctrlKey) {
    toggleChip(id)
    anchorIndex.value = index
    return
  }

  if (props.exclusiveClick) {
    selectedIds.value = new Set([id])
    anchorIndex.value = index
    return
  }

  if (selectedIds.value.has(id)) {
    const next = new Set(selectedIds.value)
    next.delete(id)
    selectedIds.value = next
  } else {
    selectedIds.value = new Set([id])
  }

  anchorIndex.value = index
}

function rectsIntersect(a: DOMRect, b: DOMRect) {
  return a.left < b.right && a.right > b.left && a.top < b.bottom && a.bottom > b.top
}

function pointerPageCoords(clientX: number, clientY: number) {
  return {
    x: clientX + window.scrollX,
    y: clientY + window.scrollY,
  }
}

function elementPageRect(element: HTMLElement) {
  const client = element.getBoundingClientRect()
  return new DOMRect(
    client.left + window.scrollX,
    client.top + window.scrollY,
    client.width,
    client.height,
  )
}

function getMarqueePageRect() {
  if (!originPage || !currentPage) return null

  const left = Math.min(originPage.x, currentPage.x)
  const top = Math.min(originPage.y, currentPage.y)

  return new DOMRect(
    left,
    top,
    Math.max(Math.abs(currentPage.x - originPage.x), 1),
    Math.max(Math.abs(currentPage.y - originPage.y), 1),
  )
}

function updateCurrentPageFromPointer(pageX: number, pageY: number) {
  currentPage = { x: pageX, y: pageY }
}

function syncMarquee() {
  const pageRect = getMarqueePageRect()
  if (!pageRect) return

  marquee.value = {
    left: pageRect.left - window.scrollX,
    top: pageRect.top - window.scrollY,
    width: pageRect.width,
    height: pageRect.height,
  }

  updateMarqueePreview()
}

function getMarqueeHitIds() {
  const marqueeRect = getMarqueePageRect()
  if (!marqueeRect) return new Set<string>()

  const hitIds = new Set<string>()

  for (const item of props.items) {
    const element = chipElements.value.get(item.id)
    if (!element) continue

    if (rectsIntersect(elementPageRect(element), marqueeRect)) {
      hitIds.add(item.id)
    }
  }

  return hitIds
}

function updateMarqueePreview() {
  const hitIds = getMarqueeHitIds()
  const preview = new Set(marqueeBaseSelection)

  for (const id of hitIds) {
    if (preview.has(id)) {
      preview.delete(id)
    } else {
      preview.add(id)
    }
  }

  previewIds.value = preview
}

function chipFromTarget(target: EventTarget | null) {
  return (target as HTMLElement | null)?.closest<HTMLElement>('[data-chip-id]') ?? null
}

function resetDragState() {
  stopAutoScroll()
  pointerOriginClient = null
  originPage = null
  currentPage = null
  pendingChipId = null
  pointerMoved = false
  activePointerId = null
  marqueeActive.value = false
  marquee.value = null
  previewIds.value = new Set()
  marqueeBaseSelection = new Set()
}

function syncCurrentPageFromClientPointer() {
  const page = pointerPageCoords(lastPointerClientX, lastPointerClientY)
  updateCurrentPageFromPointer(page.x, page.y)
}

function scrollDeltaForRegion(clientY: number, regionTop: number, regionBottom: number) {
  const height = regionBottom - regionTop
  if (height <= 0) return 0

  const relativeY = clientY - regionTop

  if (relativeY < autoScrollEdgePx) {
    return -autoScrollMaxSpeedPx * (1 - Math.max(0, relativeY) / autoScrollEdgePx)
  }

  if (relativeY > height - autoScrollEdgePx) {
    return autoScrollMaxSpeedPx * (1 - Math.max(0, height - relativeY) / autoScrollEdgePx)
  }

  return 0
}

function scrollableAncestors() {
  const containers: HTMLElement[] = []
  let node = surfaceRef.value?.parentElement ?? null

  while (node) {
    const { overflowY } = getComputedStyle(node)
    if (/(auto|scroll|overlay)/.test(overflowY) && node.scrollHeight > node.clientHeight + 1) {
      containers.push(node)
    }
    node = node.parentElement
  }

  return containers
}

function performAutoScroll() {
  const clientY = lastPointerClientY

  const documentScrollable =
    document.documentElement.scrollHeight > document.documentElement.clientHeight + 1

  if (documentScrollable) {
    const delta = scrollDeltaForRegion(clientY, 0, window.innerHeight)
    if (delta !== 0) {
      window.scrollBy(0, delta)
    }
  }

  for (const container of scrollableAncestors()) {
    const rect = container.getBoundingClientRect()
    const delta = scrollDeltaForRegion(clientY, rect.top, rect.bottom)
    if (delta !== 0) {
      container.scrollTop += delta
    }
  }
}

function autoScrollStep() {
  if (!marqueeActive.value) {
    stopAutoScroll()
    return
  }

  performAutoScroll()
  syncCurrentPageFromClientPointer()
  syncMarquee()
  autoScrollRafId = requestAnimationFrame(autoScrollStep)
}

function startAutoScroll() {
  if (autoScrollRafId != null) return
  autoScrollRafId = requestAnimationFrame(autoScrollStep)
}

function stopAutoScroll() {
  if (autoScrollRafId == null) return
  cancelAnimationFrame(autoScrollRafId)
  autoScrollRafId = null
}

function detachDocumentListeners() {
  if (!documentListenersAttached) return

  document.removeEventListener('pointermove', onDocumentPointerMove)
  document.removeEventListener('pointerup', onDocumentPointerUp)
  document.removeEventListener('pointercancel', onDocumentPointerCancel)
  documentListenersAttached = false
}

function attachDocumentListeners() {
  if (documentListenersAttached) return

  document.addEventListener('pointermove', onDocumentPointerMove)
  document.addEventListener('pointerup', onDocumentPointerUp)
  document.addEventListener('pointercancel', onDocumentPointerCancel)
  documentListenersAttached = true
}

function onDocumentPointerMove(event: PointerEvent) {
  if (!pointerOriginClient || activePointerId !== event.pointerId) return

  lastPointerClientX = event.clientX
  lastPointerClientY = event.clientY

  const deltaX = event.clientX - pointerOriginClient.x
  const deltaY = event.clientY - pointerOriginClient.y

  if (!pointerMoved && Math.hypot(deltaX, deltaY) < dragThresholdPx) return

  pointerMoved = true
  marqueeActive.value = true
  pendingChipId = null
  startAutoScroll()

  updateCurrentPageFromPointer(event.pageX, event.pageY)
  syncMarquee()
  performAutoScroll()
  syncCurrentPageFromClientPointer()
  syncMarquee()
  event.preventDefault()
}

function finishPointerInteraction(event: PointerEvent) {
  if (!pointerOriginClient || activePointerId !== event.pointerId) return

  if (marqueeActive.value) {
    selectedIds.value = new Set(previewIds.value)

    const previewIndexes = props.items
      .map((item, index) => (previewIds.value.has(item.id) ? index : -1))
      .filter(index => index >= 0)

    if (previewIndexes.length) {
      anchorIndex.value = Math.min(...previewIndexes)
    }
  } else if (!pointerMoved && pendingChipId) {
    handleChipSelection(pendingChipId, event.shiftKey, event.ctrlKey || event.metaKey)
  } else if (!pointerMoved && !pendingChipId) {
    selectedIds.value = new Set()
    anchorIndex.value = -1
  }

  resetDragState()
  detachDocumentListeners()
}

function onDocumentPointerUp(event: PointerEvent) {
  finishPointerInteraction(event)
}

function onDocumentPointerCancel(event: PointerEvent) {
  if (!pointerOriginClient || activePointerId !== event.pointerId) return

  resetDragState()
  detachDocumentListeners()
}

function onPointerDown(event: PointerEvent) {
  if (event.button !== 0) return

  const chip = chipFromTarget(event.target)
  pointerOriginClient = {
    x: event.clientX,
    y: event.clientY,
  }
  originPage = { x: event.pageX, y: event.pageY }
  currentPage = { x: event.pageX, y: event.pageY }
  lastPointerClientX = event.clientX
  lastPointerClientY = event.clientY
  pendingChipId = chip?.dataset.chipId ?? null
  pointerMoved = false
  activePointerId = event.pointerId
  marqueeActive.value = false
  marquee.value = null
  marqueeBaseSelection = new Set(selectedIds.value)
  previewIds.value = new Set(marqueeBaseSelection)

  attachDocumentListeners()
  event.preventDefault()
}

onUnmounted(() => {
  stopAutoScroll()
  detachDocumentListeners()
})

const selectedCount = computed(() => selectedIds.value.size)
</script>

<template>
  <div class="selectable-chip-list">
    <p v-if="items.length" class="selectable-chip-list__hint muted">
      Click to select or deselect. Shift+click a range from the anchor. Ctrl+click to add or remove. Drag to toggle chips in the selection area.
      <span v-if="selectedCount">({{ selectedCount }} selected)</span>
    </p>

    <div
      ref="surfaceRef"
      class="selectable-chip-list__surface"
      :class="{ 'selectable-chip-list__surface--dragging': marqueeActive }"
      @pointerdown="onPointerDown"
    >
      <ul class="chip-list selectable-chip-list__items">
        <li
          v-for="item in items"
          :key="item.id"
          :ref="element => setChipElement(item.id, element as HTMLElement | null)"
          class="chip"
          :class="{ 'chip--selected': isSelected(item.id) }"
          :data-chip-id="item.id"
        >
          {{ item.label }}
        </li>
      </ul>
    </div>

    <Teleport to="body">
      <div
        v-if="marqueeActive && marquee"
        class="selectable-chip-list__marquee"
        :style="fixedMarqueeStyle"
        aria-hidden="true"
      />
    </Teleport>
  </div>
</template>
