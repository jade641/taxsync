"""
Model Training Script for TaxSync ML Service
Trains and saves ML models for tax prediction, revenue forecasting, and anomaly detection
"""

import numpy as np
import pandas as pd
from sklearn.ensemble import RandomForestRegressor, IsolationForest
from sklearn.linear_model import LinearRegression
from sklearn.model_selection import train_test_split
from sklearn.metrics import mean_squared_error, r2_score
import joblib
import os
from datetime import datetime

# Create models directory
MODELS_DIR = "models"
os.makedirs(MODELS_DIR, exist_ok=True)

def generate_synthetic_tax_data(n_samples=1000):
    """
    Generate synthetic property tax data for training
    
    In production, replace this with actual historical data
    """
    np.random.seed(42)
    
    # Property features
    land_area = np.random.uniform(50, 5000, n_samples)
    assessed_value = np.random.uniform(500000, 50000000, n_samples)
    property_type = np.random.randint(1, 5, n_samples)  # 1-4 for different types
    region_id = np.random.randint(1, 12, n_samples)  # 11 regions in Davao
    building_age = np.random.randint(0, 50, n_samples)
    
    # Calculate target (tax amount) with realistic formula
    base_rate = np.where(property_type == 1, 0.01,
                 np.where(property_type == 2, 0.02,
                 np.where(property_type == 3, 0.025, 0.005)))
    
    regional_factor = 1.0 + (region_id * 0.001)
    area_factor = 1.0 + (land_area / 10000) * 0.1
    age_factor = 1.0 - (np.minimum(building_age, 50) / 100) * 0.2
    
    tax_amount = assessed_value * base_rate * regional_factor * area_factor * age_factor
    
    # Add some noise
    tax_amount += np.random.normal(0, tax_amount * 0.05, n_samples)
    tax_amount = np.maximum(tax_amount, 0)  # Ensure non-negative
    
    # Create DataFrame
    df = pd.DataFrame({
        'land_area': land_area,
        'assessed_value': assessed_value,
        'property_type': property_type,
        'region_id': region_id,
        'building_age': building_age,
        'tax_amount': tax_amount
    })
    
    return df

def train_tax_prediction_model():
    """Train Random Forest model for tax prediction"""
    print("=" * 60)
    print("Training Tax Prediction Model")
    print("=" * 60)
    
    # Generate training data
    df = generate_synthetic_tax_data(n_samples=2000)
    
    # Prepare features and target
    X = df[['land_area', 'assessed_value', 'property_type', 'region_id', 'building_age']]
    y = df['tax_amount']
    
    # Split data
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=0.2, random_state=42
    )
    
    # Train model
    print("Training Random Forest Regressor...")
    model = RandomForestRegressor(
        n_estimators=100,
        max_depth=15,
        min_samples_split=5,
        min_samples_leaf=2,
        random_state=42,
        n_jobs=-1
    )
    
    model.fit(X_train, y_train)
    
    # Evaluate
    train_score = model.score(X_train, y_train)
    test_score = model.score(X_test, y_test)
    
    y_pred = model.predict(X_test)
    rmse = np.sqrt(mean_squared_error(y_test, y_pred))
    
    print(f"Training R² Score: {train_score:.4f}")
    print(f"Testing R² Score: {test_score:.4f}")
    print(f"RMSE: ₱{rmse:,.2f}")
    
    # Feature importance
    feature_importance = pd.DataFrame({
        'feature': X.columns,
        'importance': model.feature_importances_
    }).sort_values('importance', ascending=False)
    
    print("\nFeature Importance:")
    print(feature_importance.to_string(index=False))
    
    # Save model
    model_path = os.path.join(MODELS_DIR, "tax_prediction_model.pkl")
    joblib.dump(model, model_path)
    print(f"\n✓ Model saved to: {model_path}")
    
    return model

