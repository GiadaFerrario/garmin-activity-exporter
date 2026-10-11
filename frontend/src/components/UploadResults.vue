<script setup lang="ts">
import type { UploadResponseItem } from '@/api/uploadResponseItem.ts'
import IconButton from '@/components/IconButton.vue'

const props = defineProps<{ responses: UploadResponseItem[] }>()

const emit = defineEmits<{
  (event: 'show-text', response: UploadResponseItem): void
}>()
</script>

<template>
  <ul class="results">
    <li v-for="response in props.responses" :key="response.fileName" class="card">
      <div class="card-main">
        <div class="card-title">
          <span class="file-name">{{ response.fileName }}</span>
          <span class="badge" :class="response.status.toLowerCase()">{{ response.status }}</span>
        </div>
        <p class="message">{{ response.message }}</p>
      </div>
      <IconButton
        v-if="response.text !== null"
        icon="file"
        label="Show text"
        @click="emit('show-text', response)"
      />
    </li>
  </ul>
</template>

<style scoped>
.results {
  list-style: none;
  padding: 0;
  margin-top: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  padding: 1rem 1.25rem;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-background-soft);
}

.card-title {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.file-name {
  font-weight: 600;
  color: var(--color-heading);
}

.message {
  opacity: 0.75;
}

.badge {
  padding: 0.1rem 0.6rem;
  border: 1px solid currentColor;
  border-radius: 999px;
  font-size: 0.8rem;
}

.badge.success {
  color: var(--color-success);
}

.badge.error {
  color: var(--color-error);
}
</style>
