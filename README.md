# ktsu.Schema

A C# library for defining, managing, and editing data structure schemas with a rich type system, type-safe identifiers, and polymorphic JSON serialization.

[![NuGet](https://img.shields.io/nuget/v/ktsu.Schema)](https://www.nuget.org/packages/ktsu.Schema)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.md)

## Overview

**ktsu.Schema** lets you define structured data models programmatically or visually, then serialize them to `.schema.json` files. It provides a foundation for code generation, data validation, and tooling that needs to understand your data structures at a metadata level.

The solution contains three projects:

- **Schema** - Core library with schema definition types, a rich type system, and JSON serialization
- **Schema.Test** - Unit tests for the core library
- **SchemaEditor** - ImGui-based desktop application for visual schema editing

## Installation

Install via NuGet:

```shell
dotnet add package ktsu.Schema
```

Or add to your project file:

```xml
<PackageReference Include="ktsu.Schema" />
```

### Requirements

- .NET 7.0, 8.0, 9.0, or 10.0

## Quick Start

### Create a Schema

```csharp
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

// Create a new schema
var schema = new Schema();

// Add an enum
var roleEnum = schema.AddEnum("UserRole".As<EnumName>());
roleEnum?.TryAddValue("Admin".As<EnumValueName>());
roleEnum?.TryAddValue("User".As<EnumValueName>());
roleEnum?.TryAddValue("Guest".As<EnumValueName>());

// Add a class with typed members
var userClass = schema.AddClass("User".As<ClassName>());
if (userClass != null)
{
    var id = userClass.AddMember("Id".As<MemberName>());
    id?.SetType(new SchemaTypes.Int());

    var name = userClass.AddMember("Name".As<MemberName>());
    name?.SetType(new SchemaTypes.String());

    var email = userClass.AddMember("Email".As<MemberName>());
    email?.SetType(new SchemaTypes.String());

    var role = userClass.AddMember("Role".As<MemberName>());
    role?.SetType(new SchemaTypes.Enum { EnumName = "UserRole".As<EnumName>() });

    var createdAt = userClass.AddMember("CreatedAt".As<MemberName>());
    createdAt?.SetType(new SchemaTypes.DateTime());
}
```

### Work with Arrays and Object References

```csharp
// Add a Project class
var projectClass = schema.AddClass("Project".As<ClassName>());
var projectId = projectClass?.AddMember("Id".As<MemberName>());
projectId?.SetType(new SchemaTypes.Int());
var projectName = projectClass?.AddMember("Name".As<MemberName>());
projectName?.SetType(new SchemaTypes.String());

// Add an array of projects to the User class
var projects = userClass?.AddMember("Projects".As<MemberName>());
projects?.SetType(new SchemaTypes.Array
{
    ElementType = new SchemaTypes.Object { ClassName = "Project".As<ClassName>() },
    Container = "vector".As<ContainerName>()
});

// Use a keyed collection (map)
var projectsMap = userClass?.AddMember("ProjectsById".As<MemberName>());
projectsMap?.SetType(new SchemaTypes.Array
{
    ElementType = new SchemaTypes.Object { ClassName = "Project".As<ClassName>() },
    Container = "map".As<ContainerName>(),
    Key = "Id".As<MemberName>()
});
```

### Create a Schema from .NET Types

```csharp
// Generate a schema class from an existing .NET type using reflection
schema.AddClass(typeof(MyExistingClass));
```

### Query the Schema

```csharp
// Retrieve classes and enums
if (schema.TryGetClass("User".As<ClassName>(), out var userClass))
{
    foreach (var member in userClass.Members)
    {
        Console.WriteLine($"  {member.Name}: {member.Type.DisplayName}");
    }
}

if (schema.TryGetEnum("UserRole".As<EnumName>(), out var roleEnum))
{
    foreach (var value in roleEnum.Values)
    {
        Console.WriteLine($"  {value}");
    }
}
```

## Type System

The schema supports a rich set of types through the `SchemaTypes` class, all inheriting from `BaseType` with polymorphic JSON serialization via `System.Text.Json`:

| Category    | Types                            |
| ----------- | -------------------------------- |
| **Numeric** | `Int`, `Long`, `Float`, `Double` |
| **Text**    | `String`                         |
| **Logic**   | `Bool`                           |
| **Temporal**| `DateTime`, `TimeSpan`           |
| **Vectors** | `Vector2`, `Vector3`, `Vector4`  |
| **Colors**  | `ColorRGB`, `ColorRGBA`          |
| **Complex** | `Array`, `Object`, `Enum`        |
| **Special** | `None`                           |

Types expose classification properties like `IsPrimitive`, `IsNumeric`, `IsIntegral`, `IsDecimal`, `IsArray`, `IsObject`, `IsContainer`, and `IsBuiltIn`.

## Strong String Types

All identifiers use type-safe semantic string wrappers from `ktsu.Semantics.Strings` to prevent mixing up different kinds of names at compile time:

- `ClassName` - Schema class names
- `MemberName` - Class member names
- `EnumName` - Enumeration names
- `EnumValueName` - Enumeration value names
- `BaseTypeName` - Type names
- `ContainerName` - Container type names
- `DataSourceName` - Data source names
- `CodeGeneratorName` - Code generator names

Convert strings using the `.As<T>()` extension method: `"User".As<ClassName>()`

## JSON Serialization

Schemas serialize to JSON using `System.Text.Json` with polymorphic type discrimination. The `TypeName` property identifies concrete types:

```json
{
    "Classes": [
        {
            "Name": "User",
            "Members": [
                {
                    "Name": "Id",
                    "Type": { "TypeName": "Int" }
                },
                {
                    "Name": "Role",
                    "Type": { "TypeName": "Enum", "EnumName": "UserRole" }
                }
            ]
        }
    ],
    "Enums": [
        {
            "Name": "UserRole",
            "Values": [
                { "Name": "Admin" },
                { "Name": "User" },
                { "Name": "Guest" }
            ]
        }
    ]
}
```

After deserialization, call `schema.Reassociate()` to re-establish parent-child relationships.

## Schema Editor

The **SchemaEditor** is an ImGui-based desktop application for visually creating and editing `.schema.json` files.

### Running the Editor

```shell
dotnet run --project SchemaEditor
```

### Features

- Tree view for navigating classes, enums, data sources, and code generators
- Property panels for editing members and types
- Type selection dialogs
- Create, open, and save schema files
- Resizable split-panel layout with persistent settings

## Building

```shell
# Build the entire solution
dotnet build

# Run tests
dotnet test

# Build a specific project
dotnet build Schema/Schema.csproj
```

## Documentation

Detailed documentation is available in the [docs/](docs/) folder:

- [Getting Started](docs/getting-started.md) - Installation and first schema
- [Basic Schema Example](docs/examples/basic-schema.md) - Complete walkthrough
- [Architecture](docs/development/architecture.md) - Design patterns and data flow
- [Schema Editor Guide](docs/features/schema-editor.md) - Visual editor usage

## License

This project is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.
