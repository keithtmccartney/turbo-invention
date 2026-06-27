<script setup lang="ts">
import { nextTick, onMounted, useTemplateRef, watch } from 'vue'
import Panel from '../components/Panel.vue'
import { useAssistantStore } from '../stores/assistant'
import { decodeAssistantText } from '../utils/format'

const assistant = useAssistantStore()
const transcriptRef = useTemplateRef<HTMLElement>('transcript')
const inputRef = useTemplateRef<HTMLTextAreaElement>('assistantInput')

function focusInput() {
  void nextTick(() => {
    void nextTick(() => {
      inputRef.value?.focus({ preventScroll: true })
    })
  })
}

async function scrollToBottom() {
  await nextTick()
  transcriptRef.value?.scrollTo({ top: transcriptRef.value.scrollHeight, behavior: 'smooth' })
}

onMounted(() => {
  focusInput()
  if (assistant.messages.length > 1) {
    void scrollToBottom()
  }
})

watch(
  () => assistant.messages.length,
  () => {
    void scrollToBottom()
  },
)

watch(
  () => assistant.loading,
  loading => {
    if (!loading) {
      focusInput()
    }
  },
)

async function sendMessage() {
  await assistant.sendMessage()
  await scrollToBottom()
}

function onComposerKeydown(event: KeyboardEvent) {
  if (event.key !== 'Enter' || event.shiftKey) return

  event.preventDefault()

  if (assistant.loading || !assistant.draft.trim()) return

  void sendMessage()
}
</script>

<template>
  <Panel
    title="Assistant"
    :searchable="false"
    onboarding-target="assistant-panel"
  >
    <div class="assistant">
      <div ref="transcript" class="assistant__transcript" data-onboarding="assistant-transcript" aria-live="polite">
        <article
          v-for="(message, index) in assistant.messages"
          :key="index"
          class="assistant__message"
          :class="[
            `assistant__message--${message.role}`,
            { 'assistant__message--meta': message.meta === 'tools' },
            { 'assistant__message--intro': message.meta === 'intro' },
          ]"
        >
          <p v-if="message.meta !== 'intro'" class="assistant__role">
            {{ message.role === 'user' ? 'You' : 'Assistant' }}
          </p>
          <p class="assistant__content">{{ decodeAssistantText(message.content) }}</p>
        </article>

        <p v-if="assistant.loading" class="assistant__status">Thinking and querying data…</p>
      </div>

      <p v-if="assistant.error" class="assistant__error" role="alert">{{ assistant.error }}</p>

      <form class="assistant__composer" data-onboarding="assistant-composer" @submit.prevent="sendMessage">
        <label class="visually-hidden" for="assistant-input">Ask a question</label>
        <textarea
          id="assistant-input"
          ref="assistantInput"
          v-model="assistant.draft"
          class="assistant__input"
          rows="3"
          placeholder="Ask about firms, locations, statistics, or recent scrapes…"
          :disabled="assistant.loading"
          @keydown="onComposerKeydown"
        />
        <button type="submit" class="assistant__send" :disabled="assistant.loading || !assistant.draft.trim()">
          Send
        </button>
      </form>
    </div>
  </Panel>
</template>
