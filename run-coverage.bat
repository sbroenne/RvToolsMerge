@echo off
REM Create directory for test results if it doesn't exist
if not exist TestResults\Coverage mkdir TestResults\Coverage

REM Run tests with coverage
dotnet test tests\RVToolsMerge.IntegrationTests\RVToolsMerge.IntegrationTests.csproj -c Release --no-restore --verbosity normal

REM Generate coverage report
dotnet tool install -g dotnet-reportgenerator-globaltool || echo "ReportGenerator is already installed"
reportgenerator -reports:".\TestResults\Coverage\coverage.cobertura.xml" -targetdir:".\TestResults\CoverageReport" -reporttypes:Html

REM Display coverage report path
echo Coverage report generated at .\TestResults\CoverageReport\index.html