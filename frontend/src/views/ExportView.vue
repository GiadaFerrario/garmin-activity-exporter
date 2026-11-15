<script setup lang="ts">
import { ref } from 'vue'
import UploadPanel from '@/components/UploadPanel.vue'
import { uploadFiles } from '@/api/activities.ts'
import type { UploadResponseItem } from '@/api/uploadResponseItem.ts'
import UploadResults from '@/components/UploadResults.vue'
import PopUp from '@/components/PopUp.vue'
import IconButton from '@/components/IconButton.vue'
import { useCopyFeedback } from '@/composables/useCopyFeedback.ts'

const results = ref<UploadResponseItem[]>([])
const errorMessage = ref<string | null>(null)
const isUploading = ref(false)
const selectedResponse = ref<UploadResponseItem | null>(null)
// "Copied to clipboard" / "Could not copy the text", shown in the popup for two seconds
const { message: copyMessage, copy, reset: resetCopyMessage } = useCopyFeedback()

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
  resetCopyMessage()
}

async function onCopy() {
  const text = selectedResponse.value?.text
  if (text == null) {
    return
  }

  await copy(text)
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
