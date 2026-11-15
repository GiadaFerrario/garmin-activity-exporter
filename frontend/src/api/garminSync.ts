import type { Sport } from '@/api/sport.ts'

export type SyncStatus = 'Saved' | 'Unsupported' | 'Postponed' | 'Error'

// What happened to one new activity of Garmin during a sync (POST /api/garmin/sync)
export interface SyncOutcome {
  garminActivityId: number
  name: string
  // the type of Garmin as it is, for example "running"
  typeKey: string
  // null when the type is not supported
  sport: Sport | null
  startTime: string
  durationInSeconds: number
  status: SyncStatus
  message: string | null
  // the id in the history, when the activity was saved
  savedId: number | null
}

export interface GarminSyncResult {
  outcomes: SyncOutcome[]
  // how many activities wait for the next sync because of the limit of downloads
  postponedCount: number
}
