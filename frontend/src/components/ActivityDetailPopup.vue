<script setup lang="ts">
import { computed } from 'vue'
import IconButton from '@/components/IconButton.vue'
import PopUp from '@/components/PopUp.vue'
import SportIcon from '@/components/SportIcon.vue'
import { useCopyFeedback } from '@/composables/useCopyFeedback.ts'
import {
  formatDistance,
  formatDuration,
  formatHeartRate,
  formatSport,
  formatStart,
} from '@/format.ts'
import type { HistoryActivity } from '@/api/historyActivity.ts'

// The activity comes from the list; the text is loaded apart (by whoever shows this popup),
// because it is written by the backend when one activity is opened
const props = defineProps<{
  activity: HistoryActivity
  text: string | null
  loading: boolean
  error: string | null
}>()

const emit = defineEmits<{ (event: 'close'): void }>()

const { message, copy } = useCopyFeedback()

// Only what the activity has
const stats = computed(() => {
  const { activity } = props
  const rows: [string, string | null][] = [
    ['Duration', formatDuration(activity.durationInSeconds)],
    ['Distance', formatDistance(activity.distanceInMeters, activity.sport)],
    ['Elevation gain', activity.elevationGain === null ? null : `${activity.elevationGain} m`],
    ['Heart rate', formatHeartRate(activity.avgHR, activity.maxHR)],
    ['Calories', activity.calories === null ? null : `${activity.calories} kcal`],
  ]
  return rows.filter((row): row is [string, string] => row[1] !== null)
})

const showText = computed(() => props.activity.hasCoachText && props.text !== null)
</script>

<template>
  <PopUp @close="emit('close')">
    <!-- Close is at the top: the text can be long and the buttons must not be at the end of it -->
    <div class="flex items-start gap-3 mb-4">
      <SportIcon :sport="activity.sport" class="shrink-0 text-accent mt-2" />
      <div class="min-w-0 flex-1">
        <h2 class="text-lg font-semibold text-heading break-words">{{ activity.name }}</h2>
        <p class="text-sm">
          {{ formatSport(activity.sport) }} · {{ formatStart(activity.startTime) }}
        </p>
      </div>
      <IconButton icon="close" label="Close" class="shrink-0" @click="emit('close')" />
    </div>

    <dl class="grid grid-cols-2 gap-x-6 gap-y-3 mb-4">
      <div v-for="[label, value] in stats" :key="label">
        <dt class="text-sm">{{ label }}</dt>
        <dd class="font-semibold text-heading">{{ value }}</dd>
      </div>
    </dl>

    <p v-if="loading" data-test="loading">Loading the text…</p>
    <p v-else-if="error" role="alert" class="text-error">{{ error }}</p>
    <div v-else-if="showText">
      <div class="flex items-center justify-between gap-3 mb-2">
        <h3 class="font-semibold text-heading">Text for the coach</h3>
        <IconButton icon="copy" label="Copy" @click="copy(text ?? '')" />
      </div>
      <p v-if="message" data-test="copy-message" role="status" class="mb-2">{{ message }}</p>
      <!-- only the text scrolls sideways on a narrow screen, not the whole dialog -->
      <pre data-test="text" class="overflow-x-auto">{{ text }}</pre>
    </div>
    <p v-else-if="!activity.hasCoachText" data-test="no-text">
      This sport has no text for the coach yet.
    </p>
  </PopUp>
</template>
