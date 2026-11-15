<script setup lang="ts">
import { computed } from 'vue'
import type { Sport } from '@/api/sport.ts'

// null is an activity of a type that is not supported: it gets the default icon
const props = defineProps<{ sport: Sport | null }>()

// The class names are written out in full: UnoCSS finds them by reading the source text,
// so a class built by concatenating strings (`i-lucide-${...}`) would never be generated.
const icons: Record<Sport, string> = {
  Run: 'i-lucide-footprints',
  Bike: 'i-lucide-bike',
  Swim: 'i-lucide-waves',
  Hiit: 'i-lucide-flame',
  Strength: 'i-lucide-dumbbell',
}
const defaultIcon = 'i-lucide-activity'

// The icon is aria-hidden in the template: it is decoration, the name of the sport is always written next to it

const iconClass = computed(() => (props.sport === null ? defaultIcon : icons[props.sport]))
</script>

<template>
  <span :class="iconClass" class="size-6" aria-hidden="true"></span>
</template>
