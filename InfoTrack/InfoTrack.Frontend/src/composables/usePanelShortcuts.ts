import { onMounted, onUnmounted } from 'vue'
import { focusPanel, getPanelCount } from '../utils/panelRegistry'

const MULTI_DIGIT_DELAY_MS = 350

function isPanelSearchInput(target: EventTarget | null): target is HTMLInputElement {
  return target instanceof HTMLInputElement && target.classList.contains('panel__search')
}

function isEditableTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false

  const tag = target.tagName
  return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || target.isContentEditable
}

function readDigit(event: KeyboardEvent): string | null {
  if (/^[0-9]$/.test(event.key)) return event.key

  const codeMatch = event.code.match(/^(?:Digit|Numpad)([0-9])$/)
  return codeMatch?.[1] ?? null
}

export function usePanelShortcuts() {
  let slashHeld = false
  let digitBuffer = ''
  let activateTimeout: ReturnType<typeof setTimeout> | null = null

  function clearActivateTimeout() {
    if (activateTimeout !== null) {
      clearTimeout(activateTimeout)
      activateTimeout = null
    }
  }

  function resetChord() {
    slashHeld = false
    digitBuffer = ''
    clearActivateTimeout()
  }

  function activateBuffer() {
    if (!digitBuffer) return

    const index = Number.parseInt(digitBuffer, 10)
    if (index > 0) focusPanel(index)

    resetChord()
  }

  function scheduleActivate() {
    clearActivateTimeout()
    activateTimeout = setTimeout(activateBuffer, MULTI_DIGIT_DELAY_MS)
  }

  function onKeyDown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      if (isPanelSearchInput(event.target)) {
        event.preventDefault()
        event.target.blur()
      }

      if (slashHeld) {
        event.preventDefault()
        resetChord()
      }

      return
    }

    if (isEditableTarget(event.target)) return

    const digit = readDigit(event)

    if (slashHeld && digit && !event.repeat) {
      event.preventDefault()
      clearActivateTimeout()
      digitBuffer += digit
      return
    }

    if ((event.key === '/' || event.code === 'Slash') && !slashHeld && !event.repeat) {
      const count = getPanelCount()
      if (count === 0) return

      event.preventDefault()

      if (count === 1) {
        focusPanel(1)
        return
      }

      slashHeld = true
      digitBuffer = ''
      clearActivateTimeout()
    }
  }

  function onKeyUp(event: KeyboardEvent) {
    if (isEditableTarget(event.target)) return

    const digit = readDigit(event)

    if (slashHeld && digit) {
      event.preventDefault()
      scheduleActivate()
      return
    }

    if (event.key === '/' || event.code === 'Slash') {
      slashHeld = false
      clearActivateTimeout()
      if (digitBuffer) activateBuffer()
    }
  }

  onMounted(() => {
    window.addEventListener('keydown', onKeyDown)
    window.addEventListener('keyup', onKeyUp)
  })

  onUnmounted(() => {
    window.removeEventListener('keydown', onKeyDown)
    window.removeEventListener('keyup', onKeyUp)
    resetChord()
  })
}
