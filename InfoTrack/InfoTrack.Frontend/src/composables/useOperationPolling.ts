import { onScopeDispose, ref } from 'vue'

export type OperationPollOptions<T> = {
  poll: () => Promise<T>
  isTerminal: (status: T) => boolean
  intervalMs?: number
  timeoutMs?: number
  onUpdate?: (status: T) => void
}

export function useOperationPolling<T>() {
  const polling = ref(false)
  const error = ref<string | null>(null)
  let timer: ReturnType<typeof setTimeout> | undefined

  onScopeDispose(() => {
    if (timer) clearTimeout(timer)
  })

  async function pollUntilComplete(options: OperationPollOptions<T>): Promise<T> {
    const intervalMs = options.intervalMs ?? 1000
    const timeoutMs = options.timeoutMs ?? 300_000
    const startedAt = Date.now()

    polling.value = true
    error.value = null

    try {
      while (true) {
        const status = await options.poll()
        options.onUpdate?.(status)

        if (options.isTerminal(status)) {
          return status
        }

        if (Date.now() - startedAt >= timeoutMs) {
          throw new Error('Operation timed out before completion.')
        }

        await new Promise<void>(resolve => {
          timer = setTimeout(resolve, intervalMs)
        })
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Operation polling failed'
      throw e
    } finally {
      polling.value = false
    }
  }

  return { polling, error, pollUntilComplete }
}
