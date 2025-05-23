@echo off
echo Running RVToolsMerge...

:: Check if the project file exists
if not exist "%~dp0src\RVToolsMerge\RVToolsMerge.csproj" (
    echo Error: Could not find project file at %~dp0src\RVToolsMerge\RVToolsMerge.csproj
    echo Please make sure you are running this script from the root directory of the RVToolsMerge project.
    exit /b 1
)

:: Check if dotnet is installed
where dotnet >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo Error: The .NET SDK is not installed or not in your PATH.
    echo Please install the .NET SDK from https://dotnet.microsoft.com/download
    exit /b 1
)

:: Run the application
dotnet run --project "%~dp0src\RVToolsMerge\RVToolsMerge.csproj" %*
