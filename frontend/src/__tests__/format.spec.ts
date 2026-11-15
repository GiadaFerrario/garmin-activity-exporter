import { describe, expect, it } from 'vitest'
import {
  formatDistance,
  formatDuration,
  formatHeartRate,
  formatSport,
  formatStart,
} from '@/format.ts'

describe('formatDuration', () => {
  it.each([
    [0, '0:00'],
    [45, '0:45'],
    [2351.18, '39:11'],
    [3599, '59:59'],
    [3600, '1:00:00'],
    [3691, '1:01:31'],
    [7384, '2:03:04'],
  ])('%s seconds is %s', (seconds, expected) => {
    expect(formatDuration(seconds)).toBe(expected)
  })

  it('rounds to the nearest second before splitting, so 59.6 s is a minute', () => {
    expect(formatDuration(59.6)).toBe('1:00')
  })

  it('shows a negative or a missing number as zero', () => {
    expect(formatDuration(-5)).toBe('0:00')
    expect(formatDuration(Number.NaN)).toBe('0:00')
  })
})

describe('formatDistance', () => {
  it('has nothing to show when the activity has no distance', () => {
    expect(formatDistance(null)).toBeNull()
  })

  it.each([
    [0, '0 m'],
    [850, '850 m'],
    [999.4, '999 m'],
    [999.6, '1.00 km'],
    [1000, '1.00 km'],
    [4864.75, '4.86 km'],
    [19721.45, '19.72 km'],
  ])('%s meters is %s', (meters, expected) => {
    expect(formatDistance(meters)).toBe(expected)
  })

  it('writes a swim in meters, however long it is', () => {
    expect(formatDistance(25, 'Swim')).toBe('25 m')
    expect(formatDistance(1050, 'Swim')).toBe('1050 m')
    expect(formatDistance(3000.4, 'Swim')).toBe('3000 m')
  })

  it('writes the other sports in km from a kilometre up', () => {
    expect(formatDistance(1050, 'Run')).toBe('1.05 km')
    expect(formatDistance(1050, 'Bike')).toBe('1.05 km')
  })
})

describe('formatSport', () => {
  it('writes HIIT in capitals, it is not a word', () => {
    expect(formatSport('Hiit')).toBe('HIIT')
  })

  it.each(['Run', 'Bike', 'Swim', 'Strength'] as const)('writes %s as it is', (sport) => {
    expect(formatSport(sport)).toBe(sport)
  })
})

describe('formatHeartRate', () => {
  it('shows the average and the maximum', () => {
    expect(formatHeartRate(162, 178)).toBe('162 / 178 bpm')
  })

  it('shows only the average when there is no maximum', () => {
    expect(formatHeartRate(162, null)).toBe('162 bpm')
  })

  it('has nothing to show without an average', () => {
    expect(formatHeartRate(null, 178)).toBeNull()
    expect(formatHeartRate(null, null)).toBeNull()
  })
})

describe('formatStart', () => {
  const iso = '2026-11-12T10:45:57+00:00'

  it('writes the weekday, the day, the month and the time', () => {
    const text = formatStart(iso, 'en-GB', 'UTC')

    expect(text).toContain('Thu')
    expect(text).toContain('12')
    expect(text).toContain('Nov')
    expect(text).toContain('10:45')
  })

  it('shows the time of the time zone that is asked for', () => {
    // in November Rome is one hour ahead of UTC
    expect(formatStart(iso, 'en-GB', 'Europe/Rome')).toContain('11:45')
  })

  it('writes the month in the language that is asked for', () => {
    expect(formatStart(iso, 'it-IT', 'UTC').toLowerCase()).toContain('nov')
    expect(formatStart(iso, 'it-IT', 'UTC').toLowerCase()).toContain('gio')
  })
})
