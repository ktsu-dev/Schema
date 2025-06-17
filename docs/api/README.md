# API Reference

This section contains detailed API documentation for all public classes, interfaces, and types in the Schema library.

## Core API

### [Schema Class](schema-core.md)

The main schema container class that manages classes, enums, data sources, and code generators.

### [Schema Types](schema-types.md)

Complete reference for all available types in the schema system, including primitives, vectors, and complex types.

### [Strong String Types](strong-strings.md)

Type-safe string types used throughout the library for compile-time safety.

## Schema Elements

### [Schema Class](schema-class.md)

User-defined classes that contain typed members representing data structures.

### [Schema Member](schema-member.md)

Properties within schema classes that have specific types and behaviors.

### [Schema Enum](schema-enum.md)

Enumeration types with named values for representing discrete choices.

### [Schema Child](schema-child.md)

Base class for all schema elements that can be contained within a schema.

## Data Management

### [Data Source](data-source.md)

Configuration for data sources that provide data for schema instances.

### [Schema Paths](schema-paths.md)

Path management utilities for handling relative and absolute paths within schemas.

## Collections

### [Schema Child Collection](schema-child-collection.md)

Collection management utilities for working with schema children.

## Code Generation

### [Schema Code Generator](schema-code-generator.md)

Configuration and utilities for generating code from schema definitions.

## Quick Reference

### Key Classes

| Class          | Purpose                                   |
| -------------- | ----------------------------------------- |
| `Schema`       | Main container for all schema definitions |
| `SchemaClass`  | User-defined class with typed members     |
| `SchemaMember` | Property within a schema class            |
| `SchemaEnum`   | Enumeration with named values             |
| `DataSource`   | Data source configuration                 |

### Key Types

| Type                   | Description                 |
| ---------------------- | --------------------------- |
| `SchemaTypes.BaseType` | Abstract base for all types |
| `SchemaTypes.Int`      | Integer type                |
| `SchemaTypes.String`   | String type                 |
| `SchemaTypes.Array`    | Array/collection type       |
| `SchemaTypes.Object`   | Reference to a schema class |
| `SchemaTypes.Enum`     | Reference to a schema enum  |

### Strong String Types

| Type             | Purpose                  |
| ---------------- | ------------------------ |
| `ClassName`      | Names of schema classes  |
| `MemberName`     | Names of class members   |
| `EnumName`       | Names of enumerations    |
| `EnumValueName`  | Names of enum values     |
| `DataSourceName` | Names of data sources    |
| `ContainerName`  | Names of container types |

## Navigation

-   **[Getting Started](../getting-started.md)** - Basic usage and setup
-   **[Features](../features/)** - Feature guides and tutorials
-   **[Examples](../examples/)** - Code examples and patterns
-   **[Development](../development/)** - Contributing and development

## API Conventions

### Naming

-   Classes use PascalCase: `SchemaClass`, `SchemaMember`
-   Methods use PascalCase: `AddClass()`, `TryGetMember()`
-   Properties use PascalCase: `Name`, `Members`
-   Strong string types end with their type: `ClassName`, `MemberName`

### Error Handling

-   Methods that can fail use the `Try...` pattern returning `bool`
-   Methods that throw exceptions are documented with possible exception types
-   Null checking is enforced through nullable reference types

### Thread Safety

-   Schema objects are not thread-safe
-   Use appropriate synchronization when accessing schemas from multiple threads

### JSON Serialization

-   All schema objects support JSON serialization using `System.Text.Json`
-   Custom converters handle strong string types and polymorphic types
-   Serialized schemas maintain full type information
