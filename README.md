# Personal Web Template

A high-performance monorepo template for .NET backends and React frontends, built on Clean Architecture principles.

## Stack

- **Backend**: .NET 10, Entity Framework Core, PostgreSQL, Scalar/OpenAPI
- **Frontend**: React, Vite, TypeScript, Redux Toolkit + RTK Query
- **Infrastructure**: Docker, Helm, Kubernetes

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [PostgreSQL](https://www.postgresql.org/) (local or Docker)
- `dotnet new` CLI

## Creating a New Project

### 1. Install the Template

```bash
dotnet new install /path/to/web-template
```

Or once published to NuGet:

```bash
dotnet new install Balenthiran.WebTemplate
```

### 2. Scaffold Your Project

Pass your solution name as `Company.ProjectName`:

```bash
dotnet new web-template -n Balenthiran.Apeify
```

This will generate:

```
Balenthiran.Apeify/
├── backend/
│   ├── Balenthiran.Apeify.Abstractions/
│   ├── Balenthiran.Apeify.DataModels/
│   ├── Balenthiran.Apeify.DomainModels/
│   ├── Balenthiran.Apeify.EntityModels/
│   ├── Balenthiran.Apeify.Services/
│   ├── Balenthiran.Apeify.Database/
│   ├── Balenthiran.Apeify.WebApi/
│   └── Balenthiran.Apeify.slnx
├── frontend/
├── scripts/
├── helm/
└── deploy.sh
```

### 3. Run the Onboarding Script

```bash
cd Balenthiran.Apeify
node scripts/init.mjs
```

This will:
- Prompt for your PostgreSQL connection details
- Generate `appsettings.Development.json` with a fresh JWT secret
- Generate `frontend/.env` with a unique project ID
- Run `dotnet restore` and `npm install`

### 4. Apply the Initial Migration

```bash
cd backend
dotnet ef database update --project Balenthiran.Apeify.Database --startup-project Balenthiran.Apeify.WebApi
```

### 5. Run the App

**Backend:**
```bash
cd backend
dotnet run --project Balenthiran.Apeify.WebApi
```

**Frontend:**
```bash
cd frontend
npm run dev
```

API docs available at: `http://localhost:5000/scalar/v1`

## Running Locally

> This section is worth keeping when you replace this README with your project spec.

### Prerequisites

`appsettings.Development.json` is gitignored (it contains secrets). You need it before the backend will start. Either run the onboarding script which generates it for you:

```bash
node scripts/init.mjs
```

Or create it manually at `backend/MacroMetrics.WebApi/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=your_db;Username=your_user;Password=your_password"
  },
  "Jwt": {
    "Secret": "your-secret-key-min-32-chars-long"
  }
}
```

Similarly, `frontend/.env` is gitignored. Create it manually at `frontend/.env` if needed:

```
VITE_API_URL=http://localhost:5000
```

### Start the backend

```bash
cd backend
dotnet run --project MacroMetrics.WebApi
```

API runs at `http://localhost:5000` — interactive docs at `http://localhost:5000/scalar/v1`.

### Start the frontend

```bash
cd frontend
npm run dev
```

App runs at `http://localhost:5173`. The Vite dev server proxies `/api` and `/openapi` to the backend automatically.

### Regenerate the API client

Run this after any backend endpoint change to keep the frontend types in sync:

```bash
cd frontend
npm run codegen
```

See `docs/specs/openapi-codegen.md` for the full workflow.

---

## Initialise as a New Git Repo

```bash
cd Balenthiran.Apeify
git init
git add .
git commit -m "Initial commit from web-template"
# Add your remote and push
```

## Kubernetes Deployment

### 1. Create the K8s Namespace

```bash
kubectl create namespace your-app
```

> This must match `KUBERNETES_NAMESPACE` in `deploy.sh` and `-n` in the commands below.

### 2. Provision a PostgreSQL Database

Create a PostgreSQL database on your cloud provider (e.g. OCI, AWS RDS, Supabase). Note down the connection string in the format:

```
Host=your-host;Database=your_db;Username=your_user;Password=your_password
```

### 3. Create the K8s Secret

```bash
kubectl create secret generic your-app-secrets \
  --from-literal=DATABASE_URL="Host=your-host;Database=your_db;Username=your_user;Password=your_password" \
  -n your-app
```

> **Important**: The secret name (`your-app-secrets`) must match `secretKeyRef.name` in `helm/values.yaml`.

### 4. Update Configuration

Update these two files before deploying:

**`helm/values.yaml`** — set `fullnameOverride`, domain, registry, and secret name.

**`deploy.sh`** — set `APP_NAME` to match `fullnameOverride` in `values.yaml`, and update `REGISTRY_NAMESPACE` and `COMPARTMENT_ID`.

### 5. Deploy

```bash
./deploy.sh
```

This will build and push Docker images, then trigger a rolling restart of both deployments.

## New Project Checklist

After scaffolding with `dotnet new web-template -n Company.ProjectName`, work through this list:

### Local setup
- [ ] Run `node scripts/init.mjs` — generates `appsettings.Development.json` and `frontend/.env`
- [ ] Run the initial EF migration: `dotnet ef database update --project *.Database --startup-project *.WebApi`
- [ ] Verify the app starts: backend on `http://localhost:5000`, frontend on `http://localhost:5173`

### Branding / placeholders
- [ ] `frontend/src/components/Hero.tsx` — replace "Your App Name" and the subtitle
- [ ] `frontend/src/App.tsx` — replace "Your Name Here" in the footer
- [ ] `frontend/src/components/Navbar.tsx` — replace "App Name" in the mobile logo
- [ ] `frontend/src/data/config.json` — add any additional nav routes
- [ ] `frontend/index.html` — update `<title>`

### Helm + deploy script
- [ ] `helm/values.yaml` — set `fullnameOverride`, `ingress.hosts[0].host`, and image repository paths
- [ ] `deploy.sh` — set `APP_NAME`, `REGION`, `REGISTRY_NAMESPACE`, `COMPARTMENT_ID`, `KUBERNETES_NAMESPACE`

### GitHub Actions
- [ ] **Private repo?** Consider removing `ci.yml` — it runs on every PR and counts against the 2,000 free minutes/month. Delete the file or disable it in the repository settings (Settings → Actions → General). The deploy workflow (`docker-build-push.yml`) is manual-only so it only runs when you trigger it, making it less of a concern.
- [ ] **Automated deploys on merge to main?** By default `docker-build-push.yml` is manual-only. To trigger it automatically on merge to main, add `push: branches: [main]` to the `on:` block:
  ```yaml
  on:
    push:
      branches:
        - main
    workflow_dispatch:
  ```
- [ ] `.github/workflows/docker-build-push.yml` — set `FRONTEND_IMAGE` and `BACKEND_IMAGE` env vars to match `helm/values.yaml`
- [ ] Repository **Variables** (Settings → Secrets and variables → Variables):
  - `OCIR_REGISTRY` — e.g. `lhr.ocir.io`
  - `OCIR_NAMESPACE` — your OCI tenancy namespace
- [ ] Repository **Secrets** (Settings → Secrets and variables → Secrets):
  - `OCIR_USERNAME` — e.g. `your-tenancy/oracleidentitycloudservice/your@email.com`
  - `OCIR_AUTH_TOKEN` — OCI auth token (OCI Console → User Settings → Auth Tokens)

### Kubernetes
- [ ] `kubectl create namespace your-app`
- [ ] Provision a PostgreSQL database and note the connection string
- [ ] `kubectl create secret generic your-app-secrets --from-literal=DATABASE_URL="..." -n your-app`

### Git & GitHub repository settings
- [ ] `git init && git add . && git commit -m "Initial commit from web-template"`
- [ ] Create `main` and `dev` branches — all agent work branches off `dev`, PRs target `dev`, `main` is developer-only
- [ ] Add remote and push
- [ ] Enable **"Automatically delete head branches"** (Settings → General) — GitHub will delete feature branches after a PR merges, safely, without any agent involvement

### SDD bootstrap — create initial issues
- [ ] Run `node scripts/init-issues.mjs` — creates all Phase 1–7 orchestrator issues with the `MVP` milestone and ensures the three workflow labels exist. This is the first action the AI takes after the repo is pushed. See [`docs/ai-workflow.md`](docs/ai-workflow.md) for the full issue structure and label-driven workflow.

### Documentation
- [ ] Overwrite this `README.md` with your project's business spec — what the product is, who it's for, and what problem it solves. Keep the **Running Locally** section (or adapt it) so contributors know how to get started
- [ ] Flesh out `docs/specs/` with feature specs before writing code — define the data model, API contracts, and UI behaviour up front
- [ ] Update `CLAUDE.md` to reflect your project's specific conventions, data files, and any decisions made during setup
- [ ] Delete any `docs/specs/` files from the template that don't apply to your project

---

## Taking PR Screenshots

The repo includes a headless-browser screenshot script (`scripts/take-screenshots.js`) that captures each UI section of the app and uploads the images as a PR comment. This is the process used by the AI agent during code review.

### Prerequisites

Both the backend and the Vite dev server must be running before you take screenshots.

**Backend** (skeleton mode — no database required):
```bash
cd backend
dotnet run --project MacroMetrics.WebApi
# runs at http://localhost:5257
```

**Frontend:**
```bash
cd frontend
npm run dev
# runs at http://localhost:5173
```

### Install Playwright

Playwright requires Chromium and a set of system libraries. On a stock Linux/AArch64 container run:

```bash
npm install playwright
npx playwright install chromium
npx playwright install-deps chromium
```

> **Note (AArch64 / OKE pods):** The container image may be missing several Chromium shared libraries (`libatk`, `libgbm`, `libxkbcommon`, etc.). Running `npx playwright install-deps chromium` installs them via `apt-get` — this requires the pod image to have `apt-get` available. If it doesn't, ask the platform team to add the Playwright system-dep layer to the Dockerfile.

### Run the script

```bash
node scripts/take-screenshots.js
```

This navigates to `http://localhost:5173`, waits for the network to go idle, and saves five screenshots to `./screenshots/`:

| File | What it shows |
|---|---|
| `01-hero.png` | Landing view with sticky mini-nav |
| `02-presets-section.png` | 9 preset ratio cards with compact sparklines |
| `03-full-page.png` | Full-page scroll capturing all four sections |
| `04-compare-prepopulated.png` | Custom comparison section after clicking a preset card |
| `05-indicators-section.png` | CAPE · UK 10yr Gilt · US 10yr Treasury indicator cards |

### Upload to GitHub

Push the screenshots to a dedicated branch and reference them in a PR comment using raw GitHub URLs:

```bash
git checkout -b screenshots/pr-<NUMBER>
git add screenshots/
git commit -m "chore: add PR screenshots for #<NUMBER>"
git push -u origin screenshots/pr-<NUMBER>
```

Then post a PR comment with Markdown image links pointing to `raw.githubusercontent.com`:

```markdown
![Hero view](https://raw.githubusercontent.com/jemmy8oy/macro-metrics/screenshots/pr-<NUMBER>/screenshots/01-hero.png)
```

---

### Complications & resolutions (first documented run — PR #37)

The first time the screenshot workflow was run (headless Playwright in a Kubernetes pod on OCI / AArch64) several issues were encountered:

| Problem | Root cause | Resolution |
|---|---|---|
| `dotnet run` crashed immediately on startup | EF Core `Database.Migrate()` throws when PostgreSQL is unreachable — even for endpoints that never touch the DB | Wrapped `Migrate()` in a `try/catch`; the backend now logs a warning and continues in skeleton mode |
| Playwright Chromium download succeeded but the browser refused to launch | The pod's AArch64 base image was missing shared libraries: `libatk-1.0`, `libgbm`, `libxkbcommon`, `libxcomposite`, and several others | Ran `npx playwright install-deps chromium`; `apt-get` installed the missing `.so` files |
| `--no-sandbox` flag required | Chromium refuses to start as root without disabling the sandbox | Passed `--no-sandbox --disable-setuid-sandbox --disable-dev-shm-usage` in the browser launch options (already baked into the script) |
| Screenshot of presets section captured before charts finished rendering | Recharts SVG draws asynchronously after the first paint | Added a 2-second `waitForTimeout` after scrolling to each section; good enough for skeleton → data transitions |
| Preset-card click couldn't be located for screenshot #4 | The card's CSS class name wasn't stable across builds | Script falls back to scrolling `#compare` directly if no preset card selector matches |

---

## GitHub Actions

Two workflows are included in `.github/workflows/`:

| Workflow | Trigger | Purpose |
|---|---|---|
| `ci.yml` | Pull requests only | Builds backend and frontend to catch compile errors — uses `ubuntu-latest` (unlimited on public repos, counts against the 2,000 min/month free allowance on private repos) |
| `docker-build-push.yml` | Manual (`workflow_dispatch`) | Builds ARM64 Docker images and pushes to OCI Container Registry |

### ARM64 Runner Note

The deploy workflow uses `ubuntu-24.04-arm` (native ARM64, required for OKE free tier). This runner is **free for public repositories**. For private repositories it requires a paid GitHub plan — see the comment at the top of `docker-build-push.yml` for the `ubuntu-latest` + QEMU alternative.

## Project Structure

| Layer | Project | Responsibility |
|---|---|---|
| API | `*.WebApi` | Routes, DI, OpenAPI, Middleware |
| Services | `*.Services` | Business logic, AutoMapper |
| Abstractions | `*.Abstractions` | Interfaces, DTOs, Contracts |
| Database | `*.Database` | EF Core DbContext, Migrations |
| Entity Models | `*.EntityModels` | Database entities |
| Domain Models | `*.DomainModels` | Business-layer objects |
| Data Models | `*.DataModels` | Request/Response DTOs |
