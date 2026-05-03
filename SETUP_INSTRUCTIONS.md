# Setup Instructions
## Property Taxation & Compliance Reporting System

This guide will walk you through setting up the complete system from scratch.

## 📋 Prerequisites Checklist

Before starting, ensure you have:

- [ ] .NET 10.0 SDK installed ([Download](https://dotnet.microsoft.com/download))
- [ ] Node.js 18+ and npm installed ([Download](https://nodejs.org/))
- [ ] MySQL 8.0+ installed and running ([Download](https://dev.mysql.com/downloads/))
- [ ] Git installed ([Download](https://git-scm.com/downloads))
- [ ] A code editor (VS Code, Visual Studio, or similar)

## 🚀 Installation Steps

### Step 1: Clone or Extract the Project

```bash
# If using Git
git clone <repository-url>
cd property-taxation-system

# Or extract the ZIP file and navigate to the folder
```

### Step 2: Database Setup

#### Option A: Using MySQL Command Line

```bash
# Login to MySQL
mysql -u root -p

# Create the database (if not exists)
CREATE DATABASE IF NOT EXISTS TaxsyncDB;

# Exit MySQL
exit

# Import the schema
mysql -u root -p TaxsyncDB < database/TaxsyncDB.sql
```

#### Option B: Using phpMyAdmin

1. Open phpMyAdmin in your browser
2. Click "New" to create a database
3. Name it `TaxsyncDB`
4. Click "Import" tab
5. Choose file: `database/TaxsyncDB.sql`
6. Click "Go" to import

#### Option C: Using MySQL Workbench

1. Open MySQL Workbench
2. Connect to your MySQL server
3. Go to Server → Data Import
4. Select "Import from Self-Contained File"
5. Choose `database/TaxsyncDB.sql`
6. Click "Start Import"

### Step 3: Backend Setup

```bash
# Navigate to backend folder
cd backend

# Restore NuGet packages
dotnet restore

# Update connection string
# Edit backend/appsettings.json
# Change: "Server=localhost;Database=TaxsyncDB;User=root;Password=YOUR_PASSWORD;"
```

**Edit `backend/appsettings.json`:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TaxsyncDB;User=root;Password=YOUR_MYSQL_PASSWORD;"
  },
  "Jwt": {
    "Key": "your-super-secret-key-min-32-characters-long-for-security-purposes",
    "Issuer": "TaxSync",
    "Audience": "TaxSyncUsers"
  }
}
```

**Run the backend:**

```bash
# Still in backend folder
dotnet run
```

You should see:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
      Now listening on: https://localhost:5001
```

**Test the API:**
- Open browser: `http://localhost:5000/swagger`
- You should see the Swagger API documentation

### Step 4: Frontend Setup

Open a **new terminal** (keep backend running):

```bash
# Navigate to frontend folder
cd frontend

# Install dependencies
npm install

# Create environment file
# Windows:
echo VITE_API_URL=http://localhost:5000/api > .env

# Mac/Linux:
echo "VITE_API_URL=http://localhost:5000/api" > .env

# Run development server
npm run dev
```

You should see:
```
  VITE v8.0.1  ready in 500 ms

  ➜  Local:   http://localhost:5173/
  ➜  Network: use --host to expose
```

**Test the frontend:**
- Open browser: `http://localhost:5173`
- You should see the landing page

### Step 5: Test the System

1. **Navigate to Login Page**
   - Click "Login" or go to `http://localhost:5173/login`

2. **Login with Test Account**
   ```
   Username: admin
   Password: password123
   ```

3. **Verify Dashboard Access**
   - You should be redirected to the dashboard
   - You should see statistics and charts

4. **Test API Endpoints**
   - Go to `http://localhost:5000/swagger`
   - Click "Authorize" button
   - Login to get a token
   - Test various endpoints

## 🐳 Docker Setup (Alternative)

If you prefer using Docker:

### Prerequisites
- Docker Desktop installed
- Docker Compose installed

### Steps

```bash
# From project root directory
docker-compose up -d

# Wait for services to start (may take 2-3 minutes)
docker-compose logs -f

# Access the application
# Frontend: http://localhost:3000
# Backend: http://localhost:5000
# Swagger: http://localhost:5000/swagger
```

### Stop Docker Services

```bash
docker-compose down

# To remove volumes (database data)
docker-compose down -v
```

## ✅ Verification Checklist

After setup, verify:

- [ ] Backend is running on port 5000
- [ ] Frontend is running on port 5173 (or 3000 for Docker)
- [ ] Swagger UI is accessible at `/swagger`
- [ ] Can login with test credentials
- [ ] Dashboard displays correctly
- [ ] Can navigate between pages
- [ ] API calls are working (check browser console)

## 🔧 Configuration

### Backend Configuration

**File**: `backend/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=HOST;Database=DB_NAME;User=USER;Password=PASSWORD;"
  },
  "Jwt": {
    "Key": "minimum-32-characters-secret-key",
    "Issuer": "TaxSync",
    "Audience": "TaxSyncUsers",
    "ExpirationHours": 24
  }
}
```

### Frontend Configuration

**File**: `frontend/.env`

```env
VITE_API_URL=http://localhost:5000/api
```

## 🧪 Test Accounts

The system comes with pre-configured test accounts:

| Username | Password | Role | Description |
|----------|----------|------|-------------|
| admin | password123 | Admin | Full system access |
| accountant1 | password123 | Accountant | Financial operations |
| auditor1 | password123 | Auditor | Audit logs access |
| staff1 | password123 | Staff | Property registration |

## 📊 Sample Data

The database includes:

- ✅ 4 test user accounts
- ✅ 1 region (Davao Region)
- ✅ 5 provinces
- ✅ 32+ cities and municipalities
- ✅ Sample barangays for major cities
- ✅ Tax rates for all property types
- ✅ System settings

## 🐛 Troubleshooting

### Backend Issues

**Problem**: `Unable to connect to database`
```bash
# Solution 1: Check MySQL is running
# Windows: Services → MySQL → Start
# Mac: System Preferences → MySQL → Start
# Linux: sudo systemctl start mysql

# Solution 2: Verify connection string
# Check username, password, and database name in appsettings.json

# Solution 3: Test MySQL connection
mysql -u root -p
```

**Problem**: `Port 5000 already in use`
```bash
# Solution: Change port in Properties/launchSettings.json
# Or kill the process using port 5000
# Windows: netstat -ano | findstr :5000
# Mac/Linux: lsof -i :5000
```

**Problem**: `Package restore failed`
```bash
# Solution: Clear NuGet cache
dotnet nuget locals all --clear
dotnet restore
```

### Frontend Issues

**Problem**: `npm install fails`
```bash
# Solution 1: Clear npm cache
npm cache clean --force
npm install

# Solution 2: Delete node_modules and reinstall
rm -rf node_modules package-lock.json
npm install

# Solution 3: Use different registry
npm install --registry=https://registry.npmjs.org/
```

**Problem**: `Port 5173 already in use`
```bash
# Solution: Kill the process
# Windows: netstat -ano | findstr :5173
# Mac/Linux: lsof -i :5173
# Or change port in vite.config.ts
```

**Problem**: `API calls failing (CORS errors)`
```bash
# Solution 1: Verify backend is running
# Check http://localhost:5000/api/health

# Solution 2: Check CORS configuration in backend/Program.cs
# Ensure frontend URL is in AllowedOrigins

# Solution 3: Verify .env file
# Check VITE_API_URL in frontend/.env
```

### Database Issues

**Problem**: `Database import fails`
```bash
# Solution 1: Check MySQL version (must be 8.0+)
mysql --version

# Solution 2: Import with error handling
mysql -u root -p TaxsyncDB < database/TaxsyncDB.sql --force

# Solution 3: Import in sections
# Split the SQL file and import tables separately
```

**Problem**: `Foreign key constraint fails`
```bash
# Solution: Disable foreign key checks temporarily
mysql -u root -p
SET FOREIGN_KEY_CHECKS=0;
SOURCE database/TaxsyncDB.sql;
SET FOREIGN_KEY_CHECKS=1;
```

## 🔒 Security Notes

### For Development

- Default JWT key is provided for testing
- Default passwords are simple for testing
- CORS is open for localhost

### For Production

1. **Change JWT Secret Key**
   - Generate a strong random key (32+ characters)
   - Update in `appsettings.json`

2. **Change Default Passwords**
   - Login as admin
   - Change all default user passwords
   - Or update password hashes in database

3. **Update CORS Policy**
   - Restrict to production domain only
   - Update in `backend/Program.cs`

4. **Enable HTTPS**
   - Configure SSL certificate
   - Update frontend API URL to HTTPS

5. **Secure Database**
   - Use strong MySQL password
   - Restrict remote access
   - Enable MySQL SSL

## 📝 Next Steps

After successful setup:

1. **Explore the System**
   - Login with different roles
   - Navigate through all modules
   - Test CRUD operations

2. **Add Sample Data**
   - Register properties
   - Create tax assessments
   - Process payments

3. **Generate Reports**
   - Collection reports
   - Delinquency reports
   - Compliance reports

4. **Review Audit Logs**
   - Login as auditor
   - View activity logs
   - Filter by module/user

5. **Customize Settings**
   - Update tax rates
   - Configure system settings
   - Add more barangays

## 📚 Additional Resources

- [README.md](README.md) - Project overview
- [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md) - Technical details
- [Swagger UI](http://localhost:5000/swagger) - API documentation
- [Frontend Flow Analysis](database/FRONTEND_FLOW_ANALYSIS.md) - Frontend architecture

## 🆘 Getting Help

If you encounter issues:

1. Check this troubleshooting guide
2. Review error messages in:
   - Backend console
   - Frontend console (F12 in browser)
   - MySQL error log
3. Check audit logs in the system
4. Review API responses in Swagger
5. Contact system administrator

## ✨ Success!

If you can:
- ✅ Login to the system
- ✅ See the dashboard
- ✅ Navigate between pages
- ✅ API calls are working

**Congratulations! Your system is ready to use!** 🎉

---

**Need Help?** Check the troubleshooting section or contact support.

**Version**: 1.0.0  
**Last Updated**: May 3, 2026
