# Schema Class

**Namespace:** `ktsu.Schema.Models`
**Assembly:** ktsu.Schema.dll

The main schema container class that manages classes, enums, data sources, and code generators.

## Declaration

```csharp
public class Schema
```

## Overview

The `Schema` class is the root container for all schema definitions. It provides methods for adding, querying, and removing schema elements including classes, enums, data sources, and code generators. Schemas are serialized to and from JSON with the companion `SchemaSerializer` class while maintaining full type information.

## Properties

### Collections

#### Classes

```csharp
[JsonIgnore] public IReadOnlyCollection<SchemaClass> Classes { get; }
```

Gets the read-only collection of schema classes.

#### Enums

```csharp
[JsonIgnore] public IReadOnlyCollection<SchemaEnum> Enums { get; }
```

Gets the read-only collection of schema enums.

#### DataSources

```csharp
[JsonIgnore] public IReadOnlyCollection<DataSource> DataSources { get; }
```

Gets the read-only collection of data sources.

#### CodeGenerators

```csharp
[JsonIgnore] public IReadOnlyCollection<SchemaCodeGenerator> CodeGenerators { get; }
```

Gets the read-only collection of code generators.

### Utility Properties

#### FirstClass

```csharp
public SchemaClass? FirstClass { get; }
```

Gets the first class in the schema, or null if no classes exist.

#### LastClass

```csharp
public SchemaClass? LastClass { get; }
```

Gets the last class in the schema, or null if no classes exist.

## Methods

### Association

#### Reassociate

```csharp
public void Reassociate()
```

Re-establishes parent-child relationships between the schema and its elements. Call this after deserializing a schema manually; `SchemaSerializer.TryDeserialize()` calls it for you.

### Managing Classes

#### AddClass (by name)

```csharp
public SchemaClass? AddClass(ClassName name)
```

Adds a new class with the specified name.

**Returns:** The added schema class, or null if a class with that name already exists.

#### AddClass (from Type)

```csharp
public SchemaClass? AddClass(Type type)
```

Adds a new class based on a .NET Type, automatically creating members for public properties and fields. Referenced classes and enums are added to the schema recursively.

#### TryAddClass

```csharp
public bool TryAddClass(ClassName name)
public bool TryAddClass(Type type)
```

Tries to add a class without returning the instance.

#### GetClass / TryGetClass

```csharp
public SchemaClass? GetClass(ClassName name)
public bool TryGetClass(ClassName name, out SchemaClass? schemaClass)
```

Gets a class by name.

### Managing Enums

```csharp
public SchemaEnum? AddEnum(EnumName name)
public bool TryAddEnum(EnumName name)
public SchemaEnum? GetEnum(EnumName name)
public bool TryGetEnum(EnumName name, out SchemaEnum? schemaEnum)
```

Adds or retrieves enums by name, following the same conventions as classes.

### Managing Data Sources

```csharp
public DataSource? AddDataSource(DataSourceName name)
public bool TryAddDataSource(DataSourceName name)
public DataSource? GetDataSource(DataSourceName name)
public bool TryGetDataSource(DataSourceName name, out DataSource? dataSource)
```

Adds or retrieves data sources by name.

### Managing Code Generators

```csharp
public SchemaCodeGenerator? AddCodeGenerator(CodeGeneratorName name)
public bool TryAddCodeGenerator(CodeGeneratorName name)
public SchemaCodeGenerator? GetCodeGenerator(CodeGeneratorName name)
public bool TryGetCodeGenerator(CodeGeneratorName name, out SchemaCodeGenerator? codeGenerator)
```

Adds or retrieves code generators by name.

### Type System

#### GetTypes

```csharp
public IEnumerable<BaseType> GetTypes()
```

Gets the distinct types currently used by members across the schema.

## SchemaSerializer

**Namespace:** `ktsu.Schema.Models`

```csharp
public static class SchemaSerializer
```

Provides JSON serialization for `Schema` objects using `System.Text.Json` with camelCase property names and a `TypeName` polymorphic discriminator.

#### Serialize

```csharp
public static string Serialize(Schema schema)
```

Serializes a schema to an indented JSON string.

#### TryDeserialize

```csharp
public static bool TryDeserialize(string json, out Schema? schema)
```

Tries to deserialize a JSON string to a schema, calling `Reassociate()` automatically on success.

**Returns:** `true` if deserialization succeeded; otherwise, `false`.

## Usage Examples

### Creating a Complete Schema

```csharp
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;
using SchemaTypes = ktsu.Schema.Models.Types;

// Create new schema
Schema schema = new();

// Add enums
SchemaEnum? difficultyEnum = schema.AddEnum("Difficulty".As<EnumName>());
difficultyEnum?.TryAddValue("Easy".As<EnumValueName>());
difficultyEnum?.TryAddValue("Normal".As<EnumValueName>());
difficultyEnum?.TryAddValue("Hard".As<EnumValueName>());

// Add classes and members with types
SchemaClass? playerClass = schema.AddClass("Player".As<ClassName>());
playerClass?.AddMember("Name".As<MemberName>())?.SetType(new SchemaTypes.String());
playerClass?.AddMember("Level".As<MemberName>())?.SetType(new SchemaTypes.Int());
playerClass?.AddMember("Difficulty".As<MemberName>())?.SetType(
    new SchemaTypes.Enum { EnumName = "Difficulty".As<EnumName>() });

// Add data source
DataSource? playersDataSource = schema.AddDataSource("Players".As<DataSourceName>());
if (playersDataSource != null)
{
    playersDataSource.File = "players.json".As<RelativeFilePath>();
    playersDataSource.ClassName = "Player".As<ClassName>();
}

// Save schema
File.WriteAllText("game.schema.json", SchemaSerializer.Serialize(schema));
```

### Loading and Querying a Schema

```csharp
string json = File.ReadAllText("game.schema.json");
if (SchemaSerializer.TryDeserialize(json, out Schema? schema) && schema is not null)
{
    // List all classes
    foreach (SchemaClass schemaClass in schema.Classes)
    {
        Console.WriteLine($"Class: {schemaClass.Name}");
        foreach (SchemaMember member in schemaClass.Members)
        {
            Console.WriteLine($"  {member.Name}: {member.Type.DisplayName}");
        }
    }

    // Get specific class
    if (schema.TryGetClass("Player".As<ClassName>(), out SchemaClass? playerClass))
    {
        Console.WriteLine($"Player class has {playerClass!.Members.Count} members");
    }

    // List the types in use
    List<SchemaTypes.BaseType> usedTypes = schema.GetTypes().ToList();
    Console.WriteLine($"Types in use: {string.Join(", ", usedTypes.Select(t => t.DisplayName))}");
}
```

## Thread Safety

The `Schema` class is not thread-safe. If you need to access a schema from multiple threads, you must provide your own synchronization.

## See Also

-   [API Reference](README.md) - Quick reference for all public types
-   [Getting Started](../getting-started.md) - Basic usage walkthrough
-   [Basic Schema Example](../examples/basic-schema.md) - Complete worked example
