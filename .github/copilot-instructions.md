# RVToolsMerge Copilot Instructions

## Project Overview
RVToolsMerge is a tool for combining and processing multiple RVTools exports from VMware environments. When providing assistance with this project, understand that it deals with virtualization data, inventory management, and potentially large datasets from VMware infrastructures.

## Coding Standards
- Follow consistent naming conventions:
  - PascalCase for class names, interfaces, public members, constants, and type parameters
  - camelCase for local variables, parameters, and private fields
  - Prefix private fields with underscore (_fieldName)
  - Use meaningful, descriptive names that represent purpose
  - Avoid abbreviations unless widely recognized
  - Use verb-noun pairs for method names (e.g., GetData, ProcessFile)
  - Use nouns for property names
  - Include 'Async' suffix for async methods
  - Use 'I' prefix for interfaces
- Include XML documentation comments for public methods, classes, and interfaces
- Keep methods focused on single responsibilities with < 30 lines when possible
- Format code consistently:
  - Use 4 spaces for indentation (not tabs)
  - Keep line length reasonable (< 120 characters)
  - Use parentheses to clarify code intent even when optional
  - Add spaces after keywords (if, for, while) but not after method names
- Prefer LINQ for data manipulation when appropriate
- Use proper exception handling with specific exception types
- Validate all inputs, especially file paths and user inputs
- Use expression-bodied members for simple one-line methods and properties
- Organize using directives alphabetically and remove unused ones
- Use file-scoped namespaces
- Explicitly specify access modifiers (public, private, etc.)
- Prefer string interpolation over concatenation
- Use `var` when the type is obvious from the right side of assignment

## Excel Processing
- Assume RVTools exports are in Excel format
- Use ClosedXML for Excel file manipulation
- Be aware that ClosedXML is single-threaded, so plan parallel processing strategies accordingly
- Implement thread synchronization when multiple threads need access to ClosedXML operations
- Implement proper data validation for RVTools-specific data formats
- Handle Excel-specific exceptions gracefully (e.g., file not found, invalid format)
## VMware Specific Considerations
- Be familiar with common RVTools export tabs (vInfo, vCPU, vMemory, vDisk, etc.)
- Understand common VMware inventory objects (VMs, Hosts, Clusters, Datastores)
- Preserve relationships between different VMware objects when merging data
- Account for different vCenter versions potentially having different export formats

## Performance Guidelines
- Optimize for large dataset processing
- Consider parallel processing for CPU-intensive operations
- Implement progress reporting for long-running operations
- Cache results where appropriate to prevent redundant processing

## Testing Requirements
- Write unit tests for all core functionality
- Create integration tests for file processing capabilities
- Test with various RVTools export versions and formats
- Include edge cases like malformed files, missing data, and extremely large datasets

## User Experience
- Provide clear, friendly, and professional error messages that:
  - Use non-technical language for end-user facing errors
  - Explain what happened in simple terms
  - Suggest specific actions to resolve the issue
  - Avoid blaming the user
  - Maintain a consistent, professional tone
  - Include error codes for technical support when appropriate
  - Log detailed technical information separately from user-facing messages
- Include progress indicators for long-running operations
- Design intuitive UI for file selection and merge configuration
- Create comprehensive logs for troubleshooting

## .NET and C# Best Practices

### General C# Guidelines
- Use the latest C# language features (pattern matching, nullable reference types, records, etc.)
- Embrace `null` handling with nullable reference types (`string?`, `int?`, etc.)
- Use pattern matching for type checking and data extraction
- Prefer init-only properties for immutable data
- Leverage records for data objects, especially DTOs
- Use `is not null` instead of `!= null` for null checks
- Utilize C# attributes effectively (e.g., `[Required]`, `[JsonProperty]`)
- Use interpolated strings (`$"..."`) rather than string concatenation
- Prefer pattern-based `switch` expressions over traditional `switch` statements
- use string.Join instead of string.join

### .NET Core/6+ Specific
- Use minimal APIs where appropriate for simple endpoints
- Leverage source generators when applicable
- Prefer `IOptions<T>` pattern for configuration
- Use `System.Text.Json` for JSON processing unless Newtonsoft offers needed features
- Take advantage of performance improvements in collection types

### Async Programming
- Use async/await consistently through the entire call stack
- Avoid mixing synchronous and asynchronous code
- Prefer `Task.WhenAll` for parallel task execution
- Use `ConfigureAwait(false)` in library code
- Understand and properly handle cancellation tokens
- Leverage `IAsyncEnumerable<T>` for streamed data processing
- Avoid async void except for event handlers

### File and I/O Operations
- Use `System.IO.Abstractions` for better testability
- Prefer async file operations (`ReadAllTextAsync`, etc.)
- Use `Path.Combine()` for path manipulation instead of string concatenation
- Implement appropriate file locking strategies
- Use `Stream` properly with `using` statements or `using` declarations

### Console Application Best Practices
- Use .NET 9 features and capabilities throughout the application
- Take advantage of .NET 9's performance improvements for Excel processing
- Utilize .NET 9's improved JSON serialization for configuration files
- Always use Spectre.Console for all console output, following official best practices:
  - Use `AnsiConsole.Write` instead of `AnsiConsole.Markup` for better performance
  - Consider `Live` displays for dynamic content that changes frequently
  - Utilize `Prompt<T>` for type-safe user input with validation
  - Implement proper exception handling via `SafeExecution` extensions
  - Avoid repeated color markup in loops by pre-building strings
  - Use the Status API for long-running operations with indeterminate progress
  - Leverage word wrapping for better text display on different terminal sizes
  - Use `ProgressContext.LoggerTask` for consistent progress tracking
  - Create custom themes to maintain visual consistency
  - Implement proper graceful exit with `IDisposable` patterns for live displays
  - Utilize `Grid` and `Layout` for complex console layouts rather than hard-coded positioning
  - Create rich, colorful tables for displaying data
  - Use progress bars for long-running operations
  - Implement status spinners for operations with indeterminate duration
  - Display file trees for directory navigation
  - Create panels and layouts for organized information display
  - Use FigletText for application branding/header
- Create a clean separation between the command interface and business logic
- Design for non-interactive automation scenarios with appropriate exit codes
- Implement verbose logging options that can be toggled at runtime
- Consider support for config files alongside command-line arguments
- Provide clear help documentation accessible via command-line flags
- Include options for specifying input/output Excel file paths
- Support batch processing of multiple Excel files in a directory
- Implement proper Excel-specific file validation before processing

### Security Best Practices
- Never store sensitive data in plain text
- Use `Microsoft.Identity` libraries for authentication
- Implement proper input sanitization
- Use parameterized queries for any database operations
- Follow OWASP guidelines for secure application development
