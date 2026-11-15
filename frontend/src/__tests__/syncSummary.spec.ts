import { describe, expect, it } from 'vitest'
import { summarizeSync } from '@/syncSummary.ts'
import type { GarminSyncResult, SyncOutcome, SyncStatus } from '@/api/garminSync.ts'

function outcome(status: SyncStatus): SyncOutcome {
  return {
    garminActivityId: Math.random(),
    name: 'An activity',
    typeKey: 'running',
    sport: 'Run',
    startTime: '2026-11-12T10:45:57+00:00',
    durationInSeconds: 2351,
    status,
    message: null,
    savedId: status === 'Saved' ? 1 : null,
  }
}

function result(statuses: SyncStatus[], postponedCount = 0): GarminSyncResult {
  return { outcomes: statuses.map(outcome), postponedCount }
}

describe('summarizeSync', () => {
  it('says there is nothing new when Garmin has no new activity', () => {
    expect(summarizeSync(result([]))).toBe('Nothing new.')
  })

  it('counts the saved activities', () => {
    expect(summarizeSync(result(['Saved', 'Saved', 'Saved']))).toBe('3 saved.')
  })

  it('counts every kind of outcome, always in the same order', () => {
    const statuses: SyncStatus[] = [
      'Error',
      'Postponed',
      'Unsupported',
      'Saved',
      'Postponed',
      'Saved',
      'Postponed',
      'Postponed',
      'Error',
    ]

    expect(summarizeSync(result(statuses))).toBe('2 saved, 1 not supported, 2 failed, 4 waiting.')
  })

  it('leaves out the kinds that did not happen', () => {
    expect(summarizeSync(result(['Unsupported', 'Unsupported']))).toBe('2 not supported.')
    expect(summarizeSync(result(['Error']))).toBe('1 failed.')
  })
})
