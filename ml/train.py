from __future__ import annotations

import argparse
import json
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Literal

import joblib
import pandas as pd
from sklearn.compose import ColumnTransformer
from sklearn.ensemble import RandomForestClassifier, RandomForestRegressor
from sklearn.metrics import accuracy_score, mean_absolute_error, r2_score
from sklearn.model_selection import train_test_split
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import OneHotEncoder, StandardScaler
from sklearn.impute import SimpleImputer


TaskType = Literal["regression", "classification"]


@dataclass(frozen=True)
class TrainingSummary:
    task: TaskType
    target: str
    n_rows: int
    n_features: int
    metrics: dict[str, float]


def _infer_task_type(y: pd.Series) -> TaskType:
    # Heuristic:
    # - object/bool/category => classification
    # - numeric with very few unique values (e.g. 0/1, 1-5 rating) => classification
    # - otherwise => regression
    if pd.api.types.is_bool_dtype(y) or pd.api.types.is_categorical_dtype(y) or pd.api.types.is_object_dtype(y):
        return "classification"

    unique_count = y.nunique(dropna=True)
    if unique_count <= 20 and pd.api.types.is_integer_dtype(y):
        return "classification"

    return "regression"


def _build_pipeline(X: pd.DataFrame, task: TaskType, random_state: int) -> Pipeline:
    categorical_cols = [c for c in X.columns if pd.api.types.is_object_dtype(X[c]) or pd.api.types.is_categorical_dtype(X[c])]
    numeric_cols = [c for c in X.columns if c not in categorical_cols]

    preprocessor = ColumnTransformer(
        transformers=[
            (
                "cat",
                Pipeline(
                    steps=[
                        ("impute", SimpleImputer(strategy="most_frequent")),
                        (
                            "onehot",
                            OneHotEncoder(handle_unknown="ignore"),
                        ),
                    ]
                ),
                categorical_cols,
            ),
            (
                "num",
                Pipeline(
                    steps=[
                        ("impute", SimpleImputer(strategy="median")),
                        ("scale", StandardScaler()),
                    ]
                ),
                numeric_cols,
            ),
        ],
        remainder="drop",
    )

    if task == "classification":
        model = RandomForestClassifier(
            n_estimators=300,
            random_state=random_state,
            n_jobs=-1,
        )
    else:
        model = RandomForestRegressor(
            n_estimators=500,
            random_state=random_state,
            n_jobs=-1,
        )

    return Pipeline(steps=[("preprocess", preprocessor), ("model", model)])


def train(
    data_path: Path,
    target: str,
    model_out: Path,
    test_size: float,
    random_state: int,
) -> TrainingSummary:
    df = pd.read_csv(data_path)

    if target not in df.columns:
        raise ValueError(
            f"Target column '{target}' not found. Available columns: {list(df.columns)}"
        )

    df = df.dropna(subset=[target]).copy()

    y = df[target]
    X = df.drop(columns=[target])

    task = _infer_task_type(y)
    pipeline = _build_pipeline(X, task=task, random_state=random_state)

    X_train, X_test, y_train, y_test = train_test_split(
        X,
        y,
        test_size=test_size,
        random_state=random_state,
        stratify=y if task == "classification" else None,
    )

    pipeline.fit(X_train, y_train)

    y_pred = pipeline.predict(X_test)

    if task == "classification":
        metrics = {
            "accuracy": float(accuracy_score(y_test, y_pred)),
        }
    else:
        metrics = {
            "r2": float(r2_score(y_test, y_pred)),
            "mae": float(mean_absolute_error(y_test, y_pred)),
        }

    model_out.parent.mkdir(parents=True, exist_ok=True)
    payload = {
        "pipeline": pipeline,
        "task": task,
        "target": target,
        "feature_columns": list(X.columns),
        "metrics": metrics,
    }

    joblib.dump(payload, model_out)

    summary = TrainingSummary(
        task=task,
        target=target,
        n_rows=int(df.shape[0]),
        n_features=int(X.shape[1]),
        metrics=metrics,
    )

    # Also write a small JSON summary alongside the model.
    json_out = model_out.with_suffix(".summary.json")
    json_out.write_text(json.dumps(asdict(summary), indent=2), encoding="utf-8")

    return summary


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Train a baseline ML model for property taxation datasets (CSV)."
    )
    parser.add_argument(
        "--data",
        type=Path,
        default=Path("data") / "property_tax.csv",
        help="Path to the CSV dataset.",
    )
    parser.add_argument(
        "--target",
        type=str,
        required=True,
        help="Target column name (e.g. tax_due / tax_amount / assessed_value).",
    )
    parser.add_argument(
        "--model-out",
        type=Path,
        default=Path("artifacts") / "model.joblib",
        help="Where to write the trained model.",
    )
    parser.add_argument(
        "--test-size",
        type=float,
        default=0.2,
        help="Test split fraction.",
    )
    parser.add_argument(
        "--random-state",
        type=int,
        default=42,
        help="Random seed.",
    )

    args = parser.parse_args()

    summary = train(
        data_path=args.data,
        target=args.target,
        model_out=args.model_out,
        test_size=args.test_size,
        random_state=args.random_state,
    )

    print("Training complete")
    print(summary)


if __name__ == "__main__":
    main()
