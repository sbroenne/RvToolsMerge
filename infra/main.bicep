targetScope = 'resourceGroup'

@description('The name of the environment for resource naming')
@minLength(3)
@maxLength(10)
param environmentName string = 'dev'

@description('Location for all resources')
param location string = resourceGroup().location

@description('VM administrator username')
param adminUsername string = 'azureuser'

@description('VM administrator password')
@secure()
@minLength(12)
param adminPassword string

@description('GitHub Personal Access Token for runner registration')
@secure()
param githubToken string

@description('GitHub repository URL (e.g., https://github.com/username/repo)')
param githubRepositoryUrl string

@description('Name for the GitHub runner')
param runnerName string = 'azure-windows-runner'

@description('VM size for the GitHub runner (ARM-based for cost efficiency)')
param vmSize string = 'Standard_B2as_v2'

@description('Windows 11 Pro version (fixed for consistency and optimal AHUB licensing)')
param windowsVersion string = 'win11-23h2-pro'

@description('Enable Azure Hybrid Use Benefit for Windows Client licensing')
param enableAHUB bool = true

// Generate a unique suffix for resource names
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = {
  'azd-env-name': environmentName
  project: 'github-runner'
  costCenter: 'development'
}

// Virtual Network
resource virtualNetwork 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: 'vnet-runner-${resourceToken}'
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/16'
      ]
    }
    subnets: [
      {
        name: 'default'
        properties: {
          addressPrefix: '10.0.0.0/24'
          networkSecurityGroup: {
            id: networkSecurityGroup.id
          }
        }
      }
    ]
  }
}

// Network Security Group
resource networkSecurityGroup 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: 'nsg-runner-${resourceToken}'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowRDP'
        properties: {
          description: 'Allow RDP access for administration'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '3389'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 1000
          direction: 'Inbound'
        }
      }
      {
        name: 'AllowHTTPS'
        properties: {
          description: 'Allow HTTPS outbound for GitHub API access'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 1001
          direction: 'Outbound'
        }
      }
      {
        name: 'AllowHTTP'
        properties: {
          description: 'Allow HTTP outbound for package downloads'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '80'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 1002
          direction: 'Outbound'
        }
      }
      {
        name: 'AllowDNS'
        properties: {
          description: 'Allow DNS resolution'
          protocol: 'Udp'
          sourcePortRange: '*'
          destinationPortRange: '53'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 1003
          direction: 'Outbound'
        }
      }
    ]
  }
}

// Public IP Address
resource publicIP 'Microsoft.Network/publicIPAddresses@2024-05-01' = {
  name: 'pip-runner-${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
    dnsSettings: {
      domainNameLabel: 'github-runner-${resourceToken}'
    }
  }
}

// Network Interface
resource networkInterface 'Microsoft.Network/networkInterfaces@2024-05-01' = {
  name: 'nic-runner-${resourceToken}'
  location: location
  tags: tags
  properties: {
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          privateIPAllocationMethod: 'Dynamic'
          publicIPAddress: {
            id: publicIP.id
          }
          subnet: {
            id: virtualNetwork.properties.subnets[0].id
          }
        }
      }
    ]
    networkSecurityGroup: {
      id: networkSecurityGroup.id
    }
  }
}

// User-assigned Managed Identity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-runner-${resourceToken}'
  location: location
  tags: tags
}

// Virtual Machine
resource virtualMachine 'Microsoft.Compute/virtualMachines@2024-07-01' = {
  name: 'vm-runner-${resourceToken}'
  location: location
  tags: union(tags, {
    'azd-service-name': 'github-runner'
  })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    hardwareProfile: {
      vmSize: vmSize
    }
    osProfile: {
      computerName: 'github-runner'
      adminUsername: adminUsername
      adminPassword: adminPassword
      windowsConfiguration: {
        enableAutomaticUpdates: true
        provisionVMAgent: true
        patchSettings: {
          patchMode: 'AutomaticByOS'
          assessmentMode: 'ImageDefault'
        }
        timeZone: 'UTC'
      }
    }
    storageProfile: {
      imageReference: {
        publisher: 'MicrosoftWindowsDesktop'
        offer: 'Windows-11'
        sku: windowsVersion
        version: 'latest'
      }
      osDisk: {
        name: 'osdisk-runner-${resourceToken}'
        caching: 'ReadWrite'
        createOption: 'FromImage'
        managedDisk: {
          storageAccountType: 'StandardSSD_LRS'
        }
        diskSizeGB: 128
      }
    }
    networkProfile: {
      networkInterfaces: [
        {
          id: networkInterface.id
        }
      ]
    }
    diagnosticsProfile: {
      bootDiagnostics: {
        enabled: true
      }
    }
    licenseType: enableAHUB ? 'Windows_Client' : null
  }
}

// Custom Script Extension to install development tools and setup GitHub runner
resource setupExtension 'Microsoft.Compute/virtualMachines/extensions@2024-07-01' = {
  parent: virtualMachine
  name: 'SetupRunner'
  properties: {
    publisher: 'Microsoft.Compute'
    type: 'CustomScriptExtension'
    typeHandlerVersion: '1.10'
    autoUpgradeMinorVersion: true
    settings: {
      commandToExecute: 'powershell.exe -ExecutionPolicy Unrestricted -Command "New-Item -Path C:\\setup -ItemType Directory -Force; Set-Content -Path C:\\setup\\repo.txt -Value \'${githubRepositoryUrl}\'; Set-Content -Path C:\\setup\\name.txt -Value \'${runnerName}\'; winget install --id Git.Git --silent --accept-package-agreements --accept-source-agreements; winget install --id Microsoft.DotNet.SDK.9 --silent --accept-package-agreements --accept-source-agreements; winget install --id OpenJS.NodeJS.LTS --silent --accept-package-agreements --accept-source-agreements; winget install --id Python.Python.3.12 --silent --accept-package-agreements --accept-source-agreements; Write-Host \'Setup completed. Use install-runner.ps1 script to configure GitHub Actions runner.\'"'
    }
    protectedSettings: {
      commandToExecute: 'powershell.exe -Command "Set-Content -Path C:\\setup\\token.txt -Value \'${githubToken}\'"'
    }
  }
}

// Outputs
output resourceGroupName string = resourceGroup().name
output virtualMachineName string = virtualMachine.name
output publicIPAddress string = publicIP.properties.ipAddress
output dnsName string = publicIP.properties.dnsSettings.fqdn
output managedIdentityId string = managedIdentity.id
output virtualNetworkId string = virtualNetwork.id
output adminUsername string = adminUsername
output rdpConnectionString string = '${publicIP.properties.dnsSettings.fqdn}:3389'
output estimatedMonthlyCost string = '~$23 USD (ARM-based efficiency + Standard SSD)'
output azureHybridUseBenefit string = enableAHUB ? 'Enabled (up to 40% savings)' : 'Disabled'
