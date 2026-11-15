<script setup lang="ts">
import PopUp from '@/components/PopUp.vue'

// busy: the action is running, the buttons cannot be pressed again
defineProps<{
  title: string
  message: string
  confirmLabel: string
  busy?: boolean
}>()

const emit = defineEmits<{
  (event: 'confirm'): void
  (event: 'cancel'): void
}>()
</script>

<template>
  <PopUp @close="emit('cancel')">
    <h2 class="text-lg font-semibold text-heading mb-2">{{ title }}</h2>
    <p class="mb-4">{{ message }}</p>
    <div class="flex gap-3">
      <button
        type="button"
        data-test="confirm"
        class="btn"
        :disabled="busy"
        @click="emit('confirm')"
      >
        {{ confirmLabel }}
      </button>
      <button
        type="button"
        data-test="cancel"
        class="min-h-11 py-2 px-4 border border-accent rounded-md bg-transparent text-accent cursor-pointer hover:bg-surface-mute disabled:opacity-40 disabled:cursor-not-allowed"
        :disabled="busy"
        @click="emit('cancel')"
      >
        Cancel
      </button>
    </div>
  </PopUp>
</template>
