<script setup lang="ts">
// The icon classes are written out in full: UnoCSS finds them by reading the source text.
const tabs = [
  { to: '/garmin', label: 'Garmin', icon: 'i-lucide-watch' },
  { to: '/history', label: 'History', icon: 'i-lucide-history' },
  { to: '/export', label: 'Export', icon: 'i-lucide-file-text' },
]
</script>

<template>
  <!-- The bar is as wide as the screen, its tabs sit in the central column;
       the bottom padding keeps the tabs above the home indicator of phones -->
  <nav
    aria-label="Main"
    class="fixed inset-x-0 bottom-0 z-10 border-t border-line bg-surface pb-[env(safe-area-inset-bottom)]"
  >
    <ul class="column flex">
      <li v-for="tab in tabs" :key="tab.to" class="flex-1">
        <!-- RouterLink adds aria-current="page" and the active-class to the open tab -->
        <RouterLink
          :to="tab.to"
          active-class="text-accent"
          class="flex flex-col items-center justify-center gap-1 min-h-14 text-xs"
        >
          <span :class="tab.icon" class="size-6" aria-hidden="true"></span>
          {{ tab.label }}
        </RouterLink>
      </li>
    </ul>
  </nav>
</template>
