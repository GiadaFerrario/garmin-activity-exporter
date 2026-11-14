<script setup lang="ts">
const emit = defineEmits<{ (event: 'close'): void }>()
</script>

<template>
  <!-- On a phone the dialog is a sheet at the bottom of the screen (above the bottom bar, z-50),
       from the sm breakpoint up it is a box in the middle -->
  <div
    class="fixed inset-0 z-50 bg-black/50 flex items-end justify-center sm:items-center"
    @click.self="emit('close')"
  >
    <div
      class="w-full sm:w-auto max-w-full sm:max-w-[90vw] max-h-[90dvh] overflow-auto bg-surface p-6 pb-[calc(1.5rem+env(safe-area-inset-bottom))] sm:pb-6 rounded-t-lg sm:rounded-lg"
      role="dialog"
      aria-modal="true"
    >
      <slot />
    </div>
  </div>
</template>
