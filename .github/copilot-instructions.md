# RVToolsMerge Copilot Coding Agent Instructions

## Project Overview

RVToolsMerge is a tool for combining and processing multiple RVTools exports from VMware environments. It deals with virtualization data, inventory management, and potentially large datasets from VMware infrastructures.

## Copilot Coding Agent Usage Guidelines

-   Copilot agent is enabled for this repository and may be assigned issues or pull requests for code, documentation, or test tasks.
-   The agent must follow all standards and best practices in this file and the main README.md.
-   Write clear, descriptive issues and PRs with context and acceptance criteria.
-   Use checklists for multi-step tasks.
-   Document code and design decisions in comments and PRs.
-   Review and test all Copilot agent contributions before merging.
-   Use Copilot agent for repetitive, boilerplate, or test code generation.
-   Reference and update this file and README.md if project standards change.

## Coding Standards

-   Follow consistent naming conventions:
    -   PascalCase for class names, interfaces, public members, constants, and type parameters
    -   camelCase for local variables, parameters, and private fields
    -   Prefix private fields with underscore (\_fieldName)
    -   Use meaningful, descriptive names that represent purpose
    -   Avoid abbreviations unless widely recognized
    -   Use verb-noun pairs for method names (e.g., GetData, ProcessFile)
    -   Use nouns for property names
    -   Include 'Async' suffix for async methods
    -   Use 'I' prefix for interfaces
-   Include XML documentation comments for public methods, classes, and interfaces
-   Keep methods focused on single responsibilities with < 30 lines when possible
-   Format code consistently:
    -   Use 4 spaces for indentation (not tabs)
    -   Keep line length reasonable (< 120 characters)
    -   Use parentheses to clarify code intent even when optional
    -   Add spaces after keywords (if, for, while) but not after method names
-   Prefer LINQ for data manipulation when appropriate
-   Use proper exception handling with specific exception types
-   Validate all inputs, especially file paths and user inputs
-   Use expression-bodied members for simple one-line methods and properties
-   Organize using directives alphabetically and remove unused ones
-   Use file-scoped namespaces
-   Explicitly specify access modifiers (public, private, etc.)
-   Use `var` when the type is obvious from the right side of assignment
-   Make sure that UI elements are separated from business logic and are in different files
-   Make sure that source files are organized in a way that makes sense for the project

## Excel Processing

-   Assume RVTools exports are in Excel format
-   Use ClosedXML for Excel file manipulation
-   Be aware that ClosedXML is single-threaded, so plan parallel processing strategies accordingly
-   Implement thread synchronization when multiple threads need access to ClosedXML operations
-   Implement proper data validation for RVTools-specific data formats
-   Handle Excel-specific exceptions gracefully (e.g., file not found, invalid format)

## VMware Specific Considerations

-   Be familiar with common RVTools export tabs (vInfo, vCPU, vMemory, vDisk, etc.)
-   Understand common VMware inventory objects (VMs, Hosts, Clusters, Datastores)
-   Preserve relationships between different VMware objects when merging data
-   Account for different vCenter versions potentially having different export formats

## Performance Guidelines

-   Optimize for large dataset processing
-   Consider parallel processing for CPU-intensive operations
-   Implement progress reporting for long-running operations
-   Cache results where appropriate to prevent redundant processing

## Testing Requirements

-   Write unit tests for all core functionality
-   Create integration tests for file processing capabilities
-   Test with various RVTools export versions and formats
-   Include edge cases like malformed files, missing data, and extremely large datasets

## User Experience

-   Provide clear, friendly, and professional error messages that:
    -   Use non-technical language for end-user facing errors
    -   Explain what happened in simple terms
    -   Suggest specific actions to resolve the issue
    -   Avoid blaming the user
    -   Maintain a consistent, professional tone
    -   Include error codes for technical support when appropriate
    -   Log detailed technical information separately from user-facing messages
-   Include progress indicators for long-running operations
-   Design intuitive interactive console UI for file selection and merge configuration
-   Create comprehensive logs for troubleshooting
-   Optimize for interactive user engagement and feedback
-   Provide immediate feedback for all user actions
-   Implement clear navigation between different interactive steps

## .NET and C# Best Practices

### General C# Guidelines

-   Use the latest C# language features (pattern matching, nullable reference types, records, etc.)
-   Embrace `null` handling with nullable reference types (`string?`, `int?`, etc.)
-   Use pattern matching for type checking and data extraction
-   Prefer init-only properties for immutable data
-   Leverage records for data objects, especially DTOs
-   Use `is not null` instead of `!= null` for null checks
-   Utilize C# attributes effectively (e.g., `[Required]`, `[JsonProperty]`)
-   Use interpolated strings (`$"..."`) rather than string concatenation
-   Prefer pattern-based `switch` expressions over traditional `switch` statements
-   Use lowercase `string` for type declarations and static method calls (`string.Join`, `string.IsNullOrEmpty`, `string.Empty`) following Microsoft's C# coding conventions
-   Use minimal APIs where appropriate for simple endpoints
-   Leverage source generators when applicable
-   Prefer `IOptions<T>` pattern for configuration
-   Use `System.Text.Json` for JSON processing unless Newtonsoft offers needed features
-   Take advantage of performance improvements in collection types

### Async Programming

-   Use async/await consistently through the entire call stack
-   Avoid mixing synchronous and asynchronous code
-   Prefer `Task.WhenAll` for parallel task execution
-   Understand and properly handle cancellation tokens
-   Leverage `IAsyncEnumerable<T>` for streamed data processing
-   Avoid async void except for event handlers

### File and I/O Operations

