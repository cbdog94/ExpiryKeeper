import { ref } from 'vue'
import api from '@/services/api'

// Timeout wrapper for promises that might hang
function withTimeout(promise, ms = 10000) {
  return Promise.race([
    promise,
    new Promise((_, reject) => setTimeout(() => reject(new Error('操作超时，请重试')), ms))
  ])
}

export function usePush() {
  const isSupported = ref('serviceWorker' in navigator && 'PushManager' in window)
  const isSubscribed = ref(false)
  const loading = ref(false)
  const error = ref(null)

  async function getVapidKey() {
    const { data } = await api.get('/push/vapid-key')
    return data.publicKey
  }

  function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4)
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/')
    const rawData = window.atob(base64)
    return Uint8Array.from([...rawData].map(char => char.charCodeAt(0)))
  }

  async function subscribe() {
    loading.value = true
    error.value = null
    try {
      const registration = await withTimeout(navigator.serviceWorker.ready, 8000)
      const vapidKey = await getVapidKey()

      const subscription = await withTimeout(
        registration.pushManager.subscribe({
          userVisibleOnly: true,
          applicationServerKey: urlBase64ToUint8Array(vapidKey)
        }),
        15000
      )

      const { endpoint, keys } = subscription.toJSON()
      await api.post('/push/subscribe/webpush', {
        endpoint,
        p256dh: keys.p256dh,
        auth: keys.auth
      })

      isSubscribed.value = true
    } catch (e) {
      error.value = friendlyError(e)
      throw e
    } finally {
      loading.value = false
    }
  }

  async function unsubscribe() {
    loading.value = true
    error.value = null
    try {
      const registration = await withTimeout(navigator.serviceWorker.ready, 8000)
      const subscription = await registration.pushManager.getSubscription()
      if (subscription) {
        await api.delete('/push/subscribe/webpush', { data: { endpoint: subscription.endpoint } })
        await subscription.unsubscribe()
      }
      isSubscribed.value = false
    } catch (e) {
      error.value = friendlyError(e)
      throw e
    } finally {
      loading.value = false
    }
  }

  async function checkSubscriptionStatus() {
    if (!isSupported.value) return
    try {
      const registration = await withTimeout(navigator.serviceWorker.ready, 5000)
      const subscription = await registration.pushManager.getSubscription()
      isSubscribed.value = !!subscription
    } catch {
      // Silently fail — don't block UI
    }
  }

  async function sendTest() {
    await api.post('/notifications/test')
  }

  function friendlyError(e) {
    const msg = e?.message ?? ''
    if (msg.includes('超时')) return '操作超时，请确认已添加到主屏幕后重试'
    if (msg.includes('permission')) return '通知权限被拒绝，请在系统设置中开启'
    if (msg.includes('applicationServerKey')) return 'VAPID Key 格式错误，请联系管理员'
    return `订阅失败：${msg}`
  }

  return { isSupported, isSubscribed, loading, error, subscribe, unsubscribe, checkSubscriptionStatus, sendTest }
}
