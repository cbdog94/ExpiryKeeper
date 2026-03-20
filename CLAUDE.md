# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**效期管家** — A PWA for tracking product expiration dates (medicine, food, daily goods) with barcode scanning, photo OCR, and push notifications. Full-stack: ASP.NET Core 10 API + Vue 3 SPA, deployed to Azure App Service.

## Commands

### Backend (.NET)
```bash
# Run API (dev, port 5090)
cd src/ExpiryKeeper.Api && dotnet run

# Build
dotnet build src/ExpiryKeeper.Api/ExpiryKeeper.Api.csproj

# Publish for deployment
dotnet publish src/ExpiryKeeper.Api/ExpiryKeeper.Api.csproj -c Release -o ./publish

# EF migrations
dotnet ef migrations add <Name> --project src/ExpiryKeeper.Api
dotnet ef database update --project src/ExpiryKeeper.Api
```

### Frontend (Vue)
```bash
cd src/expiry-keeper-web

pnpm dev      # Dev server on :5173, proxies /api → :5090
pnpm build    # Production build → dist/ + PWA service worker
```

### Deploy to Azure
```bash
# From repo root — zip must be built from inside publish/ dir
cd publish && zip -r ../publish.zip .
az webapp deploy --resource-group <rg-group> --name <web-app-name> --src-path publish.zip --type zip
```

## Architecture

### Request Flow
```
Browser (Vue SPA)
  → MSAL acquires Azure AD JWT
  → Axios attaches Bearer token to every /api request
  → ASP.NET Core validates JWT (Microsoft.Identity.Web)
  → Controllers extract UserOid from "oid" claim
  → EF Core queries are always scoped to UserOid
```

### Backend Structure (`src/ExpiryKeeper.Api/`)

- **Controllers**: `MedicinesController`, `OcrController`, `PushController`, `NotificationsController`
- **Services/Notifications/**: Strategy pattern — `INotificationProvider` implemented by `WebPushProvider` and `BarkProvider`. `NotificationService` iterates all registered providers.
- **Services/OcrService**: Sends all images in a **single** Azure OpenAI GPT call (multi-image in one message). The `ChatClient` is created once in the constructor and reused.
- **BackgroundServices/ExpiryCheckHostedService**: Runs daily at 01:00 UTC, notifies users of items expiring within `NotifyDaysBefore` days.
- **Data/AppDbContext**: EF Core with SQL Server. Migrations auto-apply on startup. Indexes on `UserOid` for all user-scoped tables.

### Frontend Structure (`src/expiry-keeper-web/src/`)

- **auth/msalConfig.js**: MSAL instance and `getAccessToken()` helper (silent → redirect fallback)
- **services/api.js**: Axios with request interceptor that attaches Bearer token
- **stores/medicines.js**: Pinia store; computes `expiredCount` and `expiringSoonCount` (used for tab badges)
- **composables/usePush.js**: Web Push subscribe/unsubscribe with `withTimeout()` wrappers
- **sw.js**: Custom service worker (injectManifest strategy) — handles `push` and `notificationclick` events alongside Workbox precaching


### Auth in Azure vs Local

- **Azure**: Connection string uses `Authentication=Active Directory Managed Identity`. Web App has System Managed Identity with `db_datareader`+`db_datawriter` on SQL and `Cognitive Services OpenAI User` on AOAI.
- **Local**: `appsettings.Development.json` uses `Authentication=Active Directory Default` (az CLI credentials via `DefaultAzureCredential`).

### PWA Notes

- `vite.config.js` uses `strategies: 'injectManifest'` with custom `src/sw.js` — do **not** switch to `generateSW` as push handlers would be lost.
- iOS requires HTTPS (ngrok for local mobile testing) and app added to home screen for push notifications.
- Build target is `['es2015', 'safari13']` for iOS compatibility.
