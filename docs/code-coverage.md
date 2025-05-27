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

The coverage report is generated during GitHub Actions workflows:

1. **Dedicated Coverage Workflow**: A standalone workflow specifically for detailed coverage reporting

### Coverage Badges

[![Code Coverage](https://github.com/sbroenne/RVToolsMerge/raw/gh-pages/badges/coverage.svg)](https://github.com/sbroenne/RVToolsMerge/actions/workflows/code-coverage.yml)
[![Alternative Coverage](https://img.shields.io/badge/coverage-check%20report-brightgreen)](https://github.com/sbroenne/RVToolsMerge/actions/workflows/code-coverage.yml)

Coverage badges are automatically generated and updated when code is pushed to the main branch.

### PR Coverage Comments

When you create a PR, the coverage workflow will automatically comment with a coverage summary, helping reviewers understand the impact of your changes on test coverage.

### Manually Triggering Coverage Reports

You can manually trigger the dedicated coverage workflow from the "Actions" tab in GitHub by:

1. Selecting the "Code Coverage Report" workflow
2. Clicking "Run workflow"
3. Optionally enabling "Generate full detailed report" for more comprehensive output
4. Clicking "Run workflow" again

The generated reports will be available as artifacts in the workflow run.