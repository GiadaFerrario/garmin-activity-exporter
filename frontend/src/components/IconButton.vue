<script setup lang="ts">
type Icon = 'file' | 'copy' | 'close' | 'trash' | 'refresh'

defineProps<{
  icon: Icon
  label: string
  disabled?: boolean
}>()

// Icons of the Lucide set (ISC license), as classes of UnoCSS. Written out in full, see SportIcon.
const icons: Record<Icon, string> = {
  file: 'i-lucide-file-text',
  copy: 'i-lucide-copy',
  close: 'i-lucide-x',
  trash: 'i-lucide-trash-2',
  refresh: 'i-lucide-refresh-cw',
}
</script>

<template>
  <button
    class="inline-flex items-center justify-center p-3 border border-accent rounded-md bg-transparent text-accent cursor-pointer hover:bg-surface-mute disabled:opacity-40 disabled:cursor-not-allowed"
    type="button"
    :aria-label="label"
    :title="label"
    :disabled="disabled"
  >
    <span :class="icons[icon]" class="size-[18px]" aria-hidden="true"></span>
  </button>
</template>
