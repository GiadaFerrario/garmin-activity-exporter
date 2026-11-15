import type { Sport } from '@/api/sport.ts'

// An activity of the history, as the backend sends it (GET /api/history and GET /api/history/{id}).
// The list has no text; the detail has it when the sport has one (hasCoachText).
export interface HistoryActivity {
  id: number
  garminActivityId: number
  name: string
  sport: Sport
  // ISO date with offset, in UTC
  startTime: string
  durationInSeconds: number
  distanceInMeters: number | null
  elevationGain: number | null
  avgHR: number | null
  maxHR: number | null
  calories: number | null
  hasCoachText: boolean
  text: string | null
}
