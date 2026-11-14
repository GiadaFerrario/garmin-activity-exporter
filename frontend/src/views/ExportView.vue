<script setup lang="ts">
import { ref } from 'vue'
import UploadPanel from '@/components/UploadPanel.vue'
import { uploadFiles } from '@/api/activities.ts'
import type { UploadResponseItem } from '@/api/uploadResponseItem.ts'
import UploadResults from '@/components/UploadResults.vue'
import PopUp from '@/components/PopUp.vue'
import IconButton from '@/components/IconButton.vue'

const results = ref<UploadResponseItem[]>([])
const errorMessage = ref<string | null>(null)
const isUploading = ref(false)
const selectedResponse = ref<UploadResponseItem | null>(null)
// Feedback shown in the popup after pressing Copy (success or failure); null hides it
const copyMessage = ref<string | null>(null)
// Handle of the timer that hides the message: kept so that a new copy can cancel the previous timer
let copyMessageTimer: ReturnType<typeof setTimeout> | undefined

async function onUpload(files: File[]) {
  errorMessage.value = null
  isUploading.value = true
  try {
    // call uploadFiles and store the outcomes in results
    results.value = await uploadFiles(files)
  } catch (error) {
    // the backend is unreachable or answered with an HTTP error: show a message
    errorMessage.value = error instanceof Error ? error.message : 'Unexpected error'
  } finally {
    isUploading.value = false
  }
}

function onShowText(response: UploadResponseItem) {
  selectedResponse.value = response
  clearTimeout(copyMessageTimer)
  copyMessage.value = null
}

// Shows a message for 2 seconds; a newer message replaces the previous one and restarts the timer
function showCopyMessage(message: string) {
  clearTimeout(copyMessageTimer)
  copyMessage.value = message
  copyMessageTimer = setTimeout(() => (copyMessage.value = null), 2000)
}

async function onCopy() {
  const text = selectedResponse.value?.text
  if (text == null) {
    return
  }

  try {
    await navigator.clipboard.writeText(text)
    showCopyMessage('Copied to clipboard')
  } catch {
    // the browser refused (permission denied, insecure context...)
    showCopyMessage('Could not copy the text')
  }
}
</script>

<template>
  <div>
    <UploadPanel @upload="onUpload" />

    <p v-if="isUploading">Uploading...</p>
    <p v-if="errorMessage">{{ errorMessage }}</p>

    <UploadResults :responses="results" @show-text="onShowText" />

    <PopUp v-if="selectedResponse !== null" @close="selectedResponse = null">
      <h2 class="font-[ui-monospace,SFMono-Regular,Menlo,monospace] text-[0.95rem] mb-3 font-bold">
        {{ selectedResponse.fileName }}
      </h2>
      <!-- only the text scrolls sideways on a narrow screen, not the whole dialog -->
      <pre class="overflow-x-auto">{{ selectedResponse.text }}</pre>
      <div class="flex gap-3 mt-4">
        <IconButton icon="copy" label="Copy" @click="onCopy" />
        <IconButton icon="close" label="Close" @click="selectedResponse = null" />
      </div>
      <p v-if="copyMessage">{{ copyMessage }}</p>
    </PopUp>
  </div>
</template>
