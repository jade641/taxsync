# Property Taxation & Compliance Reporting System

A production-ready full-stack web application for managing property taxation and compliance reporting across the Davao Region, Philippines.

## 🎯 System Overview

This system provides comprehensive property tax management, payment processing, compliance tracking, and reporting capabilities for the entire Davao Region including all provinces, cities, municipalities, and barangays.

### Key Features

- **User Management** - Role-based access control (Admin, Accountant, Auditor, Staff)
- **Property Registration** - Complete property records with geographic hierarchy
- **Tax Computation Engine** - Automated tax calculation with configurable rates
- **Payment Management** - Multi-method payment processing with receipt generation
- **Penalties & Deadlines** - Automatic penalty calculation for late payments
- **Compliance Tracking** - Monitor compliance requirements and deadlines
- **Audit System** - Complete activity logging and audit trail
- **Reporting Module** - Generate collection, delinquency, and compliance reports
- **Dashboard Analytics** - Real-time charts and statistics

## 🛠️ Tech Stack

- **Backend**: ASP.NET Core Web API (.NET 10.0) with Entity Framework Core
- **Frontend**: React.js 19 + TypeScript + Tailwind CSS 4
- **Database**: MySQL 8.0+
- **Authentication**: JWT with BCrypt password hashing
- **API Documentation**: Swagger/OpenAPI

## 📋 Prerequisites

- .NET 10.0 SDK or later
- Node.js 18+ and npm
- MySQL 8.0+
- Git

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone <repository-url>
cd property-taxation-system
```

### 2. Database Setup

```bash
# Create the database
mysql -u root -p

# Import the schema
mysql -u root -p < database/TaxsyncDB.sql
```

Or use phpMyAdmin to import `database/TaxsyncDB.sql`

### 3. Backend Setup

```bash
cd backend

# Restore NuGet packages
dotnet restore

# Update connection string in appsettings.json
# Edit: "Server=localhost;Database=TaxsyncDB;User=root;Password=YOUR_PASSWORD;"

# Run the application
dotnet run
```

The API will be available at `https://localhost:5001` or `http://localhost:5000`

Swagger documentation: `http://localhost:5000/swagger`

### 4. Frontend Setup

```bash
cd frontend

# Install dependencies
npm install

# Create .env file
echo "VITE_API_URL=http://localhost:5000/api" > .env

# Run development server
npm run dev
```

The frontend will be available at `http://localhost:5173`

## 🔐 Default Test Credentials

```
Admin Account:
Username: admin
Password: password123

Accountant Account:
Username: accountant1
Password: password123

Auditor Account:
Username: auditor1
Password: password123

Staff Account:
Username: staff1
Password: password123
```

## 📁 Project Structure

```
property-taxation-system/
├── backend/
│   ├── Controllers/          # API endpoints
│   │   ├── AuthController.cs
│   │   ├── UsersController.cs
│   │   ├── PropertiesController.cs
│   │   ├── TaxController.cs
│   │   ├── PaymentsController.cs
│   │   ├── GeographyController.cs
│   │   └── AuditController.cs
│   ├── Models/              # Entity models
│   │   ├── User.cs
│   │   ├── Region.cs
│   │   ├── Property.cs
│   │   ├── TaxAssessment.cs
│   │   ├── Payment.cs
│   │   └── AuditLog.cs
│   ├── DTOs/                # Data Transfer Objects
│   │   ├── AuthDtos.cs
│   │   ├── UserDtos.cs
│   │   ├── PropertyDtos.cs
│   │   ├── TaxDtos.cs
│   │   └── PaymentDtos.cs
│   ├── Services/            # Business logic layer
│   │   ├── AuthService.cs
│   │   ├── UserService.cs
│   │   ├── PropertyService.cs
│   │   ├── TaxService.cs
│   │   ├── PaymentService.cs
│   │   ├── GeographyService.cs
│   │   └── AuditService.cs
│   ├── Data/                # Database context
│   │   └── ApplicationDbContext.cs
│   ├── Program.cs           # Application entry point
│   ├── appsettings.json     # Configuration
│   └── backend.csproj       # Project file
├── frontend/
│   ├── src/
│   │   ├── components/      # Reusable UI components
│   │   ├── pages/           # Page components
│   │   ├── context/         # React context
│   │   ├── services/        # API service layer
│   │   └── App.tsx          # Main app component
│   ├── package.json
│   └── vite.config.ts
├── database/
│   ├── TaxsyncDB.sql        # Complete database schema
│   └── FRONTEND_FLOW_ANALYSIS.md
├── IMPLEMENTATION_GUIDE.md  # Detailed implementation guide
└── README.md                # This file
```

