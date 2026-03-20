<template>
  <div class="scanner-wrap">
    <video ref="videoEl" class="scanner-video" autoplay playsinline muted />
    <div class="scanner-overlay">
      <div class="scanner-frame" />
      <p class="scanner-hint">将条形码对准框内</p>
    </div>
    <van-button type="default" size="small" class="scanner-close" @click="emit('close')">取消</van-button>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { BrowserMultiFormatReader } from '@zxing/browser'

const emit = defineEmits(['detected', 'close', 'error'])
const videoEl = ref(null)
let reader = null
let controls = null

onMounted(async () => {
  reader = new BrowserMultiFormatReader()
  try {
    controls = await reader.decodeFromVideoDevice(null, videoEl.value, (result, err) => {
      if (result) {
        emit('detected', result.getText())
        stopScanning()
      }
      // Ignore continuous decode errors (no barcode in frame)
    })
  } catch (e) {
    emit('error', e.message)
  }
})

function stopScanning() {
  controls?.stop()
}

onUnmounted(() => stopScanning())
</script>

<style scoped>
.scanner-wrap {
  position: fixed; inset: 0; background: #000; z-index: 2000;
  display: flex; align-items: center; justify-content: center;
}
.scanner-video { width: 100%; height: 100%; object-fit: cover; }
.scanner-overlay {
  position: absolute; inset: 0;
  display: flex; flex-direction: column; align-items: center; justify-content: center;
}
.scanner-frame {
  width: min(85vw, 360px); height: min(28vw, 120px);
  border: 2px solid #1989fa;
  border-radius: 8px;
  box-shadow: 0 0 0 9999px rgba(0,0,0,0.5);
}
.scanner-hint { color: #fff; margin-top: 16px; font-size: 14px; }
.scanner-close { position: absolute; bottom: 48px; }
</style>
