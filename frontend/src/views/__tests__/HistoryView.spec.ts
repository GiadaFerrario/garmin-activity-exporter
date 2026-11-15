import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import HistoryView from '@/views/HistoryView.vue'
import ActivityCard from '@/components/ActivityCard.vue'
import ActivityDetailPopup from '@/components/ActivityDetailPopup.vue'
import ConfirmPopup from '@/components/ConfirmPopup.vue'
import { ApiError } from '@/api/http.ts'
import { deleteHistoryActivity, getHistoryActivity, listHistory } from '@/api/history.ts'
import type { HistoryActivity } from '@/api/historyActivity.ts'

vi.mock('@/api/history.ts', () => ({
  listHistory: vi.fn<() => Promise<HistoryActivity[]>>(),
  getHistoryActivity: vi.fn<(id: number) => Promise<HistoryActivity>>(),
  deleteHistoryActivity: vi.fn<(id: number) => Promise<void>>(),
}))
const listMock = vi.mocked(listHistory)
const getMock = vi.mocked(getHistoryActivity)
const deleteMock = vi.mocked(deleteHistoryActivity)

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason: unknown) => void
  const promise = new Promise<T>((res, rej) => {
    resolve = res
    reject = rej
  })
  return { promise, resolve, reject }
}

const run: HistoryActivity = {
  id: 2,
  garminActivityId: 25053166010,
  name: 'Morning run',
  sport: 'Run',
  startTime: '2026-11-12T10:45:57+00:00',
  durationInSeconds: 2351.18,
  distanceInMeters: 4864.75,
  elevationGain: null,
  avgHR: 162,
  maxHR: 178,
  calories: 410,
  hasCoachText: true,
  text: null,
}

const hiit: HistoryActivity = {
  ...run,
  id: 3,
  name: 'HIIT session',
  sport: 'Hiit',
  distanceInMeters: null,
  hasCoachText: false,
}

// RouterLink needs a router: a plain link is enough here
const stubs = { RouterLink: { template: '<a><slot /></a>' } }

async function mountLoaded(items: HistoryActivity[] = [run, hiit]) {
  listMock.mockResolvedValue(items)
  const wrapper = mount(HistoryView, { global: { stubs } })
  await flushPromises()
  return wrapper
}

