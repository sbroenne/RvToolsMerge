# RVTools Column Name Mappings

The application standardizes column names across different RVTools exports. Below is a reference of the mappings applied during processing:

## vInfo Sheet Mappings

| Original Column Name | Standardized Name |
|---------------------|-------------------|
| vInfoVMName | VM |
| vInfoPowerstate | Powerstate |
| vInfoTemplate | Template |
| vInfoCPUs | CPUs |
| vInfoMemory | Memory |
| vInfoProvisioned | Provisioned MiB |
| vInfoInUse | In Use MiB |
| vInfoOS | OS according to the configuration file |
| vInfoDataCenter | Datacenter |
| vInfoCluster | Cluster |
| vInfoHost | Host |
| vInfoSRMPlaceHolder | SRM Placeholder |
| vInfoOSTools | OS according to the VMware Tools |

## vHost Sheet Mappings

| Original Column Name | Standardized Name |
|---------------------|-------------------|
| vHostName | Host |
| vHostDatacenter | Datacenter |
| vHostCluster | Cluster |
| vHostvSANFaultDomainName | vSAN Fault Domain Name |
| vHostCpuModel | CPU Model |
| vHostCpuMhz | Speed |
| vHostNumCPU | # CPU |
| vHostNumCpu | # CPU |
| vHostCoresPerCPU | Cores per CPU |
| vHostNumCpuCores | # Cores |
| vHostOverallCpuUsage | CPU usage % |
| vHostMemorySize | # Memory |
| vHostOverallMemoryUsage | Memory usage % |
| vHostvCPUs | # vCPUs |
| vHostVCPUsPerCore | vCPUs per Core |

## vPartition Sheet Mappings

| Original Column Name | Standardized Name |
|---------------------|-------------------|
| vPartitionDisk | Disk |
| vPartitionVMName | VM |
| vPartitionConsumedMiB | Consumed MiB |
| vPartitionCapacityMiB | Capacity MiB |

## vMemory Sheet Mappings

| Original Column Name | Standardized Name |
|---------------------|-------------------|
| vMemoryVMName | VM |
| vMemorySizeMiB | Size MiB |
| vMemoryReservation | Reservation |

> **Note:** These mappings help normalize column names between different versions of RVTools exports.
