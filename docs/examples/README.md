# Examples

This section provides practical examples and tutorials for common use cases with the Schema library.

## Quick Start Examples

### [Basic Schema Creation](basic-schema.md)

Learn how to create your first schema with classes, members, and enums.

### [Loading and Saving](loading-saving.md)

Examples of loading existing schemas and saving changes safely.

### [Working with Types](working-with-types.md)

How to use different types including primitives, arrays, and custom objects.

## Real-World Use Cases

### [Game Data Management](game-data.md)

Complete example of managing game data including players, levels, and configuration.

### [Configuration System](configuration-system.md)

Build a type-safe configuration system for applications.

### [API Data Models](api-data-models.md)

Define API request/response models and validation schemas.

### [Document Schema](document-schema.md)

Create schemas for structured documents and content management.

## Advanced Examples

### [Complex Relationships](complex-relationships.md)

Handle complex object relationships and circular references.

### [Collections and Arrays](collections-examples.md)

Work with arrays, maps, and keyed collections effectively.

### [Code Generation](code-generation-examples.md)

Generate C# classes, TypeScript interfaces, and other code from schemas.

### [Schema Migration](migration-examples.md)

Handle schema evolution and data migration scenarios.

## Integration Examples

### [ASP.NET Core Integration](aspnet-integration.md)

Integrate schemas with ASP.NET Core applications for API validation.

### [Entity Framework Integration](ef-integration.md)

Use schemas to define Entity Framework models and database schemas.

### [JSON Schema Export](json-schema-examples.md)

Export schemas to JSON Schema format for API documentation.

### [Validation Examples](validation-examples.md)

Implement custom validation rules and error handling.

## Editor Examples

### [Schema Editor Workflows](editor-workflows.md)

Common workflows and best practices for using the visual editor.

### [Import/Export Examples](import-export.md)

Examples of importing from various sources and exporting to different formats.

## Performance Examples

### [Large Schema Management](large-schemas.md)

Techniques for working with large, complex schemas efficiently.

### [Memory Optimization](memory-optimization.md)

Optimize memory usage when working with many schema instances.

## Quick Reference

### Example Categories

| Category        | Use Case              | Examples                                                                             |
| --------------- | --------------------- | ------------------------------------------------------------------------------------ |
| **Basics**      | Learning fundamentals | [Basic Schema](basic-schema.md), [Types](working-with-types.md)                      |
| **Real-World**  | Production scenarios  | [Game Data](game-data.md), [APIs](api-data-models.md)                                |
| **Advanced**    | Complex features      | [Relationships](complex-relationships.md), [Generation](code-generation-examples.md) |
| **Integration** | Framework integration | [ASP.NET](aspnet-integration.md), [EF](ef-integration.md)                            |
| **Performance** | Optimization          | [Large Schemas](large-schemas.md), [Memory](memory-optimization.md)                  |

### By Complexity Level

#### 🟢 **Beginner**

-   [Basic Schema Creation](basic-schema.md)
-   [Loading and Saving](loading-saving.md)
-   [Working with Types](working-with-types.md)
-   [Editor Workflows](editor-workflows.md)

#### 🟡 **Intermediate**

-   [Game Data Management](game-data.md)
-   [Configuration System](configuration-system.md)
-   [Collections and Arrays](collections-examples.md)
-   [Import/Export](import-export.md)

#### 🔴 **Advanced**

-   [Complex Relationships](complex-relationships.md)
-   [Code Generation](code-generation-examples.md)
-   [Schema Migration](migration-examples.md)
-   [Large Schema Management](large-schemas.md)

### By Feature

| Feature               | Examples                                                                         |
| --------------------- | -------------------------------------------------------------------------------- |
| **Classes & Members** | [Basic Schema](basic-schema.md), [Game Data](game-data.md)                       |
| **Enums**             | [Basic Schema](basic-schema.md), [Configuration](configuration-system.md)        |
| **Arrays**            | [Collections](collections-examples.md), [API Models](api-data-models.md)         |
| **Data Sources**      | [Game Data](game-data.md), [Documents](document-schema.md)                       |
| **Code Generation**   | [Code Generation](code-generation-examples.md), [ASP.NET](aspnet-integration.md) |
| **Validation**        | [Validation](validation-examples.md), [API Models](api-data-models.md)           |

## Getting Started with Examples

### For New Users

1. Start with [Basic Schema Creation](basic-schema.md)
2. Try [Loading and Saving](loading-saving.md)
3. Explore [Working with Types](working-with-types.md)
4. Practice with [Editor Workflows](editor-workflows.md)

### For Specific Use Cases

-   **Game Development**: [Game Data Management](game-data.md)
-   **Web APIs**: [API Data Models](api-data-models.md)
-   **Configuration**: [Configuration System](configuration-system.md)
-   **Documents**: [Document Schema](document-schema.md)

### For Advanced Features

-   **Code Generation**: [Code Generation Examples](code-generation-examples.md)
-   **Large Systems**: [Large Schema Management](large-schemas.md)
-   **Integration**: [ASP.NET Core](aspnet-integration.md)

## Example Projects

### Sample Applications

Each example includes a complete, runnable project demonstrating the concepts:

```
examples/
├── BasicSchema/          # Simple schema creation
├── GameData/            # Game data management
├── ConfigSystem/        # Application configuration
├── ApiModels/           # REST API models
├── DocumentSchema/      # Document management
└── Integration/         # Framework integrations
    ├── AspNetCore/      # ASP.NET Core example
    └── EntityFramework/ # EF Core example
```

### Running Examples

1. Clone the repository
2. Navigate to an example directory
3. Run `dotnet restore` to install dependencies
4. Run `dotnet run` to execute the example
5. Follow the README in each example directory

## Contributing Examples

We welcome contributions of new examples! See the [contributing guide](../development/contributing.md) for details on:

-   Example structure and format
-   Documentation requirements
-   Code quality standards
-   Testing expectations

### Suggesting Examples

If you have ideas for examples that would be helpful:

1. Open an issue with the "example-request" label
2. Describe the use case and target audience
3. Provide any relevant context or requirements

## Common Patterns

### Schema Creation Pattern

```csharp
// 1. Create schema
var schema = new Schema();

// 2. Add classes and enums
var myClass = schema.AddClass("MyClass".As<ClassName>());

// 3. Add members
var member = myClass?.AddMember("Property".As<MemberName>());

// 4. Set types
member.Type = new SchemaTypes.String();

// 5. Save
schema.Save();
```

### Loading Pattern

```csharp
if (Schema.TryLoad(filePath, out Schema? schema))
{
    // Work with loaded schema
    foreach (var schemaClass in schema.Classes)
    {
        // Process classes
    }
}
```

### Type-Safe Access Pattern

```csharp
if (schema.TryGetClass("User".As<ClassName>(), out SchemaClass? userClass))
{
    if (userClass.TryGetMember("Name".As<MemberName>(), out SchemaMember? nameMember))
    {
        // Work with member
    }
}
```

## Navigation

-   **[Getting Started](../getting-started.md)** - Basic setup and concepts
-   **[API Reference](../api/)** - Detailed API documentation
-   **[Features](../features/)** - Feature guides and tutorials
-   **[Development](../development/)** - Contributing and development setup
