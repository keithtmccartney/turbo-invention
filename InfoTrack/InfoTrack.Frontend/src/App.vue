<script setup lang="ts">
import { computed, onMounted, nextTick, watch } from "vue";
import { RouterView, useRoute, useRouter } from "vue-router";
import { useAppStore } from "./stores/app";
import { useOnboardingStore } from "./stores/onboarding";
import { useDashboardInterstitialStore } from "./stores/dashboardInterstitial";
import ThemeToggle from "./components/ThemeToggle.vue";
import SidebarNav from "./components/SidebarNav.vue";
import SidebarFooter from "./components/SidebarFooter.vue";
import HeaderHistory from "./components/HeaderHistory.vue";
import OnboardingTour from "./components/OnboardingTour.vue";
import DashboardInterstitial from "./components/DashboardInterstitial.vue";
import { usePanelShortcuts } from "./composables/usePanelShortcuts";
import { useMainContentFocus } from "./composables/useMainContentFocus";

const route = useRoute();
const router = useRouter();
const store = useAppStore();
const onboarding = useOnboardingStore();
const dashboardInterstitial = useDashboardInterstitialStore();

usePanelShortcuts();
useMainContentFocus();

onMounted(async () => {
  await nextTick();
  onboarding.tryStartInitialTour();
});

watch(
  () => route.name,
  (name) => {
    if (name !== "dashboard" && !dashboardInterstitial.isAccessLocked) {
      dashboardInterstitial.dismiss();
    }
  },
);

function goHome() {
  if (dashboardInterstitial.isAccessLocked) return;

  if (route.name !== "dashboard") {
    router.replace({ name: "dashboard" });
  }
}

const pageTitle = computed(() =>
  String(route.meta.title ?? "Conveyancing Market Intelligence"),
);
const pageSubtitle = computed(() =>
  String(
    route.meta.subtitle ??
      "Solicitor listings scraped from solicitors.com, grouped by location.",
  ),
);

</script>

<template>
  <div class="app-shell" :class="{ 'app-shell--access-locked': dashboardInterstitial.isAccessLocked }">
    <aside class="sidebar">
      <button
        type="button"
        class="brand"
        data-onboarding="brand"
        aria-label="Go to dashboard"
        @click="goHome"
      >
        <img
          src="/infotrack-logo-white.png"
          alt="InfoTrack"
          class="brand-logo"
          width="280"
          height="72"
        />
      </button>
      <SidebarNav />
      <div class="sidebar-illustration" aria-hidden="true">
        <img
          src="/side-panel-property-report.png"
          alt=""
          class="sidebar-illustration__image"
          width="600"
          height="600"
        />
      </div>
      <SidebarFooter />
    </aside>

    <div class="theme-toggle-anchor">
      <ThemeToggle />
    </div>

    <main ref="mainContent" class="content" tabindex="-1">
      <header class="content-header" aria-labelledby="page-title">
        <h1 id="page-title">{{ pageTitle }}</h1>
        <p class="subtitle">{{ pageSubtitle }}</p>
        <HeaderHistory />
      </header>

      <div class="content-body">
        <div v-if="store.error" class="banner error">{{ store.error }}</div>
        <div v-if="store.loading" class="banner">Loading…</div>

        <div class="content-view">
          <RouterView v-slot="{ Component }">
            <component :is="Component" v-if="Component" :key="route.fullPath" />
          </RouterView>
        </div>
      </div>
    </main>

    <OnboardingTour />
    <DashboardInterstitial />
  </div>
</template>
