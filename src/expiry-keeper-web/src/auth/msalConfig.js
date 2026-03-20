import { PublicClientApplication } from '@azure/msal-browser'

const msalConfig = {
  auth: {
    clientId: '297c800b-6631-40a4-8d94-7985a1afc243',
    authority: 'https://login.microsoftonline.com/8bb671eb-729b-4e24-a28d-fe6566dbee22',
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin
  },
  cache: {
    cacheLocation: 'localStorage',
    storeAuthStateInCookie: true // iOS Safari compatibility
  }
}

export const loginRequest = {
  scopes: ['api://297c800b-6631-40a4-8d94-7985a1afc243/Medicine.Access']
}

export const msalInstance = new PublicClientApplication(msalConfig)

export async function initializeMsal() {
  await msalInstance.initialize()
  const result = await msalInstance.handleRedirectPromise()
  return result
}

export async function getAccessToken() {
  const accounts = msalInstance.getAllAccounts()
  if (accounts.length === 0) return null

  try {
    const response = await msalInstance.acquireTokenSilent({
      ...loginRequest,
      account: accounts[0]
    })
    return response.accessToken
  } catch {
    // Silent acquisition failed, redirect to login
    await msalInstance.acquireTokenRedirect({ ...loginRequest, account: accounts[0] })
    return null
  }
}
