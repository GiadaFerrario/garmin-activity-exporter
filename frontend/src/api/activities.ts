import type { UploadResponseItem } from '@/api/uploadResponseItem.ts'

// Sends the selected .fit files to the backend and returns one outcome per file
export async function uploadFiles(files: File[]): Promise<UploadResponseItem[]> {
  const body = new FormData()
  for (const file of files) {
    // The field name must match the parameter name of the backend endpoint ("files")
    body.append('files', file)
  }

  // No Content-Type header: the browser builds the multipart boundary on its own
  const response = await fetch('/api/activities', { method: 'POST', body })

  // fetch only rejects on network errors: HTTP errors (4xx/5xx) must be checked by hand
  if (!response.ok) {
    throw new Error(`Upload failed (HTTP ${response.status})`)
  }

  return (await response.json()) as UploadResponseItem[]
}
