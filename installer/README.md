# RVToolsMerge MSI Installer

This directory contains the WiX Toolset configuration files for creating Windows MSI installers for RVToolsMerge.

## Files

- **`RVToolsMerge.wxs`** - Main WiX source file defining the installer structure, features, and UI
- **`RVToolsMerge.Installer.wixproj`** - WiX project file for building the MSI
- **`License.rtf`** - License agreement displayed during installation (RTF format)
- **`build-msi.bat`** - Manual build script for local MSI creation (Windows only)

## MSI Installer Features

The Windows MSI installer provides:

- **Professional installation experience** with proper Windows Installer integration
- **Desktop and Start Menu shortcuts** for easy access
- **Add/Remove Programs integration** with proper uninstall support
- **Application icon** embedded throughout the installation experience
- **License agreement** display during installation
- **Optional application launch** after installation completes
- **Upgrade support** for version updates

## Automatic Build Process

MSI files are automatically created during the CI/CD release process:

1. **Trigger**: Release builds (`Configuration: Release`) on Windows runners
2. **Dependencies**: WiX Toolset 6.0.1 is automatically installed during the build
3. **Output**: MSI files are created for both `win-x64` and `win-arm64` architectures
4. **Artifacts**: MSI files are uploaded as separate artifacts and included in GitHub releases

## Manual Build Process

To build MSI installers locally (Windows only):

### Prerequisites

```powershell
# Install WiX Toolset 6.0.1
dotnet tool install --global wix --version 6.0.1
```

### Build Steps

1. **Publish the application first**:
   ```powershell
   dotnet publish ../src/RVToolsMerge/RVToolsMerge.csproj --configuration Release --runtime win-x64 --self-contained true --output ../publish
   ```

2. **Build the MSI**:
   ```powershell
   cd installer
   wix build RVToolsMerge.wxs -define PublishDir="../publish" -out "RVToolsMerge-1.3.3-win-x64.msi" -ext WixToolset.UI.wixext
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

### Installation Directory
- **Default**: `C:\Program Files\RVToolsMerge\`
- **User selectable**: Yes, user can choose custom installation directory

### Shortcuts Created
- **Desktop**: `RVToolsMerge` shortcut pointing to the main executable
- **Start Menu**: `RVToolsMerge` shortcut in the programs menu

### Files Installed
- **Main executable**: `RVToolsMerge.exe`
- **Application icons**: `app-icon.ico`, `app-icon.png`, `app-icon.svg`
- **All dependencies**: Included in the self-contained executable

### Registry Integration
- **Add/Remove Programs**: Full integration with Windows software management
- **Product information**: Version, publisher, description, and support links
- **Uninstall support**: Clean removal of all installed files and shortcuts

## Troubleshooting

### Common Issues

1. **WiX not found**: Ensure WiX Toolset 6.0.1 is installed globally
2. **Publish directory missing**: Make sure to publish the application before building the MSI
3. **Permission errors**: Run command prompt as Administrator if needed
4. **Version mismatch**: Update version numbers in the WiX source file when releasing new versions

### Logs and Debugging

- **MSI installation logs**: Use `msiexec /i installer.msi /l*v install.log` to generate detailed installation logs
- **WiX build logs**: WiX provides detailed build output for troubleshooting

## Version Management

When releasing new versions:

1. **Version binding**: The MSI version is automatically extracted from the executable using `!(bind.FileVersion.RVToolsMerge.exe)`
2. **Upgrade support**: The MSI includes major upgrade logic to handle version updates
3. **GUID management**: Component and upgrade GUIDs should remain stable across versions

## Security Considerations

- **Code signing**: For production releases, MSI files should be code-signed with a valid certificate
- **Publisher verification**: Unsigned MSI files will show security warnings during installation
- **Admin privileges**: The installer requests admin privileges for proper system integration