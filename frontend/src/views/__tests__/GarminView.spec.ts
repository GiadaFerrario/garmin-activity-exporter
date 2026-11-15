import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import GarminView from '@/views/GarminView.vue'
import SyncOutcomeCard from '@/components/SyncOutcomeCard.vue'
import { ApiError } from '@/api/http.ts'
import { syncGarmin } from '@/api/garmin.ts'
import type { GarminSyncResult, SyncOutcome } from '@/api/garminSync.ts'

vi.mock('@/api/garmin.ts', () => ({ syncGarmin: vi.fn<() => Promise<GarminSyncResult>>() }))
const syncMock = vi.mocked(syncGarmin)

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason: unknown) => void
  const promise = new Promise<T>((res, rej) => {
    resolve = res
    reject = rej
  })
  return { promise, resolve, reject }
}

const saved: SyncOutcome = {
  garminActivityId: 1,
  name: 'Morning run',
  typeKey: 'running',
  sport: 'Run',
  startTime: '2026-11-12T10:45:57+00:00',
  durationInSeconds: 2351,
  status: 'Saved',
  message: null,
  savedId: 2,
}

const walking: SyncOutcome = {
  ...saved,
  garminActivityId: 2,
  name: 'Evening walk',
  typeKey: 'walking',
  sport: null,
  status: 'Unsupported',
  message: "The activity type 'walking' is not supported.",
  savedId: null,
}

const result = (outcomes: SyncOutcome[], postponedCount = 0): GarminSyncResult => ({
  outcomes,
  postponedCount,
})

describe('GarminView', () => {
  beforeEach(() => {
    syncMock.mockReset()
  })

  it('starts with a Refresh button and an explanation, and no list', () => {
    const wrapper = mount(GarminView)

    expect(wrapper.get('[data-test="refresh"]').attributes('disabled')).toBeUndefined()
    expect(wrapper.find('[data-test="intro"]').exists()).toBe(true)
    expect(wrapper.findAllComponents(SyncOutcomeCard)).toHaveLength(0)
    expect(wrapper.find('[data-test="summary"]').exists()).toBe(false)
  })

  it('asks the backend to bring the new activities when Refresh is pressed', async () => {
    syncMock.mockResolvedValue(result([]))
    const wrapper = mount(GarminView)

    await wrapper.get('[data-test="refresh"]').trigger('click')

    expect(syncMock).toHaveBeenCalledTimes(1)
  })

  it('says it is working and cannot be pressed again while it waits', async () => {
    const pending = deferred<GarminSyncResult>()
    syncMock.mockReturnValue(pending.promise)
    const wrapper = mount(GarminView)

    await wrapper.get('[data-test="refresh"]').trigger('click')
    await nextTick()

    const button = wrapper.get('[data-test="refresh"]')
    expect(button.attributes('disabled')).toBeDefined()
    expect(button.text()).toContain('Refreshing')
    await button.trigger('click')
    expect(syncMock).toHaveBeenCalledTimes(1)

    pending.resolve(result([]))
    await flushPromises()
    expect(wrapper.get('[data-test="refresh"]').attributes('disabled')).toBeUndefined()
    expect(wrapper.get('[data-test="refresh"]').text()).toContain('Refresh')
    expect(wrapper.get('[data-test="refresh"]').text()).not.toContain('Refreshing')
  })

  it('shows the summary and a card for every outcome, in the order of the backend', async () => {
    syncMock.mockResolvedValue(result([saved, walking]))
    const wrapper = mount(GarminView)

    await wrapper.get('[data-test="refresh"]').trigger('click')
    await flushPromises()

    expect(wrapper.get('[data-test="summary"]').text()).toBe('1 saved, 1 not supported.')
    const cards = wrapper.findAllComponents(SyncOutcomeCard)
    expect(cards.map((card) => card.props('outcome').garminActivityId)).toEqual([1, 2])
    // the explanation is only for the first time
    expect(wrapper.find('[data-test="intro"]').exists()).toBe(false)
  })

  it('says there is nothing new', async () => {
    syncMock.mockResolvedValue(result([]))
    const wrapper = mount(GarminView)

    await wrapper.get('[data-test="refresh"]').trigger('click')
    await flushPromises()

    expect(wrapper.get('[data-test="summary"]').text()).toBe('Nothing new.')
    expect(wrapper.findAllComponents(SyncOutcomeCard)).toHaveLength(0)
  })

  it('tells to refresh again when some activities are waiting for the next sync', async () => {
    syncMock.mockResolvedValue(result([saved], 3))
    const wrapper = mount(GarminView)

    await wrapper.get('[data-test="refresh"]').trigger('click')
    await flushPromises()

    const note = wrapper.get('[data-test="postponed"]').text()
    expect(note).toContain('3')
    expect(note).toContain('refresh again')
  })

  it('has no note about waiting activities when none is waiting', async () => {
    syncMock.mockResolvedValue(result([saved], 0))
    const wrapper = mount(GarminView)

    await wrapper.get('[data-test="refresh"]').trigger('click')
    await flushPromises()

    expect(wrapper.find('[data-test="postponed"]').exists()).toBe(false)
  })

  it('shows the message of the error and lets the user try again', async () => {
    syncMock.mockRejectedValueOnce(new ApiError('Garmin is not reachable.', 502))
    const wrapper = mount(GarminView)

    await wrapper.get('[data-test="refresh"]').trigger('click')
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toBe('Garmin is not reachable.')
    expect(wrapper.get('[data-test="refresh"]').attributes('disabled')).toBeUndefined()

    syncMock.mockResolvedValueOnce(result([saved]))
    await wrapper.get('[data-test="refresh"]').trigger('click')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').exists()).toBe(false)
    expect(wrapper.findAllComponents(SyncOutcomeCard)).toHaveLength(1)
  })

  it('keeps the list of the last refresh when the next one fails', async () => {
    syncMock.mockResolvedValueOnce(result([saved]))
    const wrapper = mount(GarminView)
    await wrapper.get('[data-test="refresh"]').trigger('click')
    await flushPromises()

    syncMock.mockRejectedValueOnce(new ApiError('Garmin is not reachable.', 502))
    await wrapper.get('[data-test="refresh"]').trigger('click')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').exists()).toBe(true)
    expect(wrapper.findAllComponents(SyncOutcomeCard)).toHaveLength(1)
  })

  it('replaces the list with the one of the new refresh', async () => {
    syncMock.mockResolvedValueOnce(result([saved, walking]))
    const wrapper = mount(GarminView)
    await wrapper.get('[data-test="refresh"]').trigger('click')
    await flushPromises()

    syncMock.mockResolvedValueOnce(result([walking]))
    await wrapper.get('[data-test="refresh"]').trigger('click')
    await flushPromises()

    expect(wrapper.findAllComponents(SyncOutcomeCard)).toHaveLength(1)
    expect(wrapper.get('[data-test="summary"]').text()).toBe('1 not supported.')
  })

  it('shows an error that is not an ApiError too', async () => {
    syncMock.mockRejectedValueOnce(new Error('boom'))
    const wrapper = mount(GarminView)

    await wrapper.get('[data-test="refresh"]').trigger('click')
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toBe('boom')
  })
})
