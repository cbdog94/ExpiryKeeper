<template>
  <van-tabbar v-model="active" route safe-area-inset-bottom>
    <van-tabbar-item name="home" icon="home-o" to="/">首页</van-tabbar-item>
    <van-tabbar-item name="scan" icon="scan" to="/scan">扫码</van-tabbar-item>
    <van-tabbar-item name="medicines" icon="records-o" to="/medicines"
      :badge="badgeCount || undefined">清单</van-tabbar-item>
    <van-tabbar-item name="settings" icon="setting-o" to="/settings">设置</van-tabbar-item>
  </van-tabbar>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useMedicinesStore } from '@/stores/medicines'

const store = useMedicinesStore()
const active = ref('home')
const badgeCount = computed(() => store.expiredCount + store.expiringSoonCount || 0)
</script>
