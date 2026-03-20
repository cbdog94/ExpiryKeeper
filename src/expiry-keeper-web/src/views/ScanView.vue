<template>
  <div class="page">
    <van-nav-bar title="扫码" left-arrow @click-left="router.back()" />

    <div v-if="!scanning" class="scan-entry">
      <van-empty image="search" description="点击下方按钮开始扫描条形码" />
      <div class="scan-btn-wrap">
        <van-button type="primary" round icon="scan" block @click="startScan">开始扫码</van-button>
      </div>
      <van-cell-group inset title="或手动输入条形码">
        <van-field v-model="manualBarcode" placeholder="输入条形码编号" clearable>
          <template #button>
            <van-button size="small" type="primary" @click="onBarcodeDetected(manualBarcode)">确认</van-button>
          </template>
        </van-field>
      </van-cell-group>
    </div>

    <BarcodeScanner
      v-if="scanning"
      @detected="onBarcodeDetected"
      @close="scanning = false"
      @error="onScanError"
    />
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { showToast } from 'vant'
import BarcodeScanner from '@/components/BarcodeScanner.vue'

const router = useRouter()
const scanning = ref(false)
const manualBarcode = ref('')

function startScan() {
  scanning.value = true
}

async function onBarcodeDetected(barcode) {
  scanning.value = false
  if (!barcode) return
  // Navigate to add form with barcode prefilled
  router.push({ path: '/medicines/add', query: { barcode } })
}

function onScanError(msg) {
  scanning.value = false
  showToast(`摄像头错误: ${msg}`)
}
</script>

<style scoped>
.page { min-height: 100vh; background: #f7f8fa; }
.scan-entry { padding: 32px 0; }
.scan-btn-wrap { padding: 0 32px 24px; }
</style>
