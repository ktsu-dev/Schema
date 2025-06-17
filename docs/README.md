# Schema

**Version:** 1.3.2

A powerful .NET library for defining, managing, and manipulating document schemas with a visual editor. This library provides a comprehensive type system for creating strongly-typed data structures that can be serialized, validated, and code-generated.

## Overview

The Schema library enables developers to:

-   **Define Data Structures**: Create classes, enums, and complex types using a schema definition system
-   **Visual Editing**: Use the included SchemaEditor for graphical schema design
-   **Type Safety**: Leverage strong string types and compile-time type checking
-   **Code Generation**: Generate code from schema definitions
-   **Serialization**: Save and load schemas as JSON with full type information
-   **Data Sources**: Manage multiple data sources within a single schema

## Key Features

### 🏗️ **Schema Definition System**

-   Define classes with typed members
-   Create enumerations with named values
-   Support for primitive types (int, float, string, bool, etc.)
-   Complex types including arrays, objects, and containers
-   Vector types (Vector2, Vector3, Vector4) and color types (RGB, RGBA)

### 🎨 **Visual Schema Editor**

-   ImGui-based desktop application for schema editing
-   Tree-based navigation of schema elements
-   Interactive property editing
-   Real-time schema validation

### 🔧 **Type System**

-   Strong string types for compile-time safety
-   Polymorphic type system with JSON serialization support
-   Built-in and custom type support
-   Container types with keying support

### 📊 **Data Management**

-   Multiple data source support
-   Project-relative path management
-   Safe file operations with backup/recovery

## Quick Start

```csharp
// Load an existing schema
if (Schema.TryLoad("path/to/schema.json".As<AbsoluteFilePath>(), out Schema? schema))
{
    // Add a new class
    var userClass = schema.AddClass("User".As<ClassName>());

    // Add members to the class
    var nameMember = userClass?.AddMember("Name".As<MemberName>());
    var ageMember = userClass?.AddMember("Age".As<MemberName>());

    // Save the schema
    schema.Save();
}
```

## Project Structure

-   **[Schema](api/schema-core.md)** - Core library containing the schema definition system
-   **[SchemaEditor](features/schema-editor.md)** - Visual editor application
-   **[Schema.Test](development/testing.md)** - Test suite for the library

## Documentation

-   **[Getting Started](getting-started.md)** - Setup and basic usage
-   **[API Reference](api/)** - Detailed API documentation
-   **[Features](features/)** - In-depth feature guides
-   **[Examples](examples/)** - Code examples and tutorials
-   **[Development](development/)** - Contributing and development setup

## Dependencies

This project uses the ktsu.dev ecosystem of libraries:

-   `ktsu.StrongPaths` - Type-safe file and directory path handling
-   `ktsu.StrongStrings` - Strong string type system
-   `ktsu.ToStringJsonConverter` - JSON serialization utilities
-   `ktsu.ImGuiApp` - ImGui application framework (SchemaEditor)
-   `ktsu.ImGuiPopups` - ImGui popup utilities (SchemaEditor)
-   `ktsu.ImGuiWidgets` - ImGui widget library (SchemaEditor)

## License

Licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.

## Contributing

Contributions are welcome! Please see the [development guide](development/contributing.md) for details on how to contribute to this project.
