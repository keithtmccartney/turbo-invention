<script setup lang="ts">
import { ref } from 'vue'
import { useAppStore } from '../stores/app'

const store = useAppStore()
const legalOpen = ref(false)

function toggleLegal() {
  legalOpen.value = !legalOpen.value
}
</script>

<template>
  <div class="sidebar-footer">
    <button
      type="button"
      class="sidebar-footer__scrape-btn"
      data-onboarding="run-scrape"
      :disabled="store.scraping || !store.canRunScrape"
      :title="store.canRunScrape ? undefined : 'Add at least one location on the Locations page before running a scrape.'"
      @click="store.runScrape()"
    >
      {{ store.scraping ? 'Scraping…' : 'Run Scrape' }}
    </button>

    <div class="sidebar-footer__legal-wrap">
      <button
        type="button"
        class="sidebar-footer__legal-toggle"
        :aria-expanded="legalOpen"
        aria-controls="sidebar-legal-panel"
        @click="toggleLegal"
      >
        <span>Legal &amp; accreditations</span>
        <svg
          class="sidebar-footer__legal-toggle-icon"
          :class="{ 'sidebar-footer__legal-toggle-icon--open': legalOpen }"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <path d="M6 9l6 6 6-6" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
        </svg>
      </button>

      <div v-show="legalOpen" id="sidebar-legal-panel" class="sidebar-footer__legal">
        <div class="sidebar-footer__copyright">
          <p>© 2025 InfoTrack Limited. All Rights Reserved</p>
          <p>
            InfoTrack Limited is a private limited company, incorporated in England and Wales with company no.
            09474590, whose registered office is at 10 John Street, London, England, WC1N 2EB.
          </p>
          <p>
            InfoTrack Limited is authorised and regulated by the Financial Conduct Authority for insurance backed
            products under registration number 914586.
          </p>
        </div>
        <div class="sidebar-footer__row">
          <ul class="sidebar-footer__logos">
            <li>
              <img src="/footer-logos/copso-logo-white.svg" alt="Copso Executive Member" loading="lazy" />
            </li>
            <li>
              <img
                src="/footer-logos/os-licensedpartner-logo.svg"
                alt="Ordnance Survey Licensed Partner"
                loading="lazy"
              />
            </li>
            <li>
              <img src="/footer-logos/searchcode-logo-white.svg" alt="Searchcode Logo" loading="lazy" />
            </li>
            <li>
              <img
                src="/footer-logos/property-ombudsman-logo-white.svg"
                alt="Property Ombudsman Logo"
                loading="lazy"
              />
            </li>
            <li>
              <img
                src="/footer-logos/conveyancing-association-white-logo.svg"
                alt="Conveyancing association logo"
                loading="lazy"
              />
            </li>
            <li>
              <img
                src="/footer-logos/kae-2024-emblem-white.svg"
                alt="King's Award for Enterprise 2024 emblem"
                loading="lazy"
              />
            </li>
          </ul>
          <div class="sidebar-footer__social">
            <a
              class="sidebar-footer__social-link"
              href="https://www.linkedin.com/company/infotrack-uk"
              target="_blank"
              rel="noopener noreferrer"
              aria-label="LinkedIn"
            >
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path
                  d="M20.5 2h-17A1.5 1.5 0 002 3.5v17A1.5 1.5 0 003.5 22h17a1.5 1.5 0 001.5-1.5v-17A1.5 1.5 0 0020.5 2zM8 19H5v-9h3zM6.5 8.25A1.75 1.75 0 118.25 6.5 1.75 1.75 0 016.5 8.25zM19 19h-3v-4.6c0-1.1-.02-2.5-1.52-2.5-1.52 0-1.75 1.18-1.75 2.4V19h-3v-9h2.9v1.24h.04a3.18 3.18 0 012.88-1.58c3.08 0 3.65 2.03 3.65 4.66V19z"
                />
              </svg>
            </a>
            <a
              class="sidebar-footer__social-link"
              href="https://www.youtube.com/channel/UCR73nJqNX-dO-7EuxW_lbwQ"
              target="_blank"
              rel="noopener noreferrer"
              aria-label="YouTube"
            >
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path
                  d="M21.6 7.2a2.5 2.5 0 00-1.76-1.77C18.09 5 12 5 12 5s-6.09 0-7.84.43A2.5 2.5 0 002.4 7.2 26.3 26.3 0 002 12a26.3 26.3 0 00.4 4.8 2.5 2.5 0 001.76 1.77C5.91 19 12 19 12 19s6.09 0 7.84-.43a2.5 2.5 0 001.76-1.77A26.3 26.3 0 0022 12a26.3 26.3 0 00-.4-4.8zM10 15.5v-7l6 3.5-6 3.5z"
                />
              </svg>
            </a>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
