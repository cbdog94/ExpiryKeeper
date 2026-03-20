<template>
  <div class="page">
    <van-nav-bar title="设置" />

    <!-- 账号 -->
    <van-cell-group inset title="账号">
      <van-cell :title="authStore.displayName" icon="contact-o" label="已登录" />
      <van-cell title="退出登录" icon="sign" is-link @click="authStore.logout()" />
    </van-cell-group>

    <!-- Web Push -->
    <van-cell-group inset title="Web 推送">
      <template v-if="!push.isSupported.value">
        <van-cell title="当前环境不支持推送" label="需要 iOS 16.4+ 且从主屏幕打开" icon="warning-o" />
      </template>
      <template v-else>
        <van-cell title="通知状态" icon="bell" center>
          <template #right-icon>
            <div class="cell-right">
              <van-tag :type="push.isSubscribed.value ? 'success' : 'default'">
                {{ push.isSubscribed.value ? '已开启' : '未开启' }}
              </van-tag>
              <van-button
                v-if="!push.isSubscribed.value"
                type="primary" size="small" round
                :loading="push.loading.value" loading-text="开启中..."
                @click="enablePush"
              >开启</van-button>
              <template v-else>
                <van-button
                  type="primary" plain size="small" round
                  :loading="testing" loading-text="发送中..."
                  @click="sendTest"
                >测试</van-button>
                <van-button
                  type="default" size="small" round
                  :loading="push.loading.value" loading-text="关闭中..."
                  @click="push.unsubscribe()"
                >关闭</van-button>
              </template>
            </div>
          </template>
        </van-cell>
      </template>
    </van-cell-group>

    <!-- 结果反馈 -->
    <div v-if="testResult" class="test-result" :class="testResult.type">
      <van-icon :name="testResult.type === 'success' ? 'success' : 'cross'" />
      {{ testResult.message }}
    </div>

    <!-- Bark -->
    <van-cell-group inset title="Bark 推送">
      <van-cell title="通知状态" icon="phone-o" center>
        <template #right-icon>
          <div class="cell-right">
            <van-tag :type="barkSaved ? 'success' : 'default'">
              {{ barkSaved ? '已配置' : '未配置' }}
            </van-tag>
            <template v-if="barkSaved">
              <van-button
                type="primary" plain size="small" round
                :loading="barkTesting" loading-text="发送中..."
                @click="testBark"
              >测试</van-button>
              <van-button
                type="danger" plain size="small" round
                :loading="barkRemoving" loading-text="移除中..."
                @click="removeBark"
              >移除</van-button>
            </template>
          </div>
        </template>
      </van-cell>

      <van-cell>
        <template #title>
          <van-field
            v-model="barkUrl"
            placeholder="粘贴 Bark 链接，如 https://api.day.one/push/xxx"
            clearable
            :disabled="barkSaving || barkRemoving"
          >
            <template #button>
              <van-button
                type="primary" size="small" round
                :loading="barkSaving" loading-text="保存中..."
                :disabled="!barkUrl.trim()"
                @click="saveBark"
              >保存</van-button>
            </template>
          </van-field>
        </template>
      </van-cell>
    </van-cell-group>

    <!-- iOS 安装提示 -->
    <van-cell-group inset title="安装提示">
      <van-cell icon="apps-o" title="添加到主屏幕"
        label="Safari → 分享按钮 → 添加到主屏幕，可获得完整通知支持" />
    </van-cell-group>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { showToast } from 'vant'
import { useAuthStore } from '@/stores/auth'
import { usePush } from '@/composables/usePush'
import api from '@/services/api'

const authStore = useAuthStore()
const push = usePush()
const testing = ref(false)
const testResult = ref(null)

// Bark
const barkUrl = ref('')
const barkSaved = ref(false)
const barkSaving = ref(false)
const barkRemoving = ref(false)
const barkTesting = ref(false)

onMounted(async () => {
  push.checkSubscriptionStatus()
  await loadBarkStatus()
})

async function loadBarkStatus() {
  try {
    const { data } = await api.get('/push/subscriptions')
    const bark = data.find(s => s.provider === 'Bark' || s.provider === 1)
    if (bark) {
      barkSaved.value = true
      const serverUrl = bark.barkServerUrl ?? 'https://api.day.one/push'
      barkUrl.value = `${serverUrl.replace(/\/$/, '')}/${bark.barkDeviceKey}`
    }
  } catch { /* ignore */ }
}

function parseBarkUrl(raw) {
  try {
    const u = new URL(raw.trim())
    const parts = u.pathname.split('/').filter(Boolean)
    if (!parts.length) return null
    const deviceKey = parts[parts.length - 1]
    const serverPath = parts.slice(0, -1).join('/')
    const serverUrl = serverPath ? `${u.origin}/${serverPath}` : u.origin
    return { deviceKey, serverUrl }
  } catch {
    return null
  }
}

async function saveBark() {
  const parsed = parseBarkUrl(barkUrl.value)
  if (!parsed) { showToast('链接格式不正确'); return }
  barkSaving.value = true
  try {
    await api.post('/push/subscribe/bark', { deviceKey: parsed.deviceKey, serverUrl: parsed.serverUrl })
    barkSaved.value = true
    showToast('Bark 已保存')
  } catch {
    showToast('保存失败，请重试')
  } finally {
    barkSaving.value = false
  }
}

async function removeBark() {
  barkRemoving.value = true
  try {
    await api.delete('/push/subscribe/bark')
    barkSaved.value = false
    barkUrl.value = ''
    showToast('已移除')
  } catch {
    showToast('移除失败，请重试')
  } finally {
    barkRemoving.value = false
  }
}

async function testBark() {
  barkTesting.value = true
  try {
    await api.post('/notifications/test', { title: '测试通知', body: 'Bark 推送配置正常！' })
    showToast('已发送，请检查 Bark App')
  } catch {
    showToast('发送失败')
  } finally {
    barkTesting.value = false
  }
}

async function enablePush() {
  const permission = await Notification.requestPermission()
  if (permission !== 'granted') {
    showToast('请在系统设置中允许通知权限')
    return
  }
  try {
    await push.subscribe()
    showToast('推送通知已开启')
  } catch {
    showToast(push.error.value ?? '开启失败，请重试')
  }
}

async function sendTest() {
  testing.value = true
  testResult.value = null
  try {
    await api.post('/notifications/test', { title: '测试通知', body: '推送配置正常，到期时将自动提醒！' })
    testResult.value = { type: 'success', message: '已发送，请检查通知' }
  } catch {
    testResult.value = { type: 'fail', message: '发送失败，请检查配置' }
  } finally {
    testing.value = false
    setTimeout(() => testResult.value = null, 4000)
  }
}
</script>

<style scoped>
.page { padding-bottom: 120px; min-height: 100vh; }
.cell-right {
  display: flex;
  align-items: center;
  gap: 6px;
}
.test-result {
  margin: 0 16px 8px;
  padding: 10px 14px;
  border-radius: 8px;
  font-size: 14px;
  display: flex;
  align-items: center;
  gap: 6px;
}
.test-result.success { background: #e8f7ef; color: #07c160; }
.test-result.fail { background: #fff0f0; color: #ee0a24; }
</style>
