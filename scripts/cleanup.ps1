# Cleanup Script for GitHub Actions Runner
# This script removes all Azure resources created for the GitHub runner

param(
    [Parameter(Mandatory=$false)]
    [string]$EnvironmentName = "dev",
    
    [Parameter(Mandatory=$false)]
    [switch]$Force
)

$ErrorActionPreference = "Stop"

Write-Host "🗑️ GitHub Actions Runner Cleanup" -ForegroundColor Red
Write-Host "================================" -ForegroundColor Red

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

$resourceGroup = "rg-github-runner-$EnvironmentName"

# Check if resource group exists
try {
    $rgInfo = az group show --name $resourceGroup | ConvertFrom-Json
    Write-Host "📁 Found resource group: $resourceGroup" -ForegroundColor Yellow
    Write-Host "   Location: $($rgInfo.location)" -ForegroundColor White
    Write-Host "   Tags: $($rgInfo.tags | ConvertTo-Json -Compress)" -ForegroundColor White
} catch {
    Write-Host "❌ Resource group '$resourceGroup' not found." -ForegroundColor Red
    exit 1
}

# List resources that will be deleted
Write-Host "`n📋 Resources to be deleted:" -ForegroundColor Yellow
try {
    $resources = az resource list --resource-group $resourceGroup | ConvertFrom-Json
    foreach ($resource in $resources) {
        Write-Host "   - $($resource.type): $($resource.name)" -ForegroundColor White
    }
    
    if ($resources.Count -eq 0) {
        Write-Host "   No resources found in the resource group." -ForegroundColor White
    }
} catch {
    Write-Host "   Could not list resources." -ForegroundColor Red
}

# Confirm deletion
if (-not $Force) {
    Write-Host "`n⚠️  WARNING: This will permanently delete all resources in the resource group!" -ForegroundColor Red
    Write-Host "   This action cannot be undone." -ForegroundColor Red
    $confirm = Read-Host "`n🔍 Are you sure you want to continue? Type 'DELETE' to confirm"
    
    if ($confirm -ne 'DELETE') {
        Write-Host "❌ Cleanup cancelled." -ForegroundColor Yellow
        exit 0
    }
}

try {
    Write-Host "`n🗑️ Deleting resource group and all resources..." -ForegroundColor Red
    Write-Host "   This may take several minutes..." -ForegroundColor Yellow
    
    az group delete --name $resourceGroup --yes --no-wait
    
    Write-Host "`n✅ Deletion initiated successfully!" -ForegroundColor Green
    Write-Host "   The resource group '$resourceGroup' is being deleted in the background." -ForegroundColor White
    Write-Host "   You can monitor progress in the Azure Portal." -ForegroundColor White
    
    Write-Host "`n🔍 To check deletion status:" -ForegroundColor Cyan
    Write-Host "   az group show --name $resourceGroup" -ForegroundColor White
    Write-Host "   (This command will fail when deletion is complete)" -ForegroundColor White
    
} catch {
    Write-Host "`n❌ Cleanup failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "You may need to manually delete resources in the Azure Portal." -ForegroundColor Red
    exit 1
}
