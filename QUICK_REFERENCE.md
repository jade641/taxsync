# Quick Reference Guide
## Property Taxation & Compliance Reporting System

## 🚀 Quick Start Commands

### Start Development Environment

```bash
# Terminal 1 - Backend
cd backend
dotnet run

# Terminal 2 - Frontend  
cd frontend
npm run dev
```

### Start with Docker

```bash
docker-compose up -d
```

---

## 🔑 Test Credentials

| Username | Password | Role |
|----------|----------|------|
| admin | password123 | Admin |
| accountant1 | password123 | Accountant |
| auditor1 | password123 | Auditor |
| staff1 | password123 | Staff |

---

## 🌐 Access URLs

### Local Development
- **Frontend**: http://localhost:5173
- **Backend API**: http://localhost:5000
- **Swagger Docs**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/api/health

### Docker Deployment
- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:5000
- **Swagger Docs**: http://localhost:5000/swagger

---

## 📡 Common API Endpoints

### Authentication
```bash
# Login
POST /api/auth/login
Body: { "username": "admin", "password": "password123" }

# Get Current User
GET /api/auth/me
Header: Authorization: Bearer {token}
```

### Properties
```bash
# List Properties
GET /api/properties?page=1&pageSize=50

# Create Property
POST /api/properties
Header: Authorization: Bearer {token}
Body: { "ownerId": 1, "propertyType": "residential", ... }
```

### Tax Assessments
```bash
# Compute Tax
POST /api/tax/compute
Body: { "propertyType": "residential", "assessedValue": 500000, "taxYear": 2024 }

# Create Assessment
POST /api/tax/assessments
Header: Authorization: Bearer {token}
Body: { "propertyId": 1, "taxYear": 2024, ... }
```

### Payments
```bash
# Create Payment
POST /api/payments
Header: Authorization: Bearer {token}
Body: { "assessmentId": 1, "payerId": 1, "amountPaid": 10000, ... }

# Get Receipt
GET /api/payments/{id}/receipt
```

---

## 💰 Tax Computation Formula

```
Basic Tax = Assessed Value × Tax Rate
SEF Tax = Assessed Value × 0.01 (1%)
Penalty = Total Amount × 0.02 × Months Overdue
Total Tax = Basic Tax + SEF Tax + Penalty - Discounts
```

### Tax Rates
- Residential: 2.0%
- Commercial: 3.0%
- Industrial: 3.5%
- Agricultural: 1.5%
- Mixed Use: 2.5%

---

## 🗺️ Geographic Hierarchy

```
Davao Region (Region XI)
├── Davao del Sur
│   ├── Digos City
│   ├── Bansalan
│   └── ... (10 municipalities)
├── Davao del Norte
│   ├── Tagum City
│   ├── Panabo City
│   ├── Samal City
│   └── ... (8 municipalities)
├── Davao de Oro
│   ├── Nabunturan
│   └── ... (11 municipalities)
├── Davao Oriental
│   ├── Mati City
│   └── ... (10 municipalities)
└── Davao Occidental
    ├── Malita
    └── ... (5 municipalities)
```

---

## 👥 Role Permissions

### Admin
✅ All permissions  
✅ User management  
✅ System configuration  
✅ Tax rate management  

### Accountant
✅ Payment processing  
✅ Financial reports  
✅ Tax assessment approval  
✅ Receipt generation  

### Auditor
✅ View audit logs  
✅ Compliance reports  
✅ Read-only access  

### Staff
✅ Property registration  
✅ Tax assessment creation  
✅ Document upload  
✅ Basic reports  

---

## 🗄️ Database Tables

### Core Tables (20+)
- users
- regions, provinces, cities, barangays
- properties, property_documents
- tax_rates, tax_assessments
- payments, payment_receipts
- activity_logs
- compliance_requirements, compliance_records
- system_settings

---

## 🔧 Configuration Files

### Backend
```
backend/appsettings.json
- ConnectionStrings:DefaultConnection
- Jwt:Key, Jwt:Issuer, Jwt:Audience
```

### Frontend
```
frontend/.env
- VITE_API_URL=http://localhost:5000/api
```

### Docker
```
docker-compose.yml
- MySQL configuration
- Backend environment variables
- Frontend build arguments
```

---

## 🐛 Common Issues & Solutions

### Backend won't start
```bash
# Check .NET version
dotnet --version

# Restore packages
dotnet restore

# Check MySQL connection
mysql -u root -p
```

### Frontend won't start
```bash
# Clear cache and reinstall
rm -rf node_modules package-lock.json
npm install

# Check Node version
node --version
```

