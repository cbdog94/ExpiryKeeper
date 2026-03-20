<template>
  <div class="page">
    <van-nav-bar :title="isEdit ? '编辑记录' : '添加记录'" left-arrow @click-left="router.back()" />

    <!-- OCR 拍照引导 -->
    <div class="ocr-wrap">
      <div class="ocr-header">
        <span class="ocr-title">拍照识别</span>
        <van-tag type="default" size="small" plain>可选</van-tag>
        <span class="ocr-hint">拍照可自动填写信息，也可直接手动填写</span>
      </div>
      <div class="photo-slots">
        <!-- 名称面 -->
        <div class="photo-slot" @click="triggerCapture('name')">
          <div v-if="!photos.name" class="slot-empty">
            <van-icon name="photograph" size="28" color="#1989fa" />
            <span class="slot-label-main">名称/正面</span>
            <span class="slot-label-sub">品名、品牌</span>
          </div>
          <template v-else>
            <img :src="photos.name.url" class="slot-img" />
            <div class="slot-retake" @click.stop="triggerCapture('name')">
              <van-icon name="replay" size="14" color="#fff" />重拍
            </div>
          </template>
          <div class="slot-tag" :class="photos.name ? 'done' : ''">名称面</div>
        </div>

        <!-- 有效期面 -->
        <div class="photo-slot" @click="triggerCapture('expiry')">
          <div v-if="!photos.expiry" class="slot-empty">
            <van-icon name="photograph" size="28" color="#1989fa" />
            <span class="slot-label-main">有效期面</span>
            <span class="slot-label-sub">生产日期、到期日</span>
          </div>
          <template v-else>
            <img :src="photos.expiry.url" class="slot-img" />
            <div class="slot-retake" @click.stop="triggerCapture('expiry')">
              <van-icon name="replay" size="14" color="#fff" />重拍
            </div>
          </template>
          <div class="slot-tag" :class="photos.expiry ? 'done' : ''">有效期面</div>
        </div>
      </div>

      <van-button
        v-if="hasPhotos"
        type="primary" round block icon="search"
        :loading="ocring" loading-text="识别中..."
        style="margin-top:10px"
        @click="runOcr"
      >开始识别</van-button>

      <input ref="fileInput" type="file" accept="image/*" capture="environment"
        style="display:none" @change="onImageSelected" />
    </div>

    <van-form @submit="onSubmit">
      <van-cell-group inset>

        <!-- 条形码（可选） -->
        <van-field
          v-model="form.barcode"
          name="barcode"
          label="条形码"
          placeholder="可选，开封商品可不填"
          clearable
          center
        >
          <template #button>
            <van-button size="small" type="primary" @click="scanning = true">扫码</van-button>
          </template>
        </van-field>

        <!-- 名称（必填） -->
        <van-field
          v-model="form.name"
          name="name"
          label="名称"
          placeholder="请输入商品名称"
          :rules="[{ required: true, message: '请输入名称' }]"
        />

        <!-- 分类（选择器） -->
        <van-field
          v-model="form.category"
          name="category"
          label="分类"
          placeholder="请选择"
          readonly
          is-link
          @click="showCategoryPicker = true"
        />

        <!-- 保质期 -->
        <van-field
          v-model="form.expireDate"
          name="expireDate"
          label="保质期至"
          placeholder="请选择日期"
          :rules="[{ required: true, message: '请选择保质期' }]"
          readonly
          @click="showDatePicker = true"
        />

        <van-field v-model="form.notes" name="notes" label="备注" type="textarea" rows="2" autosize />

        <van-field name="notifyDaysBefore" label="提前提醒">
          <template #input>
            <van-stepper v-model="form.notifyDaysBefore" min="1" max="30" />
            <span style="margin-left:8px;font-size:14px;color:#999">天</span>
          </template>
        </van-field>
      </van-cell-group>

      <div style="margin:24px 16px">
        <van-button type="primary" native-type="submit" block round :loading="saving">
          {{ isEdit ? '保存修改' : '添加记录' }}
        </van-button>
      </div>
    </van-form>

    <!-- 内嵌扫码 -->
    <BarcodeScanner
      v-if="scanning"
      @detected="onBarcodeScanned"
      @close="scanning = false"
      @error="onScanError"
    />

    <!-- 分类选择器 -->
    <van-popup v-model:show="showCategoryPicker" position="bottom">
      <van-picker
        :columns="categoryOptions"
        @confirm="onCategoryConfirm"
        @cancel="showCategoryPicker = false"
        title="选择分类"
      />
    </van-popup>

    <!-- 日期选择器 -->
    <van-popup v-model:show="showDatePicker" position="bottom">
      <van-date-picker
        v-model="pickerDate"
        title="选择保质期"
        :min-date="minDate"
        :max-date="maxDate"
        @confirm="onDateConfirm"
        @cancel="showDatePicker = false"
      />
    </van-popup>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { showToast } from 'vant'
