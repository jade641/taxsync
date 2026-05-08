# Startup and Connectivity Contract

This project now uses one stable network contract for local mode and one for Docker mode. The frontend always calls `/api`; the environment decides whether `/api` is handled by the Vite dev proxy or by nginx in Docker.

## Stable ports

| Service | Local mode | Docker mode |
| --- | --- | --- |
| Frontend | `http://127.0.0.1:5173` | `http://localhost:3000` |
| Backend API | `http://localhost:5000` | host `http://localhost:5000`, container `http://backend:8080` |
| Backend health | `http://localhost:5000/api/health` | `http://localhost:5000/api/health` |
| MySQL | `localhost:3306` from `backend/appsettings.json` | host `localhost:3307`, container `mysql:3306` |

## Files that own startup and networking

- `backend/Program.cs`: config-driven CORS, fixed runtime-mode validation, MySQL startup retry, DB-backed `/api/health`, HTTPS redirection controlled by config.
- `backend/Properties/launchSettings.json`: one local API profile on `http://127.0.0.1:5000`.
- `backend/appsettings.json`: local CORS origins, frontend public URL, database retry settings, runtime mode.
- `frontend/vite.config.ts`: fixed Vite port `5173`, strict port mode, local `/api` proxy.
- `frontend/.env.development`: local frontend runtime config.
- `frontend/.env.production`: production/Docker build runtime config.
- `frontend/src/config/apiBase.ts`: normalizes environment API config and rejects Docker localhost API targets.
- `frontend/src/lib/apiClient.ts`: health check, retry, timeout, auth headers, and friendly API errors.
- `frontend/nginx.conf`: Docker same-origin `/api` proxy to `backend:8080`.
- `docker-compose.yml`: Docker runtime mode, health checks, fixed host ports, no local/Docker database port overlap.
- `backend/run-dev.ps1`, `frontend/run-dev.ps1`, `scripts/start-local.ps1`, `scripts/start-docker.ps1`, `scripts/stop-docker.ps1`: repeatable startup commands with mode guards.

## Local mode commands

Start both services with health sequencing:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\start-local.ps1
```

Or start them separately:

```powershell
powershell -ExecutionPolicy Bypass -File .\backend\run-dev.ps1
powershell -ExecutionPolicy Bypass -File .\frontend\run-dev.ps1
```

The frontend opens at `http://127.0.0.1:5173`. Login calls `/api/auth/login`; Vite proxies that to `http://localhost:5000`.

## Docker mode commands

Stop local mode first, then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\start-docker.ps1
```

Stop Docker mode with:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\stop-docker.ps1
```

The Docker frontend opens at `http://localhost:3000`. It also calls `/api`; nginx proxies that to `http://backend:8080` inside the Docker network.

## Environment examples

Local frontend (`frontend/.env.development`):

```env
VITE_RUNTIME_MODE=local
VITE_DEV_HOST=127.0.0.1
VITE_DEV_PORT=5173
VITE_API_BASE_URL=/api
VITE_API_PROXY_TARGET=http://localhost:5000
```

Docker frontend build (`frontend/.env.docker.example`):

```env
VITE_RUNTIME_MODE=docker
VITE_API_BASE_URL=/api
```

Production frontend build (`frontend/.env.production`):

```env
VITE_RUNTIME_MODE=docker
VITE_API_BASE_URL=/api
```

Docker root environment (`.env.example`):

```env
MYSQL_ROOT_PASSWORD=taxsync_password
MYSQL_DATABASE=TaxsyncDB
MYSQL_USER=taxsync_user
MYSQL_PASSWORD=taxsync_password
JWT_KEY=your-super-secret-key-min-32-characters-long-for-security-purposes
JWT_ISSUER=TaxSync
JWT_AUDIENCE=TaxSyncUsers
JWT_EXPIRATION_HOURS=24
FRONTEND_PUBLIC_URL=http://localhost:3000
```

## Why login failed after restart

The previous setup had several race and mismatch points:

- Backend local launch used inconsistent hosts and ports, while the frontend and docs expected a fixed backend at `http://localhost:5000`.
- Frontend had a hardcoded fallback to `http://localhost:5192`, but Docker injected `http://localhost:5000/api`.
- Vite did not use `strictPort`, so it could silently move away from `5173` if the port was occupied.
- CORS allowed only a small hardcoded origin list, so a moved Vite port or hostname change could block browser requests.
- Docker built a `.NET 10` project with `.NET 9` images.
- Docker production mode could enable HTTPS redirection without a configured certificate path.
- `ServerVersion.AutoDetect(connectionString)` touched MySQL during backend service registration, before MySQL was guaranteed to be ready after a PC restart.
- Login used raw `fetch`, so backend startup delays surfaced as raw `Failed to fetch` instead of a helpful retry and error message.

The new flow removes those mismatches: fixed ports, a single localhost backend target, `/api` same-origin routing, config-driven CORS, database startup retry, health checks, no Docker auto-restart in local workflows, and frontend reconnect logic.

## Recommended folder structure

```text
TaxSync/
  backend/
    Program.cs
    appsettings.json
    Properties/launchSettings.json
    run-dev.ps1
  frontend/
    .env.development
    .env.development.example
    .env.docker.example
    run-dev.ps1
    vite.config.ts
    nginx.conf
    src/config/apiBase.ts
    src/lib/apiClient.ts
  scripts/
    start-local.ps1
    start-docker.ps1
    stop-docker.ps1
  docker-compose.yml
  .env.example
```