# ktsu.Schema

A C# library for defining, managing, and editing data structure schemas with a rich type system, type-safe identifiers, and polymorphic JSON serialization.

[![License](https://img.shields.io/github/license/ktsu-dev/Schema.svg?label=License&logo=nuget)](LICENSE.md)
[![NuGet Version](https://img.shields.io/nuget/v/ktsu.Schema?label=Stable&logo=nuget)](https://nuget.org/packages/ktsu.Schema)
[![NuGet Version](https://img.shields.io/nuget/vpre/ktsu.Schema?label=Latest&logo=nuget)](https://nuget.org/packages/ktsu.Schema)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ktsu.Schema?label=Downloads&logo=nuget)](https://nuget.org/packages/ktsu.Schema)
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/ktsu-dev/Schema?label=Commits&logo=github)](https://github.com/ktsu-dev/Schema/commits/main)
[![GitHub contributors](https://img.shields.io/github/contributors/ktsu-dev/Schema?label=Contributors&logo=github)](https://github.com/ktsu-dev/Schema/graphs/contributors)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/ktsu-dev/Schema/dotnet.yml?branch=main&label=Build&logo=github)](https://github.com/ktsu-dev/Schema/actions)

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

- .NET 8.0, 9.0, or 10.0

## Quick Start

### Create a Schema

```csharp
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;
using SchemaTypes = ktsu.Schema.Models.Types;

// Create a new schema
Schema schema = new();

// Add an enum
SchemaEnum? roleEnum = schema.AddEnum("UserRole".As<EnumName>());
roleEnum?.TryAddValue("Admin".As<EnumValueName>());
roleEnum?.TryAddValue("User".As<EnumValueName>());
roleEnum?.TryAddValue("Guest".As<EnumValueName>());

// Add a class with typed members
SchemaClass? userClass = schema.AddClass("User".As<ClassName>());
if (userClass != null)
{
    SchemaMember? id = userClass.AddMember("Id".As<MemberName>());
    id?.SetType(new SchemaTypes.Int());

    SchemaMember? name = userClass.AddMember("Name".As<MemberName>());
    name?.SetType(new SchemaTypes.String());

    SchemaMember? email = userClass.AddMember("Email".As<MemberName>());
    email?.SetType(new SchemaTypes.String());

    SchemaMember? role = userClass.AddMember("Role".As<MemberName>());
    role?.SetType(new SchemaTypes.Enum { EnumName = "UserRole".As<EnumName>() });

    SchemaMember? createdAt = userClass.AddMember("CreatedAt".As<MemberName>());
    createdAt?.SetType(new SchemaTypes.DateTime());
}
```

### Work with Arrays and Object References

```csharp
// Add a Project class
SchemaClass? projectClass = schema.AddClass("Project".As<ClassName>());
SchemaMember? projectId = projectClass?.AddMember("Id".As<MemberName>());
projectId?.SetType(new SchemaTypes.Int());
SchemaMember? projectName = projectClass?.AddMember("Name".As<MemberName>());
projectName?.SetType(new SchemaTypes.String());

// Add an array of projects to the User class
SchemaMember? projects = userClass?.AddMember("Projects".As<MemberName>());
projects?.SetType(new SchemaTypes.Array
{
    ElementType = new SchemaTypes.Object { ClassName = "Project".As<ClassName>() },
    Container = "vector".As<ContainerName>()
});

// Use a keyed collection (map)
SchemaMember? projectsMap = userClass?.AddMember("ProjectsById".As<MemberName>());
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

### Serialize and Deserialize

```csharp
// Serialize to JSON
string json = SchemaSerializer.Serialize(schema);

// Deserialize from JSON (automatically calls Reassociate())
if (SchemaSerializer.TryDeserialize(json, out Schema? loaded))
{
    Console.WriteLine($"Loaded {loaded.Classes.Count} classes");
}
```

### Query the Schema

```csharp
// Retrieve classes and enums
if (schema.TryGetClass("User".As<ClassName>(), out SchemaClass? foundClass))
{
    foreach (SchemaMember member in foundClass.Members)
    {
        Console.WriteLine($"  {member.Name}: {member.Type.DisplayName}");
    }
}

if (schema.TryGetEnum("UserRole".As<EnumName>(), out SchemaEnum? foundEnum))
{
    foreach (EnumValueName value in foundEnum.Values)
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

Use `SchemaSerializer` for JSON serialization with `System.Text.Json`. The serializer uses camelCase property names and polymorphic type discrimination via the `TypeName` discriminator:

```json
{
  "classes": [
    {
      "name": "User",
      "description": "",
      "members": [
        {
          "name": "Id",
          "description": "",
          "type": { "TypeName": "Int" },
          "memberDescription": ""
        },
        {
          "name": "Role",
          "description": "",
          "type": { "TypeName": "Enum", "enumName": "UserRole" },
          "memberDescription": ""
        }
      ]
    }
  ],
  "enums": [
    {
      "name": "UserRole",
      "description": "",
      "values": ["Admin", "User", "Guest"]
    }
  ],
  "dataSources": [],
  "codeGenerators": []
}
```

`SchemaSerializer.TryDeserialize()` automatically calls `Reassociate()` to re-establish parent-child relationships after deserialization.

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

Building the solution requires the **.NET SDK 10.0.400 or newer**. This is the floor
pinned in `global.json`: the analyzers shipped by `ktsu.Sdk` are built against the
Roslyn version in that feature band, and an older 10.0.x SDK fails the build with
`CS9057` before compiling any source. An SDK newer than the floor is fine —
`global.json` sets `rollForward: latestFeature`.

```shell
# Build the entire solution
dotnet build

# Run tests
dotnet test

# Build a specific project
dotnet build Schema/Schema.csproj
```

## License

This project is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.
