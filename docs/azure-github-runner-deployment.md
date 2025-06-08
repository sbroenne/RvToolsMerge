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
-   Active Azure subscription with appropriate permissions

### Required Permissions

-   **Contributor** role on the Azure subscription or resource group
-   **Virtual Machine Contributor** role (minimum)
-   **Network Contributor** role (for networking resources)

### GitHub Requirements

-   GitHub Personal Access Token with the following scopes:
    -   `repo` (Full control of repositories) — required for repository-level runners.
    -   `admin:repo_hook` (Full control of repository hooks) — required to manage runner registration and webhook events.

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

### Azure CLI with Bicep

```cmd
rem Create resource group (specify your preferred Azure region)
az group create --name rg-github-runner-dev --location swedencentral

rem Deploy infrastructure
az deployment group create ^
  --resource-group rg-github-runner-dev ^
  --template-file infra\main.bicep ^
  --parameters infra\main.parameters.dev.json ^
  --parameters adminPassword="YourSecurePassword123!" ^
  --parameters githubToken="ghp_xxxxxxxxxxxxxxxxxxxx" ^
  --parameters githubRepositoryUrl="https://github.com/username/RvToolsMerge" ^
  --parameters runnerName="azure-windows-runner-gui" ^
  --parameters enableAHUB=false ^
  --parameters location="swedencentral"
```

**Note**: The parameters file (`main.parameters.dev.json`) contains default values for most parameters. You can override the `location` parameter to deploy to a different Azure region. The default is `swedencentral`.

## Parameter Validation and Requirements

### Required Parameters

The deployment scripts and Bicep template enforce the following validation rules:

**adminPassword**:

-   Minimum 12 characters
-   Must meet Azure VM password complexity requirements

**githubToken**:

-   Must be a valid GitHub Personal Access Token
-   Required scopes: `repo` and `admin:repo_hook`

**githubRepositoryUrl**:

-   Must be a valid GitHub repository URL format
-   Example: `https://github.com/username/repository`
-   Used for runner registration and API calls

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

### 1. Verify Runner Registration

Check your GitHub repository:

1. Go to **Settings** → **Actions** → **Runners**
2. Look for your runner name (e.g., "azure-windows-runner" or the name you specified)
3. Status should show as "Idle" (green)

**Monitor Installation Progress**: The runner installation typically takes 5-10 minutes after VM deployment completes.

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

**Default Runner Names**:

-   Bicep deployment: "azure-windows-runner-gui"
-   The actual name depends on the parameters used in deployment

### 3. Monitor Installation Progress

To check the runner installation status:

```cmd
az vm run-command invoke ^
  --resource-group rg-github-runner-dev ^
  --name vm-github-runner-dev ^
  --command-id RunPowerShellScript ^
  --scripts "Get-Content C:\runner-install.log"
```

**Installation Log Location**: The runner installation creates a detailed log at `C:\runner-install.log` on the VM, which includes:

-   Download and extraction progress
-   GitHub API communication
-   Service installation status
-   Error details if installation fails

## Pre-Installed Signing and Build Tools

The VM comes with the following tools pre-installed for code signing and artifact creation:

-   **Windows Package Manager (winget)** for native package management
-   **Git** for version control and source code access
-   **.NET 9 SDK** for building .NET applications and creating signed artifacts
-   **Node.js LTS** for JavaScript/TypeScript build processes
-   **Python 3.12** for build automation scripts
-   **Windows PowerShell** and **PowerShell 7** for signing automation and cross-platform scripting
-   **Windows SDK** (available) with signtool.exe for authenticode signing
-   **MSBuild** for building and packaging applications

> **Note**: The infrastructure uses winget (Windows Package Manager) instead of third-party package managers like Chocolatey. Winget is Microsoft's native package manager for Windows, providing better security, reliability, and integration for code signing environments.

## Security Considerations

### Network Security

-   RDP (3389) is restricted to your current public IP by default
-   All outbound traffic is allowed for package downloads and GitHub communication
-   No inbound HTTP/HTTPS traffic allowed by default

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
Additional Check: Service status with Get-Service -Name "actions.runner.*"
Common causes: Invalid GitHub token, network issues, incorrect repository URL, insufficient token permissions
```

**3. Can't RDP to VM**

```
Check: Network Security Group rules
Verify: Your current public IP is allowed
Action: Update NSG rules if your IP changed
```

**4. VM Performance Issues**

```
Solution: Consider upgrading to a larger AMD-based VM size for better performance
Monitor: CPU and memory usage in Azure portal
Current: Standard_B2as_v2 (2 vCPU, 8GB RAM) - AMD-based for efficiency
Upgrade options: Standard_B4as_v2 (4 vCPU, 16GB RAM) or Standard_B8as_v2 (8 vCPU, 32GB RAM)
Action: Resize VM through Azure portal or update vmSize parameter in deployment
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

