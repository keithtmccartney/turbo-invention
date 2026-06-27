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
const marqueeBoundaryPanel = ref<HTMLElement | null>(null)

const marquee = ref<{ left: number; top: number; width: number; height: number } | null>(null)
const previewIds = ref<Set<string>>(new Set())
const marqueeActive = ref(false)

const anchorIndex = ref(-1)
const dragThresholdPx = 4
const autoScrollEdgePx = 80
const autoScrollMaxSpeedPx = 16

let pointerOriginClient: { x: number; y: number } | null = null
let originLocal: { x: number; y: number } | null = null
let pendingChipId: string | null = null
let pointerMoved = false
let activePointerId: number | null = null
let marqueeBaseSelection = new Set<string>()
let documentListenersAttached = false
let lastPointerClientX = 0
let lastPointerClientY = 0
let autoScrollRafId: number | null = null

const displaySelectedIds = computed(() => (marqueeActive.value ? previewIds.value : selectedIds.value))

const marqueeStyle = computed(() => {
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

function resolveMarqueeBoundaryPanel() {
  return surfaceRef.value?.closest<HTMLElement>('.panel') ?? null
}

function setMarqueeBoundaryActive(active: boolean) {
  if (active) {
    if (!marqueeBoundaryPanel.value) {
      marqueeBoundaryPanel.value = resolveMarqueeBoundaryPanel()
    }

    marqueeBoundaryPanel.value?.classList.add('panel--marquee-active')
    return
  }

  marqueeBoundaryPanel.value?.classList.remove('panel--marquee-active')
  marqueeBoundaryPanel.value = null
}

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

function elementClientRect(element: HTMLElement) {
  return element.getBoundingClientRect()
}

function getMarqueeBoundaryClientRect() {
  const surface = surfaceRef.value
  if (!surface) return null

  const boundary = surface.closest<HTMLElement>('.panel') ?? surface
  return elementClientRect(boundary)
}

function clientToBoundaryLocal(clientX: number, clientY: number, boundaryRect: DOMRect) {
  return {
    x: Math.min(Math.max(clientX - boundaryRect.left, 0), boundaryRect.width),
    y: Math.min(Math.max(clientY - boundaryRect.top, 0), boundaryRect.height),
  }
}

type LocalRect = { left: number; top: number; width: number; height: number }

function getMarqueeLocalRect(): LocalRect | null {
  if (originLocal === null) return null

  const boundaryRect = getMarqueeBoundaryClientRect()
  if (!boundaryRect || boundaryRect.width <= 0 || boundaryRect.height <= 0) return null

  const current = clientToBoundaryLocal(lastPointerClientX, lastPointerClientY, boundaryRect)

  let left = Math.min(originLocal.x, current.x)
  let top = Math.min(originLocal.y, current.y)
  let right = Math.max(originLocal.x, current.x)
  let bottom = Math.max(originLocal.y, current.y)

  left = Math.max(0, left)
  top = Math.max(0, top)
  right = Math.min(boundaryRect.width, right)
  bottom = Math.min(boundaryRect.height, bottom)

  if (right <= left) {
    right = Math.min(left + 1, boundaryRect.width)
  }

  if (bottom <= top) {
    bottom = Math.min(top + 1, boundaryRect.height)
  }

  if (right <= left || bottom <= top) {
    return null
  }

  return {
    left,
    top,
    width: right - left,
    height: bottom - top,
  }
}

function localRectToClient(local: LocalRect, boundaryRect: DOMRect) {
  return new DOMRect(
    boundaryRect.left + local.left,
    boundaryRect.top + local.top,
    local.width,
    local.height,
  )
}

function getMarqueeClientRect() {
  const boundaryRect = getMarqueeBoundaryClientRect()
  const local = getMarqueeLocalRect()
  if (!local || !boundaryRect) return null

  return localRectToClient(local, boundaryRect)
}

function syncMarquee() {
  const boundaryRect = getMarqueeBoundaryClientRect()
  const local = getMarqueeLocalRect()
  if (!local || !boundaryRect) return

  marquee.value = {
    left: boundaryRect.left + local.left,
    top: boundaryRect.top + local.top,
    width: local.width,
    height: local.height,
  }

  updateMarqueePreview()
}

function getMarqueeHitIds() {
  const marqueeRect = getMarqueeClientRect()
  if (!marqueeRect || marqueeRect.width <= 0 || marqueeRect.height <= 0) return new Set<string>()

  const hitIds = new Set<string>()

  for (const item of props.items) {
    const element = chipElements.value.get(item.id)
    if (!element) continue

    if (rectsIntersect(elementClientRect(element), marqueeRect)) {
      hitIds.add(item.id)
    }
  }

  return hitIds
}

function setsEqual(a: Set<string>, b: Set<string>) {
  return a.size === b.size && [...a].every(id => b.has(id))
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

  if (!setsEqual(preview, previewIds.value)) {
    previewIds.value = preview
  }
}

function chipFromTarget(target: EventTarget | null) {
  return (target as HTMLElement | null)?.closest<HTMLElement>('[data-chip-id]') ?? null
}

function resetDragState() {
  stopAutoScroll()
  setMarqueeBoundaryActive(false)
  pointerOriginClient = null
  originLocal = null
  pendingChipId = null
  pointerMoved = false
  activePointerId = null
  marqueeActive.value = false
  marquee.value = null
  previewIds.value = new Set()
  marqueeBaseSelection = new Set()
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

function performAutoScroll() {
  const clientY = lastPointerClientY
  let scrolled = false

  if (document.documentElement.scrollHeight > document.documentElement.clientHeight + 1) {
    const delta = scrollDeltaForRegion(clientY, 0, window.innerHeight)
    if (delta !== 0) {
      const maxScroll = Math.max(0, document.documentElement.scrollHeight - window.innerHeight)
      const clamped = Math.min(Math.max(window.scrollY + delta, 0), maxScroll)

      if (clamped !== window.scrollY) {
        window.scrollTo({ top: clamped })
        scrolled = true
      }
    }
  }

  for (const container of scrollableAncestors()) {
    if (marqueeBoundaryPanel.value && !container.contains(marqueeBoundaryPanel.value)) continue

    const rect = container.getBoundingClientRect()
    const delta = scrollDeltaForRegion(clientY, rect.top, rect.bottom)
    if (delta === 0) continue

    const maxScroll = Math.max(0, container.scrollHeight - container.clientHeight)
    const clamped = Math.min(Math.max(container.scrollTop + delta, 0), maxScroll)

    if (clamped !== container.scrollTop) {
      container.scrollTop = clamped
      scrolled = true
    }
  }

  return scrolled
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

function autoScrollStep() {
  if (!marqueeActive.value) {
    stopAutoScroll()
    return
  }

  if (performAutoScroll()) {
    syncMarquee()
  }

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
  setMarqueeBoundaryActive(true)
  pendingChipId = null
  startAutoScroll()

  syncMarquee()
  if (performAutoScroll()) {
    syncMarquee()
  }
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
  const boundaryRect = getMarqueeBoundaryClientRect()
  originLocal = boundaryRect
    ? clientToBoundaryLocal(event.clientX, event.clientY, boundaryRect)
    : { x: event.clientX, y: event.clientY }
  lastPointerClientX = event.clientX
  lastPointerClientY = event.clientY
  pendingChipId = chip?.dataset.chipId ?? null
  pointerMoved = false
  activePointerId = event.pointerId
  marqueeBoundaryPanel.value = resolveMarqueeBoundaryPanel()
  marqueeActive.value = false
  marquee.value = null
  marqueeBaseSelection = new Set(selectedIds.value)
  previewIds.value = new Set(marqueeBaseSelection)

  attachDocumentListeners()
  event.preventDefault()
}

onUnmounted(() => {
  stopAutoScroll()
  setMarqueeBoundaryActive(false)
  detachDocumentListeners()
})

const selectedCount = computed(() => selectedIds.value.size)

function selectAll() {
  selectedIds.value = new Set(props.items.map(item => item.id))
  anchorIndex.value = -1
}

function selectNone() {
  selectedIds.value = new Set()
  anchorIndex.value = -1
}
</script>

<template>
  <div class="selectable-chip-list">
    <div v-if="items.length" class="selectable-chip-list__hint-row">
      <p class="selectable-chip-list__hint muted">
        Click to select or deselect. Shift+click a range from the anchor. Ctrl+click to add or remove. Drag to toggle chips in the selection area.
        <span v-if="selectedCount">({{ selectedCount }} selected)</span>
      </p>
      <div class="selectable-chip-list__bulk-actions">
        <button type="button" class="selectable-chip-list__bulk-action" @click="selectAll">
          All
        </button>
        <span class="selectable-chip-list__bulk-separator" aria-hidden="true">·</span>
        <button type="button" class="selectable-chip-list__bulk-action" @click="selectNone">
          None
        </button>
      </div>
    </div>

    <div
      ref="surface"
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
        :style="marqueeStyle"
        aria-hidden="true"
      />
    </Teleport>
  </div>
</template>
