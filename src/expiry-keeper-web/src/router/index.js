import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'home', component: () => import('@/views/HomeView.vue'), meta: { requiresAuth: true } },
    { path: '/scan', name: 'scan', component: () => import('@/views/ScanView.vue'), meta: { requiresAuth: true } },
    { path: '/medicines', name: 'medicines', component: () => import('@/views/MedicinesView.vue'), meta: { requiresAuth: true } },
    { path: '/medicines/add', name: 'add-medicine', component: () => import('@/views/MedicineFormView.vue'), meta: { requiresAuth: true } },
    { path: '/medicines/:id/edit', name: 'edit-medicine', component: () => import('@/views/MedicineFormView.vue'), meta: { requiresAuth: true } },
    { path: '/settings', name: 'settings', component: () => import('@/views/SettingsView.vue'), meta: { requiresAuth: true } }
  ]
})

router.beforeEach(async (to) => {
  if (!to.meta.requiresAuth) return true
  const auth = useAuthStore()
  if (!auth.initialized) await auth.initialize()
  if (!auth.isLoggedIn) {
    await auth.login()
    return false
  }
  return true
})

export default router
