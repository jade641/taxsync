from __future__ import annotations

import os
from pathlib import Path
from typing import Any

import joblib
import pandas as pd
from fastapi import Body, FastAPI, HTTPException
from pydantic import BaseModel


DEFAULT_MODEL_PATH = Path(os.getenv("TAXSYNC_MODEL_PATH", "artifacts/model.joblib"))

class PredictResponse(BaseModel):
    prediction: Any
    task: str


def _load_model(model_path: Path) -> dict[str, Any]:
    if not model_path.exists():
        raise FileNotFoundError(
            f"Model not found at '{model_path}'. Train first: python train.py --target <col>"
        )

    payload = joblib.load(model_path)
    if not isinstance(payload, dict) or "pipeline" not in payload:
        raise ValueError("Invalid model payload. Expected a dict with key 'pipeline'.")

    return payload


def _as_features(body: dict[str, Any]) -> dict[str, Any]:
    # If caller sent {features: {...}} use that; otherwise treat body as the feature map.
    if "features" in body and isinstance(body["features"], dict):
        return body["features"]

    return body


app = FastAPI(title="TaxSync ML Service", version="0.1.0")


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/predict", response_model=PredictResponse)
def predict(body: dict[str, Any] = Body(...)) -> PredictResponse:
    model_path = DEFAULT_MODEL_PATH

    try:
        payload = _load_model(model_path)
    except Exception as ex:
        raise HTTPException(status_code=500, detail=str(ex)) from ex

    pipeline = payload["pipeline"]
    feature_columns = payload.get("feature_columns")
    task = payload.get("task", "unknown")

    features = _as_features(body)

    if not isinstance(features, dict) or len(features) == 0:
        raise HTTPException(
            status_code=400,
            detail="Missing features. Send JSON like {\"features\": { ... }}",
        )

    X = pd.DataFrame([features])
    if isinstance(feature_columns, list) and len(feature_columns) > 0:
        # Align to training columns; unknown columns are dropped.
        X = X.reindex(columns=feature_columns, fill_value=None)

    try:
        pred = pipeline.predict(X)
    except Exception as ex:
        raise HTTPException(status_code=400, detail=f"Prediction failed: {ex}") from ex

    value: Any
    if hasattr(pred, "tolist"):
        value = pred.tolist()[0]
    else:
        value = pred[0]

    return PredictResponse(prediction=value, task=str(task))
