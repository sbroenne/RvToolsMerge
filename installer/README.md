# RVToolsMerge MSI Installer

This directory contains the WiX Toolset configuration files for creating Windows MSI installers for RVToolsMerge.

## Files

-   **`RVToolsMerge.wxs`** - Main WiX source file defining the installer structure, features, and UI
-   **`RVToolsMerge.Installer.wixproj`** - WiX project file for building the MSI
-   **`License.rtf`** - License agreement displayed during installation (RTF format)
-   **`build-msi.bat`** - Manual build script for local MSI creation (Windows only)

## MSI Installer Features

The Windows MSI installer provides:

-   **Professional installation experience** with proper Windows Installer integration and GUI dialogs
-   **Installation directory selection** allowing users to choose their preferred installation location
-   **Unattended/Silent installation support** for automated deployments and enterprise scenarios
-   **Success confirmation dialog** displaying completion message and usage instructions
-   **Command-line access** by adding RVToolsMerge to the user's PATH environment variable
-   **Add/Remove Programs integration** with proper uninstall support
-   **Application icon** embedded throughout the installation experience
-   **License agreement** display during installation (when License.rtf is present)
-   **Version upgrade support** with automatic detection and seamless updates to newer versions

## Automatic Build Process

MSI files are automatically created during the CI/CD release process:

1. **Trigger**: Release builds (`Configuration: Release`) on Windows runners
2. **Dependencies**: WiX Toolset 6.0.1 is automatically installed during the build
3. **Output**: MSI files are created for both `win-x64` and `win-arm64` architectures
4. **Artifacts**: MSI files are uploaded as separate artifacts and included in GitHub releases

## Manual Build Process

To build MSI installers locally (Windows only):

## Unattended Installation

The MSI installer supports silent/unattended installation for automated deployments and enterprise scenarios.

### Silent Installation Commands

```powershell
# Basic silent installation (default location)
msiexec /i RVToolsMerge.msi /qn

# Silent installation with custom directory
msiexec /i RVToolsMerge.msi /qn INSTALLFOLDER="C:\Tools\RVToolsMerge"

# Silent installation with logging
msiexec /i RVToolsMerge.msi /qn /L*V "install.log"

# Silent installation for all users (requires admin privileges)
msiexec /i RVToolsMerge.msi /qn ALLUSERS=1

# Silent uninstallation
msiexec /x RVToolsMerge.msi /qn
```

### Installation Parameters

-   **`/qn`** - Completely silent installation (no UI)
-   **`/qb`** - Basic UI with progress bar only
-   **`/qi`** - Reduced UI (minimal dialogs)
-   **`INSTALLFOLDER="path"`** - Custom installation directory
-   **`ALLUSERS=1`** - Install for all users (requires admin privileges)
-   **`/L*V "logfile.log"`** - Generate detailed installation log

### Enterprise Deployment

For enterprise environments, you can deploy the MSI using:

-   **Group Policy Software Installation**
-   **Microsoft System Center Configuration Manager (SCCM)**
-   **PowerShell Desired State Configuration (DSC)**
-   **Ansible, Puppet, or Chef automation tools**
-   **Custom deployment scripts**

Example PowerShell deployment script:

```powershell
# Deploy RVToolsMerge silently to multiple machines
$computers = @("PC1", "PC2", "PC3")
$msiPath = "\\server\share\RVToolsMerge.msi"

foreach ($computer in $computers) {
    Invoke-Command -ComputerName $computer -ScriptBlock {
        param($msi)
        Start-Process -FilePath "msiexec.exe" -ArgumentList "/i `"$msi`" /qn" -Wait
    } -ArgumentList $msiPath
}
```

### Prerequisites

```powershell
# Install WiX Toolset 6.0.1
dotnet tool install --global wix --version 6.0.1
# Add WiX UI extension
wix extension add WixToolset.UI.wixext
```

### Build Steps

1. **Publish the application first**:

    ```powershell
    dotnet publish ./src/RVToolsMerge/RVToolsMerge.csproj --configuration Release --runtime win-x64 --self-contained true --output ./publish
    ```

2. **Build the MSI**:
    ```powershell
    cd installer
    wix build RVToolsMerge.wxs -define PublishDir="../publish" -out "RVToolsMerge-win-x64.msi" -ext WixToolset.UI.wixext
    ```

### Alternative: Use the Build Script

```cmd
# Set required environment variables
set PUBLISH_DIR=..\publish
set OUTPUT_DIR=..\msi-output
set VERSION=1.3.3

