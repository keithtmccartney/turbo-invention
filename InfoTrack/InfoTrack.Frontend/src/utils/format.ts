const locale = 'en-GB'

export function formatDateTime(value: string | Date): string {
  const date = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(date.getTime())) return '—'

  // dateStyle/timeStyle cannot be combined with timeZoneName in Intl.DateTimeFormat.
  return date.toLocaleString(locale, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    timeZoneName: 'short',
  })
}

export function formatSignalType(signalType: string): string {
  return signalType
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .toLowerCase()
    .replace(/^./, char => char.toUpperCase())
}

export function formatDuration(milliseconds?: number | null): string {
  if (milliseconds == null) return '—'

  if (milliseconds < 1000) return `${milliseconds}ms`

  const seconds = milliseconds / 1000
  if (seconds < 60) return `${seconds.toFixed(1)}s`

  const minutes = Math.floor(seconds / 60)
  const remainingSeconds = Math.round(seconds % 60)
  return `${minutes}m ${remainingSeconds}s`
}

export function decodeAssistantText(text: string): string {
  return text
    .replace(/\\u([0-9a-fA-F]{4})/g, (_, hex: string) => String.fromCharCode(parseInt(hex, 16)))
    .replace(/&nbsp;?/gi, ' ')
    .replace(/&#160;|&#xA0;/gi, ' ')
    .replace(/&amp;/gi, '&')
    .replace(/&quot;/gi, '"')
    .replace(/&#39;/gi, "'")
    .replace(/&lt;/gi, '<')
    .replace(/&gt;/gi, '>')
    .replace(/\u00A0/g, ' ')
}
