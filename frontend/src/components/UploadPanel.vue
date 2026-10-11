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
  <section class="upload-panel">
    <label class="dropzone">
      <input ref="fileInput" type="file" multiple accept=".fit" hidden @change="onFilesChosen" />
      <span>Choose your .fit files</span>
    </label>

    <ul v-if="selectedFiles.length > 0" class="file-list">
      <li v-for="file in selectedFiles" :key="file.name">{{ file.name }}</li>
    </ul>

    <button :disabled="selectedFiles.length === 0" @click="onUploadClicked">Upload</button>
  </section>
</template>

<style scoped>
.upload-panel {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  align-items: flex-start;
}

.dropzone {
  display: block;
  width: 100%;
  padding: 2rem;
  text-align: center;
  border: 2px dashed var(--color-border-hover);
  border-radius: 8px;
  background: var(--color-background-soft);
  cursor: pointer;
}

.dropzone:hover {
  border-color: var(--color-accent);
}

.file-list {
  list-style: none;
  padding: 0;
}
</style>
