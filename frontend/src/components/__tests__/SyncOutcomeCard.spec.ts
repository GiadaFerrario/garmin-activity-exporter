import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import SyncOutcomeCard from '@/components/SyncOutcomeCard.vue'
import type { SyncOutcome } from '@/api/garminSync.ts'

const saved: SyncOutcome = {
  garminActivityId: 1,
  name: 'Morning run',
  typeKey: 'running',
  sport: 'Run',
  startTime: '2026-11-12T10:45:57+00:00',
  durationInSeconds: 2351.18,
  status: 'Saved',
  message: null,
  savedId: 2,
}

describe('SyncOutcomeCard', () => {
  it('shows the name, the duration and the icon of the sport', () => {
    const wrapper = mount(SyncOutcomeCard, { props: { outcome: saved } })

    expect(wrapper.text()).toContain('Morning run')
    expect(wrapper.text()).toContain('39:11')
    expect(wrapper.find('.i-lucide-footprints').exists()).toBe(true)
  })

  it.each([
    ['Saved', 'Saved'],
    ['Unsupported', 'Not supported'],
    ['Postponed', 'Waiting'],
    ['Error', 'Error'],
  ] as const)('says %s with the words "%s"', (status, words) => {
    const wrapper = mount(SyncOutcomeCard, { props: { outcome: { ...saved, status } } })

    expect(wrapper.get('[data-test="status"]').text()).toBe(words)
  })

  it('shows the message of an activity that was not saved', () => {
    const wrapper = mount(SyncOutcomeCard, {
      props: { outcome: { ...saved, status: 'Error', message: 'Download refused', savedId: null } },
    })

    expect(wrapper.get('[data-test="message"]').text()).toBe('Download refused')
  })

  it('has no message for a saved activity', () => {
    expect(
      mount(SyncOutcomeCard, { props: { outcome: saved } })
        .find('[data-test="message"]')
        .exists(),
    ).toBe(false)
  })

  it('shows only the name and a default icon for a type that is not supported', () => {
    const walking: SyncOutcome = {
      ...saved,
      name: 'Evening walk',
      typeKey: 'walking',
      sport: null,
      status: 'Unsupported',
      message: "The activity type 'walking' is not supported.",
      savedId: null,
    }

    const wrapper = mount(SyncOutcomeCard, { props: { outcome: walking } })

    expect(wrapper.text()).toContain('Evening walk')
    expect(wrapper.find('.i-lucide-activity').exists()).toBe(true)
    // no duration for what is only listed
    expect(wrapper.text()).not.toContain('39:11')
  })
})
