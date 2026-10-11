<script setup lang="ts">
import { ref } from 'vue'

const selectedFiles = ref<File[]>([])

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
  console.log("Emitting", selectedFiles.value)
}
</script>

<template>
  <section>
    <input type="file" multiple accept=".fit" @change="onFilesChosen" />
    <ul>
      <li v-for="file in selectedFiles" :key="file.name">{{ file.name }}</li>
    </ul>
    <button :disabled="selectedFiles.length === 0" @click="onUploadClicked">Upload</button>
  </section>
</template>
