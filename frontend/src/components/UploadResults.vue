<script setup lang="ts">
import type { UploadResponseItem } from '@/api/uploadResponseItem.ts'

const props = defineProps<{ responses: UploadResponseItem[] }>()

const emit = defineEmits<{
  (event: 'show-text', response: UploadResponseItem): void
}>()
</script>

<template>
  <ul>
    <li v-for="response in props.responses" :key="response.fileName">
      {{ response.fileName }}: {{ response.status }} - {{ response.message }}
      <button
        v-if="response.text != null"
        @click="emit('show-text', response)"
      >
        Show text
      </button>
    </li>
  </ul>
</template>
