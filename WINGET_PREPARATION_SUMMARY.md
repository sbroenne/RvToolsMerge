# Winget Submission Preparation - Changes Summary

## Overview

This document summarizes the changes made to prepare RVToolsMerge for winget submission, focusing on version consistency requirements and validation.

## Problem Statement

The issue requested a review of the project against winget repository guidelines and rules, specifically asking to check versioning to ensure it mirrors what's in the installer.

## Key Finding

**Critical Issue Identified**: Potential version mismatch between winget manifest PackageVersion and MSI installer ProductVersion.

### The Issue

1. **Project Configuration**:
   - Package Version (in `.csproj`): `1.4.2` (3-part semantic version)
   - File Version (in `.csproj`): `1.4.2.0` (4-part version)
   - MSI uses: `!(bind.FileVersion.RVToolsMerge.exe)` which binds to `1.4.2.0`

2. **Winget Manifest**:
   - Uses `{{VERSION}}` placeholder which gets populated from git tag
   - Git tag format: `v1.4.2` → manifest version: `1.4.2`

3. **Windows Installer Behavior**:
   - MSI ProductVersion can be 4-part (e.g., `1.4.2.0`)
   - **BUT** Windows Installer only uses first 3 parts (major.minor.build)
   - The 4th part (revision) is **ignored** by Windows Installer
   - Effective MSI ProductVersion: `1.4.2`

4. **Winget Requirement**:
   - PackageVersion in manifest **must match** MSI ProductVersion
   - Mismatch causes installation and upgrade failures

## Solution Implemented

### 1. Enhanced Manifest Generation Script

**File**: `.github/scripts/generate-winget-manifests.ps1`

**Changes**:
- Added `Get-MsiProductVersion` function to extract ProductVersion from MSI files
- Implemented version normalization to 3-part format (MSI behavior)
- Added validation to compare MSI ProductVersion with provided version
- Added warnings if versions don't match
- Added check to ensure x64 and ARM64 versions match

**Code Added**:
```powershell
function Get-MsiProductVersion {
    # Extracts ProductVersion from MSI
    # Normalizes to 3-part version (MSI only uses major.minor.build)
    # Returns normalized version for comparison
}

# Validation logic:
- Extract ProductVersion from x64 and ARM64 MSI files
- Normalize to 3-part version
- Compare with provided version from git tag
- Warn if mismatches detected
- Fail if x64 and ARM64 versions don't match
```

### 2. Version Consistency Tests

**File**: `tests/RVToolsMerge.IntegrationTests/WingetVersionConsistencyTests.cs`

**Tests Added**:
1. `ProjectVersion_Should_BeThreePartSemanticVersion` - Validates package version format
2. `FileVersion_Should_BeFourPartVersion` - Validates file/assembly version format
3. `FileVersion_Should_MatchPackageVersionWithZeroRevision` - Validates version synchronization
4. `WingetManifestTemplate_Should_UseVersionPlaceholder` - Validates template uses placeholders
5. `WingetInstallerManifest_Should_UseVersionForDisplayVersion` - Validates DisplayVersion
6. `WingetInstallerManifest_Should_NotHaveProductCodeField` - Validates ProductCode handling
7. `WixConfiguration_Should_BindToFileVersion` - Validates WiX version binding
8. `VersionManagementWorkflow_Should_UseThreePartVersions` - Validates workflow version handling

**Purpose**: Automated validation to prevent version inconsistencies

### 3. Documentation Updates

#### `.github/winget-templates/README.md`

Added comprehensive "Version Consistency" section explaining:
- How project versions are configured
- MSI version behavior (3-part vs 4-part)
- Automated validation process
- Version alignment across all components

#### `docs/winget-submission-setup.md`

Added:
- "Version Mismatch Issues" troubleshooting section
- "Version Consistency Requirements" section with detailed explanation
- How version consistency is maintained
- MSI version behavior details
- Automated validation process
- Integration test information

#### `installer/README.md`

Added comprehensive "Version Consistency for Winget" section:
- Project file configuration details
- MSI version binding explanation
- Windows Installer behavior
- Automated validation process
- Code examples showing version configuration

#### `CONTRIBUTING.md`

Added:
- Pre-commit checklist item for version consistency
- Detailed "Version Consistency for Winget" subsection
- Rules for maintaining version alignment
- When and how to update versions
- How to verify with integration tests

#### `docs/continuous-integration.md`

Enhanced "Version Management & Release Workflow" section:
- Detailed version update process
- 3-part vs 4-part version handling
- Winget manifest generation details
- MSI ProductVersion validation
- Consistency checks

## Version Consistency Flow

### Current State (Verified)

