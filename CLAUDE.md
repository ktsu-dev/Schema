# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Schema is a C# library for defining and managing data structure schemas. It consists of three projects:

- **Schema** - Core library providing schema definition types (classes, enums, members, types)
- **Schema.Test** - MSTest unit tests for the core library
- **SchemaEditor** - ImGui-based visual editor application for creating and editing `.schema.json` files

## Build Commands

```bash
dotnet build              # Build entire solution
dotnet test               # Run all tests
dotnet test --filter "FullyQualifiedName~TestName"  # Run specific test
dotnet run --project SchemaEditor  # Launch the visual editor
```

## Architecture

### Core Type Hierarchy

The type system uses polymorphic JSON serialization with `System.Text.Json`:

```
SchemaChild<TName> (base for top-level elements)
├── SchemaClass : SchemaChild<ClassName>
├── SchemaEnum : SchemaChild<EnumName>
├── DataSource : SchemaChild<DataSourceName>
└── SchemaCodeGenerator : SchemaChild<CodeGeneratorName>

SchemaMemberChild<TName> (base for member-level elements)
└── SchemaTypes.BaseType : SchemaMemberChild<BaseTypeName>
    ├── Primitives: Int, Long, Float, Double, String, Bool, DateTime, TimeSpan
    ├── Vectors: Vector2, Vector3, Vector4, ColorRGB, ColorRGBA
    └── Complex: Array, Object, Enum, None
```

### Semantic String Types

The library uses `ktsu.Semantics.Strings` for type-safe identifiers. Convert strings using `.As<T>()`:
- `ClassName`, `MemberName`, `EnumName`, `EnumValueName`, `BaseTypeName`, `ContainerName`

Example: `"User".As<ClassName>()`

### Parent-Child Association Pattern

Schema elements maintain parent references via `AssociateWith()` methods. After deserialization, `Schema.Reassociate()` re-establishes these relationships.

### Key Files

- `Schema/Models/Schema.cs` - Root container with CRUD operations for classes/enums
- `Schema/Models/Types/BaseType.cs` - Abstract base with `[JsonDerivedType]` attributes for polymorphic serialization
- `Schema/Models/SchemaClass.cs` - Class definitions containing `SchemaMember` collections
- `SchemaEditor/SchemaEditor.cs` - Main editor application using `ktsu.ImGui.App`

## Dependencies

- **ktsu.Semantics.Strings/Paths** - Type-safe string and path wrappers
- **ktsu.ImGui.App/Widgets/Popups** - ImGui application framework (editor only)
- **ktsu.AppDataStorage** - Persistent settings storage (editor only)
- **Polyfill** - .NET compatibility shims for multi-targeting

## Schema Files

Schema definitions are stored as `.schema.json` files using `System.Text.Json` with polymorphic type discriminators (`TypeName` property).
