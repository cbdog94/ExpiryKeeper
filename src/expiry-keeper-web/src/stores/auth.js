import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { msalInstance, loginRequest, initializeMsal } from '@/auth/msalConfig'

export const useAuthStore = defineStore('auth', () => {
  const account = ref(null)
  const initialized = ref(false)

  const isLoggedIn = computed(() => account.value !== null)
  const displayName = computed(() => account.value?.name ?? '')

  async function initialize() {
    await initializeMsal()
    const accounts = msalInstance.getAllAccounts()
    if (accounts.length > 0) account.value = accounts[0]
    initialized.value = true
  }

  async function login() {
    await msalInstance.loginRedirect(loginRequest)
  }

  async function logout() {
    await msalInstance.logoutRedirect({
      account: account.value,
      postLogoutRedirectUri: window.location.origin
    })
    account.value = null
  }

  return { account, initialized, isLoggedIn, displayName, initialize, login, logout }
})
