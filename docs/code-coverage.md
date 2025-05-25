# Code Coverage for RVToolsMerge

The RVToolsMerge project uses coverlet and ReportGenerator to provide code coverage reports for the tests.

## Running Tests with Coverage

### Unix/Linux/macOS

```bash
# From the repository root
./run-coverage.sh
```

### Windows

```powershell
# From the repository root
.\run-coverage.bat
```

## Coverage Reports

After running the coverage scripts, you can find the generated HTML reports at:

```
./TestResults/CoverageReport/index.html
```

Open this file in your browser to view a detailed code coverage report, including:

- Summary of coverage metrics
- Coverage breakdown by assembly, namespace, and class
- Line-by-line coverage highlighting
- Trending information over time

## Understanding Coverage Reports

The coverage report includes several metrics:

- **Line Coverage**: Percentage of code lines that were executed during tests
- **Branch Coverage**: Percentage of code branches (if/else, switch cases) that were executed
- **Method Coverage**: Percentage of methods that were called during tests

The report color-codes covered and uncovered code to easily identify areas that need more test coverage.

## Improving Coverage

When adding new features or fixing bugs:

1. Run the coverage report before making changes to establish a baseline
2. Write tests that specifically target the new or modified code
3. Run the coverage report again to verify the new code is adequately covered
4. Pay special attention to branch coverage for complex conditionals

## Continuous Integration

The coverage report is also generated during CI builds, allowing you to track coverage metrics over time as the project evolves.