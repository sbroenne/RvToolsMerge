# Azure GitHub Runner Deployment Guide

This guide provides comprehensive instructions for deploying a self-hosted GitHub Actions runner on Azure using Windows with GUI (not Server Core) for development and CI/CD purposes.

## Overview

The deployment creates:

-   **Windows VM with GUI**: Full Windows desktop environment (Windows 10/11 Pro/Enterprise)
-   **Development Tools**: Pre-installed with Git, VS Code, .NET SDK, Node.js, Python, and Chocolatey
-   **Premium Storage**: 128GB Premium SSD for better performance
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

## VM Size Options and Costs

| VM Size             | vCPUs | RAM   | Storage    | Est. Monthly Cost\* | Best For                          |
| ------------------- | ----- | ----- | ---------- | ------------------- | --------------------------------- |
| **Standard_B2ms**   | 2     | 8 GB  | 16 GB temp | ~$30 USD            | Light development, basic CI/CD    |
| **Standard_B4ms**   | 4     | 16 GB | 32 GB temp | ~$60 USD            | Medium development workloads      |
| **Standard_D2s_v3** | 2     | 8 GB  | 16 GB temp | ~$70 USD            | Consistent performance needs      |
| **Standard_D4s_v3** | 4     | 16 GB | 32 GB temp | ~$140 USD           | Heavy development, complex builds |
| **Standard_E2s_v3** | 2     | 16 GB | 32 GB temp | ~$120 USD           | Memory-intensive applications     |

\*Costs are estimates for East US region and don't include storage (~$15/month) and networking (~$5/month).

## Windows Version Options

| Version            | Description             | Best For                     |
| ------------------ | ----------------------- | ---------------------------- |
| **win11-23h2-pro** | Windows 11 Professional | Modern development (default) |
| **win10-22h2-pro** | Windows 10 Professional | Legacy compatibility needs   |
| **win11-23h2-ent** | Windows 11 Enterprise   | Enterprise environments      |
| **win10-22h2-ent** | Windows 10 Enterprise   | Enterprise with legacy needs |

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

```powershell
.\scripts\deploy.ps1 `
  -EnvironmentName "dev" `
  -Location "eastus" `
  -AdminUsername "azureuser" `
  -AdminPassword "YourSecurePassword123!" `
  -GitHubToken "ghp_xxxxxxxxxxxxxxxxxxxx" `
  -GitHubRepositoryUrl "https://github.com/username/RvToolsMerge" `
  -RunnerName "azure-windows-dev" `
  -VmSize "Standard_B2ms" `
  -WindowsVersion "win11-23h2-pro"
```

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
  --parameters githubRepositoryUrl="https://github.com/username/RvToolsMerge"
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

## Pre-Installed Development Tools

The VM comes with the following tools pre-installed:

-   **Chocolatey** package manager
-   **Git** for version control
-   **Visual Studio Code** with common extensions
-   **.NET 8 SDK** for .NET development
-   **Node.js LTS** for JavaScript/TypeScript projects
-   **Python 3.11** for Python development
-   **Windows PowerShell** and **PowerShell Core**

## Security Considerations

### Network Security

-   RDP (3389) is restricted to your current public IP by default
-   All outbound traffic is allowed for package downloads and GitHub communication
-   No inbound HTTP/HTTPS traffic allowed by default

### Credential Management

-   Admin credentials are required for RDP access
-   GitHub token is stored securely and used only for runner registration
-   Consider using Azure Key Vault for production deployments

### Recommended Security Practices

1. Use strong, unique passwords (minimum 12 characters)
2. Rotate GitHub tokens regularly
3. Monitor Azure Activity Logs for unauthorized access
4. Consider using Azure Bastion for secure RDP access
5. Implement Just-In-Time (JIT) access if available

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
5. **Use dev/test pricing** if you have Visual Studio subscriptions

## Advanced Configuration

### Custom Script Extensions

The deployment includes a custom script extension that:

-   Installs Chocolatey package manager
-   Installs common development tools
-   Configures GitHub runner with your repository
-   Sets up logging and error handling

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