**View runner installation logs** (on the VM):

```powershell
Get-Content C:\runner-install.log
```

**Check runner diagnostic logs** (on the VM):

```powershell
Get-ChildItem C:\actions-runner\_diag -Filter "*.log"
```

> **Note**: Commands marked "on the VM" should be run directly on the deployed Windows VM via RDP. Local deployment commands use Azure CLI.

## Cleanup and Cost Management

### Remove All Resources

To completely remove all Azure resources created by the deployment:

```cmd
rem Delete the entire resource group and all resources
az group delete --name rg-github-runner-dev --yes --no-wait
```

**Alternative - List and confirm before deletion**:

```cmd
rem List all resources in the group first
az resource list --resource-group rg-github-runner-dev --output table

rem Delete with confirmation prompt
az group delete --name rg-github-runner-dev
```

### Stop VM When Not in Use

```cmd
rem Stop VM (deallocated - no compute charges)
az vm deallocate --resource-group rg-github-runner-dev --name vm-github-runner-dev

rem Start VM
az vm start --resource-group rg-github-runner-dev --name vm-github-runner-dev
```

### Cost Optimization Tips

1. **Stop VMs when not in use** - Use auto-shutdown policies or manual stop/start
2. **Use AMD-based VM sizes** for better price-performance (already configured with Standard_B2as_v2)
3. **Monitor usage** with Azure Cost Management
4. **Set up billing alerts** to track spending
5. **Leverage Azure Hybrid Use Benefit** for Windows Client licensing cost savings (disabled by default - enable if you have eligible licenses)

**Estimated Monthly Cost**: Approximately $23 USD for the Standard_B2as_v2 AMD-based VM with Standard SSD storage (not including networking costs).

## Advanced Configuration

### Custom Script Extensions

The deployment includes a custom script extension that:

-   Installs winget package manager and signing tools
-   Installs build and signing tools (.NET SDK, Windows SDK, signtool)
-   Downloads and configures GitHub Actions Runner with your repository
-   Installs the runner as a Windows service for automatic startup
-   Sets up comprehensive logging and error handling for all operations
-   Validates service installation and provides diagnostic information

**Script Location**: The runner installation script is located at `scripts/install-runner.ps1` and handles:

-   Downloading the latest GitHub Actions Runner release
-   Configuring the runner with proper authentication tokens
-   Installing and starting the Windows service
-   Comprehensive error logging and troubleshooting information

### Scaling to Multiple Runners

To deploy multiple runners:

1. Use different `EnvironmentName` values to create separate resource groups
2. Use different `RunnerName` values to distinguish runners in GitHub
3. Deploy to different Azure regions for geographic distribution
4. Consider using Azure Virtual Machine Scale Sets for automatic scaling

**Example for multiple environments**:

```cmd
rem Deploy development runner
az group create --name rg-github-runner-dev --location swedencentral
az deployment group create ^
  --resource-group rg-github-runner-dev ^
  --template-file infra\main.bicep ^
  --parameters infra\main.parameters.dev.json ^
  --parameters adminPassword="YourSecurePassword123!" ^
  --parameters githubToken="ghp_xxxxxxxxxxxxxxxxxxxx" ^
  --parameters githubRepositoryUrl="https://github.com/username/RvToolsMerge" ^
  --parameters runnerName="azure-windows-dev"

rem Deploy staging runner
az group create --name rg-github-runner-staging --location swedencentral
az deployment group create ^
  --resource-group rg-github-runner-staging ^
  --template-file infra\main.bicep ^
  --parameters infra\main.parameters.dev.json ^
  --parameters adminPassword="YourSecurePassword123!" ^
  --parameters githubToken="ghp_xxxxxxxxxxxxxxxxxxxx" ^
  --parameters githubRepositoryUrl="https://github.com/username/RvToolsMerge" ^
  --parameters runnerName="azure-windows-staging"

rem Deploy production runner
az group create --name rg-github-runner-prod --location swedencentral
az deployment group create ^
  --resource-group rg-github-runner-prod ^
  --template-file infra\main.bicep ^
  --parameters infra\main.parameters.dev.json ^
  --parameters adminPassword="YourSecurePassword123!" ^
  --parameters githubToken="ghp_xxxxxxxxxxxxxxxxxxxx" ^
  --parameters githubRepositoryUrl="https://github.com/username/RvToolsMerge" ^
  --parameters runnerName="azure-windows-prod"
```

### Integration with Azure DevOps

While this deployment is GitHub-focused, the VM can also be configured for Azure DevOps agents by modifying the installation script.

## Support and Contributing

-   **Issues**: Report issues in the GitHub repository
-   **Documentation**: Update this guide when making changes
-   **Contributing**: Follow the project's contributing guidelines
-   **Security**: Report security issues privately to the maintainers

---

**Next Steps**: After successful deployment, test your runner by creating a simple GitHub Action workflow that targets your self-hosted runner.
