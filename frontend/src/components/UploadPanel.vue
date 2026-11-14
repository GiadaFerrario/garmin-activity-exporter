<script setup lang="ts">
import { ref } from 'vue'

const selectedFiles = ref<File[]>([])
const fileInput = ref<HTMLInputElement | null>(null)

// the events this component can send to its parent
const emit = defineEmits<{
  (event: 'upload', files: File[]): void
}>()

function onFilesChosen(event: Event) {
  const input = event.target as HTMLInputElement
  // input.files is a FileList not File[]
  selectedFiles.value = Array.from(input.files ?? [])
}

function onUploadClicked() {
  // send the 'upload' event with the selected files
  emit('upload', selectedFiles.value)
  selectedFiles.value = []
  const input = fileInput.value
  if (input !== null) {
    input.value = ''
  }
}
</script>

<template>
  <section class="flex flex-col gap-4 items-start">
    <label
      class="block w-full p-8 text-center border-2 border-dashed border-line-hover rounded-lg bg-surface-soft cursor-pointer hover:border-accent"
    >
      <input ref="fileInput" type="file" multiple accept=".fit" hidden @change="onFilesChosen" />
      <span>Choose your .fit files</span>
    </label>

    <ul v-if="selectedFiles.length > 0" class="list-none p-0">
      <li v-for="file in selectedFiles" :key="file.name">{{ file.name }}</li>
    </ul>

    <button :disabled="selectedFiles.length === 0" @click="onUploadClicked">Upload</button>
  </section>
</template>
