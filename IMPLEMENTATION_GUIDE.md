# Property Taxation & Compliance Reporting System
## Implementation Guide

## 🎯 System Overview

A production-ready full-stack web application for managing property taxation and compliance reporting across the Davao Region.

### Tech Stack
- **Backend**: ASP.NET Core Web API (C#) with Entity Framework Core
- **Frontend**: React.js + Tailwind CSS
- **Database**: MySQL
- **Authentication**: JWT-based with Role-Based Access Control (RBAC)

### Geographic Coverage
- **Region**: Davao Region (Region XI)
- **Provinces**: 
  - Davao del Sur
  - Davao del Norte
  - Davao de Oro
  - Davao Oriental
  - Davao Occidental
- **Cities**: 32+ cities and municipalities
- **Barangays**: Sample data for major cities

### User Roles
1. **Admin** - Full system access, user management, system configuration
2. **Accountant** - Financial operations, payment processing, reporting
3. **Auditor** - Audit logs review, compliance monitoring
4. **Staff** - Property registration, tax assessment creation

---

## 📁 Project Structure

```
property-taxation-system/
├── backend/
│   ├── Controllers/          # API endpoints
│   ├── Models/              # Entity models
│   ├── DTOs/                # Data Transfer Objects
│   ├── Services/            # Business logic layer
│   ├── Data/                # Database context
│   ├── Middleware/          # Custom middleware
│   ├── Program.cs           # Application entry point
│   └── appsettings.json     # Configuration
├── frontend/
│   ├── src/
│   │   ├── components/      # Reusable UI components
│   │   ├── pages/           # Page components
│   │   ├── context/         # React context (Auth, etc.)
│   │   ├── services/        # API service layer
│   │   ├── utils/           # Utility functions
│   │   └── App.tsx          # Main app component
│   └── package.json
├── database/
│   └── TaxsyncDB.sql        # Complete database schema
└── docker-compose.yml       # Docker orchestration
```

---

## 🗄️ Database Schema

### Core Tables

#### Geographic Hierarchy
- `regions` - Davao Region
- `provinces` - 5 provinces
- `cities` - Cities and municipalities
- `barangays` - Barangay-level data

#### User Management
- `users` - User accounts with roles
- `user_sessions` - JWT session tracking
- `password_reset_tokens` - Password recovery

#### Property Management
- `properties` - Property records with geographic references
- `property_documents` - Supporting documents

#### Tax System
- `tax_rates` - Configurable tax rates by property type
- `tax_assessments` - Tax computations and assessments
- `payments` - Payment records
- `payment_receipts` - Receipt generation

#### Audit & Compliance
- `activity_logs` - Complete audit trail
- `compliance_requirements` - Compliance rules
- `compliance_records` - Compliance tracking

#### Reporting
- `report_templates` - Report configurations
- `generated_reports` - Report history

---

## 🔧 Backend Implementation

### Models Created
✅ `User.cs` - User entity with roles and status
✅ `Region.cs`, `Province.cs`, `City.cs`, `Barangay.cs` - Geographic hierarchy
✅ `Property.cs` - Property entity with full address
✅ `TaxAssessment.cs` - Tax computation and tracking
✅ `Payment.cs` - Payment processing
✅ `AuditLog.cs` - Activity logging

### DTOs Created
✅ `AuthDtos.cs` - Login, Register, ChangePassword
✅ `UserDtos.cs` - User CRUD operations
✅ `PropertyDtos.cs` - Property management
✅ `TaxDtos.cs` - Tax assessment and computation
✅ `PaymentDtos.cs` - Payment processing

### Services Implemented
✅ `IAuthService` / `AuthService` - Authentication with JWT and BCrypt
✅ `IAuditService` / `AuditService` - Comprehensive audit logging
✅ `ITaxService` / `TaxService` - Tax computation engine with:
  - Configurable tax rates per property type
  - Automatic penalty calculation (2% per month)
  - SEF (Special Education Fund) tax (1%)
  - Formula: Tax = (Assessed Value × Tax Rate) + SEF Tax + Penalties - Discounts

### Database Context
✅ `ApplicationDbContext.cs` - Complete EF Core configuration with:
  - All entity mappings
  - Foreign key relationships
  - Column name mappings to match MySQL schema

---

## 🎨 Frontend Implementation

### Components Structure
```
components/
├── ui/                    # Base UI components (shadcn/ui style)
│   ├── Button.tsx
│   ├── Card.tsx
│   ├── Input.tsx
│   ├── Badge.tsx
│   └── ...
├── AppLayout.tsx          # Main layout with navigation
├── DashboardLayout.tsx    # Dashboard wrapper
├── RoleGuard.tsx          # Route protection by role
├── SearchAndFilter.tsx    # Reusable search/filter
├── Toast.tsx              # Notifications
└── ChatbotWidget.tsx      # Help widget
```

### Pages Structure
```
pages/
├── landing/               # Public landing page
├── login/                 # Authentication
├── Dashboard.tsx          # Role-based dashboard
├── PropertyRegistration.tsx
├── TaxAssessment.tsx
├── PaymentManagement.tsx
├── Compliance.tsx
├── Audit.tsx
└── Reports.tsx
```

---

## 🔐 Authentication & Authorization

### JWT Implementation
- Token generation with user claims
- 24-hour token expiration
- Role-based claims for authorization

### Password Security
- BCrypt hashing with salt
- Minimum 6 characters
- Password change with current password verification

### Role-Based Access Control (RBAC)
```
Admin:
  - All permissions
  - User management
  - System configuration
  - Tax rate management

Accountant:
  - Payment processing
  - Financial reports
  - Receipt generation

Auditor:
  - View audit logs
  - Compliance reports
  - Read-only access to all modules

Staff:
  - Property registration
  - Tax assessment creation
  - Document upload
```

---

## 💰 Tax Computation Engine

### Formula
```
Basic Tax = Assessed Value × Tax Rate
SEF Tax = Assessed Value × 0.01 (1%)
Penalty = Total Amount × 0.02 × Months Overdue
Total Tax = Basic Tax + SEF Tax + Penalty - Discounts
```

### Tax Rates (Configurable)
- Residential: 2.0%
- Commercial: 3.0%
- Industrial: 3.5%
- Agricultural: 1.5%
- Mixed Use: 2.5%

### Penalty System
- Automatic calculation based on due date
- 2% per month for late payments
- Updates assessment total amount

---

## 📊 Reporting Module

### Report Types
1. **Collection Reports**
   - Total collections by period
   - Payment method breakdown
   - Daily/Monthly/Yearly summaries

2. **Delinquency Reports**
   - Overdue assessments
   - Penalty calculations
   - Property owner contact info

3. **Compliance Reports**
   - Compliance score by property
   - Overdue requirements
   - Completion rates

4. **Property Reports**
   - Properties by area
   - Property type distribution
   - Assessed value summaries

### Export Formats
- PDF (official reports)
- CSV (data analysis)
- Excel (detailed reports)

---

## 🔍 Audit System

### Logged Actions
- User login/logout
- CRUD operations on all entities
- Payment processing
- Status changes
- Configuration updates

### Audit Log Fields
- User ID
- Action performed
- Module/Table affected
- Timestamp
- IP Address
- User Agent
- Severity (Info, Warning, Critical)
- Description

---

## 📈 Dashboard Analytics

### Metrics Displayed
1. **Financial Metrics**
   - Total collections (current year)
   - Pending payments
   - Overdue amounts
   - Collection rate

2. **Property Metrics**
   - Total properties registered
   - Properties by type
   - Properties by area
   - Active vs inactive

3. **Compliance Metrics**
   - Compliance rate
   - Overdue requirements
   - Completed assessments

4. **Charts**
   - Collection trends (line chart)
   - Property distribution (pie chart)
   - Payment methods (bar chart)
   - Geographic distribution (map/bar chart)

---

## 🚀 API Endpoints

### Authentication
```
POST   /api/auth/login
POST   /api/auth/register
POST   /api/auth/change-password
GET    /api/auth/me
```

### Users
```
GET    /api/users
GET    /api/users/{id}
POST   /api/users
PUT    /api/users/{id}
DELETE /api/users/{id}
```

### Properties
```
GET    /api/properties
GET    /api/properties/{id}
POST   /api/properties
PUT    /api/properties/{id}
DELETE /api/properties/{id}
POST   /api/properties/{id}/documents
```

### Tax Assessments
```
GET    /api/tax/assessments
GET    /api/tax/assessments/{id}
POST   /api/tax/assessments
POST   /api/tax/compute
PUT    /api/tax/assessments/{id}/approve
GET    /api/tax/rates
PUT    /api/tax/rates
```

### Payments
```
GET    /api/payments
GET    /api/payments/{id}
POST   /api/payments
GET    /api/payments/{id}/receipt
```

### Geographic Data
```
GET    /api/geography/regions
GET    /api/geography/provinces
GET    /api/geography/cities
GET    /api/geography/barangays
```

### Audit Logs
```
GET    /api/audit/logs
GET    /api/audit/logs/{id}
```

### Reports
```
POST   /api/reports/collection
POST   /api/reports/delinquency
POST   /api/reports/compliance
POST   /api/reports/property
```

---

## 🐳 Docker Setup

### Services
1. **MySQL Database** (Port 3306)
2. **Backend API** (Port 5000)
3. **Frontend** (Port 3000)

### Environment Variables
```env
# Database
DB_HOST=mysql
DB_PORT=3306
DB_NAME=TaxsyncDB
DB_USER=root
DB_PASSWORD=your_password

# JWT
JWT_KEY=your-super-secret-key-min-32-characters
JWT_ISSUER=TaxSync
JWT_AUDIENCE=TaxSyncUsers

# Frontend
VITE_API_URL=http://localhost:5000/api
```

---

## 📝 Setup Instructions

### Prerequisites
- .NET 8.0 SDK or later
- Node.js 18+ and npm
- MySQL 8.0+
- Docker (optional)

### Backend Setup
```bash
cd backend

# Install dependencies
dotnet restore

# Update connection string in appsettings.json
# Run migrations (if using EF migrations)
dotnet ef database update

# Run the application
dotnet run
```

### Frontend Setup
```bash
cd frontend

# Install dependencies
npm install

# Update API URL in .env
echo "VITE_API_URL=http://localhost:5000/api" > .env

# Run development server
npm run dev
```

### Database Setup
```bash
# Import the SQL file
mysql -u root -p < database/TaxsyncDB.sql

# Or use phpMyAdmin to import TaxsyncDB.sql
```

### Docker Setup
```bash
# Build and run all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

---

## 🧪 Testing

### Sample Test Data
The database includes:
- 4 sample users (admin, accountant, auditor, staff)
- Default password: `password123` (hashed)
- 5 provinces in Davao Region
- 32+ cities and municipalities
- Sample barangays for major cities
- Tax rates for all property types

### Test Credentials
```
Username: admin
Password: password123
Role: Admin

Username: accountant1
Password: password123
Role: Accountant

Username: auditor1
Password: password123
Role: Auditor

Username: staff1
Password: password123
Role: Staff
```

---

## 🔒 Security Features

1. **Password Hashing** - BCrypt with automatic salt
2. **JWT Authentication** - Secure token-based auth
3. **Role-Based Authorization** - Endpoint protection
4. **SQL Injection Prevention** - Parameterized queries via EF Core
5. **CORS Configuration** - Restricted origins
6. **Input Validation** - Data annotations and model validation
7. **Audit Logging** - Complete activity trail
8. **Session Management** - Token expiration and refresh

---

## 📦 NuGet Packages Required

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.0" />
```

---

## 🎯 Next Steps

### Remaining Implementation
1. ✅ Complete service layer (Property, Payment, User services)
2. ✅ Create all controllers
3. ✅ Add middleware (authentication, error handling, audit)
4. ✅ Update Program.cs with all services
5. ✅ Frontend API service layer
6. ✅ Frontend pages implementation
7. ✅ Dashboard with charts
8. ✅ Report generation
9. ✅ Docker configuration
10. ✅ Testing and documentation

---

## 📞 Support

For issues or questions:
- Check audit logs for system errors
- Review API documentation (Swagger at `/swagger`)
- Contact system administrator

---

**Version**: 1.0.0  
**Last Updated**: 2026-05-03  
**Status**: In Development
