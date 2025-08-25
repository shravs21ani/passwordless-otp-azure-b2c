# AccessOTP - Passwordless Authentication Solution

## 🚀 Overview

**AccessOTP** is a comprehensive, enterprise-grade passwordless authentication solution that implements OTP-based login with Azure AD B2C and Okta integration. Built with .NET Core 9 and React, it provides a secure, scalable, and user-friendly authentication framework.

## ✨ Key Features

- **🔐 Passwordless Authentication** - No passwords to remember or compromise
- **📱 Multi-Delivery OTP** - SMS and Email OTP delivery
- **⏱️ Smart Retry Logic** - Progressive retry intervals (30s → 1m → 1.5m)
- **🏢 Enterprise Integration** - Azure AD B2C and Okta federation
- **🔒 Security First** - JWT tokens, rate limiting, and account protection
- **📊 Scalable Architecture** - Microservices-ready with clean separation
- **🎨 Modern UI/UX** - Beautiful React frontend with Tailwind CSS

## 🏗️ Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   React App     │    │  .NET Core API  │    │  Azure B2C     │
│   (Frontend)    │◄──►│   (Backend)     │◄──►│  + Okta        │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                              │
                              ▼
                       ┌─────────────────┐
                       │   SQL Server    │
                       │   Database      │
                       └─────────────────┘
```

## 🛠️ Technology Stack

### Backend
- **.NET Core 9** - Latest LTS version
- **Entity Framework Core** - Data access layer
- **SQL Server** - Relational database
- **JWT Authentication** - Secure token-based auth
- **FluentValidation** - Input validation
- **AutoMapper** - Object mapping

### Frontend
- **React 18** - Modern UI framework
- **Tailwind CSS** - Utility-first CSS framework
- **React Router** - Client-side routing
- **React Hook Form** - Form management
- **Axios** - HTTP client
- **Lucide React** - Icon library

### Infrastructure
- **Azure App Service** - Hosting platform
- **Azure SQL Database** - Managed database
- **Azure B2C** - Identity provider
- **Okta** - Federation partner
- **Twilio** - SMS delivery
- **SendGrid** - Email delivery

## Prerequisites

- **.NET Core 9 SDK**
- **Node.js 18+** and npm
- **SQL Server** (LocalDB for development)
- **Azure CLI** (for deployment)
- **Azure Subscription** (for production)
- **Twilio Account** (for SMS)
- **SendGrid Account** (for email)

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/shravs21ani/passwordless-otp-azure-b2c.git
cd passwordless-otp-azure-b2c
```

### 2. Backend Setup

```bash
cd backend/PasswordlessOTP.API

# Restore packages
dotnet restore

# Update appsettings.json with your configuration
# See Configuration section below

# Run the application
dotnet run
```

### 3. Frontend Setup

```bash
cd frontend

# Install dependencies
npm install

# Start development server
npm start
```

### 4. Database Setup

```bash
# The database will be created automatically on first run
# Or use Entity Framework migrations:
dotnet ef database update
```

## Configuration

### Backend Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PasswordlessOTPDB;Trusted_Connection=true"
  },
  "AzureAdB2C": {
    "Instance": "https://yourtenant.b2clogin.com",
    "Domain": "yourtenant.onmicrosoft.com",
    "ClientId": "your-client-id",
    "SignUpSignInPolicyId": "B2C_1_signupsignin"
  },
  "Okta": {
    "Domain": "https://your-okta-domain.okta.com",
    "ClientId": "your-okta-client-id",
    "ClientSecret": "your-okta-client-secret"
  },
  "Notification": {
    "SMS": {
      "Provider": "Twilio",
      "AccountSid": "your-twilio-account-sid",
      "AuthToken": "your-twilio-auth-token",
      "FromNumber": "+1234567890"
    },
    "Email": {
      "Provider": "SendGrid",
      "ApiKey": "your-sendgrid-api-key",
      "FromEmail": "noreply@yourapp.com"
    }
  }
}
```

### Frontend Configuration

Create `.env` file in the frontend directory:

```env
REACT_APP_API_URL=https://localhost:7001/api
REACT_APP_AZURE_B2C_TENANT=yourtenant.onmicrosoft.com
REACT_APP_AZURE_B2C_CLIENT_ID=your-client-id
```

## Deployment

### Azure Deployment

Use the provided PowerShell script:

```powershell
.\deploy\deploy.ps1 `
  -ResourceGroupName "accessotp-rg" `
  -Location "East US" `
  -AppServicePlanName "accessotp-plan" `
  -WebAppName "accessotp-api" `
  -DatabaseServerName "accessotp-sql" `
  -DatabaseName "PasswordlessOTPDB" `
  -StorageAccountName "accessotpstorage" `
  -Environment "Production"
