@echo off
echo Building MSI installer for RVToolsMerge...

REM Check if required variables are set
if "%PUBLISH_DIR%"=="" (
    echo Error: PUBLISH_DIR environment variable not set
    exit /b 1
)

if "%OUTPUT_DIR%"=="" (
    echo Error: OUTPUT_DIR environment variable not set
    exit /b 1
)

if "%VERSION%"=="" (
    echo Error: VERSION environment variable not set
    exit /b 1
)

REM Build the MSI
echo Building MSI with WiX...
wix build RVToolsMerge.wxs -define PublishDir="%PUBLISH_DIR%" -out "%OUTPUT_DIR%\RVToolsMerge-%VERSION%.msi" -ext WixToolset.UI.wixext

if %ERRORLEVEL% neq 0 (
    echo Error: MSI build failed
    exit /b 1
)

echo MSI build completed successfully: %OUTPUT_DIR%\RVToolsMerge-%VERSION%.msi