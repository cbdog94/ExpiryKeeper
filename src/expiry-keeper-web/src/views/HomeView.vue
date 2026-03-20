<template>
  <div class="page">
    <van-nav-bar title="效期管家">
      <template #right>
        <span class="nav-user">{{ authStore.displayName }}</span>
      </template>
    </van-nav-bar>

    <van-pull-refresh v-model="refreshing" @refresh="onRefresh">

      <!-- 统计卡片 -->
      <div class="stats-row">
        <div class="stat-card" @click="router.push('/medicines')">
          <div class="stat-num">{{ store.medicines.length }}</div>
          <div class="stat-label">全部物品</div>
        </div>
        <div class="stat-card warn" @click="router.push({ path: '/medicines', query: { tab: 'soon' } })">
          <div class="stat-num">{{ store.expiringSoonCount }}</div>
          <div class="stat-label">即将到期</div>
        </div>
        <div class="stat-card danger" @click="router.push({ path: '/medicines', query: { tab: 'expired' } })">
          <div class="stat-num">{{ store.expiredCount }}</div>
          <div class="stat-label">已过期</div>
        </div>
      </div>

      <!-- 空状态：引导用户添加第一条药品 -->
      <div v-if="!store.loading && !store.medicines.length" class="onboarding">
        <img src="/icons/icon-192.png" class="onboarding-logo" alt="logo" />
        <h2 class="onboarding-title">开始记录保质期</h2>
        <p class="onboarding-desc">扫描条形码或拍照识别，记录药品、食品、日用品的保质期，到期自动提醒</p>
        <div class="onboarding-actions">
          <van-button type="primary" round icon="scan" block @click="router.push('/scan')">
            扫码添加
          </van-button>
          <van-button plain round icon="plus" block style="margin-top:12px" @click="router.push('/medicines/add')">
            手动添加
          </van-button>
        </div>
      </div>

      <template v-else>
        <!-- 已过期 -->
        <template v-if="expired.length">
          <div class="section-header">
            <span class="section-dot danger" />已过期
          </div>
          <van-cell-group inset>
            <MedicineCard v-for="m in expired" :key="m.id" :medicine="m" @edit="goEdit" @delete="handleDelete" />
          </van-cell-group>
        </template>

        <!-- 即将到期 -->
        <template v-if="expiringSoon.length">
          <div class="section-header">
            <span class="section-dot warn" />即将到期（7天内）
          </div>
          <van-cell-group inset>
            <MedicineCard v-for="m in expiringSoon" :key="m.id" :medicine="m" @edit="goEdit" @delete="handleDelete" />
          </van-cell-group>
        </template>

        <!-- 一切正常 -->
        <div v-if="!expired.length && !expiringSoon.length" class="all-good">
          <van-icon name="passed" size="48" color="#07c160" />
          <p>所有药品均在有效期内</p>
        </div>
      </template>

    </van-pull-refresh>

    <!-- FAB -->
    <van-button type="primary" round icon="plus" class="fab" @click="router.push('/medicines/add')" />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { showConfirmDialog } from 'vant'
import { useMedicinesStore } from '@/stores/medicines'
import { useAuthStore } from '@/stores/auth'
import MedicineCard from '@/components/MedicineCard.vue'

const store = useMedicinesStore()
const authStore = useAuthStore()
const router = useRouter()
const refreshing = ref(false)

const expiringSoon = computed(() =>
  store.medicines.filter(m => m.daysUntilExpiry >= 0 && m.daysUntilExpiry <= 7))
const expired = computed(() =>
  store.medicines.filter(m => m.daysUntilExpiry < 0))

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
.page { padding-bottom: 100px; background: #f7f8fa; min-height: 100vh; }
.nav-user { font-size: 14px; color: #1989fa; }

/* 统计卡片 */
.stats-row {
  display: flex; gap: 10px;
  padding: 16px 16px 8px;
}
.stat-card {
  flex: 1; background: #fff; border-radius: 12px;
  padding: 14px 8px; text-align: center;
  box-shadow: 0 1px 4px rgba(0,0,0,0.06);
  cursor: pointer; transition: opacity .15s;
}
.stat-card:active { opacity: .7; }
.stat-num { font-size: 28px; font-weight: 700; color: #323233; line-height: 1.2; }
.stat-label { font-size: 12px; color: #969799; margin-top: 4px; }
.stat-card.warn .stat-num { color: #ff976a; }
.stat-card.danger .stat-num { color: #ee0a24; }

/* 引导页 */
.onboarding {
  display: flex; flex-direction: column; align-items: center;
  padding: 48px 32px 0;
  text-align: center;
}
.onboarding-logo { width: 80px; height: 80px; border-radius: 20px; margin-bottom: 20px; }
.onboarding-title { font-size: 20px; font-weight: 600; color: #323233; margin-bottom: 10px; }
.onboarding-desc { font-size: 14px; color: #969799; line-height: 1.6; margin-bottom: 32px; }
.onboarding-actions { width: 100%; }

/* 列表区块 */
.section-header {
  display: flex; align-items: center; gap: 6px;
  padding: 16px 16px 8px;
  font-size: 14px; font-weight: 600; color: #323233;
}
.section-dot {
  width: 8px; height: 8px; border-radius: 50%; display: inline-block;
}
.section-dot.warn { background: #ff976a; }
.section-dot.danger { background: #ee0a24; }

/* 全部正常 */
.all-good {
  display: flex; flex-direction: column; align-items: center;
  gap: 12px; padding: 48px 0;
  color: #969799; font-size: 15px;
}

/* FAB */
.fab {
  position: fixed; right: 20px; bottom: 90px;
  width: 52px; height: 52px;
  box-shadow: 0 4px 12px rgba(25,137,250,0.4);
}
</style>
