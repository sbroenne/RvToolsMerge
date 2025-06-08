# GitHub Actions Runner Deployment Script (PowerShell)
# This script helps deploy the GitHub self-hosted runner to Azure

param(
    [string]$EnvironmentName = "dev",
    [string]$Location = "eastus",
    [string]$AdminUsername = "azureuser",
    [string]$AdminPassword,
    [string]$GitHubToken,
    [string]$GitHubRepositoryUrl,
    [string]$RunnerName = "azure-windows-runner",
    [ValidateSet("Standard_B2ms", "Standard_B4ms", "Standard_D2s_v3", "Standard_D4s_v3", "Standard_E2s_v3")]
    [string]$VmSize = "Standard_B2ms",
    [ValidateSet("win11-23h2-pro", "win10-22h2-pro", "win11-23h2-ent", "win10-22h2-ent")]
    [string]$WindowsVersion = "win11-23h2-pro"
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 GitHub Actions Runner Deployment" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan

# Check if Azure CLI is installed
try {
    az --version | Out-Null
} catch {
    Write-Host "❌ Azure CLI is not installed. Please install it from https://aka.ms/InstallAzureCLI" -ForegroundColor Red
    exit 1
}

# Check if user is logged in
try {
    az account show | Out-Null
} catch {
    Write-Host "🔐 Please log in to Azure..." -ForegroundColor Yellow
    az login
}

# Get current subscription
$subscriptionInfo = az account show | ConvertFrom-Json
Write-Host "📋 Using subscription: $($subscriptionInfo.name) ($($subscriptionInfo.id))" -ForegroundColor Green

# Interactive parameter collection if not provided
if (-not $AdminPassword) {
    $AdminPassword = Read-Host "🔒 Enter VM admin password (min 12 chars)" -AsSecureString
    $AdminPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($AdminPassword))
}

if (-not $GitHubToken) {
    $GitHubTokenSecure = Read-Host "🔑 Enter GitHub Personal Access Token" -AsSecureString
    $GitHubToken = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($GitHubTokenSecure))
}

if (-not $GitHubRepositoryUrl) {
    $GitHubRepositoryUrl = Read-Host "📦 Enter GitHub repository URL (e.g., https://github.com/user/repo)"
}

# Show configuration
Write-Host "`n📋 Deployment Configuration:" -ForegroundColor Cyan
Write-Host "   Environment: $EnvironmentName" -ForegroundColor White
Write-Host "   Location: $Location" -ForegroundColor White
Write-Host "   VM Size: $VmSize" -ForegroundColor White
Write-Host "   Windows Version: $WindowsVersion" -ForegroundColor White
Write-Host "   Admin User: $AdminUsername" -ForegroundColor White
Write-Host "   Runner Name: $RunnerName" -ForegroundColor White
Write-Host "   Repository: $GitHubRepositoryUrl" -ForegroundColor White

$confirm = Read-Host "`n🔍 Proceed with deployment? (y/N)"
if ($confirm -ne 'y' -and $confirm -ne 'Y') {
    Write-Host "❌ Deployment cancelled." -ForegroundColor Yellow
    exit 0
}

try {
    # Create resource group
    $resourceGroup = "rg-github-runner-$EnvironmentName"
    Write-Host "📁 Creating resource group: $resourceGroup" -ForegroundColor Yellow
    az group create --name $resourceGroup --location $Location | Out-Null
    
    # Check quota availability
    Write-Host "📊 Checking quota availability..." -ForegroundColor Yellow
    $quota = az vm list-usage --location $Location --query "[?localName=='Total Regional vCPUs'].{current:currentValue,limit:limit}" | ConvertFrom-Json
    if ($quota) {
        Write-Host "✅ Quota check completed" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Could not verify quota. Proceeding with deployment..." -ForegroundColor Yellow
    }
      # Deploy infrastructure
    Write-Host "🔨 Deploying infrastructure... This may take 5-10 minutes." -ForegroundColor Yellow
    $deployment = az deployment group create `
        --resource-group $resourceGroup `
        --template-file "infra/main.bicep" `
        --parameters `
            environmentName=$EnvironmentName `
            location=$Location `
            adminUsername=$AdminUsername `
            adminPassword=$AdminPassword `
            githubToken=$GitHubToken `
            githubRepositoryUrl=$GitHubRepositoryUrl `
            runnerName=$RunnerName `
            vmSize=$VmSize `
            windowsVersion=$WindowsVersion | ConvertFrom-Json
    
    # Get deployment outputs
    Write-Host "📤 Getting deployment outputs..." -ForegroundColor Yellow
    $outputs = $deployment.properties.outputs
    $publicIP = $outputs.publicIPAddress.value
    $dnsName = $outputs.dnsName.value
    $vmName = $outputs.virtualMachineName.value
    
    Write-Host "`n🎉 Deployment completed successfully!" -ForegroundColor Green
    Write-Host "==================================" -ForegroundColor Green
    Write-Host "🖥️  VM Name: $vmName" -ForegroundColor White
    Write-Host "🌐 Public IP: $publicIP" -ForegroundColor White
    Write-Host "🔗 DNS Name: $dnsName" -ForegroundColor White
    Write-Host "👤 Username: $AdminUsername" -ForegroundColor White
    Write-Host "🏃 Runner Name: $RunnerName" -ForegroundColor White
    
    Write-Host "`n📝 Next steps:" -ForegroundColor Cyan
    Write-Host "   1. Wait 5-10 minutes for the runner installation to complete" -ForegroundColor White
    Write-Host "   2. Check your GitHub repository's Settings > Actions > Runners" -ForegroundColor White
    Write-Host "   3. You should see '$RunnerName' listed as an active runner" -ForegroundColor White
    Write-Host "   4. You can RDP to the VM using: $dnsName`:3389" -ForegroundColor White
    
    Write-Host "`n🔍 To check installation progress:" -ForegroundColor Cyan
    Write-Host "   az vm run-command invoke --resource-group $resourceGroup --name $vmName --command-id RunPowerShellScript --scripts `"Get-Content C:\runner-install.log`"" -ForegroundColor White
      $monthlyCost = switch ($VmSize) {
        "Standard_B2ms" { "~$30" }
        "Standard_B4ms" { "~$60" }
        "Standard_D2s_v3" { "~$70" }
        "Standard_D4s_v3" { "~$140" }
        "Standard_E2s_v3" { "~$120" }
        default { "~$30-140" }
    }
    Write-Host "`n💰 Estimated monthly cost: $monthlyCost USD (not including storage and networking)" -ForegroundColor Green
    
} catch {
    Write-Host "`n❌ Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Check the error details above and try again." -ForegroundColor Red
    exit 1
}
