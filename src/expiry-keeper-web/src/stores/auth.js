import { defineStore } from 'pinia'
import { ref, computed, toRaw } from 'vue'
import { showToast } from 'vant'
import { msalInstance, loginRequest, initializeMsal } from '@/auth/msalConfig'

export const useAuthStore = defineStore('auth', () => {
  const account = ref(null)
  const initialized = ref(false)
  let initPromise = null

  const isLoggedIn = computed(() => account.value !== null)
  const displayName = computed(() => account.value?.name ?? '')

  async function initialize() {
    if (initialized.value) return
    if (initPromise) return initPromise
    initPromise = (async () => {
      await initializeMsal()
      const accounts = msalInstance.getAllAccounts()
      if (accounts.length > 0) account.value = accounts[0]
      initialized.value = true
    })()
    return initPromise
  }

  async function login() {
    await msalInstance.loginRedirect(loginRequest)
  }

  async function logout() {
    try {
      await msalInstance.logoutRedirect({
        account: toRaw(account.value),
        postLogoutRedirectUri: window.location.origin
      })
    } catch (error) {
      console.error('Logout failed:', error)
      showToast('退出登录失败，请重试')
    }
  }

  return { account, initialized, isLoggedIn, displayName, initialize, login, logout }
})
