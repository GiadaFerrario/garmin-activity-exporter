<script setup lang="ts">
import AppHeader from '@/components/AppHeader.vue'
import BottomNav from '@/components/BottomNav.vue'
</script>

<template>
  <AppHeader />
  <!-- pb: room for the fixed bottom bar (3.5 rem tabs + border) and the safe area -->
  <main class="column pt-6 pb-[calc(5rem+env(safe-area-inset-bottom))]">
    <!-- KeepAlive keeps the state of a screen (e.g. the upload results) when you change tab -->
    <RouterView v-slot="{ Component }">
      <KeepAlive>
        <component :is="Component" />
      </KeepAlive>
    </RouterView>
  </main>
  <BottomNav />
</template>
