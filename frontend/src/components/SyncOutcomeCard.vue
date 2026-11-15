<script setup lang="ts">
import { computed } from 'vue'
import SportIcon from '@/components/SportIcon.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { formatDuration, formatStart } from '@/format.ts'
import type { SyncOutcome } from '@/api/garminSync.ts'

const props = defineProps<{ outcome: SyncOutcome }>()

// An activity of a type that is not supported is only listed: its name, a default icon, nothing else
const isSupported = computed(() => props.outcome.sport !== null)

// "Not supported" says it all with the badge; the other messages explain what went wrong
const message = computed(() =>
  props.outcome.status === 'Unsupported' ? null : props.outcome.message,
)
</script>

<template>
  <li class="flex items-start gap-3 p-4 border border-line rounded-lg bg-surface-soft">
    <SportIcon :sport="outcome.sport" class="shrink-0 text-accent" />
    <div class="min-w-0 flex-1">
      <div class="flex flex-wrap items-center gap-x-3 gap-y-1">
        <span class="font-semibold text-heading">{{ outcome.name }}</span>
        <StatusBadge :status="outcome.status" />
      </div>
      <p v-if="isSupported" class="text-sm">
        {{ formatStart(outcome.startTime) }} · {{ formatDuration(outcome.durationInSeconds) }}
      </p>
      <p v-if="message" data-test="message" class="text-sm">{{ message }}</p>
    </div>
  </li>
</template>
