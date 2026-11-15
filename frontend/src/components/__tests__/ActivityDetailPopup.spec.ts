import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import ActivityDetailPopup from '@/components/ActivityDetailPopup.vue'
import type { HistoryActivity } from '@/api/historyActivity.ts'

const run: HistoryActivity = {
  id: 2,
  garminActivityId: 25053166010,
  name: 'Morning run',
  sport: 'Run',
  startTime: '2026-11-12T10:45:57+00:00',
  durationInSeconds: 2351.18,
  distanceInMeters: 4864.75,
  elevationGain: 120,
  avgHR: 162,
  maxHR: 178,
  calories: 410,
  hasCoachText: true,
  text: null,
}

const hiit: HistoryActivity = {
  ...run,
  name: 'HIIT session',
  sport: 'Hiit',
  distanceInMeters: null,
  elevationGain: null,
  hasCoachText: false,
}

function create(
  activity: HistoryActivity,
  text: string | null,
  loading = false,
  error: string | null = null,
) {
  return mount(ActivityDetailPopup, { props: { activity, text, loading, error } })
}

describe('ActivityDetailPopup', () => {
  const writeText = vi.fn<(text: string) => Promise<void>>()

  beforeEach(() => {
    writeText.mockReset()
    writeText.mockResolvedValue()
    vi.stubGlobal('navigator', { clipboard: { writeText } })
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('is a dialog with the name of the activity', () => {
    const wrapper = create(run, 'the text')

    expect(wrapper.find('[role="dialog"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('Morning run')
  })

  it('shows the numbers that the activity has', () => {
    const text = create(run, 'the text').text()

    expect(text).toContain('39:11')
    expect(text).toContain('4.86 km')
    expect(text).toContain('120 m')
    expect(text).toContain('162 / 178 bpm')
    expect(text).toContain('410 kcal')
  })

  it('leaves out the numbers that the activity does not have', () => {
    const text = create(hiit, null).text()

    expect(text).not.toContain(' km')
    expect(text).not.toContain('Elevation')
  })

  it('shows the coach text', () => {
    const wrapper = create(run, 'Distance, time\n1km, 07:17')

    expect(wrapper.get('[data-test="text"]').text()).toContain('1km, 07:17')
  })

  it('copies the coach text and says so', async () => {
    const wrapper = create(run, 'Distance, time\n1km, 07:17')

    await wrapper.get('button[aria-label="Copy"]').trigger('click')
    await flushPromises()

    expect(writeText).toHaveBeenCalledWith('Distance, time\n1km, 07:17')
    expect(wrapper.get('[data-test="copy-message"]').text()).toBe('Copied to clipboard')
  })

  it('has no copy button for a sport without a coach text, and says why', () => {
    const wrapper = create(hiit, null)

    expect(wrapper.find('button[aria-label="Copy"]').exists()).toBe(false)
    expect(wrapper.find('[data-test="text"]').exists()).toBe(false)
    expect(wrapper.get('[data-test="no-text"]').text()).toContain('no text')
  })

  it('says it is loading the text while it waits for it', () => {
    const wrapper = create(run, null, true)

    expect(wrapper.find('[data-test="loading"]').exists()).toBe(true)
    expect(wrapper.find('button[aria-label="Copy"]').exists()).toBe(false)
  })

  it('shows the error when the text could not be loaded', () => {
    const wrapper = create(run, null, false, 'The backend cannot be reached. Is it running?')

    expect(wrapper.get('[role="alert"]').text()).toBe(
      'The backend cannot be reached. Is it running?',
    )
    expect(wrapper.find('button[aria-label="Copy"]').exists()).toBe(false)
  })

  it('asks to close with the button', async () => {
    const wrapper = create(run, 'the text')

    await wrapper.get('button[aria-label="Close"]').trigger('click')

    expect(wrapper.emitted('close')).toHaveLength(1)
  })

  it('asks to close when the dark area around the dialog is pressed', async () => {
    const wrapper = create(run, 'the text')

    await wrapper.get('[data-test="backdrop"]').trigger('click')

    expect(wrapper.emitted('close')).toHaveLength(1)
  })
})
