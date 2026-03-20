import axios from 'axios'
import { getAccessToken } from '@/auth/msalConfig'

const api = axios.create({ baseURL: '/api' })

api.interceptors.request.use(async (config) => {
  const token = await getAccessToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

export default api
