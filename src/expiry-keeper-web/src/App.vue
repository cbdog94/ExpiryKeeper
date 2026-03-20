<template>
  <div class="app-container">
    <router-view v-if="authStore.initialized" />
    <van-loading v-else class="app-loading" size="36px" vertical>加载中...</van-loading>
    <BottomTabBar v-if="authStore.isLoggedIn" />
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import BottomTabBar from '@/components/BottomTabBar.vue'

const authStore = useAuthStore()
onMounted(() => authStore.initialize())
</script>

<style>
* { box-sizing: border-box; margin: 0; padding: 0; }
body { font-family: -apple-system, BlinkMacSystemFont, 'PingFang SC', sans-serif; background: #f7f8fa; }
.app-container { display: flex; flex-direction: column; min-height: 100vh; min-height: 100dvh; }
.app-loading { position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%); }
</style>
