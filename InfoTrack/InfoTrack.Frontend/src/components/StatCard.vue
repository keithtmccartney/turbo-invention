<script setup lang="ts">
defineProps<{ label: string; value: string | number; accent?: string }>()
</script>

<template>
  <article class="stat-card" :data-accent="accent">
    <p>{{ label }}</p>
    <strong>{{ value }}</strong>
  </article>
</template>
