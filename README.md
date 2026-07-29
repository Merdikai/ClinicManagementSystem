# Clinic Management System

## 🔧 Setup Instructions

### 1. Database Configuration

1. Copy `appsettings.template.json` to `appsettings.json`
2. Copy `appsettings.Development.template.json` to `appsettings.Development.json`
3. Update the connection string with your PostgreSQL credentials:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=ClinicManagementDb;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
   }