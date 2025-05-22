#!/bin/bash
echo "Running RVToolsMerge..."

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_PATH="$SCRIPT_DIR/src/RVToolsMerge/RVToolsMerge.csproj"

# Check if project file exists
if [ ! -f "$PROJECT_PATH" ]; then
    echo "Error: Could not find project file at $PROJECT_PATH"
    exit 1
fi

# Run the application
dotnet run --project "$PROJECT_PATH" "$@"