### Database connection fails
```bash
# Verify MySQL is running
# Windows: Services → MySQL
# Mac: System Preferences → MySQL
# Linux: sudo systemctl status mysql

# Test connection
mysql -u root -p -e "SHOW DATABASES;"
```

### CORS errors
```bash
# Check backend CORS policy in Program.cs
# Verify frontend URL matches allowed origins
# Check .env file has correct API URL
```

---

## 📊 Sample API Requests

### Login and Get Token
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"password123"}'
```

### Create Property
```bash
curl -X POST http://localhost:5000/api/properties \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "ownerId": 1,
    "propertyType": "residential",
    "addressLine1": "123 Main St",
    "regionId": 1,
    "provinceId": 1,
    "cityId": 1,
    "barangayId": 1,
    "marketValue": 500000,
    "assessedValue": 400000
  }'
```

### Compute Tax
```bash
curl -X POST http://localhost:5000/api/tax/compute \
  -H "Content-Type: application/json" \
  -d '{
    "propertyType": "residential",
    "assessedValue": 500000,
    "taxYear": 2024
  }'
```

---

## 🔍 Useful SQL Queries

### Check User Accounts
```sql
SELECT user_id, username, email, role, status 
FROM users;
```

### View Properties
```sql
SELECT p.property_id, p.property_number, p.property_type,
       CONCAT(u.first_name, ' ', u.last_name) as owner,
       c.city_name, b.barangay_name
FROM properties p
JOIN users u ON p.owner_id = u.user_id
JOIN cities c ON p.city_id = c.city_id
JOIN barangays b ON p.barangay_id = b.barangay_id;
```

### Tax Assessment Summary
```sql
SELECT ta.assessment_id, ta.tax_year, ta.quarter,
       p.property_number, ta.total_amount, ta.status
FROM tax_assessments ta
JOIN properties p ON ta.property_id = p.property_id
ORDER BY ta.created_at DESC;
```

### Payment Summary
```sql
SELECT DATE_FORMAT(payment_date, '%Y-%m') as month,
       COUNT(*) as transactions,
       SUM(amount_paid) as total_collected
FROM payments
WHERE status = 'completed'
GROUP BY DATE_FORMAT(payment_date, '%Y-%m')
ORDER BY month DESC;
```

---

## 📦 Package Management

### Backend (NuGet)
```bash
# Restore packages
dotnet restore

# Add package
dotnet add package PackageName

# Update packages
dotnet list package --outdated
dotnet add package PackageName --version X.X.X
```

### Frontend (npm)
```bash
# Install packages
npm install

# Add package
npm install package-name

# Update packages
npm outdated
npm update
```

---

## 🔄 Git Commands

```bash
# Clone repository
git clone <repository-url>

# Check status
git status

# Create branch
git checkout -b feature/new-feature

# Commit changes
git add .
git commit -m "Description of changes"

# Push changes
git push origin feature/new-feature
```

---

## 📝 Logging & Debugging

### Backend Logs
```bash
# View console output
dotnet run

# Enable detailed logging
# Edit appsettings.json:
"LogLevel": {
  "Default": "Debug",
  "Microsoft.EntityFrameworkCore": "Information"
}
```

### Frontend Logs
```bash
# Browser console (F12)
# Check Network tab for API calls
# Check Console tab for errors
```

### Database Logs
```bash
# MySQL error log location
# Windows: C:\ProgramData\MySQL\MySQL Server 8.0\Data\*.err
# Mac: /usr/local/mysql/data/*.err
# Linux: /var/log/mysql/error.log
```

---

## 🎯 Testing Checklist

- [ ] Backend starts without errors
- [ ] Frontend starts without errors
- [ ] Can login with test credentials
- [ ] Dashboard displays correctly
- [ ] Can create property
- [ ] Can create tax assessment
- [ ] Can process payment
- [ ] Can view audit logs
- [ ] API calls work (check Network tab)
- [ ] No console errors

---

## 📞 Quick Help

### Documentation
- [README.md](README.md) - Overview
- [SETUP_INSTRUCTIONS.md](SETUP_INSTRUCTIONS.md) - Setup guide
- [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md) - Technical details
- [DEPLOYMENT_SUMMARY.md](DEPLOYMENT_SUMMARY.md) - Deployment info

### Online Resources
- Swagger UI: http://localhost:5000/swagger
- Health Check: http://localhost:5000/api/health

### Support
- Check audit logs in system
- Review error messages in console
- Check database connection
- Verify configuration files

---

**Version**: 1.0.0  
**Last Updated**: May 3, 2026

**Quick Tip**: Keep this file open while developing for quick reference!
