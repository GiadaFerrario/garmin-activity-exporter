import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import SportIcon from '@/components/SportIcon.vue'
import type { Sport } from '@/api/sport.ts'

describe('SportIcon', () => {
  it.each<[Sport, string]>([
    ['Run', 'i-lucide-footprints'],
    ['Bike', 'i-lucide-bike'],
    ['Swim', 'i-lucide-waves'],
    ['Hiit', 'i-lucide-flame'],
    ['Strength', 'i-lucide-dumbbell'],
  ])('shows the icon of %s', (sport, iconClass) => {
    expect(mount(SportIcon, { props: { sport } }).classes()).toContain(iconClass)
  })

  it('shows a default icon for a sport that is not supported', () => {
    expect(mount(SportIcon, { props: { sport: null } }).classes()).toContain('i-lucide-activity')
  })

  it('is decoration: the name of the sport is always written next to it', () => {
    expect(mount(SportIcon, { props: { sport: 'Run' } }).attributes('aria-hidden')).toBe('true')
  })
})