import { useMedicinesStore } from '@/stores/medicines'
import api from '@/services/api'
import BarcodeScanner from '@/components/BarcodeScanner.vue'

const router = useRouter()
const route = useRoute()
const store = useMedicinesStore()

const CATEGORIES = ['药品', '食品', '日用品', '保健品', '其他']
const categoryOptions = CATEGORIES.map(c => ({ text: c, value: c }))

const isEdit = computed(() => !!route.params.id)
const saving = ref(false)
const ocring = ref(false)
const scanning = ref(false)
const fileInput = ref(null)
const showDatePicker = ref(false)
const showCategoryPicker = ref(false)
const pickerDate = ref(['2026', '01', '01'])
const minDate = new Date()
const maxDate = new Date(2035, 0, 1)
const photos = reactive({ name: null, expiry: null })
const currentSlot = ref('name')
const hasPhotos = computed(() => photos.name || photos.expiry)

const form = reactive({
  barcode: route.query.barcode || '',
  name: '',
  expireDate: '',
  category: '',
  notes: '',
  notifyDaysBefore: 7
})

onMounted(async () => {
  if (isEdit.value) {
    await store.fetchAll()
    const m = store.medicines.find(m => m.id === Number(route.params.id))
    if (m) Object.assign(form, { ...m })
  }
})

function onDateConfirm({ selectedValues }) {
  form.expireDate = selectedValues.join('-')
  showDatePicker.value = false
}

function onCategoryConfirm({ selectedValues }) {
  form.category = selectedValues[0]
  showCategoryPicker.value = false
}

function onBarcodeScanned(barcode) {
  form.barcode = barcode
  scanning.value = false
  showToast('条形码已填入')
}

function onScanError(msg) {
  scanning.value = false
  showToast(`摄像头错误: ${msg}`)
}

function triggerCapture(slot) {
  currentSlot.value = slot
  fileInput.value.value = ''
  fileInput.value.click()
}

function onImageSelected(e) {
  const file = e.target.files?.[0]
  if (!file) return
  const prev = photos[currentSlot.value]
  if (prev) URL.revokeObjectURL(prev.url)
  photos[currentSlot.value] = { file, url: URL.createObjectURL(file) }
}

async function runOcr() {
  if (!hasPhotos.value) return
  ocring.value = true
  try {
    const formData = new FormData()
    ;[photos.name, photos.expiry].forEach(p => p && formData.append('images', p.file))
    const { data } = await api.post('/ocr/medicine', formData)
    if (data.name) form.name = data.name
    if (data.expireDate) form.expireDate = data.expireDate
    if (data.category && CATEGORIES.includes(data.category)) form.category = data.category
    if (data.manufacturer) form.notes = `品牌/厂家：${data.manufacturer}`
    if (!data.name && !data.expireDate) showToast('未能识别到信息，请手动输入')
    else showToast('识别成功，请确认信息')
  } catch {
    showToast('识别失败，请手动输入')
  } finally {
    ocring.value = false
  }
}

async function onSubmit() {
  saving.value = true
  try {
    const payload = {
      barcode: form.barcode || null,
      name: form.name,
      expireDate: form.expireDate,
      category: form.category || null,
      notes: form.notes || null,
      notifyDaysBefore: form.notifyDaysBefore
    }
    if (isEdit.value) {
      await store.update(Number(route.params.id), payload)
      showToast('修改成功')
    } else {
      await store.create(payload)
      showToast('添加成功')
    }
    router.back()
  } catch {
    showToast('保存失败，请重试')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.page { padding-bottom: 40px; }
.ocr-wrap { padding: 16px 16px 8px; }
.ocr-header {
  display: flex; align-items: center; gap: 6px; margin-bottom: 10px;
}
.ocr-title { font-size: 14px; font-weight: 600; color: #323233; }
.ocr-hint { font-size: 12px; color: #aaa; }

.photo-slots { display: flex; gap: 12px; }

.photo-slot {
  flex: 1; aspect-ratio: 3/2; border-radius: 10px;
  border: 1.5px dashed #1989fa; overflow: hidden;
  position: relative; cursor: pointer; background: #f7f9ff;
}

.slot-empty {
  width: 100%; height: 100%;
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  gap: 4px; padding: 8px;
}
.slot-label-main { font-size: 13px; font-weight: 600; color: #1989fa; }
.slot-label-sub { font-size: 11px; color: #aaa; text-align: center; }

.slot-img { width: 100%; height: 100%; object-fit: cover; display: block; }

.slot-retake {
  position: absolute; bottom: 0; left: 0; right: 0;
  background: rgba(0,0,0,0.45); color: #fff;
  font-size: 12px; text-align: center; padding: 4px 0;
  display: flex; align-items: center; justify-content: center; gap: 3px;
}

.slot-tag {
  position: absolute; top: 6px; left: 6px;
  font-size: 11px; padding: 2px 6px; border-radius: 4px;
  background: rgba(0,0,0,0.3); color: #fff;
}
.slot-tag.done { background: #07c160; }
</style>
