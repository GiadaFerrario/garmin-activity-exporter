import { request } from '@/api/http.ts'
import type { GarminSyncResult } from '@/api/garminSync.ts'

// Asks the backend to bring the new activities of Garmin into the history. A POST: it changes the history.
export function syncGarmin(): Promise<GarminSyncResult> {
  return request<GarminSyncResult>('/api/garmin/sync', { method: 'POST' })
}
