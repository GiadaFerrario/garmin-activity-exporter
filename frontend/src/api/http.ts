// An error of a call to the backend, with the HTTP status (0 when the backend could not be reached)
export class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

// The backend explains its errors as "problem details" (RFC 9457): the "detail" is a message that can be shown
async function readErrorMessage(response: Response): Promise<string> {
  try {
    const problem: unknown = await response.json()
    if (typeof problem === 'object' && problem !== null && 'detail' in problem) {
      const { detail } = problem
      if (typeof detail === 'string' && detail.length > 0) {
        return detail
      }
    }
  } catch {
    // the body is not JSON (or it is empty): the status is all we have
  }

  return `The request failed (HTTP ${response.status})`
}

// Calls the backend and returns the JSON of the answer; a 204 has no body and gives back undefined.
// Every failure is an ApiError.
export async function request<T>(url: string, init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(url, init)
  } catch {
    // fetch only rejects when the request itself failed: the backend is down, or there is no network
    throw new ApiError('The backend cannot be reached. Is it running?', 0)
  }

  if (!response.ok) {
    throw new ApiError(await readErrorMessage(response), response.status)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}
