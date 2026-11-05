# ExpiryKeeper.Api

ASP.NET Core 10 后端，提供效期管家 PWA 所需的 REST API，包含商品效期管理、多渠道推送通知和 AI 拍照识别功能。

## 技术栈

- **框架**: ASP.NET Core 10 + Entity Framework Core
- **数据库**: Azure SQL Server（Managed Identity 无密码连接）
- **认证**: Azure AD（Microsoft.Identity.Web，JWT Bearer）
- **AI/OCR**: Azure OpenAI GPT 视觉模型
- **推送**: Web Push（VAPID）+ Bark

### 依赖安全

`Microsoft.Identity.Web` 已升级至 4.17.0，其 TokenCache 包的 `System.Security.Cryptography.Pkcs` 最低依赖已提升至修复版本 10.0.13。继续显式引用 10.0.14，以保留当前 .NET 10 服务更新补丁，避免解析回较旧的补丁版本。

本轮保持 .NET 10、EF Core 和 OpenAPI 的现有兼容版本线；`Azure.AI.OpenAI` 保留明确固定的 2.2.0-beta.4，不自动引入其他预览版本。依赖升级验证只执行还原、构建、发布和安全审计，不启动自动迁移数据库的 API。

## 快速开始

### 前置依赖

- .NET 10 SDK
- Docker（本地 SQL Server）或 Azure SQL
- `az login`（Development 环境的 OCR 使用 AzureCliCredential；SQL 连接仍使用连接字符串配置的认证方式）

### 本地运行

```bash
# 运行 API（端口 5090）
dotnet run
```

首次启动会自动执行 EF Core 数据库迁移。

### 配置

在 `appsettings.Development.json` 中填写：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;Authentication=Active Directory Default;"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "ClientId": "<client-id>",
    "Audience": "api://<client-id>"
  },
  "AzureOpenAI": {
    "Endpoint": "https://<resource>.openai.azure.com/",
    "DeploymentName": "gpt-4o"
  },
  "Vapid": {
    "Subject": "mailto:<email>",
    "PublicKey": "<base64-public-key>",
    "PrivateKey": "<base64-private-key>"
  },
  "AllowedOrigins": ["http://localhost:5173"]
}
```

生成 VAPID 密钥：
```bash
npx web-push generate-vapid-keys
```

## API 端点

所有端点均需 Azure AD JWT 认证（`Authorization: Bearer <token>`），标注 `*` 的除外。

### 商品管理 `/api/medicines`

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/medicines` | 获取当前用户所有商品 |
| GET | `/api/medicines/{id}` | 获取单个商品 |
| POST | `/api/medicines` | 新增商品 |
| PUT | `/api/medicines/{id}` | 更新商品 |
| DELETE | `/api/medicines/{id}` | 删除商品 |
| POST | `/api/medicines/lookup` | 条形码查询商品信息（预留） |

`MedicineDto` 包含计算字段 `daysUntilExpiry`（负数表示已过期）。

### 拍照识别 `/api/ocr`

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/ocr/medicine` | 分析商品包装图片 |

- 接受 1–5 张图片（`multipart/form-data`，字段名 `images`），总大小 ≤ 30MB
- 所有图片在**同一次** OpenAI 请求中分析，模型可跨图推理
- 返回：`name`、`expireDate`（YYYY-MM-DD）、`manufacturer`、`category`、`rawText`
- 分类枚举：`药品` / `食品` / `日用品` / `保健品` / `其他`

#### OCR 阶段耗时

OCR 的 `TokenCredential` 和 `ChatClient` 在进程内按单例复用，以保留 SDK 的令牌缓存；`OcrService` 仍为 Scoped，不共享请求图片或识别结果。Development 环境仅使用 `AzureCliCredential`，需先执行 `az login`，不会探测本地不存在的托管身份。其他环境保留 `DefaultAzureCredential`，Azure App Service 可继续使用系统托管身份，不需要添加密钥配置。

客户端首次解析时读取 `AzureOpenAI` 配置；修改终结点或部署名称后需重启应用。凭据选择或令牌获取失败会沿原调用路径抛出，不会回退到匿名调用或其他模型。

`OcrService` 使用名为 `ExpiryKeeper.Ocr` 的 `ActivitySource` 提供诊断跨度，可由 .NET `ActivityListener` 或 OpenTelemetry 订阅：

| 跨度 | 范围 |
|------|------|
| `ocr.client.initialize` | 首次解析单例时创建 Azure OpenAI 客户端和凭据对象；后续请求不再产生此跨度，不包含首次获取令牌 |
| `ocr.analyze` | 整个识别服务方法，包含下列四个子阶段 |
| `ocr.images.encode` | 并行读取图片及 Base64 编码 |
| `ocr.request.build` | 构造多图消息 |
| `ocr.model.call` | SDK 调用，包含令牌获取、网络、Azure 处理及 SDK 重试 |
| `ocr.response.parse` | 提取、记录和解析模型返回文本 |

未订阅时不会导出跨度，也不会改变接口返回结构。图片内容和识别文本不写入跨度标签。`ocr.model.call` 不是纯模型推理时间；客户端端到端时间还包含上传、ASP.NET Core 请求处理及响应传输。嵌套跨度不能重复相加。

### 推送订阅 `/api/push`

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/push/vapid-key` | 获取 VAPID 公钥 *（匿名可访问）* |
| POST | `/api/push/subscribe/webpush` | 订阅 Web Push |
| DELETE | `/api/push/subscribe/webpush` | 取消 Web Push |
| POST | `/api/push/subscribe/bark` | 配置 Bark 推送 |
| DELETE | `/api/push/subscribe/bark` | 移除 Bark 配置 |
| GET | `/api/push/subscriptions` | 查看当前订阅列表 |

### 通知 `/api/notifications`

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/notifications/test` | 发送测试通知（向所有已配置渠道） |

## 核心设计

### 数据隔离

所有数据操作都通过 Azure AD `oid` claim 作为 `UserOid` 进行过滤，用户之间完全隔离。

### 推送通知架构

`INotificationProvider` 策略模式，当前实现：
- **WebPushProvider**：W3C Web Push 标准，VAPID 加密
- **BarkProvider**：Bark iOS App，`GET {serverUrl}/{deviceKey}/{title}/{body}`

`NotificationService` 遍历所有注册的 Provider，所有发送结果记录到 `NotificationLogs`。

### 定时效期检查

`ExpiryCheckHostedService` 每天 UTC 01:00（北京时间 09:00）执行，查找距过期天数 ≤ `NotifyDaysBefore` 的商品并推送提醒。

## 数据迁移

```bash
# 新增迁移
dotnet ef migrations add <MigrationName>

# 应用迁移
dotnet ef database update
```

## Azure 部署

生产环境使用 System Managed Identity，需在 Azure SQL 中创建外部用户并授权：

```sql
CREATE USER [<webapp-name>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<webapp-name>];
ALTER ROLE db_datawriter ADD MEMBER [<webapp-name>];
```

Azure OpenAI 需为 Managed Identity 分配 `Cognitive Services OpenAI User` 角色。

生产连接串使用 `Authentication=Active Directory Managed Identity`，本地开发使用 `Authentication=Active Directory Default`（依赖 `az login`）。