# Run the build script
build-msi.bat
```

## MSI Configuration Details

### Cabinet File Handling

-   **Embedded cabinet**: Cabinet files (`.cab`) are embedded directly in the MSI for single-file distribution
-   **Compression**: High compression level applied to reduce installer size
-   **Custom cabinet name**: Uses `RVToolsMerge.cab` for better identification

### Unattended Installation Support

-   **Silent installation**: Supports `/qn`, `/qb`, and `/qi` modes for automated deployment
-   **Custom installation directory**: Accepts `INSTALLFOLDER` parameter for non-default locations
-   **User context support**: Can install per-user or for all users based on privileges and parameters
-   **Enterprise deployment ready**: Compatible with Group Policy, SCCM, and other deployment tools
-   **Detailed logging**: Supports comprehensive installation logging for troubleshooting

### Installation Directory

-   **Default**: `C:\Program Files\RVToolsMerge\`
-   **User selectable**: Yes, user can choose custom installation directory

### Shortcuts Created

-   **Command-line access**: `RVToolsMerge` command is available from any command prompt or PowerShell window after installation
-   **PATH integration**: The installation directory is added to the user's PATH environment variable

### Files Installed

-   **Main executable**: `RVToolsMerge.exe`
-   **Application icons**: `app-icon.png`, `app-icon.svg` (in Resources/Icons subdirectory)
-   **All dependencies**: Included in the self-contained executable
-   **Cabinet files**: Embedded `.cab` files containing compressed installation data

### Registry Integration

-   **Add/Remove Programs**: Full integration with Windows software management
-   **Product information**: Version, publisher, description, and support links
-   **Uninstall support**: Clean removal of all installed files and shortcuts

## Troubleshooting

### Common Issues

1. **WiX not found**: Ensure WiX Toolset 6.0.1 is installed globally
2. **Publish directory missing**: Make sure to publish the application before building the MSI
3. **Permission errors**: Run command prompt as Administrator if needed
4. **Version mismatch**: Update version numbers in the WiX source file when releasing new versions

### Logs and Debugging

-   **MSI installation logs**: Use `msiexec /i installer.msi /l*v install.log` to generate detailed installation logs
-   **WiX build logs**: WiX provides detailed build output for troubleshooting

## Version Management and Upgrade Support

### Automatic Version Upgrades

The MSI installer **fully supports upgrading to newer versions** with the following features:

1. **Major Upgrade Logic**: Configured with `<MajorUpgrade>` element that automatically:

    - Detects existing installations
    - Removes the previous version before installing the new version
    - Prevents downgrades with clear error message: "A newer version of RVToolsMerge is already installed."
    - Maintains user settings and PATH environment variable

2. **Version Binding**: The MSI version is automatically extracted from the executable using `!(bind.FileVersion.RVToolsMerge.exe)`

    - Ensures MSI version always matches the application version
    - No manual version updates required in installer configuration

3. **GUID Management**:
    - **ProductCode**: Automatically generated for each version using `ProductCode="*"` to trigger upgrades
    - **UpgradeCode**: Remains stable across all versions (`A7B8C9D0-E1F2-4A5B-8C9D-0E1F2A5B8C9D`)
    - **Component GUIDs**: Use automatic generation (`Guid="*"`) for proper upgrade handling

### Upgrade Process

When a user installs a newer version:

1. **Detection**: Windows Installer detects the existing installation using the UpgradeCode
2. **Removal**: Previous version is automatically uninstalled
3. **Installation**: New version is installed to the same location
4. **Preservation**: User PATH settings and installation directory are preserved
5. **Completion**: User can immediately use the new version from command line

### Silent Upgrades

Upgrade installations work seamlessly with silent installation modes:

```cmd
# Silent upgrade to newer version
msiexec /i "RVToolsMerge-1.4.0-win-x64.msi" /qn

# Upgrade with basic progress indicator
msiexec /i "RVToolsMerge-1.4.0-win-x64.msi" /qb

