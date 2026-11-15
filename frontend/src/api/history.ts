import { request } from '@/api/http.ts'
import type { HistoryActivity } from '@/api/historyActivity.ts'

// The activities of the history, the most recent first, without the coach text
export function listHistory(): Promise<HistoryActivity[]> {
  return request<HistoryActivity[]>('/api/history')
}

// One activity with its coach text (null for the sports that have none)
export function getHistoryActivity(id: number): Promise<HistoryActivity> {
  return request<HistoryActivity>(`/api/history/${id}`)
}

// The activity leaves the history; the next sync does not bring it back
export function deleteHistoryActivity(id: number): Promise<void> {
  return request<void>(`/api/history/${id}`, { method: 'DELETE' })
}
