import { createRouter, createWebHistory } from 'vue-router'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    // The app opens on the Garmin tab: Refresh, then the new activities are in the history
    { path: '/', redirect: '/garmin' },
    // The views are loaded when they are opened for the first time (lazy loading)
    { path: '/garmin', name: 'garmin', component: () => import('@/views/GarminView.vue') },
    { path: '/history', name: 'history', component: () => import('@/views/HistoryView.vue') },
    { path: '/export', name: 'export', component: () => import('@/views/ExportView.vue') },
  ],
})
