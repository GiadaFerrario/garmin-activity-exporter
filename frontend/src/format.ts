import type { Sport } from '@/api/sport.ts'

// 2351 s -> "39:11", 3691 s -> "1:01:31". Rounded to the nearest second first.
export function formatDuration(totalSeconds: number): string {
  const seconds = Number.isFinite(totalSeconds) ? Math.max(0, Math.round(totalSeconds)) : 0
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  const rest = String(seconds % 60).padStart(2, '0')

  return hours > 0 ? `${hours}:${String(minutes).padStart(2, '0')}:${rest}` : `${minutes}:${rest}`
}

// 850 -> "850 m", 4864.75 -> "4.86 km". A swim is always in meters. Null when there is no distance.
export function formatDistance(meters: number | null, sport?: Sport): string | null {
  if (meters === null) {
    return null
  }

  const rounded = Math.round(meters)
  if (sport === 'Swim' || rounded < 1000) {
    return `${rounded} m`
  }

  return `${(meters / 1000).toFixed(2)} km`
}

// The name of a sport as it is written for people: HIIT is an acronym
export function formatSport(sport: Sport): string {
  return sport === 'Hiit' ? 'HIIT' : sport
}

// "162 / 178 bpm"; only the average if there is no maximum; null if there is no average
export function formatHeartRate(average: number | null, maximum: number | null): string | null {
  if (average === null) {
    return null
  }

  return maximum === null ? `${average} bpm` : `${average} / ${maximum} bpm`
}

// "Thu 12 Nov, 10:45", in the language and the time zone of the browser (they can be given, for the tests)
export function formatStart(iso: string, locale?: string, timeZone?: string): string {
  return new Intl.DateTimeFormat(locale, {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
    timeZone,
  }).format(new Date(iso))
}
