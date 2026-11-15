import type { GarminSyncResult } from '@/api/garminSync.ts'

// One line about a refresh: "3 saved, 1 not supported, 2 failed, 4 waiting."
export function summarizeSync(result: GarminSyncResult): string {
  if (result.outcomes.length === 0) {
    return 'Nothing new.'
  }

  const count = (status: string) =>
    result.outcomes.filter((outcome) => outcome.status === status).length
  const parts: [number, string][] = [
    [count('Saved'), 'saved'],
    [count('Unsupported'), 'not supported'],
    [count('Error'), 'failed'],
    [count('Postponed'), 'waiting'],
  ]

  return (
    parts
      .filter(([number]) => number > 0)
      .map(([number, label]) => `${number} ${label}`)
      .join(', ') + '.'
  )
}
