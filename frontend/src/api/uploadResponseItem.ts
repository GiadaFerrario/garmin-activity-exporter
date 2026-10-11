import type { UploadStatus } from '@/api/uploadStatus.ts'

export interface UploadResponseItem {
  fileName: string,
  status: UploadStatus,
  message: string,
  text: string | null
}