```

### Manual Deployment

1. **Create Azure Resources**
   - App Service Plan
   - Web App
   - SQL Database
   - Storage Account

2. **Configure App Settings**
   - Connection strings
   - Azure B2C settings
   - Notification service credentials

3. **Deploy Application**
   - Use Azure DevOps
   - GitHub Actions
   - Manual deployment

## Azure B2C Setup

### 1. Create B2C Tenant

```bash
az ad b2c tenant create \
  --location "United States" \
  --name "yourtenant" \
  --resource-group "your-rg"
```

### 2. Configure Custom Policies

Upload the provided custom policies:
- `TrustFrameworkBase.xml`
- `TrustFrameworkExtensions.xml`
- `SignUpOrSignIn.xml`

### 3. Configure Okta Federation

1. Create SAML application in Okta
2. Configure Azure B2C as Identity Provider
3. Set up claim mapping

## OTP Flow

### 1. User Login
```
User enters email/phone → Selects delivery method → Clicks "Send OTP"
```

### 2. OTP Generation
```
API generates 6-digit code → Sends via SMS/Email → Stores in database
```

### 3. OTP Validation
```
User enters code → API validates → Issues JWT token → Creates session
```

### 4. Retry Logic
```
30s → 1m → 1.5m intervals → Maximum 3 retries → Account blocking
```

## Testing

### API Testing

```bash
# Health check
curl https://localhost:7001/api/otp/health

# Generate OTP
curl -X POST https://localhost:7001/api/otp/generate \
  -H "Content-Type: application/json" \
  -d '{"identifier":"test@example.com","deliveryMethod":"email"}'

# Validate OTP
curl -X POST https://localhost:7001/api/otp/validate \
  -H "Content-Type: application/json" \
  -d '{"identifier":"test@example.com","otpCode":"123456"}'
```

### Frontend Testing

```bash
cd frontend
npm test
```

## Security Features

- **Rate Limiting** - Prevents brute force attacks
- **Account Blocking** - Temporary blocking after failed attempts
- **Secure OTP Generation** - Cryptographically secure random numbers
- **JWT Token Security** - Short-lived tokens with refresh mechanism
- **HTTPS Enforcement** - All communications encrypted
- **CORS Protection** - Controlled cross-origin access

## Monitoring & Logging

- **Structured Logging** - Serilog integration
- **Performance Metrics** - Response time tracking
- **Error Tracking** - Exception logging and monitoring
- **Health Checks** - Service availability monitoring

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **Documentation**: [Wiki](https://github.com/shravs21ani/passwordless-otp-azure-b2c/wiki)
- **Issues**: [GitHub Issues](https://github.com/shravs21ani/passwordless-otp-azure-b2c/issues)
- **Discussions**: [GitHub Discussions](https://github.com/shravs21ani/passwordless-otp-azure-b2c/discussions)

## Acknowledgments

- **Azure B2C** - Identity platform
- **Okta** - Federation partner
- **Twilio** - SMS delivery
- **SendGrid** - Email delivery
- **.NET Community** - Framework and tools
- **React Community** - Frontend ecosystem

---

**Built with LOVE for secure, passwordless authentication**
