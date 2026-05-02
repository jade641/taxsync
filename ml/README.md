# TaxSync ML (Python)

This folder adds a small Machine Learning component for your **property taxation** project.

## What it does

- `train.py`: trains a baseline model from a CSV dataset (property taxation)
- `serve.py`: FastAPI service that loads the trained model and exposes `POST /predict`

## 1) Install Python deps

From `TaxSync/ml`:

- Create/activate your venv (you already have `ml/venv/` in this repo)
- Install requirements:

`pip install -r requirements.txt`

Recommended Python versions: 3.11–3.12 (most ML packages like scikit-learn tend to support these reliably).

## 2) Put your dataset

Place your CSV at:

- `ml/data/property_tax.csv`

## 3) Train

You must tell the target column name (depends on your dataset):

PowerShell:

`./train.ps1 -Target tax_amount`

or:

`python train.py --data data/property_tax.csv --target tax_amount`

Model output:

- `ml/artifacts/model.joblib`
- `ml/artifacts/model.summary.json`

## 4) Run inference API

PowerShell:

`./run_api.ps1`

Health check:

- `GET http://127.0.0.1:8000/health`

Predict (example):

`POST http://127.0.0.1:8000/predict`

Body can be either:

- `{ "features": { "lot_area": 120, "zone": "R1" } }`
- or just `{ "lot_area": 120, "zone": "R1" }`

## Notes

- The training script auto-detects **classification vs regression** based on the target column.
- If your dataset has different column names, just send those exact feature keys at prediction time.
