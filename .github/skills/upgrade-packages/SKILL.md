---
name: upgrade-packages
description: Upgrade ExpiryKeeper frontend pnpm packages and backend NuGet packages safely. Use when asked to update dependencies, upgrade frontend or backend packages, check outdated packages, or remediate dependency advisories. Covers Vue/Vite/PWA compatibility, ASP.NET Core/EF Core alignment, lockfiles, and build validation.
---

# Upgrade packages

Upgrade dependencies in this repository, not application features. Support frontend-only,
backend-only, named-package, security-only, or full-stack requests. For an audit or plan
request, report findings without editing files.

## Repository context

Read the current files before choosing versions; these paths are relative to the
repository root, not this skill's directory:

- `CLAUDE.md`: architecture and development constraints.
- `src/expiry-keeper-web/package.json` and `pnpm-lock.yaml`: frontend dependencies,
  Node engines, scripts, and overrides.
- `src/expiry-keeper-web/vite.config.js` and `src/expiry-keeper-web/src/sw.js`:
  build targets and custom PWA.
- `src/ExpiryKeeper.Api/ExpiryKeeper.Api.csproj`: backend packages and target framework.
- `.github/workflows/azure-deploy.yml`: CI Node, pnpm, .NET, and publish configuration.
- Any SDK pins, central package files, local tool manifests, tests, or additional
  project instructions present when the skill runs.

## 1. Establish scope and baseline

1. Inspect `git status --short --branch`. Preserve user changes, including existing
   manifest edits. Do not reset, stash, switch branches, commit, or push unless asked.
2. Record current declared and lockfile-resolved versions for the requested packages.
   Compare the installed frontend graph with `pnpm -C src/expiry-keeper-web list --depth 0`;
   an existing node_modules directory may differ from the lockfile. Check installed
   Node, pnpm, and .NET SDK versions against manifests and CI.
3. Default to stable patch/minor upgrades within the current runtime and compatible
   package lines. Treat breaking `0.x` minor updates as breaking upgrades too.
   When the user explicitly requests latest versions or major upgrades, evaluate
   those releases and implement required source migrations within scope.
4. Do not introduce prereleases automatically. This repository already has explicitly
   pinned prereleases (including Vue and Azure.AI.OpenAI); evaluate those separately.
   Keep a preview pin unless a compatible replacement is verified, never silently
   downgrade it to an older stable release, and explain any preview retained.
5. Run the existing build/test commands for affected areas as a baseline when feasible.
   Record pre-existing failures separately. Do not install new tooling merely to
   inspect dependencies; restore missing dependencies after a missing-dependency
   failure or after changing manifests.

## 2. Discover candidates

Use package registries and official release notes/migration guides, not remembered
version numbers. Check peer dependencies, engines, target frameworks, advisories,
and transitive dependencies before selecting versions.

Run the following commands from the repository root. Use pnpm's explicit directory
option; do not rely on a previous tool call's working directory.

```bash
pnpm -C src/expiry-keeper-web outdated
pnpm -C src/expiry-keeper-web audit
pnpm -C src/expiry-keeper-web view <package> versions --json
pnpm -C src/expiry-keeper-web view <package>@<candidate> peerDependencies engines --json
pnpm -C src/expiry-keeper-web why <transitive-package>
```

Backend commands, from the repository root with the .NET 10 SDK:

```bash
dotnet package list --project src/ExpiryKeeper.Api/ExpiryKeeper.Api.csproj --outdated --highest-minor
dotnet package list --project src/ExpiryKeeper.Api/ExpiryKeeper.Api.csproj --vulnerable --include-transitive
dotnet package list --project src/ExpiryKeeper.Api/ExpiryKeeper.Api.csproj --deprecated
```

Package listing can restore dependencies; disclose missing SDK/feed/authentication
blockers rather than claiming an empty result means packages are current or safe.
Omit `--highest-minor` when evaluating explicitly requested major upgrades.
Use `--include-prerelease` only for a deliberate preview comparison. Interpret
outdated/audit exit codes together with their output; advisory findings are not
the same as registry failures. Never suppress errors or disable auditing to pass.
Capture each command's result separately; a later successful command or JSON formatter
must not hide an earlier failure. For verbose audits, summarize `--json` output by
package, severity, patched range, and dependency path, and check for registry errors.

Summarize the candidate set and compatibility risks before editing. If a candidate
requires an unrequested runtime migration or unrelated architectural rewrite, retain
that package and report the blocker while continuing independent safe upgrades.

## 3. Apply coherent upgrades

### Frontend

- Use pnpm only; do not generate npm or Yarn lockfiles. Upgrade selected packages
  with `pnpm -C src/expiry-keeper-web update '<package>@<candidate-range>' ...`, then
  inspect both the manifest and generated lockfile. Preserve existing range/pin
  conventions (for example, `^1.2.3` versus `1.2.3`), dependency sections, and
  unrelated user edits.
- Evaluate Vue, its compiler dependencies, Vue Router, Pinia, and Vant together.
  Evaluate Vite with its Vue/devtools/PWA plugins and Node engine requirements.
  Keep the directly declared Workbox packages on compatible versions.
- Inspect required and optional peer metadata. A direct update can leave stale
  auto-installed peers. If refreshing the parent leaves an unmet required peer,
  explicitly declare the compatible peer in the appropriate dependency section
  (for example, `workbox-build` as a development dependency). Do not add optional
  peers unless used, or disable peer checks to hide mismatches.
- Re-audit after direct updates. For remaining transitive findings, inspect parent
  ranges with `pnpm view` and paths with `pnpm why`, then use a targeted refresh:

  ```bash
  pnpm -C src/expiry-keeper-web update <affected-transitive-package> --depth Infinity
  ```

  Verify that the resolved version actually changed and satisfies the parent range;
  this is not a substitute for an upstream upgrade when that range excludes the fix.
