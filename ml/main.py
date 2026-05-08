"""
TaxSync Machine Learning Service
FastAPI-based ML service for property tax prediction, revenue forecasting, and anomaly detection
"""

from fastapi import FastAPI, HTTPException, status
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field, validator
from typing import List, Optional, Dict, Any
import pandas as pd
import numpy as np
from datetime import datetime
import joblib
import os
import logging

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# Initialize FastAPI app
app = FastAPI(
    title="TaxSync ML Service",
    description="Machine Learning API for Property Tax Prediction and Analytics",
    version="1.0.0"
)

# CORS configuration
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # In production, specify exact origins
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Model paths
MODELS_DIR = "models"
os.makedirs(MODELS_DIR, exist_ok=True)

TAX_PREDICTION_MODEL = os.path.join(MODELS_DIR, "tax_prediction_model.pkl")
REVENUE_FORECAST_MODEL = os.path.join(MODELS_DIR, "revenue_forecast_model.pkl")
ANOMALY_DETECTION_MODEL = os.path.join(MODELS_DIR, "anomaly_detection_model.pkl")

# Global model storage
models = {
    "tax_prediction": None,
    "revenue_forecast": None,
    "anomaly_detection": None
}

# ==================== REQUEST/RESPONSE MODELS ====================

class PropertyFeatures(BaseModel):
    """Property features for tax prediction"""
    land_area: float = Field(..., gt=0, description="Land area in square meters")
    assessed_value: float = Field(..., gt=0, description="Assessed property value")
    property_type: str = Field(..., description="Type of property (Residential, Commercial, Industrial, Agricultural)")
    region_id: int = Field(..., gt=0, description="Region ID")
    building_age: Optional[int] = Field(0, ge=0, description="Age of building in years")
    
    @validator('property_type')
    def validate_property_type(cls, v):
        valid_types = ['Residential', 'Commercial', 'Industrial', 'Agricultural']
        if v not in valid_types:
            raise ValueError(f'Property type must be one of {valid_types}')
        return v

class TaxPredictionRequest(BaseModel):
    """Request model for tax prediction"""
    properties: List[PropertyFeatures]

class TaxPredictionResponse(BaseModel):
    """Response model for tax prediction"""
    predictions: List[Dict[str, Any]]
    model_version: str
    timestamp: str

class RevenueForecastRequest(BaseModel):
    """Request model for revenue forecasting"""
    historical_data: List[Dict[str, Any]]
    forecast_periods: int = Field(..., gt=0, le=24, description="Number of periods to forecast (max 24)")
    
class RevenueForecastResponse(BaseModel):
    """Response model for revenue forecasting"""
    forecasts: List[Dict[str, Any]]
    confidence_intervals: List[Dict[str, Any]]
    model_version: str
    timestamp: str

class AnomalyDetectionRequest(BaseModel):
    """Request model for anomaly detection"""
    transactions: List[Dict[str, Any]]
    
class AnomalyDetectionResponse(BaseModel):
    """Response model for anomaly detection"""
    anomalies: List[Dict[str, Any]]
    total_transactions: int
    anomaly_count: int
    anomaly_percentage: float
    timestamp: str

class HealthResponse(BaseModel):
    """Health check response"""
    status: str
    service: str
    version: str
    timestamp: str
    models_loaded: Dict[str, bool]

# ==================== HELPER FUNCTIONS ====================

def load_models():
    """Load ML models from disk"""
    try:
        if os.path.exists(TAX_PREDICTION_MODEL):
            models["tax_prediction"] = joblib.load(TAX_PREDICTION_MODEL)
            logger.info("Tax prediction model loaded successfully")
        else:
            logger.warning(f"Tax prediction model not found at {TAX_PREDICTION_MODEL}")
            
        if os.path.exists(REVENUE_FORECAST_MODEL):
            models["revenue_forecast"] = joblib.load(REVENUE_FORECAST_MODEL)
            logger.info("Revenue forecast model loaded successfully")
        else:
            logger.warning(f"Revenue forecast model not found at {REVENUE_FORECAST_MODEL}")
            
        if os.path.exists(ANOMALY_DETECTION_MODEL):
            models["anomaly_detection"] = joblib.load(ANOMALY_DETECTION_MODEL)
            logger.info("Anomaly detection model loaded successfully")
        else:
            logger.warning(f"Anomaly detection model not found at {ANOMALY_DETECTION_MODEL}")
            
    except Exception as e:
        logger.error(f"Error loading models: {str(e)}")

