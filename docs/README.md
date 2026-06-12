# Schema

A .NET library for defining, managing, and editing data structure schemas with a rich type system, type-safe identifiers, and polymorphic JSON serialization.

## Overview

The Schema library enables developers to:

-   **Define Data Structures**: Create classes, enums, and complex types using a schema definition system
-   **Visual Editing**: Use the included SchemaEditor for graphical schema design
-   **Type Safety**: Leverage semantic string types and compile-time type checking
-   **Serialization**: Save and load schemas as JSON with full type information
-   **Data Sources**: Declare which data files are bound to which schema classes

## Key Features

### 🏗️ **Schema Definition System**

-   Define classes with typed members
-   Create enumerations with named values
-   Support for primitive types (int, long, float, double, string, bool, etc.)
-   Complex types including arrays, objects, and containers
-   Vector types (Vector2, Vector3, Vector4) and color types (RGB, RGBA)

### 🎨 **Visual Schema Editor**

-   ImGui-based desktop application for schema editing
-   Tree-based navigation of schema elements
-   Interactive property editing

### 🔧 **Type System**

-   Semantic string types for compile-time safety
-   Polymorphic type system with JSON serialization support
-   Container types with keying support

## Quick Start

```csharp
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using SchemaTypes = ktsu.Schema.Models.Types;

// Create a schema and add a class
Schema schema = new();
SchemaClass? userClass = schema.AddClass("User".As<ClassName>());
userClass?.AddMember("Name".As<MemberName>())?.SetType(new SchemaTypes.String());
userClass?.AddMember("Age".As<MemberName>())?.SetType(new SchemaTypes.Int());

// Serialize to JSON and save
string json = SchemaSerializer.Serialize(schema);
File.WriteAllText("user.schema.json", json);

// Load it back (Reassociate() is called automatically)
if (SchemaSerializer.TryDeserialize(File.ReadAllText("user.schema.json"), out Schema? loaded))
{
    Console.WriteLine($"Loaded {loaded!.Classes.Count} classes");
}
```

## Project Structure

-   **[Schema](api/schema-core.md)** - Core library containing the schema definition system
-   **[SchemaEditor](features/schema-editor.md)** - Visual editor application
-   **Schema.Test** - MSTest suite for the library (see the [development guide](development/README.md))

## Documentation

-   **[Getting Started](getting-started.md)** - Setup and basic usage
-   **[API Reference](api/README.md)** - Detailed API documentation
-   **[Features](features/README.md)** - In-depth feature guides
-   **[Examples](examples/README.md)** - Code examples and tutorials
-   **[Development](development/README.md)** - Contributing and development setup
-   **[Roadmap](ROADMAP.md)** - Current state and planned work

## Dependencies

This project uses the ktsu.dev ecosystem of libraries:

-   `ktsu.Semantics.Strings` - Type-safe semantic string wrappers
-   `ktsu.Semantics.Paths` - Type-safe file and directory path handling
-   `ktsu.RoundTripStringJsonConverter` - JSON serialization for semantic strings
-   `ktsu.ImGui.App` / `ktsu.ImGui.Widgets` / `ktsu.ImGui.Popups` - ImGui application framework (SchemaEditor)
-   `ktsu.AppDataStorage` - Persistent settings storage (SchemaEditor)

## License

Licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.

## Contributing

Contributions are welcome! Please see the [development guide](development/README.md) for details on how to contribute to this project.
