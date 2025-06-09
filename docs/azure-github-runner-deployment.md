# Azure GitHub Runner Deployment Guide

This guide provides comprehensive instructions for deploying a Windows VM on Azure and manually configuring it as a self-hosted GitHub Actions runner for code signing and artifact security purposes.

## Overview

The deployment creates a secure Windows environment specifically designed for code signing workflows:

-   **Windows VM with GUI**: Full Windows desktop environment (Windows 11 Pro) for code signing tools
-   **Standard Storage**: 128GB Standard SSD for secure certificate storage and build artifacts
-   **Network Security**: Configured with appropriate security groups for RDP and outbound access
-   **Manual Setup**: GitHub Runner must be configured manually after VM deployment

## Prerequisites

### Required Software

-   [Azure CLI](https://aka.ms/InstallAzureCLI) installed and configured
-   Active Azure subscription with appropriate permissions

### Required Permissions

-   **Contributor** role on the Azure subscription or resource group
-   **Virtual Machine Contributor** role (minimum)
-   **Network Contributor** role (for networking resources)

### GitHub Requirements (for manual setup)

**Repository Admin Access Required**

-   Repository admin access to generate registration tokens through the GitHub web interface
-   Navigate to **Settings** → **Actions** → **Runners** → **New self-hosted runner**
-   Copy the provided registration token (valid for 1 hour)

**Note**: No Personal Access Token (PAT) is required. The registration token generated through the GitHub web interface is sufficient for runner setup and expires after 1 hour for security.

## VM Configuration

**Fixed VM Size**: The infrastructure uses `Standard_B2as_v2` (**AMD-based**) optimized for code signing workflows:

-   **Specifications**: 2 vCPU, 8 GB RAM, AMD-based architecture
-   **Performance**: Sufficient for code signing operations and artifact processing
-   **Efficiency**: AMD-based processors provide excellent price-performance ratio for signing workloads
-   **Security**: Isolated environment for certificate management and signing operations

## Cost Optimization

### Azure Hybrid Use Benefit (AHUB)

**Disabled by Default**: The deployment has Azure Hybrid Use Benefit disabled by default. You can enable it if you have eligible Windows licenses to achieve significant savings on compute costs.

**Requirements for AHUB**:

-   Valid Windows 10/11 Pro or Enterprise licenses with active Software Assurance
-   Compliance with Microsoft licensing terms
-   Only available for Windows client VMs (not Windows Server)

**To enable AHUB**: Set `enableAHUB` parameter to `true` in the deployment.

## Windows Version Support

**Fixed Configuration**: The infrastructure is configured to use Windows 11 Professional (23H2) exclusively for optimal code signing compatibility and Azure Hybrid Use Benefit support.

-   **Consistent Platform**: All deployments use the same Windows version for predictable signing behavior
-   **AHUB Optimization**: Windows 11 Pro provides the best cost optimization with Azure Hybrid Use Benefit
-   **Signing Tools Compatibility**: Full compatibility with Windows SDK, signtool.exe, and modern signing requirements
-   **Security Features**: Latest Windows security features for certificate protection

## Deployment Method

### Azure CLI with Bicep (cmd)

```cmd
rem Create resource group (specify your preferred Azure region)
az group create --name rg-github-runner-dev --location swedencentral

rem Deploy infrastructure - basic deployment
az deployment group create ^
  --resource-group rg-github-runner-dev ^
  --template-file infra\main.bicep ^
  --parameters adminPassword="YourSecurePassword123!" ^
  --parameters environmentName="dev"

rem Deploy with Azure Hybrid Use Benefit enabled (if you have eligible Windows licenses)
az deployment group create ^
  --resource-group rg-github-runner-dev ^
  --template-file infra\main.bicep ^
  --parameters adminPassword="YourSecurePassword123!" ^
  --parameters environmentName="dev" ^
  --parameters enableAHUB=true
```

**Note**: The deployment only creates the VM infrastructure. GitHub runner setup must be performed manually.

## Parameter Validation and Requirements

### Required Parameters

The deployment scripts and Bicep template enforce the following validation rules:

**adminPassword**:

-   Minimum 12 characters
-   Must meet Azure VM password complexity requirements

**environmentName**:

-   Minimum 3 characters, maximum 10 characters
-   Used as suffix for Azure resource naming
-   Helps organize multiple deployments

### Optional Parameters with Defaults

**vmSize**: `Standard_B2as_v2` (AMD-based for cost efficiency)
**windowsVersion**: `win11-23h2-pro` (fixed for AHUB compatibility)
**enableAHUB**: `false` (disabled by default - set to true if you have eligible Windows licenses)
**location**: Uses resource group location or specified region
**adminUsername**: `azureuser` (default administrator account)

## Post-Deployment Configuration

### 1. Access the Virtual Machine

**RDP Connection**:

-   Use the DNS name provided in deployment output
-   Port: 3389
-   Username: The admin username you specified
-   Password: The admin password you specified

**Example RDP connection**:

```
Computer: github-runner-[random-string].swedencentral.cloudapp.azure.com:3389
Username: azureuser
Password: [your-password]
```

### 2. Install Development Tools

After connecting to the VM via RDP, install the required development tools:

```powershell
# Install Git
winget install --id Git.Git --silent --accept-package-agreements --accept-source-agreements

# Install .NET 9 SDK
winget install --id Microsoft.DotNet.SDK.9 --silent --accept-package-agreements --accept-source-agreements

# Install PowerShell 7 (required for GitHub Actions workflows)
winget install --id Microsoft.PowerShell --silent --accept-package-agreements --accept-source-agreements
```

**Windows SDK Installation**:

The Windows SDK provides essential code signing tools including `signtool.exe` for authenticode signing. Install it manually:

1. Open a web browser and navigate to the [Windows SDK download page](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/)
2. Click **Download the installer** for the latest Windows SDK
3. Run the downloaded installer (`winsdksetup.exe`)
4. In the installer, select the following features:
    - **Windows SDK Signing Tools for Desktop Apps** (required for signtool.exe)
5. Complete the installation process

**Note**: After installation, `signtool.exe` will be available in the Windows SDK bin directory (typically `C:\Program Files (x86)\Windows Kits\10\bin\[version]\x64\`).

### 3. Manual GitHub Actions Runner Installation

**Step 1: Create Runner Directory**

```powershell
# Create runner directory
New-Item -ItemType Directory -Path "C:\actions-runner" -Force
Set-Location "C:\actions-runner"
```

**Step 2: Download Latest GitHub Actions Runner**

Download the latest GitHub Actions runner manually:

1. Go to [GitHub Actions Runner releases](https://github.com/actions/runner/releases/latest)
2. Download the latest `actions-runner-win-x64-[version].zip` file
3. Extract to `C:\actions-runner` directory

Alternatively, use PowerShell to download:

```powershell
# Download the latest stable release (check GitHub releases page for current version)
$runnerVersion = "2.311.0"  # Update this to the latest version
$downloadUrl = "https://github.com/actions/runner/releases/download/v$runnerVersion/actions-runner-win-x64-$runnerVersion.zip"

# Download and extract
Invoke-WebRequest -Uri $downloadUrl -OutFile "actions-runner-win-x64.zip"
Expand-Archive -Path "actions-runner-win-x64.zip" -DestinationPath . -Force
Remove-Item "actions-runner-win-x64.zip"
```

**Step 3: Get GitHub Registration Token**

Generate a registration token from your GitHub repository:

1. Go to your GitHub repository: https://github.com/sbroenne/RvToolsMerge
2. Navigate to **Settings** → **Actions** → **Runners**
3. Click **New self-hosted runner**
4. Select **Windows** and **x64**
5. Copy the configuration commands from the displayed instructions

**Step 4: Configure the Runner**

Follow the exact commands provided in the GitHub web interface. They will look similar to this (but use the actual commands from your GitHub page):

```cmd
# Configure the runner (use the exact commands from GitHub web interface)
.\config.cmd --url https://github.com/sbroenne/RvToolsMerge --token [YOUR-TOKEN] --name azure-windows-runner --work "_work" --labels codesign-runner --unattended --replace
```

**Important**:

-   Use the exact configuration command provided by GitHub, including the specific token and URL shown on the runner setup page
-   Add `--labels codesign-runner` to identify this runner as dedicated to code signing workflows
-   The `codesign-runner` label allows GitHub Actions workflows to specifically target this runner for signing operations

**Step 5: Test the Runner**

```powershell
# Test runner with single job
.\run.cmd --once
```

**Step 6: Run as Interactive Process (Required for Code Signing)**

> **Important**: For code signing workflows, the runner must run as an interactive process in a user session, not as a Windows service. Code signing certificates are only accessible in interactive sessions.

**Option A: Run Interactively (Recommended for Code Signing)**

```powershell
# Start runner interactively (keeps terminal open)
.\run.cmd
```

**Option B: Run as Background Process in Interactive Session**

```powershell
# Start runner as background process while maintaining interactive session
Start-Process -FilePath ".\run.cmd" -WorkingDirectory "C:\actions-runner" -WindowStyle Minimized
```

**Important Notes for Code Signing**:

-   The VM must remain logged in with the user session active
-   Do not run the runner as a Windows service for code signing workflows
-   Code signing certificates (especially those stored in Windows Certificate Store) require an interactive desktop session
-   Consider using automatic login or keeping the RDP session connected

**Step 7: Verify Interactive Session**

Ensure the runner is running in an interactive session:

```powershell
# Check if runner process is running in current session
Get-Process -Name "Runner.Listener" -IncludeUserName | Select-Object Name, UserName, SessionId

# Verify current session ID
query session $env:USERNAME
```

### 4. Verify Runner Registration

Check your GitHub repository:

1. Go to **Settings** → **Actions** → **Runners**
2. Look for your runner name (e.g., "azure-windows-runner")
3. Status should show as "Idle" (green)
4. Verify the `codesign-runner` label is displayed on the runner

## Available Tools on the VM

After manual installation, the VM will have the following tools available:

-   **Windows 11 Pro**: Base operating system with GUI
-   **Git**: Version control and source code access
-   **.NET 9 SDK**: Building .NET applications and creating signed artifacts
-   **PowerShell 7**: Cross-platform automation and scripting (required for GitHub Actions workflows)
-   **Windows PowerShell** (built-in): Windows-specific scripting
-   **Windows SDK**: Tools including signtool.exe for authenticode signing
-   **MSBuild**: Building and packaging applications

## Security Considerations

### Network Security

-   RDP (3389) is restricted to your current public IP by default
-   All outbound traffic is allowed for package downloads and GitHub communication
-   No inbound HTTP/HTTPS traffic allowed by default

### Additional Security Setup

After VM deployment, consider implementing additional security measures:

-   Change default RDP port
-   Configure Windows Firewall rules
-   Install anti-malware software
-   Configure automatic Windows Updates
-   Implement certificate management for code signing

## Troubleshooting

### Common Issues

**1. Deployment Fails with Quota Error**

```
Solution: Check your Azure subscription quotas
Command: az vm list-usage --location swedencentral
Action: Request quota increase if needed
```

**2. Runner Doesn't Appear in GitHub**

```
Check: Runner configuration command output
Verify: GitHub token has correct permissions
Common causes: Invalid GitHub token, network issues, incorrect repository URL
```

**3. Can't RDP to VM**

```
Check: Network Security Group rules
Verify: Your current public IP is allowed
Action: Update NSG rules if your IP changed
```

**4. VM Performance Issues**

```
Solution: Consider upgrading to a larger AMD-based VM size
Monitor: CPU and memory usage in Windows Task Manager
Current: Standard_B2as_v2 (2 vCPU, 8GB RAM)
Upgrade options: Standard_B4as_v2 (4 vCPU, 16GB RAM) or Standard_B8as_v2 (8 vCPU, 32GB RAM)
```

### Debugging Commands

**Check VM status**:

```cmd
az vm show --resource-group rg-github-runner-dev --name vm-runner-[token] --show-details
```

**View deployment logs**:

```cmd
az deployment group show --resource-group rg-github-runner-dev --name main
```

**Check runner service status** (on the VM):

```powershell
Get-Service -Name "actions.runner.*" -ErrorAction SilentlyContinue
```

**Manual runner service management** (on the VM):

```powershell
# Using the runner's built-in service commands
cd C:\actions-runner

# Check service status
.\svc.cmd status

# Stop the service
.\svc.cmd stop

# Start the service
.\svc.cmd start

# Uninstall the service
.\svc.cmd uninstall
```

**Manual runner process management** (on the VM):

```powershell
# Start runner interactively (for testing)
cd C:\actions-runner
.\run.cmd

# Start runner as background process
Start-Process -FilePath ".\run.cmd" -WorkingDirectory "C:\actions-runner" -WindowStyle Hidden

# Stop manually started runner process
Get-Process -Name "Runner.Listener" -ErrorAction SilentlyContinue | Stop-Process -Force
```

**Check runner logs** (on the VM):

```powershell
# View runner diagnostic logs
Get-ChildItem C:\actions-runner\_diag -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 5

# View latest log file
Get-Content (Get-ChildItem C:\actions-runner\_diag -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
```

## Cleanup and Cost Management

### Remove All Resources

To completely remove all Azure resources created by the deployment:

```cmd
rem Delete the entire resource group and all resources
az group delete --name rg-github-runner-dev --yes --no-wait
```

### Stop VM When Not in Use

> **Important**: The VM must remain running for the GitHub Actions runner to be available for workflows. Stopping the VM will make the runner unavailable until the VM is restarted.

```cmd
rem Only stop VM during maintenance windows or when runner is not needed
az vm deallocate --resource-group rg-github-runner-dev --name vm-runner-[token]

rem Start VM to restore runner availability
az vm start --resource-group rg-github-runner-dev --name vm-runner-[token]
```

### Cost Optimization Tips

1. **Use AMD-based VM sizes** for better price-performance (already configured)
2. **Monitor usage** with Azure Cost Management
3. **Set up billing alerts** to track spending
4. **Leverage Azure Hybrid Use Benefit** if you have eligible Windows licenses
5. **Consider GitHub-hosted runners** for infrequent workloads

**Estimated Monthly Cost**: Approximately $69 USD for continuous operation of Standard_B2as_v2 AMD-based VM (not including networking costs).
