# AGENTS.md - ProtoBufferParser Development Guide

This document provides coding agents with essential information about building, testing, and contributing to the ProtoBufferParser project.

## Project Overview

- **Language**: C# 12
- **Framework**: .NET 8.0
- **Project Type**: Console Application (Compiler)
- **Build System**: MSBuild via .NET CLI
- **IDE**: Visual Studio 2022

### Project Goal

This project is a **compiler that converts Google Protocol Buffer IDL files (.proto) to Unreal Engine C++ structs (.cpp)**.

- **Input**: `.proto` files (Protocol Buffer IDL)
- **Output**: `.h` and `.cpp` files (Unreal Engine compatible C++ structs)
- **Parser Technology**: ANTLR4 for lexical analysis and parsing
- **Target**: Generate Unreal Engine compatible data structures from protobuf definitions

### Key Features

- **Naming Convention**:
  - Struct: `FMessageNameProto` (no underscores)
  - Enum: `EEnumNameProto` (no underscores)
  - File: `MEMessageNameProto.h` / `MEMessageNameProto.cpp`
  - Nested: `FOuterInnerProto` (flattened, no underscores)

- **Marshaling**:
  - Each generated struct includes a constructor that accepts the corresponding Protocol Buffer message type
  - Automatic conversion from Proto → Unreal types (e.g., `string` → `FString`, `repeated` → `TArray`)
  - Example: `FPlayerProto(const ::Player& proto)`

## Build, Test, and Run Commands

### Building the Project

```bash
# Build in Debug configuration (default)
dotnet build

# Build in Release configuration
dotnet build -c Release

# Clean build outputs
dotnet clean

# Restore NuGet packages (usually automatic)
dotnet restore

# Build and run
dotnet run

# Run in Release mode
dotnet run -c Release
```

### Testing

Currently, there is **no test project** configured. To run tests when a test project is added:

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run a specific test
dotnet test --filter "FullyQualifiedName~Namespace.ClassName.TestMethodName"

# Run tests in a specific file/class
dotnet test --filter "FullyQualifiedName~Namespace.ClassName"

# Run tests by category/trait
dotnet test --filter "Category=Unit"
```

**Note**: When creating tests, use xUnit, NUnit, or MSTest. Create a separate test project (e.g., `ProtoBufferParser.Tests`).

### Code Analysis and Linting

```bash
# No linting tools currently configured
# To add analyzers, install NuGet packages like:
# - StyleCop.Analyzers
# - Microsoft.CodeAnalysis.NetAnalyzers
# - SonarAnalyzer.CSharp
```

## Code Style Guidelines

### General Principles

1. **Follow Microsoft C# Coding Conventions**: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
2. **Write clean, readable, and maintainable code**
3. **Prefer clarity over cleverness**
4. **Keep methods small and focused (Single Responsibility Principle)**

### Naming Conventions

- **PascalCase**: Classes, methods, properties, public fields, namespaces, enums
  - `public class ProtoParser { }`
  - `public void ParseMessage() { }`
  - `public string FileName { get; set; }`

- **camelCase**: Private fields (with underscore prefix), local variables, parameters
  - `private string _buffer;`
  - `private readonly ILogger _logger;`
  - `public void Process(string fileName) { }`

- **UPPER_CASE**: Constants
  - `private const int MAX_BUFFER_SIZE = 1024;`

- **Interfaces**: Prefix with `I`
  - `public interface IProtoParser { }`

### File Organization

```
ProtoBufferParser/
├── Models/           # Data models and DTOs
├── Services/         # Business logic
├── Parsers/          # Protocol buffer parsing logic
├── Utilities/        # Helper classes
├── Interfaces/       # Interface definitions
├── Extensions/       # Extension methods
└── Program.cs        # Entry point
```

### Imports and Using Directives

- **Implicit usings are enabled** (System, System.Collections.Generic, System.IO, System.Linq, System.Net.Http, System.Threading, System.Threading.Tasks are auto-imported)
- Place using directives at the top of the file, outside namespace declaration
- Order: System namespaces first, then third-party, then local
- Remove unused usings

```csharp
using System.Text;
using Google.Protobuf;
using ProtoBufferParser.Models;
```

### Formatting

- **Indentation**: 4 spaces (no tabs)
- **Braces**: Allman style (braces on new line)
```csharp
public void Method()
{
    if (condition)
    {
        // code
    }
}
```
- **Line length**: Prefer lines under 120 characters
- **Blank lines**: One blank line between methods, two between classes

### Types and Nullability

- **Nullable reference types are enabled** (`<Nullable>enable</Nullable>`)
- Use nullable annotations appropriately:
  - `string?` for nullable strings
  - `string` for non-nullable strings
- Initialize non-nullable properties in constructor or with default values
- Use null-coalescing operators: `??`, `??=`, `?.`

```csharp
public class Parser
{
    public string FileName { get; init; } = string.Empty;
    public string? OptionalField { get; set; }
    
    public void Process(string? input)
    {
        var value = input ?? "default";
    }
}
```

### Error Handling

- Use exceptions for exceptional conditions, not control flow
- Throw specific exception types, not generic `Exception`
- Use `ArgumentNullException.ThrowIfNull()` for parameter validation (.NET 8+)
- Catch specific exceptions, avoid empty catch blocks

```csharp
public void ParseFile(string filePath)
{
    ArgumentNullException.ThrowIfNull(filePath);
    
    if (!File.Exists(filePath))
    {
        throw new FileNotFoundException($"File not found: {filePath}");
    }
    
    try
    {
        // parsing logic
    }
    catch (IOException ex)
    {
        throw new InvalidOperationException("Failed to parse file", ex);
    }
}
```

### Async/Await

- Use async/await for I/O operations
- Suffix async methods with `Async`
- Always use `ConfigureAwait(false)` in library code (not required for console apps)
- Avoid `async void` except for event handlers

```csharp
public async Task<string> ReadFileAsync(string path)
{
    return await File.ReadAllTextAsync(path);
}
```

### Comments and Documentation

- Use XML documentation for public APIs
- Write comments for complex logic, not obvious code
- Keep comments up to date with code changes

```csharp
/// <summary>
/// Parses a protocol buffer message from the specified file.
/// </summary>
/// <param name="filePath">The path to the .proto file.</param>
/// <returns>A parsed message object.</returns>
/// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
public Message Parse(string filePath)
{
    // Complex parsing logic here
}
```

## Best Practices

1. **Prefer `var` when type is obvious** from right-hand side
2. **Use expression-bodied members** for simple properties/methods
3. **Use pattern matching** where appropriate
4. **Leverage LINQ** for collection operations
5. **Use `nameof()`** instead of string literals
6. **Dispose resources properly** with `using` statements or `IDisposable`
7. **Make classes `sealed` by default** unless designed for inheritance

## Project-Specific Notes

- This is a Protocol Buffer parser project
- Consider adding `Google.Protobuf` NuGet package for protobuf support
- When implementing, focus on clear parsing logic and error messages
- Structure code to handle various .proto file formats gracefully

## Before Committing

- Ensure code builds without errors: `dotnet build`
- Run all tests (when available): `dotnet test`
- Review changes for style consistency
- Update documentation if public APIs change
