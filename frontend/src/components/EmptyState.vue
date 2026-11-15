<script setup lang="ts">
// icon: the classes of an icon (written in full by whoever uses this, see SportIcon)
defineProps<{ icon: string; title: string }>()
</script>

<template>
  <section class="py-10 text-center">
    <span :class="icon" class="size-10 text-accent" aria-hidden="true"></span>
    <h2 class="mt-3 mb-1 text-lg font-semibold text-heading">{{ title }}</h2>
    <p><slot /></p>
  </section>
</template>