- Review MSAL authentication changes and ZXing scanning changes at their call sites.
- Investigate `pnpm.overrides` (including `serialize-javascript`) with `pnpm why`.
  Prefer an upstream parent upgrade over a forced incompatible transitive version.
  When the upgraded parent itself requires a patched version, remove the redundant
  override, run `pnpm -C src/expiry-keeper-web install` to regenerate the lockfile,
  and verify the unforced graph with `pnpm why` and a fresh audit.
- Never hand-edit the lockfile or use blanket `--latest`, forced audit fixes,
  peer-dependency bypasses, or new overrides without verifying compatibility.

### Backend

- Select explicit NuGet versions compatible with the current target framework:

  ```bash
  dotnet add src/ExpiryKeeper.Api/ExpiryKeeper.Api.csproj package <package> --version <candidate> --no-restore
  ```

- Apply the selected coherent set before restoring once, avoiding temporary
  mismatches between EF Core or OpenAPI packages. `--no-restore` does not validate
  compatibility: a successful restore followed by build/publish is mandatory.
- Keep Microsoft.EntityFrameworkCore.SqlServer and Microsoft.EntityFrameworkCore.Tools
  on the same release; align any local `dotnet-ef` tool if present. Preserve Tools
  `PrivateAssets` and `IncludeAssets`.
- Keep Microsoft.AspNetCore.OpenApi compatible with the target framework. Verify
  Microsoft.OpenApi compatibility separately; its version does not match ASP.NET's.
- Check Azure.AI.OpenAI and its transitive OpenAI SDK API together, plus Azure.Identity,
  Microsoft.Identity.Web, and WebPush migration requirements at their usage sites.
- Preserve nullable settings and package metadata. Do not change the target framework
  or CI SDK/runtime versions unless the requested upgrade requires and authorizes it.
- Do not generate/apply EF migrations or start the real API merely to validate package
  changes: startup automatically applies migrations and can access external services.

For both stacks, fix source incompatibilities caused by selected upgrades with small,
complete changes. Update directly affected documentation and CI only when necessary.
Do not deploy, modify cloud resources, or touch credentials as part of this skill.

## 4. Verify behavior and reproducibility

Run validation for each changed stack. For full-stack upgrades, validate both:

```bash
# From the repository root:
pnpm -C src/expiry-keeper-web install --frozen-lockfile --strict-peer-dependencies
pnpm -C src/expiry-keeper-web build
pnpm -C src/expiry-keeper-web audit

dotnet restore src/ExpiryKeeper.Api/ExpiryKeeper.Api.csproj
dotnet build src/ExpiryKeeper.Api/ExpiryKeeper.Api.csproj -c Release --no-restore
dotnet publish src/ExpiryKeeper.Api/ExpiryKeeper.Api.csproj -c Release --no-restore
dotnet package list --project src/ExpiryKeeper.Api/ExpiryKeeper.Api.csproj --vulnerable --include-transitive
```

Run existing focused tests/lint scripts if present; do not invent commands or add
a test framework just for upgrades. Add focused regression coverage for behavior
changes using existing test infrastructure where available.
If a frozen install skips resolution, its success alone does not prove peer
compatibility; also inspect peer warnings from the preceding update/install.

Check these invariants when affected by the upgrade:

- PWA uses `injectManifest`, not `generateSW`; built output retains the custom
  `push` and `notificationclick` handlers and precaching.
  Inspect emitted syntax before writing assertions: minification can change quotes
  and property order, and content-hashed precache entries may have null revisions.
  Verify concrete entries such as `index.html`, no unresolved `self.__WB_MANIFEST`,
  and agreement with the current build's entry count rather than a hard-coded count
  from a previous run. Distinguish a brittle assertion from an actual build defect.
- Build targets remain `es2015` and `safari13`; a successful build alone does not
  prove runtime browser compatibility for new dependency APIs.
- MSAL silent/redirect authentication and Axios bearer tokens remain intact.
- API authorization and EF queries remain scoped to the authenticated user's OID.
- OCR still sends all images in one request and reuses its chat client.
- Web Push/Bark notifications, barcode scanning, item CRUD, routing, and expiry
  badges retain their behavior.

Use `pnpm -C src/expiry-keeper-web dev:mock --host 127.0.0.1 --port <free-port> --strictPort`
for safe frontend smoke testing where useful, and verify the server is responsive.
Check dashboard counts, list navigation, search, and an edit/save request with a
successful response and expected UI result. Use only the isolated in-memory mock.
Mock mode cannot validate real authentication, OCR, push,
or SQL integration. Report those checks as unverified without an authorized test
environment; do not claim a build or mock test proves them. Stop only processes
started for this task and remove only task-created temporary outputs.

## 5. Report the outcome

Review `git diff --check`, the manifest/lockfile/source diff, and final git status.
Preserve existing line endings. If the check flags CRLF as trailing whitespace,
first compare the original and edited file; for unchanged CRLF style, use
`git -c core.whitespace=cr-at-eol diff --check` without changing global Git settings
or rewriting unrelated lines.
Ensure no secrets, unrelated files, or generated build artifacts are included.
Report:

- Packages changed, previous and selected versions, and major/preview decisions.
- Compatibility fixes and any override changes with their rationale.
- Exact validation commands and results, separating pre-existing failures.
- Remaining advisories, deprecation/peer warnings, intentionally deferred major
  upgrades, and integration checks not performed. Distinguish audit findings from
  proof that a vulnerable code path is exploitable in this application.

Do not declare success if changed code fails validation. If blocked by tooling,
network, or credentials, state what was completed and what remains unverified.