def train_revenue_forecast_model():
    """Train Linear Regression model for revenue forecasting"""
    print("\n" + "=" * 60)
    print("Training Revenue Forecast Model")
    print("=" * 60)
    
    # Generate synthetic time series data
    np.random.seed(42)
    n_periods = 36  # 3 years of monthly data
    
    # Create trend with seasonality
    time = np.arange(n_periods)
    trend = 5000000 + (time * 50000)  # Growing trend
    seasonality = 500000 * np.sin(time * 2 * np.pi / 12)  # Annual seasonality
    noise = np.random.normal(0, 200000, n_periods)
    
    revenue = trend + seasonality + noise
    revenue = np.maximum(revenue, 0)
    
    # Prepare features (time-based)
    X = time.reshape(-1, 1)
    y = revenue
    
    # Split data
    split_idx = int(len(X) * 0.8)
    X_train, X_test = X[:split_idx], X[split_idx:]
    y_train, y_test = y[:split_idx], y[split_idx:]
    
    # Train model
    print("Training Linear Regression model...")
    model = LinearRegression()
    model.fit(X_train, y_train)
    
    # Evaluate
    train_score = model.score(X_train, y_train)
    test_score = model.score(X_test, y_test)
    
    y_pred = model.predict(X_test)
    rmse = np.sqrt(mean_squared_error(y_test, y_pred))
    
    print(f"Training R² Score: {train_score:.4f}")
    print(f"Testing R² Score: {test_score:.4f}")
    print(f"RMSE: ₱{rmse:,.2f}")
    
    # Save model
    model_path = os.path.join(MODELS_DIR, "revenue_forecast_model.pkl")
    joblib.dump(model, model_path)
    print(f"\n✓ Model saved to: {model_path}")
    
    return model

def train_anomaly_detection_model():
    """Train Isolation Forest for anomaly detection"""
    print("\n" + "=" * 60)
    print("Training Anomaly Detection Model")
    print("=" * 60)
    
    # Generate synthetic transaction data
    np.random.seed(42)
    n_samples = 1000
    
    # Normal transactions
    normal_amounts = np.random.normal(50000, 15000, int(n_samples * 0.95))
    normal_amounts = np.maximum(normal_amounts, 1000)
    
    # Anomalous transactions
    anomaly_amounts = np.concatenate([
        np.random.uniform(200000, 500000, int(n_samples * 0.03)),  # High values
        np.random.uniform(100, 1000, int(n_samples * 0.02))  # Low values
    ])
    
    # Combine
    all_amounts = np.concatenate([normal_amounts, anomaly_amounts])
    np.random.shuffle(all_amounts)
    
    X = all_amounts.reshape(-1, 1)
    
    # Train model
    print("Training Isolation Forest...")
    model = IsolationForest(
        contamination=0.05,  # Expected proportion of anomalies
        random_state=42,
        n_estimators=100
    )
    
    model.fit(X)
    
    # Evaluate
    predictions = model.predict(X)
    anomaly_count = np.sum(predictions == -1)
    anomaly_percentage = (anomaly_count / len(X)) * 100
    
    print(f"Total samples: {len(X)}")
    print(f"Detected anomalies: {anomaly_count}")
    print(f"Anomaly percentage: {anomaly_percentage:.2f}%")
    
    # Save model
    model_path = os.path.join(MODELS_DIR, "anomaly_detection_model.pkl")
    joblib.dump(model, model_path)
    print(f"\n✓ Model saved to: {model_path}")
    
    return model

def main():
    """Main training pipeline"""
    print("\n" + "=" * 60)
    print("TaxSync ML Model Training Pipeline")
    print(f"Started at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("=" * 60)
    
    try:
        # Train all models
        tax_model = train_tax_prediction_model()
        revenue_model = train_revenue_forecast_model()
        anomaly_model = train_anomaly_detection_model()
        
        print("\n" + "=" * 60)
        print("✓ All models trained successfully!")
        print("=" * 60)
        print(f"\nModels saved in: {os.path.abspath(MODELS_DIR)}")
        print("\nYou can now start the ML service with:")
        print("  python main.py")
        print("  or")
        print("  uvicorn main:app --reload")
        
    except Exception as e:
        print(f"\n✗ Error during training: {str(e)}")
        raise

if __name__ == "__main__":
    main()
