# Examples

This section contains practical examples demonstrating how to use the Schema library for common scenarios.

## Available Examples

### [Basic Schema Creation](basic-schema.md)

Create a complete schema from scratch with classes, members, enums, and data sources, then serialize it to a `.schema.json` file.

### [Dependency Injection](dependency-injection.md)

Register and consume schemas through a dependency injection container.

## Common Patterns

### Schema Creation Pattern

```csharp
// 1. Create schema
Schema schema = new();

// 2. Add classes and enums
SchemaClass? myClass = schema.AddClass("MyClass".As<ClassName>());

// 3. Add members and set types
myClass?.AddMember("Property".As<MemberName>())?.SetType(new SchemaTypes.String());

// 4. Serialize and save
File.WriteAllText("my.schema.json", SchemaSerializer.Serialize(schema));
```

### Loading Pattern

```csharp
string json = File.ReadAllText("my.schema.json");
if (SchemaSerializer.TryDeserialize(json, out Schema? schema) && schema is not null)
{
    // Work with loaded schema
    foreach (SchemaClass schemaClass in schema.Classes)
    {
        // Process classes
    }
}
```

### Type-Safe Access Pattern

```csharp
if (schema.TryGetClass("User".As<ClassName>(), out SchemaClass? userClass))
{
    if (userClass!.TryGetMember("Name".As<MemberName>(), out SchemaMember? nameMember))
    {
        // Work with member
    }
}
```

### Creating Schemas from .NET Types

```csharp
// Generate a schema class (and any referenced classes/enums) via reflection
schema.AddClass(typeof(MyExistingClass));
```

## Suggesting Examples

If you have ideas for examples that would be helpful:

1. Open an issue describing the use case and target audience
2. Provide any relevant context or requirements

## Navigation

-   **[Getting Started](../getting-started.md)** - Basic setup and concepts
-   **[API Reference](../api/README.md)** - Detailed API documentation
-   **[Features](../features/README.md)** - Feature guides and tutorials
-   **[Development](../development/README.md)** - Contributing and development setup
