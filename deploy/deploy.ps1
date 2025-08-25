# AccessOTP Deployment Script
# This script deploys the AccessOTP solution to Azure

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$Location,
    
    [Parameter(Mandatory=$true)]
    [string]$AppServicePlanName,
    
    [Parameter(Mandatory=$true)]
    [string]$WebAppName,
    
    [Parameter(Mandatory=$true)]
    [string]$DatabaseServerName,
    
    [Parameter(Mandatory=$true)]
    [string]$DatabaseName,
    
    [Parameter(Mandatory=$true)]
    [string]$StorageAccountName,
    
    [Parameter(Mandatory=$false)]
    [string]$Environment = "Production"
)

Write-Host "🚀 Starting AccessOTP deployment..." -ForegroundColor Green

# Check if Azure CLI is installed
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI is not installed. Please install it from https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
}

# Check if user is logged in
$account = az account show 2>$null
if (-not $account) {
    Write-Host "Please log in to Azure..." -ForegroundColor Yellow
    az login
}

# Create Resource Group
Write-Host "📦 Creating Resource Group: $ResourceGroupName" -ForegroundColor Blue
az group create --name $ResourceGroupName --location $Location

# Create App Service Plan
Write-Host "📋 Creating App Service Plan: $AppServicePlanName" -ForegroundColor Blue
az appservice plan create `
    --name $AppServicePlanName `
    --resource-group $ResourceGroupName `
    --location $Location `
    --sku B1 `
    --is-linux

# Create Web App
Write-Host "🌐 Creating Web App: $WebAppName" -ForegroundColor Blue
az webapp create `
    --name $WebAppName `
    --resource-group $ResourceGroupName `
    --plan $AppServicePlanName `
    --runtime "DOTNETCORE:9.0"

# Create SQL Database Server
Write-Host "🗄️ Creating SQL Database Server: $DatabaseServerName" -ForegroundColor Blue
az sql server create `
    --name $DatabaseServerName `
    --resource-group $ResourceGroupName `
    --location $Location `
    --admin-user "sqladmin" `
    --admin-password (Read-Host "Enter SQL Server admin password" -AsSecureString | ConvertFrom-SecureString -AsPlainText)

# Create SQL Database
Write-Host "📊 Creating SQL Database: $DatabaseName" -ForegroundColor Blue
az sql db create `
    --name $DatabaseName `
    --resource-group $ResourceGroupName `
    --server $DatabaseServerName `
    --service-objective Basic

# Create Storage Account
Write-Host "💾 Creating Storage Account: $StorageAccountName" -ForegroundColor Blue
az storage account create `
    --name $StorageAccountName `
    --resource-group $ResourceGroupName `
    --location $Location `
    --sku Standard_LRS

# Get Storage Account Key
$storageKey = az storage account keys list `
    --resource-group $ResourceGroupName `
    --account-name $StorageAccountName `
    --query "[0].value" `
    --output tsv

# Configure Web App Settings
Write-Host "⚙️ Configuring Web App Settings..." -ForegroundColor Blue
az webapp config appsettings set `
    --resource-group $ResourceGroupName `
    --name $WebAppName `
    --settings `
        "ConnectionStrings__DefaultConnection"="Server=tcp:$DatabaseServerName.database.windows.net,1433;Database=$DatabaseName;User ID=sqladmin;Password=$(Read-Host "Enter SQL Server admin password" -AsSecureString | ConvertFrom-SecureString -AsPlainText);Encrypt=true;Connection Timeout=30;" `
        "AzureAdB2C__Instance"="https://yourtenant.b2clogin.com" `
        "AzureAdB2C__Domain"="yourtenant.onmicrosoft.com" `
        "AzureAdB2C__ClientId"="your-client-id" `
        "AzureAdB2C__SignUpSignInPolicyId"="B2C_1_signupsignin" `
        "Notification__SMS__Provider"="Twilio" `
        "Notification__SMS__AccountSid"="your-twilio-account-sid" `
        "Notification__SMS__AuthToken"="your-twilio-auth-token" `
        "Notification__SMS__FromNumber"="+1234567890" `
        "Notification__Email__Provider"="SendGrid" `
        "Notification__Email__ApiKey"="your-sendgrid-api-key" `
        "Notification__Email__FromEmail"="noreply@yourapp.com" `
        "Notification__Email__FromName"="AccessOTP" `
        "ASPNETCORE_ENVIRONMENT"=$Environment

# Enable HTTPS
Write-Host "🔒 Enabling HTTPS..." -ForegroundColor Blue
az webapp update `
    --resource-group $ResourceGroupName `
    --name $WebAppName `
    --https-only true

# Configure CORS
Write-Host "🌍 Configuring CORS..." -ForegroundColor Blue
az webapp cors add `
    --resource-group $ResourceGroupName `
    --name $WebAppName `
    --allowed-origins "https://localhost:3000" "https://yourdomain.com"

# Deploy from GitHub (if specified)
$githubRepo = Read-Host "Enter GitHub repository URL (or press Enter to skip)"
if ($githubRepo) {
    Write-Host "📥 Deploying from GitHub: $githubRepo" -ForegroundColor Blue
    az webapp deployment source config `
        --resource-group $ResourceGroupName `
        --name $WebAppName `
        --repo-url $githubRepo `
        --branch main `
        --manual-integration
}

# Get Web App URL
$webAppUrl = az webapp show `
    --resource-group $ResourceGroupName `
    --name $WebAppName `
    --query "defaultHostName" `
    --output tsv

Write-Host "✅ Deployment completed successfully!" -ForegroundColor Green
Write-Host "🌐 Web App URL: https://$webAppUrl" -ForegroundColor Cyan
Write-Host "📊 Database Server: $DatabaseServerName.database.windows.net" -ForegroundColor Cyan
Write-Host "💾 Storage Account: $StorageAccountName" -ForegroundColor Cyan

# Output connection strings and configuration
Write-Host "`n📋 Configuration Summary:" -ForegroundColor Yellow
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor White
Write-Host "Location: $Location" -ForegroundColor White
Write-Host "Web App: $WebAppName" -ForegroundColor White
Write-Host "Database: $DatabaseName" -ForegroundColor White

Write-Host "`n🔧 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Update appsettings.json with your Azure B2C configuration" -ForegroundColor White
Write-Host "2. Configure Okta federation in Azure B2C" -ForegroundColor White
Write-Host "3. Set up Twilio and SendGrid credentials" -ForegroundColor White
Write-Host "4. Deploy your application code" -ForegroundColor White
Write-Host "5. Test the OTP authentication flow" -ForegroundColor White

