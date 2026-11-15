import { onScopeDispose, ref } from 'vue'

// Copies a text to the clipboard and keeps a short message about it ("Copied to clipboard"), that goes
// away by itself. A composable: the logic lives here and every screen that needs it calls it in its setup.
export function useCopyFeedback(messageDurationMs = 2000) {
  // null = nothing to show
  const message = ref<string | null>(null)
  // The timer that hides the message: kept so that a new message can cancel the previous timer
  let timer: ReturnType<typeof setTimeout> | undefined

  function show(text: string) {
    clearTimeout(timer)
    message.value = text
    timer = setTimeout(() => (message.value = null), messageDurationMs)
  }

  async function copy(text: string) {
    try {
      await navigator.clipboard.writeText(text)
      show('Copied to clipboard')
    } catch {
      // the browser refused (permission denied, insecure context, no clipboard...)
      show('Could not copy the text')
    }
  }

  function reset() {
    clearTimeout(timer)
    message.value = null
  }

  // No timer left behind when the component that uses this goes away
  onScopeDispose(() => clearTimeout(timer))

  return { message, copy, reset }
}
