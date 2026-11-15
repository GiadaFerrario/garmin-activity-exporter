<script setup lang="ts">
import { computed } from 'vue'
import IconButton from '@/components/IconButton.vue'
import SportIcon from '@/components/SportIcon.vue'
import {
  formatDistance,
  formatDuration,
  formatHeartRate,
  formatSport,
  formatStart,
} from '@/format.ts'
import type { HistoryActivity } from '@/api/historyActivity.ts'

const props = defineProps<{ activity: HistoryActivity }>()

const emit = defineEmits<{
  (event: 'open'): void
  (event: 'delete'): void
}>()

// Only what the activity has: a HIIT has no distance, a ride without a strap has no heart rate
const numbers = computed(() =>
  [
    formatDuration(props.activity.durationInSeconds),
    formatDistance(props.activity.distanceInMeters, props.activity.sport),
    formatHeartRate(props.activity.avgHR, props.activity.maxHR),
    props.activity.calories === null ? null : `${props.activity.calories} kcal`,
  ]
    .filter((value) => value !== null)
    .join(' · '),
)
</script>

<template>
  <li class="flex items-center gap-2 border border-line rounded-lg bg-surface-soft">
    <!-- the whole card is one button, so that it is easy to press and works with the keyboard;
         the delete button is next to it, not inside it (a button cannot hold a button) -->
    <button
      type="button"
      data-test="open"
      class="flex flex-1 min-w-0 items-center gap-3 p-4 text-left cursor-pointer rounded-lg hover:bg-surface-mute"
      @click="emit('open')"
    >
      <SportIcon :sport="activity.sport" class="shrink-0 text-accent" />
      <span class="min-w-0">
        <span class="block font-semibold text-heading truncate">{{ activity.name }}</span>
        <span class="block text-sm"
          >{{ formatSport(activity.sport) }} · {{ formatStart(activity.startTime) }}</span
        >
        <span class="block text-sm">{{ numbers }}</span>
      </span>
    </button>
    <IconButton
      icon="trash"
      :label="`Delete ${activity.name}`"
      class="mr-3 shrink-0"
      @click="emit('delete')"
    />
  </li>
</template>