```
Project File (.csproj):
├─ <Version>1.4.2</Version>                    (3-part package version)
├─ <FileVersion>1.4.2.0</FileVersion>         (4-part file version)
└─ <AssemblyVersion>1.4.2.0</AssemblyVersion> (4-part assembly version)

WiX Configuration (RVToolsMerge.wxs):
└─ Version="!(bind.FileVersion.RVToolsMerge.exe)"  (binds to 1.4.2.0)

MSI Installer:
└─ ProductVersion: 1.4.2.0 (from FileVersion binding)
   └─ Effective Version: 1.4.2 (Windows Installer uses only first 3 parts)

Git Tag:
└─ v1.4.2 (semantic version with 'v' prefix)

Winget Manifest:
└─ PackageVersion: 1.4.2 (from git tag, v prefix removed)
└─ DisplayVersion: 1.4.2 (from {{VERSION}} placeholder)

✅ Result: All versions aligned correctly!
```

### Validation Process

1. **During Manifest Generation**:
   ```
   1. Download x64 and ARM64 MSI files
   2. Extract ProductVersion from each MSI
   3. Normalize to 3-part version (remove 4th part if present)
   4. Compare with release tag version
   5. Warn if mismatches detected
   6. Fail if x64 ≠ ARM64
   ```

2. **During Development**:
   ```
   1. Run WingetVersionConsistencyTests
   2. Verify version formats
   3. Verify version alignment
   4. Verify workflow configuration
   ```

## Benefits

1. **Prevents Installation Failures**: Ensures winget can properly install and upgrade packages
2. **Early Detection**: Integration tests catch version mismatches before release
3. **Automated Validation**: Manifest generation script validates versions automatically
4. **Clear Documentation**: Contributors understand version requirements
5. **Troubleshooting Guidance**: Documentation helps resolve version issues

## Compliance with Winget Guidelines

### Version Requirements ✅
- Uses semantic versioning (X.Y.Z format)
- PackageVersion matches MSI ProductVersion
- Versions are consistent across architectures
- Automated validation ensures compliance

### Manifest Requirements ✅
- Follows winget manifest schema v1.6.0
- Version manifest includes PackageVersion
- Installer manifest includes DisplayVersion matching ProductVersion
- ProductCode is dynamically extracted (not hardcoded)

### MSI Requirements ✅
- Uses auto-generated ProductCode (`ProductCode="*"`)
- Maintains stable UpgradeCode for upgrade detection
- Supports silent installation
- Version information correctly bound from executable

## Testing

All changes have been tested:

```bash
# Build verification
dotnet build --configuration Release
✅ Build succeeded: 0 Warning(s), 0 Error(s)

# Test verification
dotnet test --configuration Release --no-build
✅ Test summary: total: 164, failed: 0, succeeded: 164, skipped: 0

# New tests specifically
dotnet test --filter "FullyQualifiedName~WingetVersionConsistencyTests"
✅ Passed: 8/8 tests
```

## Files Changed

### Code Changes
1. `.github/scripts/generate-winget-manifests.ps1` - Added ProductVersion extraction and validation
2. `tests/RVToolsMerge.IntegrationTests/WingetVersionConsistencyTests.cs` - New test file (8 tests)

### Documentation Changes
3. `.github/winget-templates/README.md` - Added version consistency section
4. `docs/winget-submission-setup.md` - Added troubleshooting and requirements
5. `installer/README.md` - Added version consistency section
6. `CONTRIBUTING.md` - Added contributor guidance
7. `docs/continuous-integration.md` - Enhanced workflow documentation

## Recommendations

### For Next Release

1. **Before Creating Release**:
   - Run `WingetVersionConsistencyTests` to verify version alignment
   - Review version numbers in `.csproj`
   - Ensure FileVersion = PackageVersion + `.0`

2. **During Release**:
   - Use version management workflow for automated version updates
   - Verify manifest generation completes without warnings
   - Check that both x64 and ARM64 MSI versions match

3. **After Release**:
   - Download and review generated winget manifests
   - Verify PackageVersion matches git tag
   - Test manifest validation with `winget validate`

### For Winget Submission

When ready to submit to winget:

1. **Use Winget Submission Workflow**:
   - Navigate to Actions → Winget Submission Preparation
   - Run with `dryRun: true` first to test
   - Review artifacts and submission information
   - Run with `dryRun: false` to create submission branch

2. **Verify Manifests**:
   - Check PackageVersion matches release version
   - Verify DisplayVersion in installer manifest
   - Confirm SHA256 hashes are correct
   - Validate ProductCode values are present

3. **Submit to Microsoft**:
   - Use provided PR creation URL
   - Follow winget community review process
   - Address any feedback from reviewers

## Conclusion

The project is now **ready for winget submission** with:

✅ Version consistency validation implemented
✅ Comprehensive testing in place
✅ Documentation updated for contributors
✅ Automated validation during manifest generation
✅ Clear troubleshooting guidance

The versioning is correctly configured and mirrors what's in the installer, with automated checks to prevent future mismatches.
