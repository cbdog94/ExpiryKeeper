import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '@/services/api'

export const useMedicinesStore = defineStore('medicines', () => {
  const medicines = ref([])
  const loading = ref(false)
  const error = ref(null)

  const expiredCount = computed(() =>
    medicines.value.filter(m => m.daysUntilExpiry < 0).length)

  const expiringSoonCount = computed(() =>
    medicines.value.filter(m => m.daysUntilExpiry >= 0 && m.daysUntilExpiry <= 7).length)

  async function fetchAll() {
    loading.value = true
    error.value = null
    try {
      const { data } = await api.get('/medicines')
      medicines.value = data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function create(payload) {
    const { data } = await api.post('/medicines', payload)
    medicines.value.push(data)
    return data
  }

  async function update(id, payload) {
    const { data } = await api.put(`/medicines/${id}`, payload)
    const idx = medicines.value.findIndex(m => m.id === id)
    if (idx !== -1) medicines.value[idx] = data
    return data
  }

  async function remove(id) {
    await api.delete(`/medicines/${id}`)
    medicines.value = medicines.value.filter(m => m.id !== id)
  }

  async function lookupBarcode(barcode) {
    try {
      const { data } = await api.post('/medicines/lookup', { barcode })
      return data
    } catch {
      return null
    }
  }

  return { medicines, loading, error, expiredCount, expiringSoonCount, fetchAll, create, update, remove, lookupBarcode }
})
