<script setup lang="ts">
import type { SyncStatus } from '@/api/garminSync.ts'

defineProps<{ status: SyncStatus }>()

const labels: Record<SyncStatus, string> = {
  Saved: 'Saved',
  Unsupported: 'Not supported',
  Postponed: 'Waiting',
  Error: 'Error',
}

// Written out in full, see SportIcon. Not supported has no color: it is not good news and not bad news.
const colors: Record<SyncStatus, string> = {
  Saved: 'text-success',
  Unsupported: '',
  Postponed: 'text-accent',
  Error: 'text-error',
}
</script>

<template>
  <span
    data-test="status"
    class="py-[0.1rem] px-[0.6rem] border border-current rounded-[999px] text-[0.8rem] whitespace-nowrap"
    :class="colors[status]"
    >{{ labels[status] }}</span
  >
</template>
