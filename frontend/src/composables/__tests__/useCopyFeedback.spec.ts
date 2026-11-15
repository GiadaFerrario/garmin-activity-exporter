import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope } from 'vue'
import { useCopyFeedback } from '@/composables/useCopyFeedback.ts'

describe('useCopyFeedback', () => {
  const writeText = vi.fn<(text: string) => Promise<void>>()

  beforeEach(() => {
    vi.useFakeTimers()
    writeText.mockReset()
    vi.stubGlobal('navigator', { clipboard: { writeText } })
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
  })

  // The composable cleans up with the scope it lives in (a component, here a scope made by hand)
  function create(durationMs?: number) {
    const scope = effectScope()
    const feedback = scope.run(() => useCopyFeedback(durationMs))!
    return { feedback, scope }
  }

  it('starts without a message', () => {
    expect(create().feedback.message.value).toBeNull()
  })

  it('copies the text and says so', async () => {
    writeText.mockResolvedValue()
    const { feedback } = create()

    await feedback.copy('the text')

    expect(writeText).toHaveBeenCalledWith('the text')
    expect(feedback.message.value).toBe('Copied to clipboard')
  })

  it('says it could not copy when the browser refuses', async () => {
    writeText.mockRejectedValue(new Error('denied'))
    const { feedback } = create()

    await feedback.copy('the text')

    expect(feedback.message.value).toBe('Could not copy the text')
  })

  it('hides the message after two seconds', async () => {
    writeText.mockResolvedValue()
    const { feedback } = create()
    await feedback.copy('the text')

    vi.advanceTimersByTime(1999)
    expect(feedback.message.value).not.toBeNull()
    vi.advanceTimersByTime(1)
    expect(feedback.message.value).toBeNull()
  })

  it('keeps the message for the time it is asked for', async () => {
    writeText.mockResolvedValue()
    const { feedback } = create(500)
    await feedback.copy('the text')

    vi.advanceTimersByTime(499)
    expect(feedback.message.value).not.toBeNull()
    vi.advanceTimersByTime(1)
    expect(feedback.message.value).toBeNull()
  })

  it('restarts the time when a new message replaces the old one', async () => {
    writeText.mockResolvedValue()
    const { feedback } = create()
    await feedback.copy('one')
    vi.advanceTimersByTime(1500)

    await feedback.copy('two')
    vi.advanceTimersByTime(1500)

    // 3 s since the first copy, but only 1.5 s since the second one: still visible
    expect(feedback.message.value).toBe('Copied to clipboard')
    vi.advanceTimersByTime(500)
    expect(feedback.message.value).toBeNull()
  })

  it('reset hides the message at once and stops the timer', async () => {
    writeText.mockResolvedValue()
    const { feedback } = create()
    await feedback.copy('the text')

    feedback.reset()

    expect(feedback.message.value).toBeNull()
    expect(vi.getTimerCount()).toBe(0)
  })

  it('stops its timer when the component that uses it goes away', async () => {
    writeText.mockResolvedValue()
    const { feedback, scope } = create()
    await feedback.copy('the text')

    scope.stop()

    expect(vi.getTimerCount()).toBe(0)
  })

  it('says it could not copy when the browser has no clipboard', async () => {
    vi.stubGlobal('navigator', {})
    const { feedback } = create()

    await feedback.copy('the text')

    expect(feedback.message.value).toBe('Could not copy the text')
  })
})