describe('HistoryView', () => {
  beforeEach(() => {
    listMock.mockReset()
    getMock.mockReset()
    deleteMock.mockReset()
  })

  describe('the list', () => {
    it('shows a card for every activity, in the order of the backend', async () => {
      const wrapper = await mountLoaded()

      const cards = wrapper.findAllComponents(ActivityCard)
      expect(cards.map((card) => card.props('activity').id)).toEqual([2, 3])
    })

    it('says it is loading while it waits for the list', async () => {
      const pending = deferred<HistoryActivity[]>()
      listMock.mockReturnValue(pending.promise)

      const wrapper = mount(HistoryView, { global: { stubs } })
      await wrapper.vm.$nextTick()

      expect(wrapper.find('[data-test="loading"]').exists()).toBe(true)
      pending.resolve([run])
      await flushPromises()
      expect(wrapper.find('[data-test="loading"]').exists()).toBe(false)
    })

    it('explains how to fill an empty history', async () => {
      const wrapper = await mountLoaded([])

      expect(wrapper.get('[data-test="empty"]').text()).toContain('Garmin')
      expect(wrapper.findAllComponents(ActivityCard)).toHaveLength(0)
    })

    it('shows the error of the list and loads it again with Retry', async () => {
      listMock.mockRejectedValueOnce(
        new ApiError('The backend cannot be reached. Is it running?', 0),
      )
      const wrapper = mount(HistoryView, { global: { stubs } })
      await flushPromises()

      expect(wrapper.get('[data-test="error"]').text()).toContain('The backend cannot be reached')
      expect(wrapper.find('[data-test="empty"]').exists()).toBe(false)

      listMock.mockResolvedValueOnce([run])
      await wrapper.get('[data-test="retry"]').trigger('click')
      await flushPromises()

      expect(wrapper.find('[data-test="error"]').exists()).toBe(false)
      expect(wrapper.findAllComponents(ActivityCard)).toHaveLength(1)
    })
  })

  describe('opening an activity', () => {
    it('shows the popup at once and loads the text of an activity that has one', async () => {
      const wrapper = await mountLoaded()
      const pending = deferred<HistoryActivity>()
      getMock.mockReturnValue(pending.promise)

      await wrapper.findAllComponents(ActivityCard)[0]!.get('[data-test="open"]').trigger('click')

      expect(getMock).toHaveBeenCalledWith(2)
      const popup = wrapper.getComponent(ActivityDetailPopup)
      expect(popup.text()).toContain('Morning run')
      expect(popup.find('[data-test="loading"]').exists()).toBe(true)

      pending.resolve({ ...run, text: 'Distance, time\n1km, 07:17' })
      await flushPromises()
      expect(popup.find('[data-test="loading"]').exists()).toBe(false)
      expect(popup.get('[data-test="text"]').text()).toContain('1km, 07:17')
    })

    it('does not ask for a text that does not exist (HIIT)', async () => {
      const wrapper = await mountLoaded()

      await wrapper.findAllComponents(ActivityCard)[1]!.get('[data-test="open"]').trigger('click')

      expect(getMock).not.toHaveBeenCalled()
      const popup = wrapper.getComponent(ActivityDetailPopup)
      expect(popup.text()).toContain('HIIT session')
      expect(popup.find('[data-test="no-text"]').exists()).toBe(true)
    })

    it('shows the error in the popup when the text cannot be loaded', async () => {
      const wrapper = await mountLoaded()
      getMock.mockRejectedValue(new ApiError('The request failed (HTTP 404)', 404))

      await wrapper.findAllComponents(ActivityCard)[0]!.get('[data-test="open"]').trigger('click')
      await flushPromises()

      expect(wrapper.getComponent(ActivityDetailPopup).get('[role="alert"]').text()).toBe(
        'The request failed (HTTP 404)',
      )
    })

    it('closes the popup', async () => {
      const wrapper = await mountLoaded()
      getMock.mockResolvedValue({ ...run, text: 'the text' })
      await wrapper.findAllComponents(ActivityCard)[0]!.get('[data-test="open"]').trigger('click')
      await flushPromises()

      await wrapper
        .getComponent(ActivityDetailPopup)
        .get('button[aria-label="Close"]')
        .trigger('click')

      expect(wrapper.findComponent(ActivityDetailPopup).exists()).toBe(false)
    })

    it('ignores the answer of an activity that was closed meanwhile', async () => {
      const wrapper = await mountLoaded()
      const first = deferred<HistoryActivity>()
      getMock.mockReturnValueOnce(first.promise)
      await wrapper.findAllComponents(ActivityCard)[0]!.get('[data-test="open"]').trigger('click')
      await wrapper
        .getComponent(ActivityDetailPopup)
        .get('button[aria-label="Close"]')
        .trigger('click')

      first.resolve({ ...run, text: 'late text' })
      await flushPromises()

      expect(wrapper.findComponent(ActivityDetailPopup).exists()).toBe(false)
    })
  })

  describe('deleting an activity', () => {
    it('asks for a confirmation that names the activity', async () => {
      const wrapper = await mountLoaded()

      await wrapper.get('button[aria-label="Delete Morning run"]').trigger('click')

      const confirm = wrapper.getComponent(ConfirmPopup)
      expect(confirm.text()).toContain('Morning run')
      expect(deleteMock).not.toHaveBeenCalled()
    })

    it('deletes it when confirmed and takes the card away', async () => {
      const wrapper = await mountLoaded()
      deleteMock.mockResolvedValue()
      await wrapper.get('button[aria-label="Delete Morning run"]').trigger('click')

      await wrapper.getComponent(ConfirmPopup).get('[data-test="confirm"]').trigger('click')
      await flushPromises()

      expect(deleteMock).toHaveBeenCalledWith(2)
      expect(wrapper.findComponent(ConfirmPopup).exists()).toBe(false)
      expect(
        wrapper.findAllComponents(ActivityCard).map((card) => card.props('activity').id),
      ).toEqual([3])
    })

    it('does nothing when cancelled', async () => {
      const wrapper = await mountLoaded()
      await wrapper.get('button[aria-label="Delete Morning run"]').trigger('click')

      await wrapper.getComponent(ConfirmPopup).get('[data-test="cancel"]').trigger('click')

      expect(deleteMock).not.toHaveBeenCalled()
      expect(wrapper.findComponent(ConfirmPopup).exists()).toBe(false)
      expect(wrapper.findAllComponents(ActivityCard)).toHaveLength(2)
    })

    it('cannot be confirmed twice while it is deleting', async () => {
      const wrapper = await mountLoaded()
      const pending = deferred<void>()
      deleteMock.mockReturnValue(pending.promise)
      await wrapper.get('button[aria-label="Delete Morning run"]').trigger('click')

      await wrapper.getComponent(ConfirmPopup).get('[data-test="confirm"]').trigger('click')
      await wrapper.vm.$nextTick()

      expect(
        wrapper.getComponent(ConfirmPopup).get('[data-test="confirm"]').attributes('disabled'),
      ).toBeDefined()
      await wrapper.getComponent(ConfirmPopup).get('[data-test="confirm"]').trigger('click')
      expect(deleteMock).toHaveBeenCalledTimes(1)

      pending.resolve()
      await flushPromises()
    })

    it('keeps the card and shows the error when the delete fails', async () => {
      const wrapper = await mountLoaded()
      deleteMock.mockRejectedValue(new ApiError('The request failed (HTTP 500)', 500))
      await wrapper.get('button[aria-label="Delete Morning run"]').trigger('click')

      await wrapper.getComponent(ConfirmPopup).get('[data-test="confirm"]').trigger('click')
      await flushPromises()

      expect(wrapper.get('[data-test="delete-error"]').text()).toContain(
        'The request failed (HTTP 500)',
      )
      expect(wrapper.findComponent(ConfirmPopup).exists()).toBe(false)
      expect(wrapper.findAllComponents(ActivityCard)).toHaveLength(2)
    })

    it('forgets the old delete error when something else is deleted', async () => {
      const wrapper = await mountLoaded()
      deleteMock.mockRejectedValueOnce(new ApiError('The request failed (HTTP 500)', 500))
      await wrapper.get('button[aria-label="Delete Morning run"]').trigger('click')
      await wrapper.getComponent(ConfirmPopup).get('[data-test="confirm"]').trigger('click')
      await flushPromises()

      deleteMock.mockResolvedValueOnce()
      await wrapper.get('button[aria-label="Delete HIIT session"]').trigger('click')
      await wrapper.getComponent(ConfirmPopup).get('[data-test="confirm"]').trigger('click')
      await flushPromises()

      expect(wrapper.find('[data-test="delete-error"]').exists()).toBe(false)
    })

    it('shows the empty history when the last activity is deleted', async () => {
      const wrapper = await mountLoaded([run])
      deleteMock.mockResolvedValue()
      await wrapper.get('button[aria-label="Delete Morning run"]').trigger('click')

      await wrapper.getComponent(ConfirmPopup).get('[data-test="confirm"]').trigger('click')
      await flushPromises()

      expect(wrapper.find('[data-test="empty"]').exists()).toBe(true)
    })
  })
})