def encode_property_type(property_type: str) -> int:
    """Encode property type to numeric value"""
    encoding = {
        'Residential': 1,
        'Commercial': 2,
        'Industrial': 3,
        'Agricultural': 4
    }
    return encoding.get(property_type, 1)

def calculate_tax_rate(property_type: str, region_id: int) -> float:
    """Calculate tax rate based on property type and region"""
    base_rates = {
        'Residential': 0.01,
        'Commercial': 0.02,
        'Industrial': 0.025,
        'Agricultural': 0.005
    }
    
    # Regional adjustment factor (simplified)
    regional_factor = 1.0 + (region_id * 0.001)
    
    base_rate = base_rates.get(property_type, 0.01)
    return base_rate * regional_factor

# ==================== API ENDPOINTS ====================

@app.on_event("startup")
async def startup_event():
    """Load models on startup"""
    logger.info("Starting TaxSync ML Service...")
    load_models()
    logger.info("TaxSync ML Service started successfully")

@app.get("/", response_model=Dict[str, str])
async def root():
    """Root endpoint"""
    return {
        "service": "TaxSync ML Service",
        "version": "1.0.0",
        "status": "running",
        "endpoints": {
            "health": "/health",
            "predict": "/predict",
            "forecast": "/forecast",
            "anomaly_detection": "/anomaly-detection"
        }
    }

@app.get("/health", response_model=HealthResponse)
async def health_check():
    """Health check endpoint"""
    return HealthResponse(
        status="healthy",
        service="TaxSync ML Service",
        version="1.0.0",
        timestamp=datetime.utcnow().isoformat(),
        models_loaded={
            "tax_prediction": models["tax_prediction"] is not None,
            "revenue_forecast": models["revenue_forecast"] is not None,
            "anomaly_detection": models["anomaly_detection"] is not None
        }
    )

@app.post("/predict", response_model=TaxPredictionResponse)
async def predict_tax(request: TaxPredictionRequest):
    """
    Predict property tax for given properties
    
    Uses a combination of ML model (if available) and rule-based calculation
    """
    try:
        predictions = []
        
        for prop in request.properties:
            # Prepare features
            features = {
                'land_area': prop.land_area,
                'assessed_value': prop.assessed_value,
                'property_type_encoded': encode_property_type(prop.property_type),
                'region_id': prop.region_id,
                'building_age': prop.building_age or 0
            }
            
            # Calculate tax using rule-based approach
            tax_rate = calculate_tax_rate(prop.property_type, prop.region_id)
            base_tax = prop.assessed_value * tax_rate
            
            # Apply area adjustment
            area_factor = 1.0 + (prop.land_area / 10000) * 0.1
            
            # Apply age depreciation for buildings
            age_factor = 1.0 - (min(prop.building_age or 0, 50) / 100) * 0.2
            
            predicted_tax = base_tax * area_factor * age_factor
            
            # If ML model is available, blend predictions
            if models["tax_prediction"] is not None:
                try:
                    feature_array = np.array([[
                        features['land_area'],
                        features['assessed_value'],
                        features['property_type_encoded'],
                        features['region_id'],
                        features['building_age']
                    ]])
                    ml_prediction = models["tax_prediction"].predict(feature_array)[0]
                    # Blend: 70% ML, 30% rule-based
                    predicted_tax = (ml_prediction * 0.7) + (predicted_tax * 0.3)
                except Exception as e:
                    logger.warning(f"ML prediction failed, using rule-based: {str(e)}")
            
            predictions.append({
                "land_area": prop.land_area,
                "assessed_value": prop.assessed_value,
                "property_type": prop.property_type,
                "predicted_tax": round(predicted_tax, 2),
                "tax_rate": round(tax_rate * 100, 3),
                "confidence": 0.85 if models["tax_prediction"] else 0.70
            })
        
        return TaxPredictionResponse(
            predictions=predictions,
            model_version="1.0.0",
            timestamp=datetime.utcnow().isoformat()
        )
        
    except Exception as e:
        logger.error(f"Error in tax prediction: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Prediction error: {str(e)}"
        )