## 🗄️ Database Schema

### Geographic Hierarchy
- **regions** - Davao Region (Region XI)
- **provinces** - 5 provinces (Davao del Sur, del Norte, de Oro, Oriental, Occidental)
- **cities** - 32+ cities and municipalities
- **barangays** - Barangay-level data

### Core Tables
- **users** - User accounts with roles
- **properties** - Property records
- **tax_rates** - Configurable tax rates
- **tax_assessments** - Tax computations
- **payments** - Payment records
- **activity_logs** - Audit trail

## 🔧 Configuration

### Backend Configuration (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TaxsyncDB;User=root;Password=YOUR_PASSWORD;"
  },
  "Jwt": {
    "Key": "your-super-secret-key-min-32-characters-long",
    "Issuer": "TaxSync",
    "Audience": "TaxSyncUsers",
    "ExpirationHours": 24
  }
}
```

### Frontend Configuration (.env)

```env
VITE_API_URL=http://localhost:5000/api
```

## 📊 API Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration
- `POST /api/auth/change-password` - Change password
- `GET /api/auth/me` - Get current user

### Users (Admin only)
- `GET /api/users` - List users
- `GET /api/users/{id}` - Get user details
- `POST /api/users` - Create user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### Properties
- `GET /api/properties` - List properties
- `GET /api/properties/{id}` - Get property details
- `POST /api/properties` - Create property
- `PUT /api/properties/{id}` - Update property
- `DELETE /api/properties/{id}` - Delete property

### Tax Assessments
- `POST /api/tax/compute` - Compute tax
- `GET /api/tax/assessments` - List assessments
- `GET /api/tax/assessments/{id}` - Get assessment details
- `POST /api/tax/assessments` - Create assessment
- `PUT /api/tax/assessments/{id}/approve` - Approve assessment
- `GET /api/tax/rates` - Get tax rates
- `PUT /api/tax/rates` - Update tax rate

### Payments
- `GET /api/payments` - List payments
- `GET /api/payments/{id}` - Get payment details
- `POST /api/payments` - Create payment
- `GET /api/payments/{id}/receipt` - Get payment receipt
- `GET /api/payments/collections/total` - Get total collections

### Geography
- `GET /api/geography/regions` - List regions
- `GET /api/geography/provinces` - List provinces
- `GET /api/geography/cities` - List cities
- `GET /api/geography/barangays` - List barangays

### Audit (Admin/Auditor only)
- `GET /api/audit/logs` - Get audit logs

## 💰 Tax Computation

### Formula
```
Basic Tax = Assessed Value × Tax Rate
SEF Tax = Assessed Value × 0.01 (1%)
Penalty = Total Amount × 0.02 × Months Overdue
Total Tax = Basic Tax + SEF Tax + Penalty - Discounts
```

### Default Tax Rates
- **Residential**: 2.0%
- **Commercial**: 3.0%
- **Industrial**: 3.5%
- **Agricultural**: 1.5%
- **Mixed Use**: 2.5%

### Penalty System
- 2% per month for late payments
- Automatically calculated based on due date
- Updates assessment total amount

## 👥 User Roles & Permissions

### Admin
- Full system access
- User management (CRUD)
- System configuration
- Tax rate management
- All reports

### Accountant
- Payment processing
- Financial reports
- Receipt generation
- Tax assessment approval

### Auditor
- View audit logs
- Compliance reports
- Read-only access to all modules

### Staff
- Property registration
- Tax assessment creation
- Document upload
- Basic reports

## 🔒 Security Features

- **JWT Authentication** - Secure token-based authentication
- **BCrypt Password Hashing** - Industry-standard password security
- **Role-Based Authorization** - Endpoint protection by role
- **SQL Injection Prevention** - Parameterized queries via EF Core
- **CORS Configuration** - Restricted origins
- **Input Validation** - Data annotations and model validation
- **Audit Logging** - Complete activity trail

## 📈 Dashboard Features

- Total collections (current year)
- Pending payments
- Overdue amounts
- Collection rate
- Properties by type
- Properties by area
- Compliance metrics
- Interactive charts (Recharts)

## 🧪 Testing

### Run Backend Tests
```bash
cd backend
dotnet test
```

### Run Frontend Tests
```bash
cd frontend
npm test
```

### API Testing
Use Swagger UI at `http://localhost:5000/swagger` or import the API collection into Postman.

