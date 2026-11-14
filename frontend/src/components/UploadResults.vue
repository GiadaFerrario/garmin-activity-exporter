<script setup lang="ts">
import type { UploadResponseItem } from '@/api/uploadResponseItem.ts'
import type { UploadStatus } from '@/api/uploadStatus.ts'
import IconButton from '@/components/IconButton.vue'

const props = defineProps<{ responses: UploadResponseItem[] }>()

const emit = defineEmits<{
  (event: 'show-text', response: UploadResponseItem): void
}>()

// The class names are written out in full: UnoCSS finds them by reading the source text,
// so a class built by concatenating strings (e.g. `text-${status}`) would never be generated.
const badgeColors: Record<UploadStatus, string> = {
  Success: 'text-success',
  Error: 'text-error',
  AlreadyUploaded: '',
}
</script>

<template>
  <ul class="list-none p-0 mt-6 flex flex-col gap-3">
    <li
      v-for="response in props.responses"
      :key="response.fileName"
      class="flex justify-between items-center gap-4 py-4 px-5 border border-line rounded-lg bg-surface-soft"
    >
      <div>
        <div class="flex items-center gap-3">
          <span class="font-semibold text-heading">{{ response.fileName }}</span>
          <span
            class="py-[0.1rem] px-[0.6rem] border border-current rounded-[999px] text-[0.8rem]"
            :class="badgeColors[response.status]"
            >{{ response.status }}</span
          >
        </div>
        <p class="opacity-75">{{ response.message }}</p>
      </div>
      <IconButton
        v-if="response.text !== null"
        icon="file"
        label="Show text"
        @click="emit('show-text', response)"
      />
    </li>
  </ul>
</template>
