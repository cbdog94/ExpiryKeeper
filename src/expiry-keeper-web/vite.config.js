import { fileURLToPath, URL } from 'node:url'
import { execSync } from 'node:child_process'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { VitePWA } from 'vite-plugin-pwa'

const gitCommit = (() => {
  try {
    const d = new Date(execSync('git log -1 --format=%ci').toString().trim())
    const yyyy = d.getFullYear()
    const mm = String(d.getMonth() + 1).padStart(2, '0')
    const dd = String(d.getDate()).padStart(2, '0')
    return `${yyyy}.${mm}.${dd}`
  } catch { return 'unknown' }
})()

async function mockApiPlugin() {
  const { createDb, createHandlers } = await import('./src/mocks/handlers.js')
  const db = createDb()
  const handlers = createHandlers(db)

  function matchRoute(method, urlPath) {
    for (const [key, fn] of Object.entries(handlers)) {
      const [m, pattern] = key.split(' ')
      if (m !== method) continue
      const paramNames = []
      const regex = new RegExp(
        '^' + pattern.replace(/:(\w+)/g, (_, name) => { paramNames.push(name); return '([^/]+)' }) + '$'
      )
      const match = urlPath.match(regex)
      if (match) {
        const params = Object.fromEntries(paramNames.map((n, i) => [n, match[i + 1]]))
        return { fn, params }
      }
    }
    return null
  }

  return {
    name: 'mock-api',
    configureServer(server) {
      server.middlewares.use('/api', (req, res, next) => {
        const urlPath = req.url.split('?')[0]
        const method = req.method
        const matched = matchRoute(method, urlPath)
        if (!matched) return next()

        const ct = req.headers['content-type'] || ''
        const parseBody = ct.includes('application/json')
          ? new Promise(resolve => {
              let raw = ''
              req.on('data', c => (raw += c))
              req.on('end', () => { try { resolve(JSON.parse(raw || '{}')) } catch { resolve({}) } })
            })
          : Promise.resolve({})

        parseBody.then(body => {
          const { status, body: resBody } = matched.fn(body, matched.params)
          res.statusCode = status
          res.setHeader('Content-Type', 'application/json')
          res.end(resBody !== null ? JSON.stringify(resBody) : '')
        })
      })
    },
  }
}

export default defineConfig(async ({ mode }) => {
  const isMock = mode === 'mock'

  return {
    define: {
      __GIT_COMMIT__: JSON.stringify(gitCommit)
    },
    plugins: [
      vue(),
      VitePWA({
        strategies: 'injectManifest',
        srcDir: 'src',
        filename: 'sw.js',
        registerType: 'autoUpdate',
        includeAssets: ['favicon.ico', 'apple-touch-icon.png'],
        manifest: {
          name: '效期管家',
          short_name: '效期管家',
          description: '扫码记录商品保质期，到期自动提醒',
          theme_color: '#1989fa',
          background_color: '#ffffff',
          display: 'standalone',
          orientation: 'portrait',
          scope: '/',
          start_url: '/',
          icons: [
            { src: 'icons/icon-192.png', sizes: '192x192', type: 'image/png' },
            { src: 'icons/icon-512.png', sizes: '512x512', type: 'image/png' },
            { src: 'icons/icon-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' }
          ]
        },
        injectManifest: {
          globPatterns: ['**/*.{js,css,html,ico,png,svg}']
        },
        devOptions: {
          enabled: !isMock,
          type: 'module'
        }
      }),
      isMock && await mockApiPlugin(),
    ].filter(Boolean),
    resolve: {
      alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) }
    },
    build: {
      target: ['es2015', 'safari13']
    },
    server: {
      host: true,
      allowedHosts: true,
      ...(!isMock && {
        proxy: {
          '/api': {
            target: 'http://localhost:5090',
            changeOrigin: true,
            configure: (proxy) => {
              proxy.on('proxyReq', (proxyReq, req) => {
                console.log(`[proxy] ${req.method} ${req.url} -> ${proxyReq.path}`)
              })
            }
          }
        }
      })
    }
  }
})
