# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Schema is a C# library for defining and managing data structure schemas. It consists of five projects:

- **Schema** - Core library providing schema definition types (classes, enums, members, types)
- **Schema.Test** - MSTest unit tests for the core library
- **SchemaEditor** - ImGui-based visual editor application for creating and editing `.schema.json` files
- **SchemaEditor.Test** - Headless UI tests for the editor, driven through `ktsu.ImGui.App.Testing`
- **SchemaTool** - Command line entry point for validating schemas and running their code generators

## Build Commands

```bash
dotnet build              # Build entire solution
dotnet test               # Run all tests
dotnet test --filter "FullyQualifiedName~TestName"  # Run specific test
dotnet run --project SchemaEditor  # Launch the visual editor
dotnet run --project SchemaTool -- generate my.schema.json  # Run a schema's code generators
```

## Architecture

### Core Type Hierarchy

The type system uses polymorphic JSON serialization with `System.Text.Json`:

```
SchemaChild<TName> (base for named elements)
├── SchemaClass : SchemaChild<ClassName>
├── SchemaEnum : SchemaChild<EnumName>
├── DataSource : SchemaChild<DataSourceName>
├── SchemaCodeGenerator : SchemaChild<CodeGeneratorName>
└── SchemaClassChild<TName> : SchemaChild<TName>
    └── SchemaMember : SchemaClassChild<MemberName>

BaseType (types, in ktsu.Schema.Models.Types)
├── Primitives: Int, Long, Float, Double, String, Bool, DateTime, TimeSpan
├── Vectors: Vector2, Vector3, Vector4, ColorRGB, ColorRGBA
└── Complex: Array, Object, Enum, None
```

A type is not a named child of the schema: it has no name or description of its own and exists only
as the type of the member holding it. `BaseType.TypeName` reports which type it is, and is the same
value written as the file's `TypeName` discriminator.

### Contracts

`ktsu.Schema.Contracts` is the abstraction seam the models implement: `Schema : ISchema`,
`SchemaClass : ISchemaClass`, `SchemaMember : ISchemaMember`, `SchemaEnum : ISchemaEnum`,
`BaseType : ISchemaType`. Inject `ISchema` where a consumer only defines and reads schema elements.

Entities are abstracted; values are not. Name types (`ClassName`, `MemberName`, …) and
`SchemaChildDescription` appear in the contracts as themselves — a semantic string is already an
abstraction over `string`, and wrapping it again would make `ISchemaChildSet<out TValue, TName>`
unusable, since a covariant element type cannot coexist with a varying name type.

Collections on the contracts are read-only views (`ISchemaChildSet`). Mutation lives on the owning
element (`ISchema.AddClass`, `ISchemaClass.AddMember`), which is what enforces name uniqueness and
establishes parent association.

### Semantic String Types

The library uses `ktsu.Semantics.Strings` for type-safe identifiers. Convert strings using `.As<T>()`:
- `ClassName`, `MemberName`, `EnumName`, `EnumValueName`, `BaseTypeName`, `ContainerName`, `DataSourceName`, `CodeGeneratorName`

Example: `"User".As<ClassName>()`

### Parent-Child Association Pattern

Schema elements maintain parent references via `AssociateWith()` methods. After deserialization, `Schema.Reassociate()` re-establishes these relationships.

### Key Files

- `Schema/Contracts/` - The `ISchema` abstraction seam implemented by the models
- `Schema/Models/Schema.cs` - Root container with CRUD operations for classes/enums
- `Schema/Models/SchemaChildSet.cs` - Order-preserving, name-unique view owning the uniqueness rule
- `Schema/Models/Types/BaseType.cs` - Abstract base with `[JsonDerivedType]` attributes for polymorphic serialization
- `Schema/Models/SchemaClass.cs` - Class definitions containing `SchemaMember` collections
- `SchemaEditor/SchemaEditor.cs` - Main editor application using `ktsu.ImGui.App`
- `SchemaEditor/EditorHost.cs` - Builds the `ImGuiAppConfig`; `CreateConfig` is what the tests drive too
- `SchemaEditor/EditorTheme.cs` - The ktsu.ThemeProvider theme, and the one definition of how a validation issue is coloured
- `SchemaEditor/Program.cs` - The entry point, and the only file excluded from coverage measurement
- `SchemaEditor.Test/EditorHarness.cs` - Runs a real editor headlessly, frames advanced by the test
- `SchemaEditor.Test/WidgetHarness.cs` - A headless frame containing only the widget under test

## Dependencies

- **ktsu.Semantics.Strings/Paths** - Type-safe string and path wrappers
- **ktsu.ImGui.App/Widgets/Popups** - ImGui application framework (editor only)
- **ktsu.AppDataStorage** - Persistent settings storage (editor only)
- **Polyfill** - .NET compatibility shims for multi-targeting

## Schema Files

Schema definitions are stored as `.schema.json` files using `System.Text.Json` with polymorphic type discriminators (`TypeName` property).
