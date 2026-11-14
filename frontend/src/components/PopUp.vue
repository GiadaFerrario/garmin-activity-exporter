<script setup lang="ts">
const emit = defineEmits<{ (event: 'close'): void }>()
</script>

<template>
  <div
    class="fixed inset-0 bg-black/50 flex items-center justify-center"
    @click.self="emit('close')"
  >
    <div
      class="bg-surface p-6 rounded-lg max-w-[90vw] max-h-[90vh] overflow-auto"
      role="dialog"
      aria-modal="true"
    >
      <slot />
    </div>
  </div>
</template>
