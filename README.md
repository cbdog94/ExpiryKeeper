# 效期管家

扫码记录商品保质期，到期自动提醒。PWA 应用，支持条形码扫描、拍照 OCR 识别、推送通知。

## 技术栈

- **前端**：Vue 3 + Vite + Vant + Pinia，PWA（Service Worker）
- **后端**：ASP.NET Core 10 Web API
- **认证**：Azure AD（MSAL）
- **数据库**：Azure SQL
- **OCR**：Azure OpenAI GPT（多图单次调用）

## 开发

### 前端

```bash
cd src/expiry-keeper-web

pnpm dev          # 开发服务器（:5173），需要后端运行在 :5090
pnpm dev:mock     # 开发服务器（:5173），无需后端，使用内存 mock 数据
pnpm build        # 生产构建
```

#### Mock 模式

`pnpm dev:mock` 启动时无需后端，适用于纯前端开发和移动端调试（本机 IP 访问）：

- 所有 `/api/*` 请求由 Vite 中间件拦截，返回内存中的 mock 数据
- 跳过 Azure AD 登录，使用 mock 账号直接进入应用
- Mock 数据定义在 `src/mocks/handlers.js`，可按需修改

### 后端

```bash
cd src/ExpiryKeeper.Api

dotnet run        # 开发服务器（:5090）
dotnet build
```

## Copilot 依赖升级技能

仓库提供 [upgrade-packages 技能](.github/skills/upgrade-packages/SKILL.md)，用于升级前端 pnpm 和后端 NuGet 依赖，检查兼容性、更新锁文件并验证构建。

在 Copilot Agent 模式中可使用以下提示：

- “使用 upgrade-packages 技能升级前端和后端依赖。”
- “只升级前端依赖，保持当前运行时和兼容版本范围。”
- “检查后端过时依赖和安全公告，先给出方案，不修改文件。”

默认不引入新的预览版本、不更改运行时、不部署，也不自动提交或推送。

## 部署

```bash
dotnet publish src/ExpiryKeeper.Api/ExpiryKeeper.Api.csproj -c Release -o ./publish
cd publish && zip -r ../publish.zip .
az webapp deploy --resource-group <rg> --name <app> --src-path publish.zip --type zip
```
