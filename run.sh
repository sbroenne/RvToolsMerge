#!/bin/bash
echo "Running RVToolsMerge..."

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_PATH="$SCRIPT_DIR/src/RVToolsMerge/RVToolsMerge.csproj"

# Check if project file exists
if [ ! -f "$PROJECT_PATH" ]; then
    echo "Error: Could not find project file at $PROJECT_PATH"
    echo "Please make sure you are running this script from the root directory of the RVToolsMerge project."
    exit 1
fi

# Check if dotnet is installed
if ! command -v dotnet &>/dev/null; then
    echo "Error: The .NET SDK is not installed or not in your PATH."
    echo "Please install the .NET SDK from https://dotnet.microsoft.com/download"
    exit 1
fi

# Run the application
dotnet run --project "$PROJECT_PATH" "$@"

# Note: Make sure this file has Unix line endings (LF, not CRLF)
# and has executable permissions set: chmod +x run.sh
