<script setup lang="ts">
import { computed, ref } from 'vue'
import SyncOutcomeCard from '@/components/SyncOutcomeCard.vue'
import { syncGarmin } from '@/api/garmin.ts'
import { summarizeSync } from '@/syncSummary.ts'
import type { GarminSyncResult } from '@/api/garminSync.ts'

// What the last refresh found. It lives only here (the tab keeps it while the app is open, see KeepAlive in App.vue):
// what matters is saved in the history by the backend.
const result = ref<GarminSyncResult | null>(null)
const errorMessage = ref<string | null>(null)
const isSyncing = ref(false)

const summary = computed(() => (result.value === null ? null : summarizeSync(result.value)))

async function refresh() {
  if (isSyncing.value) {
    return
  }

  errorMessage.value = null
  isSyncing.value = true
  try {
    result.value = await syncGarmin()
  } catch (error) {
    // the list of the last refresh stays: it is still true
    errorMessage.value = error instanceof Error ? error.message : 'Unexpected error'
  } finally {
    isSyncing.value = false
  }
}
</script>

<template>
  <section>
    <div class="flex items-center justify-between gap-3 mb-4">
      <h2 class="text-lg font-semibold text-heading">Garmin</h2>
      <button
        type="button"
        data-test="refresh"
        class="btn inline-flex items-center gap-2"
        :disabled="isSyncing"
        @click="refresh"
      >
        <span
          :class="isSyncing ? 'i-lucide-loader-circle animate-spin' : 'i-lucide-refresh-cw'"
          class="size-5"
          aria-hidden="true"
        ></span>
        {{ isSyncing ? 'Refreshing…' : 'Refresh' }}
      </button>
    </div>

    <p v-if="result === null" data-test="intro" class="mb-4">
      Press Refresh to bring the activities of the last 30 days from Garmin Connect into your
      history. Run, bike, swim, HIIT and strength are saved; the others are only listed.
    </p>

    <p v-if="errorMessage" role="alert" class="mb-4 text-error">{{ errorMessage }}</p>

    <template v-if="result !== null">
      <p data-test="summary" role="status" class="mb-1 font-semibold text-heading">{{ summary }}</p>
      <p v-if="result.postponedCount > 0" data-test="postponed" class="mb-4">
        {{ result.postponedCount }}
        {{ result.postponedCount === 1 ? 'activity is' : 'activities are' }} waiting for the next
        sync: refresh again.
      </p>

      <ul class="list-none p-0 mt-4 flex flex-col gap-3">
        <SyncOutcomeCard
          v-for="outcome in result.outcomes"
          :key="outcome.garminActivityId"
          :outcome="outcome"
        />
      </ul>
    </template>
  </section>
</template>
