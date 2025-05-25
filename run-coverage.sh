#!/bin/bash

# Create directory for test results if it doesn't exist
mkdir -p TestResults/Coverage

# Run tests with coverage
dotnet test tests/RVToolsMerge.IntegrationTests/RVToolsMerge.IntegrationTests.csproj -c Release --no-restore --verbosity normal

# Generate coverage report
dotnet tool install -g dotnet-reportgenerator-globaltool || true
reportgenerator -reports:"./TestResults/Coverage/coverage.cobertura.xml" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html

# Display coverage report path
echo "Coverage report generated at ./TestResults/CoverageReport/index.html"