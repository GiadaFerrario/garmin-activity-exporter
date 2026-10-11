<script setup lang="ts">
// Icon shapes from Feather Icons (MIT license): https://feathericons.com/
defineProps<{
  icon: 'file' | 'copy' | 'close'
  label: string
}>()
</script>

<template>
  <button class="icon-button" type="button" :aria-label="label" :title="label">
    <svg
      viewBox="0 0 24 24"
      width="18"
      height="18"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
    >
      <template v-if="icon === 'file'">
        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
        <polyline points="14 2 14 8 20 8" />
        <line x1="16" y1="13" x2="8" y2="13" />
        <line x1="16" y1="17" x2="8" y2="17" />
      </template>
      <template v-else-if="icon === 'copy'">
        <rect x="9" y="9" width="13" height="13" rx="2" ry="2" />
        <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
      </template>
      <template v-else>
        <line x1="18" y1="6" x2="6" y2="18" />
        <line x1="6" y1="6" x2="18" y2="18" />
      </template>
    </svg>
  </button>
</template>

<style scoped>
.icon-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.5rem;
  background: transparent;
  color: var(--color-accent);
}

.icon-button:hover {
  background: var(--color-background-mute);
}
</style>
