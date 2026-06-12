# API Reference

This section contains API documentation for the public types in the Schema library. All public types carry XML documentation comments in source, which IDEs surface directly; this section covers the core entry points.

## Core API

### [Schema Class](schema-core.md)

The main schema container class that manages classes, enums, data sources, and code generators, plus the `SchemaSerializer` used to persist schemas to JSON.

## Quick Reference

### Key Classes

| Class                 | Purpose                                       |
| --------------------- | --------------------------------------------- |
| `Schema`              | Main container for all schema definitions     |
| `SchemaClass`         | User-defined class with typed members         |
| `SchemaMember`        | Property within a schema class                |
| `SchemaEnum`          | Enumeration with named values                 |
| `DataSource`          | Binds a data file to a schema class           |
| `SchemaCodeGenerator` | Declares a code generation output             |
| `SchemaSerializer`    | JSON serialization for `Schema` objects       |

### Key Types

All types live in the `ktsu.Schema.Models.Types` namespace and derive from `BaseType`:

| Type                   | Description                 |
| ---------------------- | --------------------------- |
| `SchemaTypes.BaseType` | Abstract base for all types |
| `SchemaTypes.Int`      | 32-bit integer type         |
| `SchemaTypes.Long`     | 64-bit integer type         |
| `SchemaTypes.Float`    | Single-precision type       |
| `SchemaTypes.Double`   | Double-precision type       |
| `SchemaTypes.String`   | String type                 |
| `SchemaTypes.Bool`     | Boolean type                |
| `SchemaTypes.DateTime` | Date/time type              |
| `SchemaTypes.TimeSpan` | Duration type               |
| `SchemaTypes.Array`    | Array/collection type       |
| `SchemaTypes.Object`   | Reference to a schema class |
| `SchemaTypes.Enum`     | Reference to a schema enum  |
| `SchemaTypes.None`     | Unset/placeholder type      |

Vector (`Vector2`, `Vector3`, `Vector4`) and color (`ColorRGB`, `ColorRGBA`) types are also available.

### Semantic String Types

Identifiers use semantic string wrappers from `ktsu.Semantics.Strings` (namespace `ktsu.Schema.Models.Names`); convert with `"value".As<T>()`:

| Type                | Purpose                  |
| ------------------- | ------------------------ |
| `ClassName`         | Names of schema classes  |
| `MemberName`        | Names of class members   |
| `EnumName`          | Names of enumerations    |
| `EnumValueName`     | Names of enum values     |
| `BaseTypeName`      | Names of types           |
| `ContainerName`     | Names of container types |
| `DataSourceName`    | Names of data sources    |
| `CodeGeneratorName` | Names of code generators |

## API Conventions

### Naming

-   Classes, methods, and properties use PascalCase: `SchemaClass`, `AddClass()`, `Members`
-   Semantic string types end with their kind: `ClassName`, `MemberName`

### Error Handling

-   Methods that can fail use the `Try...` pattern returning `bool`
-   `Add...` methods return `null` when an element with the same name already exists
-   Null checking is enforced through nullable reference types

### Thread Safety

-   Schema objects are not thread-safe
-   Use appropriate synchronization when accessing schemas from multiple threads

### JSON Serialization

-   Use `SchemaSerializer.Serialize()` / `SchemaSerializer.TryDeserialize()`
-   camelCase property names with a `TypeName` polymorphic discriminator
-   `TryDeserialize()` calls `Reassociate()` automatically

## Navigation

-   **[Getting Started](../getting-started.md)** - Basic usage and setup
-   **[Features](../features/README.md)** - Feature guides and tutorials
-   **[Examples](../examples/README.md)** - Code examples and patterns
-   **[Development](../development/README.md)** - Contributing and development
