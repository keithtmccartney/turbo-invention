import axios from 'axios'
import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { chatApi, type ChatMessageDto } from '../api/client'

export type AssistantDisplayMessage = ChatMessageDto & { meta?: 'intro' | 'tools' }

export type AssistantQueryEntry = {
  askedAt: string
  question: string
}

const intro: AssistantDisplayMessage = {
  role: 'assistant',
  meta: 'intro',
  content:
    'Ask me about your conveyancing solicitor data — for example: "How many firms do we have?" or "Search for firms named Smith in London". I will query live data through InfoTrack tools.',
}

export const useAssistantStore = defineStore('assistant', () => {
  const messages = ref<AssistantDisplayMessage[]>([intro])
  const conversationHistory = ref<ChatMessageDto[]>([])
  const queryHistory = ref<AssistantQueryEntry[]>([])
  const draft = ref('')
  const loading = ref(false)
  const error = ref<string | null>(null)

  const queryCount = computed(() => queryHistory.value.length)
  const lastQueryAt = computed(() => queryHistory.value.at(-1)?.askedAt)
  const hasQueryHistory = computed(() => queryCount.value > 0)

  function readApiErrorMessage(e: unknown, fallback: string) {
    if (axios.isAxiosError(e)) {
      const data = e.response?.data as {
        title?: string
        detail?: string
        reply?: string
        errors?: Record<string, string[]>
      } | undefined

      const validationMessage = data?.errors?.['']?.[0]
      if (validationMessage) return validationMessage
      if (data?.reply) return data.reply
      if (data?.detail) return data.detail
      if (data?.title && data.title !== 'One or more validation errors occurred.') return data.title
    }

    return e instanceof Error ? e.message : fallback
  }

  async function sendMessage() {
    const text = draft.value.trim()
    if (!text || loading.value) return

    error.value = null
    loading.value = true

    const askedAt = new Date().toISOString()
    const userMessage: ChatMessageDto = { role: 'user', content: text }
    messages.value.push(userMessage)
    conversationHistory.value.push(userMessage)
    queryHistory.value.push({ askedAt, question: text })
    draft.value = ''

    try {
      const result = await chatApi.send(conversationHistory.value)
      const assistantMessage: ChatMessageDto = { role: 'assistant', content: result.reply }
      messages.value.push(assistantMessage)
      conversationHistory.value.push(assistantMessage)

      if (result.toolsInvoked.length > 0) {
        messages.value.push({
          role: 'assistant',
          meta: 'tools',
          content: `Tools used: ${result.toolsInvoked.join(', ')}`,
        })
      }
    } catch (err) {
      conversationHistory.value.pop()
      messages.value.pop()
      queryHistory.value.pop()
      error.value = readApiErrorMessage(err, 'Unable to reach the assistant.')
    } finally {
      loading.value = false
    }
  }

  return {
    messages,
    conversationHistory,
    queryHistory,
    draft,
    loading,
    error,
    queryCount,
    lastQueryAt,
    hasQueryHistory,
    sendMessage,
  }
})
