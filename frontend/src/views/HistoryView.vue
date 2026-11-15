<script setup lang="ts">
import { onActivated, onMounted, ref } from 'vue'
import ActivityCard from '@/components/ActivityCard.vue'
import ActivityDetailPopup from '@/components/ActivityDetailPopup.vue'
import ConfirmPopup from '@/components/ConfirmPopup.vue'
import EmptyState from '@/components/EmptyState.vue'
import { deleteHistoryActivity, getHistoryActivity, listHistory } from '@/api/history.ts'
import type { HistoryActivity } from '@/api/historyActivity.ts'

const activities = ref<HistoryActivity[]>([])
const isLoading = ref(false)
const loadError = ref<string | null>(null)

// The activity whose popup is open, and the text that is loaded for it
const selected = ref<HistoryActivity | null>(null)
const detailText = ref<string | null>(null)
const detailLoading = ref(false)
const detailError = ref<string | null>(null)

// The activity that is about to be deleted (the confirmation is open) and what happens to the delete
const toDelete = ref<HistoryActivity | null>(null)
const isDeleting = ref(false)
const deleteError = ref<string | null>(null)

function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : 'Unexpected error'
}

async function load() {
  isLoading.value = true
  loadError.value = null
  try {
    activities.value = await listHistory()
  } catch (error) {
    loadError.value = messageOf(error)
  } finally {
    isLoading.value = false
  }
}

// Loaded when the tab is created, and again every time the tab is opened after that
// (the history may have changed in the Garmin tab). KeepAlive calls onActivated at the first opening too.
onMounted(load)
let firstActivation = true
onActivated(() => {
  if (firstActivation) {
    firstActivation = false
    return
  }
  void load()
})

async function open(activity: HistoryActivity) {
  selected.value = activity
  detailText.value = null
  detailError.value = null
  // a sport without a coach text has nothing more to ask for
  detailLoading.value = activity.hasCoachText

  if (!activity.hasCoachText) {
    return
  }

  try {
    const detail = await getHistoryActivity(activity.id)
    // the popup may have been closed, or another one opened, while the text was coming
    if (selected.value?.id === activity.id) {
      detailText.value = detail.text
    }
  } catch (error) {
    if (selected.value?.id === activity.id) {
      detailError.value = messageOf(error)
    }
  } finally {
    if (selected.value?.id === activity.id) {
      detailLoading.value = false
    }
  }
}

function askToDelete(activity: HistoryActivity) {
  deleteError.value = null
  toDelete.value = activity
}

async function confirmDelete() {
  const activity = toDelete.value
  if (activity === null || isDeleting.value) {
    return
  }

  isDeleting.value = true
  try {
    await deleteHistoryActivity(activity.id)
    activities.value = activities.value.filter((item) => item.id !== activity.id)
  } catch (error) {
    deleteError.value = messageOf(error)
  } finally {
    isDeleting.value = false
    toDelete.value = null
  }
}

function cancelDelete() {
  if (!isDeleting.value) {
    toDelete.value = null
  }
}
</script>

<template>
  <section>
    <h2 class="mb-4 text-lg font-semibold text-heading">History</h2>

    <p v-if="deleteError" data-test="delete-error" role="alert" class="mb-4 text-error">
      Could not delete the activity: {{ deleteError }}
    </p>

    <div v-if="loadError" data-test="error" class="mb-4">
      <p role="alert" class="mb-3 text-error">{{ loadError }}</p>
      <button type="button" data-test="retry" class="btn" @click="load">Try again</button>
    </div>

    <p v-if="isLoading && activities.length === 0 && !loadError" data-test="loading">Loading…</p>
    <EmptyState
      v-else-if="!isLoading && !loadError && activities.length === 0"
      data-test="empty"
      icon="i-lucide-history"
      title="Nothing here yet"
    >
      Open the <RouterLink to="/garmin" class="text-accent underline">Garmin</RouterLink> tab and
      press Refresh to bring your activities here.
    </EmptyState>

    <ul v-if="activities.length > 0" class="list-none p-0 flex flex-col gap-3">
      <ActivityCard
        v-for="activity in activities"
        :key="activity.id"
        :activity="activity"
        @open="open(activity)"
        @delete="askToDelete(activity)"
      />
    </ul>

    <ActivityDetailPopup
      v-if="selected !== null"
      :activity="selected"
      :text="detailText"
      :loading="detailLoading"
      :error="detailError"
      @close="selected = null"
    />

    <ConfirmPopup
      v-if="toDelete !== null"
      title="Delete this activity?"
      :message="`${toDelete.name} will leave the history. The next refresh will not bring it back.`"
      confirm-label="Delete"
      :busy="isDeleting"
      @confirm="confirmDelete"
      @cancel="cancelDelete"
    />
  </section>
</template>
