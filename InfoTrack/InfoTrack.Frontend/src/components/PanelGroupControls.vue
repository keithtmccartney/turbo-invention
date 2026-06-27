<script setup lang="ts">
import { computed } from 'vue'
import {
  collapsiblePanelCount,
  showExpandAll,
  toggleAllPanels,
} from '../utils/panelRegistry'

const label = computed(() => (showExpandAll.value ? 'Expand all' : 'Collapse all'))
</script>

<template>
  <div v-if="collapsiblePanelCount > 1" class="panel-group-controls">
    <button type="button" class="secondary panel-group-controls__btn" @click="toggleAllPanels">
      {{ label }}
    </button>
  </div>
</template>
