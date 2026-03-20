<template>
  <van-swipe-cell>
    <van-cell :title="medicine.name" :label="medicine.category || ''" clickable @click="emit('edit', medicine)">
      <template #right-icon>
        <div class="right-wrap">
          <van-tag :type="tagType" plain>{{ tagText }}</van-tag>
          <span class="expire-date">{{ medicine.expireDate }}</span>
        </div>
      </template>
    </van-cell>
    <template #right>
      <van-button square type="danger" text="删除" @click="emit('delete', medicine.id)" />
    </template>
  </van-swipe-cell>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({ medicine: Object })
const emit = defineEmits(['edit', 'delete'])

const tagType = computed(() => {
  const d = props.medicine.daysUntilExpiry
  if (d < 0) return 'danger'
  if (d <= 7) return 'warning'
  return 'success'
})

const tagText = computed(() => {
  const d = props.medicine.daysUntilExpiry
  if (d < 0) return `已过期 ${Math.abs(d)} 天`
  if (d === 0) return '今日到期'
  if (d <= 7) return `${d} 天后到期`
  return `${d} 天`
})
</script>

<style scoped>
.right-wrap {
  display: flex;
  align-items: center;
  gap: 6px;
}
.expire-date { font-size: 12px; color: #999; white-space: nowrap; }
</style>
