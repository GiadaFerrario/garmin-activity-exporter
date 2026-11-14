import { createRouter, createWebHistory } from 'vue-router'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    // Export is the only screen that works for now: the default becomes /garmin when it exists
    { path: '/', redirect: '/export' },
    // The views are loaded when they are opened for the first time (lazy loading)
    { path: '/garmin', name: 'garmin', component: () => import('@/views/GarminView.vue') },
    { path: '/history', name: 'history', component: () => import('@/views/HistoryView.vue') },
    { path: '/export', name: 'export', component: () => import('@/views/ExportView.vue') },
  ],
})
