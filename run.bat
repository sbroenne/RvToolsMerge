@echo off
echo Running RVToolsMerge...

:: Check if the project file exists
if not exist "%~dp0src\RVToolsMerge\RVToolsMerge.csproj" (
    echo Error: Could not find project file at %~dp0src\RVToolsMerge\RVToolsMerge.csproj
    exit /b 1
)

:: Run the application
dotnet run --project "%~dp0src\RVToolsMerge\RVToolsMerge.csproj" %*
