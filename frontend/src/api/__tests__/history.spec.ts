import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/api/http.ts'
import { deleteHistoryActivity, getHistoryActivity, listHistory } from '@/api/history.ts'
import { syncGarmin } from '@/api/garmin.ts'
import type { HistoryActivity } from '@/api/historyActivity.ts'
import type { GarminSyncResult } from '@/api/garminSync.ts'

const activity: HistoryActivity = {
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

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

describe('the API of the history and of Garmin', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    fetchMock.mockReset()
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  describe('listHistory', () => {
    it('asks for the list and returns it', async () => {
      fetchMock.mockResolvedValue(json([activity]))

      const result = await listHistory()

      expect(fetchMock).toHaveBeenCalledWith('/api/history', undefined)
      expect(result).toEqual([activity])
    })
  })

  describe('getHistoryActivity', () => {
    it('asks for one activity by its id', async () => {
      fetchMock.mockResolvedValue(json({ ...activity, text: 'the text' }))

      const result = await getHistoryActivity(2)

      expect(fetchMock).toHaveBeenCalledWith('/api/history/2', undefined)
      expect(result.text).toBe('the text')
    })

    it('fails with the status when the activity does not exist', async () => {
      fetchMock.mockResolvedValue(new Response(null, { status: 404 }))

      const error = await getHistoryActivity(99).catch((e: unknown) => e)

      expect(error).toBeInstanceOf(ApiError)
      expect((error as ApiError).status).toBe(404)
      expect((error as ApiError).message).toBe('The request failed (HTTP 404)')
    })
  })

  describe('deleteHistoryActivity', () => {
    it('sends a DELETE and gives back nothing', async () => {
      fetchMock.mockResolvedValue(new Response(null, { status: 204 }))

      const result = await deleteHistoryActivity(2)

      expect(fetchMock).toHaveBeenCalledWith('/api/history/2', { method: 'DELETE' })
      expect(result).toBeUndefined()
    })

    it('fails when the activity is not there', async () => {
      fetchMock.mockResolvedValue(new Response(null, { status: 404 }))

      await expect(deleteHistoryActivity(99)).rejects.toBeInstanceOf(ApiError)
    })
  })

  describe('syncGarmin', () => {
    const result: GarminSyncResult = {
      postponedCount: 0,
      outcomes: [
        {
          garminActivityId: 1,
          name: 'Morning run',
          typeKey: 'running',
          sport: 'Run',
          startTime: '2026-11-12T10:45:57+00:00',
          durationInSeconds: 2351,
          status: 'Saved',
          message: null,
          savedId: 2,
        },
      ],
    }

    it('sends a POST and returns the outcomes', async () => {
      fetchMock.mockResolvedValue(json(result))

      const received = await syncGarmin()

      expect(fetchMock).toHaveBeenCalledWith('/api/garmin/sync', { method: 'POST' })
      expect(received).toEqual(result)
    })

    it('shows the readable message of the backend when Garmin fails', async () => {
      fetchMock.mockResolvedValue(
        json({ title: 'Garmin sync failed', status: 502, detail: 'Garmin is not reachable.' }, 502),
      )

      const error = await syncGarmin().catch((e: unknown) => e)

      expect(error).toBeInstanceOf(ApiError)
      expect((error as ApiError).message).toBe('Garmin is not reachable.')
      expect((error as ApiError).status).toBe(502)
    })
  })

  describe('errors in general', () => {
    it('uses the status when the error has no readable message', async () => {
      fetchMock.mockResolvedValue(new Response('<html>oops</html>', { status: 500 }))

      await expect(listHistory()).rejects.toThrow('The request failed (HTTP 500)')
    })

    it('uses the status when the problem has an empty detail', async () => {
      fetchMock.mockResolvedValue(json({ detail: '' }, 502))

      await expect(listHistory()).rejects.toThrow('The request failed (HTTP 502)')
    })

    it('says the backend cannot be reached when the request itself fails', async () => {
      fetchMock.mockRejectedValue(new TypeError('Failed to fetch'))

      const error = await listHistory().catch((e: unknown) => e)

      expect(error).toBeInstanceOf(ApiError)
      expect((error as ApiError).status).toBe(0)
      expect((error as ApiError).message).toContain('backend')
    })
  })
})
