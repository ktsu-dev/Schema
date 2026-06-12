# Features Guide

This section covers the key features and capabilities of the Schema library.

## Feature Guides

### [Schema Editor](schema-editor.md)

Visual editor application for creating and modifying schemas with a graphical interface.

## Feature Overview

### Schema Definition

Define classes with typed members and enumerations with named values programmatically through the `Schema` class. See [Getting Started](../getting-started.md) for a walkthrough and the [API Reference](../api/schema-core.md) for details.

### Type System

A polymorphic type system covering:

-   **Numeric**: `Int`, `Long`, `Float`, `Double`
-   **Text and logic**: `String`, `Bool`
-   **Temporal**: `DateTime`, `TimeSpan`
-   **Vectors and colors**: `Vector2`, `Vector3`, `Vector4`, `ColorRGB`, `ColorRGBA`
-   **Complex**: `Array` (with container and key configuration), `Object` (class references), `Enum` (enum references)

Types expose classification properties like `IsPrimitive`, `IsNumeric`, `IsIntegral`, `IsDecimal`, `IsArray`, `IsObject`, `IsContainer`, and `IsBuiltIn`.

### Semantic String Types

All identifiers use type-safe semantic string wrappers (`ClassName`, `MemberName`, `EnumName`, ...) to prevent mixing up different kinds of names at compile time. Convert with `"value".As<T>()`.

### Serialization

Schemas serialize to `.schema.json` files via `SchemaSerializer` using `System.Text.Json`, with camelCase property names and a `TypeName` polymorphic discriminator. Deserialization re-establishes parent-child relationships automatically.

### Data Sources

`DataSource` elements declare which data files are bound to which schema classes, so tooling can discover and process the data belonging to a schema.

### Code Generators

`SchemaCodeGenerator` elements declare code generation outputs for a schema. The generation engine itself is planned work — see the [roadmap](../ROADMAP.md).

## Navigation

-   **[Getting Started](../getting-started.md)** - Basic setup and usage
-   **[API Reference](../api/README.md)** - Detailed API documentation
-   **[Examples](../examples/README.md)** - Code examples and tutorials
-   **[Roadmap](../ROADMAP.md)** - Current state and planned features
