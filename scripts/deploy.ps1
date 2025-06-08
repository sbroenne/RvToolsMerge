#!/usr/bin/env pwsh
# GitHub Actions Runner Deployment Script (PowerShell 7)
# This script helps deploy the GitHub self-hosted runner to Azure
#
# Requirements: PowerShell 7.0 or later (cross-platform)
# Install: https://aka.ms/powershell
#
# Parameters:
# - AdminPassword: SecureString containing the VM administrator password (minimum 12 characters)
# - GitHubToken: SecureString containing the GitHub Personal Access Token with repo and admin:repo_hook permissions
#
# Example usage:
# $securePassword = ConvertTo-SecureString "YourPassword123!" -AsPlainText -Force
# $secureToken = ConvertTo-SecureString "ghp_yourtokenhere" -AsPlainText -Force
# .\deploy.ps1 -AdminPassword $securePassword -GitHubToken $secureToken

param(
    [string]$EnvironmentName = "dev",
    [string]$Location = "eastus",
    [string]$AdminUsername = "azureuser",
    [SecureString]$AdminPassword,
    [SecureString]$GitHubToken,
    [string]$GitHubRepositoryUrl,
    [string]$RunnerName = "azure-windows-runner",
    [string]$VmSize = "Standard_B2as_v2",
    [string]$WindowsVersion = "win11-23h2-pro",
    [bool]$EnableAHUB = $true
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 GitHub Actions Runner Deployment" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan

# Check if Azure CLI is installed
try {
    $null = az --version
}
catch {
    Write-Host "❌ Azure CLI is not installed. Please install it from https://aka.ms/InstallAzureCLI" -ForegroundColor Red
    exit 1
}

# Check if user is logged in
try {
    $null = az account show
}
catch {
    Write-Host "🔐 Please log in to Azure..." -ForegroundColor Yellow
    az login
}

# Get current subscription
$subscriptionInfo = az account show | ConvertFrom-Json
Write-Host "📋 Using subscription: $($subscriptionInfo.name) ($($subscriptionInfo.id))" -ForegroundColor Green

# Interactive parameter collection if not provided
if (-not $AdminPassword) {
    $AdminPassword = Read-Host "🔒 Enter VM admin password (min 12 chars)" -AsSecureString
}

if (-not $GitHubToken) {
    $GitHubToken = Read-Host "🔑 Enter GitHub Personal Access Token" -AsSecureString
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
Write-Host "   Azure Hybrid Use Benefit: $EnableAHUB" -ForegroundColor White

$confirm = Read-Host "`n🔍 Proceed with deployment? (y/N)"
if ($confirm -ne 'y' -and $confirm -ne 'Y') {
    Write-Host "❌ Deployment cancelled." -ForegroundColor Yellow
    exit 0
}

try {
    # Create resource group
    $resourceGroup = "rg-github-runner-$EnvironmentName"
    Write-Host "📁 Creating resource group: $resourceGroup" -ForegroundColor Yellow
    $null = az group create --name $resourceGroup --location $Location

    # Check quota availability
    Write-Host "📊 Checking quota availability..." -ForegroundColor Yellow
    $quota = az vm list-usage --location $Location --query "[?localName=='Total Regional vCPUs'].{current:currentValue,limit:limit}" | ConvertFrom-Json
    if ($quota) {
        Write-Host "✅ Quota check completed" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️  Could not verify quota. Proceeding with deployment..." -ForegroundColor Yellow
    }

    # Convert SecureString parameters to plain text for Azure CLI
    $adminPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($AdminPassword))
    $githubTokenPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($GitHubToken))

    # Deploy infrastructure
    Write-Host "🔨 Deploying infrastructure... This may take 5-10 minutes." -ForegroundColor Yellow
    $deploymentResult = az deployment group create `
        --resource-group $resourceGroup `
        --template-file "infra/main.bicep" `
        --parameters `
        environmentName=$EnvironmentName `
        location=$Location `
        adminUsername=$AdminUsername `
        adminPassword=$adminPasswordPlain `
        githubToken=$githubTokenPlain `
        githubRepositoryUrl=$GitHubRepositoryUrl `
        runnerName=$RunnerName `
        vmSize=$VmSize `
        windowsVersion=$WindowsVersion `
        enableAHUB=$EnableAHUB

    if (-not $deploymentResult) {
        throw "Deployment command failed - no result returned"
    }

    # Parse deployment result with error handling
    try {
        $deployment = $deploymentResult | ConvertFrom-Json
    }
    catch {
        Write-Host "❌ Failed to parse deployment result as JSON" -ForegroundColor Red
        Write-Host "Raw result: $deploymentResult" -ForegroundColor Yellow
        throw "Deployment result parsing failed: $($_.Exception.Message)"
    }

    # Clear sensitive plain text variables immediately after use
    $adminPasswordPlain = $null
    $githubTokenPlain = $null

    # Validate deployment properties exist
    if (-not $deployment.properties) {
        throw "Deployment result missing 'properties' section"
    }

    if (-not $deployment.properties.outputs) {
        throw "Deployment result missing 'outputs' section"
    }

    # Get deployment outputs with validation
    Write-Host "📤 Getting deployment outputs..." -ForegroundColor Yellow
    $outputs = $deployment.properties.outputs

    # Validate required outputs exist
    $requiredOutputs = @('publicIPAddress', 'dnsName', 'virtualMachineName')
    foreach ($outputName in $requiredOutputs) {
        if (-not $outputs.$outputName) {
            throw "Required output '$outputName' is missing from deployment result"
        }
        if (-not $outputs.$outputName.value) {
            throw "Required output '$outputName' has no value in deployment result"
        }
    }

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
    Write-Host "   2. Check your GitHub repository's Settings → Actions → Runners" -ForegroundColor White
    Write-Host "   3. You should see '$RunnerName' listed as an active runner" -ForegroundColor White
    Write-Host "   4. You can RDP to the VM using: ${dnsName}:3389" -ForegroundColor White

    Write-Host "`n🔍 To check installation progress:" -ForegroundColor Cyan
    Write-Host "   az vm run-command invoke --resource-group $resourceGroup --name $vmName --command-id RunPowerShellScript --scripts `"Get-Content C:\runner-install.log`"" -ForegroundColor White

    # ARM-based VM cost estimate
    $monthlyCost = "~$23"
    Write-Host "`n💰 Estimated monthly cost: $monthlyCost USD (ARM-based efficiency + Standard SSD, not including networking)" -ForegroundColor Green

}
catch {
    Write-Host "`n❌ Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Check the error details above and try again." -ForegroundColor Red
    exit 1
}
