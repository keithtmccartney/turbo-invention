<script setup lang="ts">
import { useRoute, useRouter } from "vue-router";
import { useDashboardInterstitialStore } from "../stores/dashboardInterstitial";

const route = useRoute();
const router = useRouter();
const dashboardInterstitial = useDashboardInterstitialStore();

const items = [
  { name: "dashboard", label: "Dashboard" },
  { name: "discovery", label: "Discovery" },
  { name: "locations", label: "Locations" },
  { name: "results", label: "Results" },
  { name: "insights", label: "Insights" },
  { name: "assistant", label: "Assistant" },
] as const;

function isActive(name: (typeof items)[number]["name"]) {
  return route.name === name;
}

function navigate(name: (typeof items)[number]["name"]) {
  if (dashboardInterstitial.isAccessLocked) return;

  if (route.name !== name) {
    router.replace({ name });
  }
}
</script>

<template>
  <nav class="sidebar-nav" data-onboarding="sidebar-nav" aria-label="Main">
    <div class="sidebar-nav__breadcrumb" data-onboarding="brand-breadcrumb">
      <a
        class="sidebar-nav__breadcrumb-link"
        href="https://www.infotrack.co.uk/solutions/"
        target="_blank"
        rel="noopener noreferrer"
        >Products</a
      >
      <span class="sidebar-nav__breadcrumb-sep" aria-hidden="true"> / </span>
      <a
        class="sidebar-nav__breadcrumb-link"
        href="https://www.infotrack.co.uk/solutions/conveyancing/"
        target="_blank"
        rel="noopener noreferrer"
        >Conveyancing</a
      >
      <span class="sidebar-nav__breadcrumb-sep" aria-hidden="true"> / </span>
      <span class="sidebar-nav__breadcrumb-current" aria-current="page"
        >Solicitor Intelligence</span
      >
    </div>

    <ul class="sidebar-nav__links">
      <li v-for="item in items" :key="item.name">
        <button
          type="button"
          class="sidebar-nav__link"
          :class="{ 'sidebar-nav__link--active': isActive(item.name) }"
          :aria-current="isActive(item.name) ? 'page' : undefined"
          :data-onboarding="`nav-${item.name}`"
          @click="navigate(item.name)"
        >
          {{ item.label }}
        </button>
      </li>
    </ul>
  </nav>
</template>
