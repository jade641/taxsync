# TaxSync

TaxSync is a small full-stack project (React + .NET) with an optional **Python Machine Learning** sidecar for **property taxation** analytics.

## Folders

- `frontend/` — Vite + React
- `backend/` — ASP.NET (minimal API)
- `ml/` — Python training + inference API (FastAPI)

## Run (local)

### 1) ML service (Python)

From `ml/`:

1) Install deps:

`pip install -r requirements.txt`

2) Put your dataset CSV in `ml/data/` (default name: `property_tax.csv`)

3) Train (you must choose your dataset's target column name):

`./train.ps1 -Target tax_amount`

4) Start API:

`./run_api.ps1`

Python API:
- `GET http://127.0.0.1:8000/health`
- `POST http://127.0.0.1:8000/predict`

### 2) Backend (.NET)

From repo root:

`dotnet run --project backend/backend.csproj`

Backend endpoints:
- `GET http://localhost:5192/weatherforecast`
- `GET http://localhost:5192/api/ml/health` (proxies Python)
- `POST http://localhost:5192/api/ml/predict` (proxies Python)

You can also use [backend/backend.http](backend/backend.http) to test.

## Config

Python URL is configured in:

- `backend/appsettings.json` → `MlService:BaseUrl`
