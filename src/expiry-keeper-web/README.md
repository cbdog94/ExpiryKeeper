# expiry-keeper-web

效期管家 PWA 前端，基于 Vue 3 + Vant，支持条形码扫描、AI 拍照识别和 Web Push 通知。

## 技术栈

- **框架**: Vue 3 + Vite 7
- **UI**: Vant 4（移动端组件库）
- **状态**: Pinia
- **认证**: Azure AD（@azure/msal-browser）
- **HTTP**: Axios（自动注入 Bearer token）
- **扫码**: @zxing/browser
- **PWA**: vite-plugin-pwa + Workbox（injectManifest 策略）
- **包管理**: pnpm

## 快速开始

```bash
pnpm install
pnpm dev       # http://localhost:5173，/api 自动代理到 :5090
pnpm build     # 输出到 dist/，同时生成 sw.js
```

### 本地 HTTPS（移动端调试必需）

Web Push 和 MSAL 需要 HTTPS。使用 ngrok：

```bash
ngrok http 5173
```

iOS Safari 需要通过 ngrok 的 HTTPS 地址访问，否则 Web Crypto API 不可用。

## 更新依赖

```bash
# 交互式升级
pnpm up -i --latest

# 或直接升级所有
pnpm update --latest
```

## 目录结构

```
src/
├── auth/msalConfig.js        # MSAL 实例、getAccessToken()
├── services/api.js           # Axios 实例（自动注入 token）
├── stores/
│   ├── auth.js               # 登录状态、MSAL 初始化
│   └── medicines.js          # 商品 CRUD、过期计算
├── composables/usePush.js    # Web Push 订阅管理
├── router/index.js           # 路由 + 认证守卫
├── views/                    # 6 个页面组件
├── components/               # BarcodeScanner、MedicineCard、BottomTabBar
└── sw.js                     # 自定义 Service Worker（勿删）
```

## 路由

| 路径 | 页面 | 说明 |
|------|------|------|
| `/` | HomeView | 首页：统计卡片 + 到期提醒 |
| `/scan` | ScanView | 条形码扫描 |
| `/medicines` | MedicinesView | 商品清单（搜索 + 分类 Tab） |
| `/medicines/add` | MedicineFormView | 新增（支持拍照识别） |
| `/medicines/:id/edit` | MedicineFormView | 编辑 |
| `/settings` | SettingsView | Web Push / Bark 推送设置 |

所有路由均需登录，未认证时自动跳转 Azure AD。

## PWA 说明

Service Worker 使用 `injectManifest` 策略，`src/sw.js` 是自定义入口，包含：
- Workbox 预缓存静态资源
- `/api/*` NetworkFirst 缓存策略
- `push` 事件处理（接收推送通知）
- `notificationclick` 事件处理（点击通知跳转）

> ⚠️ 不能改为 `generateSW` 策略，否则 push 事件处理器会丢失。

iOS 推送需满足：iOS 16.4+、Safari、已添加到主屏幕、App 完全关闭（非前台）。

## Bark 推送配置

在设置页面粘贴完整 Bark URL，例如：

```
https://api.day.one/push/xxxxxxxxxxxxxxxx
```

前端自动解析最后一段为 device key，其余为 server URL。

## 构建产物说明

`pnpm build` 会生成：
- `dist/` — 静态资源（需复制到后端 `wwwroot/`）
- `dist/sw.js` — Service Worker（由 Workbox 注入 precache manifest）

部署时需将 `dist/` 内容复制到 `ExpiryKeeper.Api/wwwroot/`，由 .NET 托管静态文件并回落 `index.html`。
