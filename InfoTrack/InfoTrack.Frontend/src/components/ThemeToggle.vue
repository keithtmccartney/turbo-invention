<script setup lang="ts">
import { useThemeStore } from '../stores/theme'

const theme = useThemeStore()
</script>

<template>
  <button
    class="theme-switch"
    type="button"
    role="switch"
    :aria-checked="theme.mode === 'dark'"
    aria-label="Toggle colour theme"
    @click="theme.toggle()"
  >
    <span class="theme-switch__track" :data-theme="theme.mode">
      <span class="theme-switch__thumb" aria-hidden="true" />
      <span class="theme-switch__option" :class="{ 'is-active': theme.mode === 'light' }">
        <svg viewBox="0 0 24 24" aria-hidden="true">
          <circle cx="12" cy="12" r="4" fill="currentColor" />
          <g stroke="currentColor" stroke-width="2" stroke-linecap="round">
            <path d="M12 2v2" />
            <path d="M12 20v2" />
            <path d="M4.93 4.93l1.41 1.41" />
            <path d="M17.66 17.66l1.41 1.41" />
            <path d="M2 12h2" />
            <path d="M20 12h2" />
            <path d="M4.93 19.07l1.41-1.41" />
            <path d="M17.66 6.34l1.41-1.41" />
          </g>
        </svg>
        <span class="sr-only">Light mode</span>
      </span>
      <span class="theme-switch__option" :class="{ 'is-active': theme.mode === 'dark' }">
        <svg viewBox="0 0 24 24" aria-hidden="true">
          <path
            fill="currentColor"
            d="M21 12.79A9 9 0 1 1 11.21 3a7 7 0 0 0 9.79 9.79z"
          />
        </svg>
        <span class="sr-only">Dark mode</span>
      </span>
    </span>
  </button>
</template>

<style scoped>
.theme-switch {
  padding: 0;
  border: 0;
  background: transparent;
  cursor: pointer;
}

.theme-switch__track {
  position: relative;
  display: grid;
  grid-template-columns: 1fr 1fr;
  align-items: center;
  width: 3.25rem;
  height: 1.75rem;
  padding: 2px;
  border: 1px solid var(--panel-border);
  border-radius: 999px;
  background: var(--surface-subtle);
}

.theme-switch__thumb {
  position: absolute;
  top: 2px;
  left: 2px;
  width: calc(50% - 2px);
  height: calc(100% - 4px);
  border-radius: 999px;
  background: var(--panel);
  box-shadow: var(--shadow);
  transition: transform 0.22s ease;
}

.theme-switch__track[data-theme='dark'] .theme-switch__thumb {
  transform: translateX(calc(100% + 2px));
}

.theme-switch__option {
  position: relative;
  z-index: 1;
  display: grid;
  place-items: center;
  color: var(--muted);
  transition: color 0.22s ease;
}

.theme-switch__option.is-active {
  color: var(--primary);
}

.theme-switch__option svg {
  width: 0.75rem;
  height: 0.75rem;
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}
</style>
