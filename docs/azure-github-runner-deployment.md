# Azure GitHub Runner Deployment Guide

This guide provides comprehensive instructions for deploying a self-hosted GitHub Actions runner on Azure using Windows with GUI for code signing and artifact security purposes.

## Overview

The deployment creates a secure Windows environment specifically designed for code signing workflows:

-   **Windows VM with GUI**: Full Windows desktop environment (Windows 10/11 Pro/Enterprise) for code signing tools
-   **Signing Tools**: Pre-installed with .NET SDK, Windows SDK, and signtool.exe for authenticode signing
-   **Standard Storage**: 128GB Standard SSD for secure certificate storage and build artifacts
-   **Network Security**: Configured with appropriate security groups for RDP and outbound access
-   **GitHub Runner**: Automatically configured as a self-hosted runner

## Prerequisites

### Required Software

-   [Azure CLI](https://aka.ms/InstallAzureCLI) installed and configured
-   PowerShell 5.1 or later
-   Active Azure subscription with appropriate permissions

### Required Permissions

-   **Contributor** role on the Azure subscription or resource group
-   **Virtual Machine Contributor** role (minimum)
-   **Network Contributor** role (for networking resources)

### GitHub Requirements

-   GitHub Personal Access Token with the following scopes:
    -   `repo` (Full control of repositories)
    -   `admin:org` (if deploying to organization repositories)
    -   `workflow` (Update GitHub Action workflows)

## VM Configuration

**Fixed VM Size**: The infrastructure uses `Standard_B2as_v2` (ARM-based) optimized for code signing workflows:

-   **Specifications**: 2 vCPU, 8 GB RAM, ARM64 architecture
-   **Cost**: Approximately $23 USD per month (before AHUB savings)
-   **Performance**: Sufficient for code signing operations and artifact processing
-   **Efficiency**: ARM-based processors provide excellent price-performance ratio for signing workloads
-   **Security**: Isolated environment for certificate management and signing operations

## Cost Optimization

### Azure Hybrid Use Benefit (AHUB)

**Enabled by Default**: The deployment automatically enables Azure Hybrid Use Benefit for Windows client licensing, which can provide up to 40% savings on compute costs if you have eligible Windows licenses.

**Requirements for AHUB**:

-   Valid Windows 10/11 Pro or Enterprise licenses with active Software Assurance
-   Compliance with Microsoft licensing terms
-   Only available for Windows client VMs (not Windows Server)

**To disable AHUB**: Set `enableAHUB` parameter to `false` in the deployment.

## Windows Version Support

**Fixed Configuration**: The infrastructure is configured to use Windows 11 Professional (23H2) exclusively for optimal code signing compatibility and Azure Hybrid Use Benefit support.

-   **Consistent Platform**: All deployments use the same Windows version for predictable signing behavior
-   **AHUB Optimization**: Windows 11 Pro provides the best cost optimization with Azure Hybrid Use Benefit
-   **Signing Tools Compatibility**: Full compatibility with Windows SDK, signtool.exe, and modern signing requirements
-   **Security Features**: Latest Windows security features for certificate protection

## Deployment Methods

### Method 1: Interactive PowerShell Script (Recommended)

1. **Clone the repository** (if not already done):

    ```cmd
    git clone https://github.com/your-username/RvToolsMerge.git
    cd RvToolsMerge
    ```

2. **Login to Azure**:

    ```cmd
    az login
    ```

3. **Run the deployment script**:

    ```cmd
    powershell -ExecutionPolicy Bypass -File scripts\deploy.ps1
    ```

4. **Follow the interactive prompts**:
    - VM admin password (minimum 12 characters)
    - GitHub Personal Access Token
    - GitHub repository URL
    - Confirm deployment settings

### Method 2: PowerShell with Parameters

For secure credential handling, use SecureString parameters:

```powershell
# Convert sensitive strings to SecureString for security
$securePassword = ConvertTo-SecureString "YourSecurePassword123!" -AsPlainText -Force
$secureToken = ConvertTo-SecureString "ghp_xxxxxxxxxxxxxxxxxxxx" -AsPlainText -Force

.\scripts\deploy.ps1 `
  -EnvironmentName "dev" `
  -Location "eastus" `
  -AdminUsername "azureuser" `
  -AdminPassword $securePassword `
  -GitHubToken $secureToken `
  -GitHubRepositoryUrl "https://github.com/username/RvToolsMerge" `
  -RunnerName "azure-windows-dev" `
  -EnableAHUB $true
```

> **Security Note**: The script now uses SecureString parameters for sensitive data (passwords and tokens) to prevent accidental exposure in logs or process lists.

### Method 3: Azure CLI with Bicep

```cmd
rem Create resource group
az group create --name rg-github-runner-dev --location eastus

rem Deploy infrastructure
az deployment group create ^
  --resource-group rg-github-runner-dev ^
  --template-file infra\main.bicep ^
  --parameters infra\main.parameters.dev.json ^
  --parameters adminPassword="YourSecurePassword123!" ^
  --parameters githubToken="ghp_xxxxxxxxxxxxxxxxxxxx" ^
  --parameters githubRepositoryUrl="https://github.com/username/RvToolsMerge" ^
  --parameters enableAHUB=true
```

## Post-Deployment Configuration

### 1. Verify Runner Registration

Check your GitHub repository:

1. Go to **Settings** → **Actions** → **Runners**
2. Look for your runner name (e.g., "azure-windows-runner-gui")
3. Status should show as "Idle" (green)

### 2. Access the Virtual Machine

**RDP Connection**:

-   Use the DNS name provided in deployment output
-   Port: 3389
-   Username: The admin username you specified
-   Password: The admin password you specified

**Example RDP connection**:

```
Computer: your-runner-name-dev.eastus.cloudapp.azure.com:3389
Username: azureuser
Password: [your-password]
```

### 3. Monitor Installation Progress

To check the runner installation status:

```cmd
az vm run-command invoke ^
  --resource-group rg-github-runner-dev ^
  --name vm-github-runner-dev ^
  --command-id RunPowerShellScript ^
  --scripts "Get-Content C:\runner-install.log"
```

## Pre-Installed Signing and Build Tools

The VM comes with the following tools pre-installed for code signing and artifact creation:

-   **Windows Package Manager (winget)** for native package management
-   **Git** for version control and source code access
-   **.NET 9 SDK** for building .NET applications and creating signed artifacts
-   **Node.js LTS** for JavaScript/TypeScript build processes
-   **Python 3.12** for build automation scripts
-   **Windows PowerShell** and **PowerShell Core** for signing automation
-   **Windows SDK** (available) with signtool.exe for authenticode signing
-   **MSBuild** for building and packaging applications

> **Note**: The infrastructure uses winget (Windows Package Manager) instead of third-party package managers like Chocolatey. Winget is Microsoft's native package manager for Windows, providing better security, reliability, and integration for code signing environments.

## Security Considerations

### Network Security

-   RDP (3389) is restricted to your current public IP by default
-   All outbound traffic is allowed for package downloads and GitHub communication
-   No inbound HTTP/HTTPS traffic allowed by default

### Credential Management

-   Admin credentials are required for RDP access to the signing environment
-   GitHub token is stored securely and used only for runner registration
-   **Code signing certificates** should be stored in Azure Key Vault for production deployments
-   Consider using Hardware Security Modules (HSM) for high-value signing certificates

### Recommended Security Practices

1. Use strong, unique passwords (minimum 12 characters) for VM access
2. Rotate GitHub tokens regularly
3. **Store signing certificates securely** using Azure Key Vault or dedicated HSM
4. **Limit certificate access** to only the signing workflows that require them
5. Monitor Azure Activity Logs for unauthorized access
6. Consider using Azure Bastion for secure RDP access without exposing VM to internet
7. Implement Just-In-Time (JIT) access if available
8. **Audit signing operations** and maintain logs of all code signing activities

## Troubleshooting

### Common Issues

**1. Deployment Fails with Quota Error**

```
Solution: Check your Azure subscription quotas
Command: az vm list-usage --location eastus
Action: Request quota increase if needed
```

**2. Runner Doesn't Appear in GitHub**

```
Check: Runner installation log on the VM
Command: Get-Content C:\runner-install.log
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
Solution: Consider upgrading to a larger VM size
Monitor: CPU and memory usage in Azure portal
Action: Resize VM through Azure portal
```

### Debugging Commands

**Check VM status**:

```cmd
az vm show --resource-group rg-github-runner-dev --name vm-github-runner-dev --show-details
```

**View deployment logs**:

```cmd
az deployment group show --resource-group rg-github-runner-dev --name main
```

**Check runner service status** (on the VM):

```powershell
Get-Service -Name "actions.runner.*"
```

## Cleanup and Cost Management

### Remove All Resources

```cmd
powershell -ExecutionPolicy Bypass -File scripts\cleanup.ps1 -EnvironmentName dev
```

### Stop VM When Not in Use

```cmd
rem Stop VM (deallocated - no compute charges)
az vm deallocate --resource-group rg-github-runner-dev --name vm-github-runner-dev

rem Start VM
az vm start --resource-group rg-github-runner-dev --name vm-github-runner-dev
```

### Cost Optimization Tips

1. **Stop VMs when not in use** - Use auto-shutdown policies
2. **Use smaller VM sizes** for light workloads
3. **Monitor usage** with Azure Cost Management
4. **Set up billing alerts** to track spending
5. **Leverage Azure Hybrid Use Benefit** for Windows Client licensing cost savings

## Advanced Configuration

### Custom Script Extensions

The deployment includes a custom script extension that:

-   Installs winget package manager and signing tools
-   Installs build and signing tools (.NET SDK, Windows SDK, signtool)
-   Configures GitHub runner with your repository for signing workflows
-   Sets up logging and error handling for signing operations

### Scaling to Multiple Runners

To deploy multiple runners:

1. Use different `runnerName` values
2. Deploy to different resource groups or regions
3. Consider using Azure Virtual Machine Scale Sets for automatic scaling

### Integration with Azure DevOps

While this deployment is GitHub-focused, the VM can also be configured for Azure DevOps agents by modifying the installation script.

## Support and Contributing

-   **Issues**: Report issues in the GitHub repository
-   **Documentation**: Update this guide when making changes
-   **Contributing**: Follow the project's contributing guidelines
-   **Security**: Report security issues privately to the maintainers

---

**Next Steps**: After successful deployment, test your runner by creating a simple GitHub Action workflow that targets your self-hosted runner.
