import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import ActivityCard from '@/components/ActivityCard.vue'
import type { HistoryActivity } from '@/api/historyActivity.ts'

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
  durationInSeconds: 2623.17,
  distanceInMeters: null,
  avgHR: 139,
  maxHR: 178,
  calories: 242,
  hasCoachText: false,
}

describe('ActivityCard', () => {
  it('shows the name, the sport, the duration, the distance, the heart rate and the calories', () => {
    const text = mount(ActivityCard, { props: { activity: run } }).text()

    expect(text).toContain('Morning run')
    expect(text).toContain('Run')
    expect(text).toContain('39:11')
    expect(text).toContain('4.86 km')
    expect(text).toContain('162 / 178 bpm')
    expect(text).toContain('410 kcal')
  })

  it('writes HIIT in capitals', () => {
    const text = mount(ActivityCard, { props: { activity: { ...hiit, name: 'Session' } } }).text()

    expect(text).toContain('HIIT ·')
    expect(text).not.toContain('Hiit')
  })

  it('shows the icon of the sport', () => {
    const wrapper = mount(ActivityCard, { props: { activity: run } })

    expect(wrapper.find('.i-lucide-footprints').exists()).toBe(true)
  })

  it('leaves out what an activity does not have (HIIT: no distance)', () => {
    const text = mount(ActivityCard, { props: { activity: hiit } }).text()

    expect(text).toContain('43:43')
    expect(text).not.toContain(' km')
    expect(text).not.toContain(' m ')
    expect(text).toContain('242 kcal')
  })

  it('leaves out the heart rate when there is none', () => {
    const text = mount(ActivityCard, {
      props: { activity: { ...run, avgHR: null, maxHR: null } },
    }).text()

    expect(text).not.toContain('bpm')
  })

  it('writes a swim in meters', () => {
    const swim: HistoryActivity = { ...run, sport: 'Swim', distanceInMeters: 1050 }

    expect(mount(ActivityCard, { props: { activity: swim } }).text()).toContain('1050 m')
  })

  it('asks to open the activity when the card is pressed', async () => {
    const wrapper = mount(ActivityCard, { props: { activity: run } })

    await wrapper.get('[data-test="open"]').trigger('click')

    expect(wrapper.emitted('open')).toHaveLength(1)
  })

  it('asks to delete the activity with a button that says which one', async () => {
    const wrapper = mount(ActivityCard, { props: { activity: run } })

    await wrapper.get('button[aria-label="Delete Morning run"]').trigger('click')

    expect(wrapper.emitted('delete')).toHaveLength(1)
    // deleting is not opening
    expect(wrapper.emitted('open')).toBeUndefined()
  })
})