-   Use `System.IO.Abstractions` for better testability
-   Prefer async file operations (`ReadAllTextAsync`, etc.)
-   Use `Path.Combine()` for path manipulation instead of string concatenation
-   Implement appropriate file locking strategies
-   Use `Stream` properly with `using` statements or `using` declarations

### Console Application Best Practices

-   Use .NET 9 features and capabilities throughout the application
-   Take advantage of .NET 9's performance improvements for Excel processing
-   Utilize .NET 9's improved JSON serialization for configuration files
-   Always use Spectre.Console for all console output, following official best practices:
    -   Use `AnsiConsole.Write` instead of `AnsiConsole.Markup` for better performance
    -   Consider `Live` displays for dynamic content that changes frequently
    -   Utilize `Prompt<T>` for type-safe user input with validation
    -   Implement proper exception handling via `SafeExecution` extensions
    -   Avoid repeated color markup in loops by pre-building strings
    -   Use the Status API for long-running operations with indeterminate progress
    -   Leverage word wrapping for better text display on different terminal sizes
    -   Use `ProgressContext.LoggerTask` for consistent progress tracking
    -   Create custom themes to maintain visual consistency
    -   Implement proper graceful exit with `IDisposable` patterns for live displays
    -   Utilize `Grid` and `Layout` for complex console layouts rather than hard-coded positioning
    -   Create rich, colorful tables for displaying data
    -   Use progress bars for long-running operations
    -   Implement status spinners for operations with indeterminate duration
    -   Display file trees for directory navigation
    -   Create panels and layouts for organized information display
    -   Use FigletText for application branding/header
-   Create a clean separation between the interactive UI code and business logic
-   Focus exclusively on interactive console experiences
-   Implement a clear, step-by-step workflow for users to follow
-   Use interactive prompts for all user inputs rather than command-line arguments
-   Create intuitive menus for operation selection
-   Implement contextual help within the interactive interface
-   Provide immediate validation feedback for user inputs
-   Allow users to navigate back to previous steps when appropriate
-   Save user preferences for repeated use
-   Include options for specifying input/output Excel file paths through interactive selection
-   Implement proper Excel-specific file validation before processing
-   Use wizard-like patterns for complex multi-step operations
-   Ensure all operations provide clear progress feedback to the user
-   Test console layouts and UI elements on different terminal sizes and resolutions

## Cross-Platform Compatibility

-   Ensure the application runs properly on Windows, Linux, and macOS
-   Use `Path.DirectorySeparatorChar` instead of hardcoded path separators (`\` or `/`)
-   Use `Environment.NewLine` instead of hardcoded newline characters
-   Consider terminal size differences between platforms for UI layouts
-   Test color schemes on different terminal types (cmd.exe, PowerShell, Terminal, iTerm, etc.)
-   Use `RuntimeInformation.IsOSPlatform()` to detect and handle platform-specific behavior
-   Implement platform-specific file path validation
-   Be mindful of file system case sensitivity differences (Linux/macOS are case-sensitive, Windows is not)
-   Use cross-platform file locking strategies
-   Handle different environment variable conventions across platforms
-   Ensure all file operations use proper cross-platform APIs
-   Test application on all target platforms before release
-   Package the application appropriately for each platform:
    -   Windows: Self-contained executable or installer
    -   macOS: Application bundle or homebrew package
    -   Linux: AppImage, deb/rpm package, or snap/flatpak
-   Implement correct file permission handling for Unix-based systems
-   Use Environment.SpecialFolder for accessing platform-specific folders
-   Respect platform-specific UI conventions and keyboard shortcuts
-   Consider adding platform detection for optimal default settings
-   Use .NET's built-in cross-platform capabilities for file system operations
-   Handle platform-specific terminal resizing events properly
-   Test keyboard input handling across different platforms and terminals

### Security Best Practices

-   Never store sensitive data in plain text
-   Use `Microsoft.Identity` libraries for authentication
-   Implement proper input sanitization
-   Use parameterized queries for any database operations
-   Follow OWASP guidelines for secure application development

## GitHub Copilot Collaboration

### Effective Prompting

-   Start files with clear, descriptive comments about functionality and purpose
-   Add comments before complex logic explaining what you're trying to achieve
-   Use XML documentation comments on interfaces to guide implementation suggestions
-   Include expected input/output examples in comments for complex algorithms
-   Write descriptive method signatures with proper parameter names before implementation
-   Start comments with "I want to..." or "I need to..." to guide Copilot's focus
-   Use TODO comments to get suggestions for specific implementation details
-   When stuck, write a comment describing the problem you're trying to solve

### Code Structure for Better Suggestions

-   Organize code with logical regions and groupings
-   Create descriptive method and class names that clearly indicate purpose
-   Include key imports/using statements at the file top to establish context
-   Implement interfaces or base classes first to guide derived class suggestions
-   Break complex functions into smaller, well-named helper methods
-   Maintain consistent patterns across similar components
-   Use design patterns consistently and explicitly name them in comments

### Documentation & Examples

-   Comment edge cases and their expected handling
-   Document performance considerations directly in code
-   Include sample usage patterns in class-level documentation
-   Reference related classes or patterns in comments for context
-   Explain "why" not just "what" for non-obvious design decisions
-   Use inline examples for complex transformations or business rules
-   Add links to relevant documentation or standards in comments

### Pair Programming with Copilot

-   Write test cases first to guide implementation suggestions
-   Create clear interfaces before implementations
-   Use a test-driven approach by describing expected behavior in tests
-   Split large files into smaller, focused components
-   Start complex algorithms with pseudocode comments
-   Scaffold class structures with properties and method signatures before implementations
-   Be explicit about exception handling requirements in comments
-   Clearly comment threading and async requirements
-   Provide example input/output data for data transformation logic