## 📦 Building for Production

### Backend
```bash
cd backend
dotnet publish -c Release -o ./publish
```

### Frontend
```bash
cd frontend
npm run build
# Output will be in the dist/ folder
```

## 🐳 Docker Deployment

Create a `docker-compose.yml` file:

```yaml
version: '3.8'

services:
  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: your_password
      MYSQL_DATABASE: TaxsyncDB
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql
      - ./database/TaxsyncDB.sql:/docker-entrypoint-initdb.d/init.sql

  backend:
    build: ./backend
    ports:
      - "5000:80"
    environment:
      ConnectionStrings__DefaultConnection: "Server=mysql;Database=TaxsyncDB;User=root;Password=your_password;"
    depends_on:
      - mysql

  frontend:
    build: ./frontend
    ports:
      - "3000:80"
    environment:
      VITE_API_URL: "http://localhost:5000/api"
    depends_on:
      - backend

volumes:
  mysql_data:
```

Run with:
```bash
docker-compose up -d
```

## 📝 Development Notes

### Adding New Features
1. Create model in `Models/`
2. Add to `ApplicationDbContext.cs`
3. Create DTOs in `DTOs/`
4. Implement service interface and class in `Services/`
5. Create controller in `Controllers/`
6. Register service in `Program.cs`
7. Update database schema

### Code Style
- Follow C# naming conventions
- Use async/await for all database operations
- Implement proper error handling
- Add XML documentation comments
- Use DTOs for all API responses

## 🐛 Troubleshooting

### Database Connection Issues
- Verify MySQL is running
- Check connection string in `appsettings.json`
- Ensure database exists and schema is imported

### JWT Authentication Issues
- Verify JWT key is at least 32 characters
- Check token expiration
- Ensure Authorization header format: `Bearer {token}`

### CORS Issues
- Verify frontend URL in CORS policy
- Check browser console for CORS errors
- Ensure credentials are included in requests

## 📚 Additional Resources

- [Implementation Guide](IMPLEMENTATION_GUIDE.md) - Detailed technical documentation
- [Frontend Flow Analysis](database/FRONTEND_FLOW_ANALYSIS.md) - Frontend architecture
- [API Documentation](http://localhost:5000/swagger) - Interactive API docs

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## 📄 License

This project is proprietary software for the Davao Region Property Taxation System.

## 📞 Support

For issues or questions:
- Check the [Implementation Guide](IMPLEMENTATION_GUIDE.md)
- Review audit logs for system errors
- Contact system administrator

---

**Version**: 1.0.0  
**Last Updated**: May 3, 2026  
**Status**: Production Ready