# Upgrade with detailed logging
msiexec /i "RVToolsMerge-1.4.0-win-x64.msi" /qn /L*V "upgrade.log"
```

### Enterprise Upgrade Deployment

For enterprise environments, upgrades can be deployed using:

-   **Group Policy Software Installation**: Deploy newer MSI to automatically upgrade all domain computers
-   **SCCM/ConfigMgr**: Create upgrade deployments with detection rules
-   **PowerShell DSC**: Use Package resource with newer version requirement
-   **Automated deployment scripts**: Include upgrade logic in deployment automation

### Winget Package Manager Upgrades

When published to winget, users can upgrade using:

```powershell
# Check for available upgrades
winget upgrade RvToolsMerge.RvToolsMerge

# Upgrade to latest version
winget upgrade RvToolsMerge.RvToolsMerge

# Upgrade all installed packages
winget upgrade --all
```

## Security Considerations

-   **Code signing**: For production releases, MSI files should be code-signed with a valid certificate
-   **Publisher verification**: Unsigned MSI files will show security warnings during installation
-   **Admin privileges**: The installer requests admin privileges for proper system integration

## Windows Package Manager (winget) Integration

RVToolsMerge is prepared for publication to the Windows Package Manager with the identifier `RvToolsMerge.RvToolsMerge`.

### Automated Winget Manifest Generation

The project includes **automated winget manifest generation** as part of the release process:

-   **Template-based generation**: Winget manifests are automatically generated from templates in `.github/winget-templates/`
-   **Dynamic SHA256 calculation**: MSI file hashes are automatically calculated and included in manifests
-   **Version synchronization**: Package version automatically matches the release version
-   **Release integration**: Generated manifests are included as artifacts in GitHub releases

### Publishing Process

To submit to the Microsoft winget-pkgs repository:

1. **Download manifests**: Get the generated winget manifest files from the GitHub release artifacts
2. **Validate manifests**: Use `winget-create validate` to verify manifest syntax (pre-validated during generation)
3. **Submit PR**: Fork [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) and submit the manifest files
4. **Review process**: Microsoft reviews and approves the submission

### MSI Compatibility Requirements

The MSI installer is configured for winget compatibility:

-   **Dynamic Product Code**: Uses auto-generated GUID (`ProductCode="*"`) that changes with each version for proper upgrade handling
-   **Stable Upgrade Code**: Uses stable GUID `A7B8C9D0-E1F2-4A5B-8C9D-0E1F2A5B8C9D` for version detection
-   **Publisher information**: Matches winget manifest publisher details
-   **Version information**: Automatically extracted from executable for consistency
-   **Silent installation**: Supports silent installation modes required by winget

### Version Consistency for Winget

**Critical Requirement**: The winget manifest PackageVersion must match the MSI installer's ProductVersion.

The project maintains version consistency through:

1. **Project File Configuration**:
   ```xml
   <Version>1.4.2</Version>              <!-- 3-part semantic version -->
   <FileVersion>1.4.2.0</FileVersion>    <!-- 4-part version -->
   <AssemblyVersion>1.4.2.0</AssemblyVersion>
   ```

2. **MSI Version Binding**:
   - WiX configuration uses `Version="!(bind.FileVersion.RVToolsMerge.exe)"`
   - This binds to the executable's FileVersion property
   - MSI ProductVersion becomes `1.4.2.0`

3. **Windows Installer Behavior**:
   - MSI installers use only the first 3 parts of the version (major.minor.build)
   - The 4th part (revision) is ignored by Windows Installer
   - Effective MSI ProductVersion: `1.4.2`

4. **Automated Validation**:
   - Manifest generation extracts ProductVersion from MSI files
   - Normalizes to 3-part version for winget compatibility
   - Validates consistency with release tag version
   - Issues warnings if mismatches are detected

This ensures the winget manifest version (`1.4.2`) matches the MSI ProductVersion (`1.4.2`), preventing installation and upgrade issues.
-   **Version information**: Automatically extracted from executable for consistency
-   **Silent installation**: Supports silent installation modes required by winget

### User Installation

Once published, users can install RVToolsMerge using:

```powershell
winget install RvToolsMerge.RvToolsMerge
```
