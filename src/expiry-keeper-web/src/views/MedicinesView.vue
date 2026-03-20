<template>
  <div class="page">
    <van-nav-bar title="物品清单">
      <template #right>
        <van-icon name="plus" size="20" @click="router.push('/medicines/add')" />
      </template>
    </van-nav-bar>

    <van-search v-model="keyword" placeholder="搜索名称" />

    <van-tabs v-model:active="activeTab" sticky offset-top="54px">
      <van-tab title="全部" name="all" />
      <van-tab title="即将到期" name="soon" :badge="store.expiringSoonCount || undefined" />
      <van-tab title="已过期" name="expired" :badge="store.expiredCount || undefined" />
    </van-tabs>

    <van-pull-refresh v-model="refreshing" @refresh="onRefresh">
      <van-loading v-if="store.loading" class="loading" />
      <van-cell-group v-else inset style="margin-top:8px">
        <MedicineCard
          v-for="m in filteredMedicines"
          :key="m.id"
          :medicine="m"
          @edit="goEdit"
          @delete="handleDelete"
        />
        <van-empty v-if="!filteredMedicines.length" description="暂无记录" />
      </van-cell-group>
    </van-pull-refresh>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { showConfirmDialog } from 'vant'
import { useMedicinesStore } from '@/stores/medicines'
import MedicineCard from '@/components/MedicineCard.vue'

const store = useMedicinesStore()
const router = useRouter()
const keyword = ref('')
const activeTab = ref('all')
const refreshing = ref(false)

const filteredMedicines = computed(() => {
  let list = store.medicines
  if (keyword.value) list = list.filter(m => m.name.includes(keyword.value))
  if (activeTab.value === 'soon') list = list.filter(m => m.daysUntilExpiry >= 0 && m.daysUntilExpiry <= 7)
  if (activeTab.value === 'expired') list = list.filter(m => m.daysUntilExpiry < 0)
  return list
})

onMounted(() => store.fetchAll())

async function onRefresh() {
  await store.fetchAll()
  refreshing.value = false
}

function goEdit(medicine) {
  router.push(`/medicines/${medicine.id}/edit`)
}

async function handleDelete(id) {
  await showConfirmDialog({ title: '确认删除', message: '删除后无法恢复' })
  await store.remove(id)
}
</script>

<style scoped>
.page { padding-bottom: 80px; }
.loading { display: flex; justify-content: center; padding: 32px; }
</style>