@app.post("/forecast", response_model=RevenueForecastResponse)
async def forecast_revenue(request: RevenueForecastRequest):
    """
    Forecast future tax revenue based on historical data
    
    Uses time series analysis and trend projection
    """
    try:
        if len(request.historical_data) < 3:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="At least 3 historical data points required"
            )
        
        # Convert to DataFrame
        df = pd.DataFrame(request.historical_data)
        
        if 'amount' not in df.columns or 'period' not in df.columns:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="Historical data must contain 'amount' and 'period' fields"
            )
        
        # Sort by period
        df = df.sort_values('period')
        amounts = df['amount'].values
        
        # Calculate trend using linear regression
        x = np.arange(len(amounts))
        z = np.polyfit(x, amounts, 1)
        trend = np.poly1d(z)
        
        # Calculate seasonality (simplified)
        detrended = amounts - trend(x)
        seasonal_factor = np.mean(np.abs(detrended)) / np.mean(amounts)
        
        # Generate forecasts
        forecasts = []
        confidence_intervals = []
        
        last_period = len(amounts)
        
        for i in range(request.forecast_periods):
            period_idx = last_period + i
            
            # Base forecast from trend
            base_forecast = trend(period_idx)
            
            # Add seasonal variation
            seasonal_adjustment = base_forecast * seasonal_factor * np.sin(i * np.pi / 6)
            forecast_value = base_forecast + seasonal_adjustment
            
            # Calculate confidence interval (simplified)
            std_dev = np.std(amounts)
            confidence_margin = std_dev * 1.96  # 95% confidence
            
            forecasts.append({
                "period": period_idx + 1,
                "forecasted_amount": round(max(0, forecast_value), 2),
                "trend": "increasing" if z[0] > 0 else "decreasing"
            })
            
            confidence_intervals.append({
                "period": period_idx + 1,
                "lower_bound": round(max(0, forecast_value - confidence_margin), 2),
                "upper_bound": round(forecast_value + confidence_margin, 2)
            })
        
        return RevenueForecastResponse(
            forecasts=forecasts,
            confidence_intervals=confidence_intervals,
            model_version="1.0.0",
            timestamp=datetime.utcnow().isoformat()
        )
        
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error in revenue forecasting: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Forecasting error: {str(e)}"
        )

@app.post("/anomaly-detection", response_model=AnomalyDetectionResponse)
async def detect_anomalies(request: AnomalyDetectionRequest):
    """
    Detect anomalous transactions using statistical methods
    
    Identifies transactions that deviate significantly from normal patterns
    """
    try:
        if len(request.transactions) < 10:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="At least 10 transactions required for anomaly detection"
            )
        
        # Convert to DataFrame
        df = pd.DataFrame(request.transactions)
        
        if 'amount' not in df.columns:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="Transactions must contain 'amount' field"
            )
        
        amounts = df['amount'].values
        
        # Calculate statistical thresholds
        mean_amount = np.mean(amounts)
        std_amount = np.std(amounts)
        
        # Z-score method for anomaly detection
        z_scores = np.abs((amounts - mean_amount) / std_amount)
        threshold = 3.0  # 3 standard deviations
        
        # IQR method for additional validation
        q1 = np.percentile(amounts, 25)
        q3 = np.percentile(amounts, 75)
        iqr = q3 - q1
        lower_bound = q1 - (1.5 * iqr)
        upper_bound = q3 + (1.5 * iqr)
        
        anomalies = []
        
        for idx, (amount, z_score) in enumerate(zip(amounts, z_scores)):
            is_anomaly = (z_score > threshold) or (amount < lower_bound) or (amount > upper_bound)
            
            if is_anomaly:
                transaction = request.transactions[idx].copy()
                transaction['anomaly_score'] = round(float(z_score), 2)
                transaction['reason'] = []
                
                if z_score > threshold:
                    transaction['reason'].append(f"Z-score: {z_score:.2f} (threshold: {threshold})")
                if amount < lower_bound:
                    transaction['reason'].append(f"Below lower bound: {lower_bound:.2f}")
                if amount > upper_bound:
                    transaction['reason'].append(f"Above upper bound: {upper_bound:.2f}")
                
                transaction['severity'] = 'high' if z_score > 4 else 'medium' if z_score > 3 else 'low'
                anomalies.append(transaction)
        
        anomaly_count = len(anomalies)
        total_count = len(request.transactions)
        
        return AnomalyDetectionResponse(
            anomalies=anomalies,
            total_transactions=total_count,
            anomaly_count=anomaly_count,
            anomaly_percentage=round((anomaly_count / total_count) * 100, 2),
            timestamp=datetime.utcnow().isoformat()
        )
        
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error in anomaly detection: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Anomaly detection error: {str(e)}"
        )

# ==================== MAIN ====================

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(
        "main:app",
        host="0.0.0.0",
        port=8000,
        reload=True,
        log_level="info"
    )
