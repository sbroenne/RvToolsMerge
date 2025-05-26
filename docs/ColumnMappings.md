# RVTools Column Name Mappings

The application standardizes column names across different RVTools exports. Below is a reference of the mappings applied during processing:

## vInfo Sheet Mappings

| Original Column Name  | Standardized Name                      |
| --------------------- | -------------------------------------- |
| vInfoVMName           | VM                                     |
| vInfoPowerstate       | Powerstate                             |
| vInfoTemplate         | Template                               |
| vInfoGuestHostName    | DNS Name                               |
| vInfoCPUs             | CPUs                                   |
| vInfoMemory           | Memory                                 |
| vInfoProvisioned      | Provisioned MiB                        |
| vInfoInUse            | In Use MiB                             |
| vInfoDataCenter       | Datacenter                             |
| vInfoCluster          | Cluster                                |
| vInfoHost             | Host                                   |
| vInfoSRMPlaceHolder   | SRM Placeholder                        |
| vInfoOSTools          | OS according to the VMware Tools       |
| vInfoOS               | OS according to the configuration file |
| vInfoPrimaryIPAddress | Primary IP Address                     |
| vInfoNetwork1         | Network #1                             |
| vInfoNetwork2         | Network #2                             |
| vInfoNetwork3         | Network #3                             |
| vInfoNetwork4         | Network #4                             |
| vInfoNetwork5         | Network #5                             |
| vInfoNetwork6         | Network #6                             |
| vInfoNetwork7         | Network #7                             |
| vInfoNetwork8         | Network #8                             |
| vInfoResourcepool     | Resource pool                          |
| vInfoFolder           | Folder                                 |

## vHost Sheet Mappings

| Original Column Name     | Standardized Name      |
| ------------------------ | ---------------------- |
| vHostName                | Host                   |
| vHostDatacenter          | Datacenter             |
| vHostCluster             | Cluster                |
| vHostvSANFaultDomainName | vSAN Fault Domain Name |
| vHostCpuModel            | CPU Model              |
| vHostCpuMhz              | Speed                  |
| vHostNumCPU              | # CPU                  |
| vHostNumCpu              | # CPU                  |
| vHostCoresPerCPU         | Cores per CPU          |
| vHostNumCpuCores         | # Cores                |
| vHostOverallCpuUsage     | CPU usage %            |
| vHostMemorySize          | # Memory               |
| vHostOverallMemoryUsage  | Memory usage %         |
| vHostvCPUs               | # vCPUs                |
| vHostVCPUsPerCore        | vCPUs per Core         |

## vPartition Sheet Mappings

| Original Column Name  | Standardized Name |
| --------------------- | ----------------- |
| vPartitionDisk        | Disk              |
| vPartitionVMName      | VM                |
| vPartitionConsumedMiB | Consumed MiB      |
| vPartitionCapacityMiB | Capacity MiB      |

## vMemory Sheet Mappings

| Original Column Name | Standardized Name |
| -------------------- | ----------------- |
| vMemoryVMName        | VM                |
| vMemorySizeMiB       | Size MiB          |
| vMemoryReservation   | Reservation       |

## Anonymization Mapping

When anonymization is enabled, the following fields are anonymized with a consistent pattern:

| Data Type  | Original Value      | Anonymized Format |
| ---------- | ------------------- | ----------------- |
| VM Name    | myVM01              | vm1               |
| DNS Name   | server.domain.local | dns1              |
| IP Address | 192.168.1.10        | ip1               |
| Cluster    | Production-Cluster  | cluster1          |
| Host       | esx01.domain.local  | host1             |
| Datacenter | DC-East             | datacenter1       |

> **Note:** These mappings help normalize column names between different versions of RVTools exports while maintaining data relationships during anonymization.

### Anonymization Map File

When anonymization is enabled, an additional Excel file is created alongside the output file. This file, named `<output_filename>_AnonymizationMap.xlsx`, contains separate worksheets for each type of anonymized data (VMs, DNS Names, Clusters, Hosts, Datacenters, and IP Addresses).

Each worksheet contains two columns:
1. **Original Value** - The original value before anonymization
2. **Anonymized Value** - The corresponding anonymized value

This mapping file can be used to:
- De-anonymize data later if needed for specific analysis
- Verify which specific entities were anonymized
- Understand the mapping between real and anonymized identifiers

The file is only created when the `-a` or `--anonymize` option is used.
