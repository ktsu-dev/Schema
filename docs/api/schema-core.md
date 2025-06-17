# Schema Class

**Namespace:** `ktsu.Schema`  
**Assembly:** Schema.dll

The main schema container class that manages classes, enums, data sources, and code generators.

## Declaration

```csharp
public partial class Schema
```

## Overview

The `Schema` class is the root container for all schema definitions. It provides methods for loading, saving, and manipulating schema elements including classes, enums, data sources, and code generators. Each schema can be serialized to and from JSON while maintaining full type information.

## Properties

### File Paths

#### FilePath

```csharp
[JsonIgnore] public AbsoluteFilePath FilePath { get; private set; }
```

Gets the absolute file path of the schema.

#### RelativePaths

```csharp
[JsonInclude] public SchemaPaths RelativePaths { get; private set; }
```

Gets the relative paths associated with the schema.

#### ProjectRootPath

```csharp
[JsonIgnore] public AbsoluteDirectoryPath ProjectRootPath { get; }
```

Gets the project root path based on the file path and relative project root path.

#### DataSourcePath

```csharp
[JsonIgnore] public AbsoluteDirectoryPath DataSourcePath { get; }
```

Gets the data source path based on the file path and relative data source path.

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

#### FileExtension

```csharp
[JsonIgnore] public static FileExtension FileExtension { get; }
```

Gets the file extension for schema files (`.schema.json`).

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

### Loading and Saving

#### TryLoad

```csharp
public static bool TryLoad(AbsoluteFilePath filePath, out Schema? schema)
```

Tries to load a schema from the specified file path.

**Parameters:**

-   `filePath`: The file path to load the schema from
-   `schema`: The loaded schema, or null if loading failed

**Returns:** `true` if the schema was successfully loaded; otherwise, `false`.

**Example:**

```csharp
if (Schema.TryLoad("myschema.schema.json".As<AbsoluteFilePath>(), out Schema? schema))
{
    Console.WriteLine($"Loaded schema with {schema.Classes.Count} classes");
}
```

#### Save

```csharp
public void Save()
```

Saves the schema to its file path. Uses atomic file operations with backup and recovery.

**Example:**

```csharp
schema.Save(); // Saves to the current FilePath
```

#### ChangeFilePath

```csharp
public void ChangeFilePath(AbsoluteFilePath newFilePath)
```

Changes the file path of the schema.

**Parameters:**

-   `newFilePath`: The new file path

### Managing Classes

#### AddClass (by name)

```csharp
public SchemaClass? AddClass(ClassName name)
```

Adds a new class with the specified name.

**Parameters:**

-   `name`: The name of the class to add

**Returns:** The added schema class, or null if a class with that name already exists.

#### AddClass (from Type)

```csharp
public SchemaClass? AddClass(Type type)
```

Adds a new class based on a .NET Type, automatically creating members for public properties.

**Parameters:**

-   `type`: The .NET type to create a schema class from

**Returns:** The added schema class, or null if failed.

#### TryAddClass

```csharp
public bool TryAddClass(ClassName name)
public bool TryAddClass(Type type)
```

Tries to add a class without returning the instance.

#### GetClass

```csharp
public SchemaClass? GetClass(ClassName name)
```

Gets a class by name.

**Parameters:**

-   `name`: The name of the class to retrieve

**Returns:** The schema class, or null if not found.

#### TryGetClass

```csharp
public bool TryGetClass(ClassName name, out SchemaClass? schemaClass)
```

Tries to get a class by name.

**Parameters:**

-   `name`: The name of the class to retrieve
-   `schemaClass`: The retrieved schema class, if found

**Returns:** `true` if the class was found; otherwise, `false`.

### Managing Enums

#### AddEnum

```csharp
public SchemaEnum? AddEnum(EnumName name)
```

Adds a new enum with the specified name.

#### TryAddEnum

```csharp
public bool TryAddEnum(EnumName name)
```

Tries to add an enum without returning the instance.

#### GetEnum

```csharp
public SchemaEnum? GetEnum(EnumName name)
```

Gets an enum by name.

#### TryGetEnum

```csharp
public bool TryGetEnum(EnumName name, out SchemaEnum? schemaEnum)
```

Tries to get an enum by name.

### Managing Data Sources

#### AddDataSource

```csharp
public DataSource? AddDataSource(DataSourceName name)
```

Adds a new data source with the specified name.

#### TryAddDataSource

```csharp
public bool TryAddDataSource(DataSourceName name)
```

Tries to add a data source without returning the instance.

#### GetDataSource

```csharp
public DataSource? GetDataSource(DataSourceName name)
```

Gets a data source by name.

### Type System

#### GetTypes

```csharp
public IEnumerable<SchemaTypes.BaseType> GetTypes()
```

Gets all available types in the schema, including built-in types and user-defined classes/enums.

**Returns:** An enumerable of all available types.

### Utility Methods

#### EnsureDirectoryExists

```csharp
public static void EnsureDirectoryExists(string path)
```

Ensures that the directory for the specified path exists.

**Parameters:**

-   `path`: The path to ensure the directory exists for

## Static Properties

### JsonSerializerOptions

```csharp
internal static JsonSerializerOptions JsonSerializerOptions { get; }
```

Gets the JSON serializer options used for schema serialization. Configured with:

-   Indented writing
-   Include fields
-   Strong string type converters
-   Enum string conversion
-   Polymorphic type support

## Usage Examples

### Creating a Complete Schema

```csharp
using ktsu.Schema;
using ktsu.StrongPaths;

// Create new schema
var schema = new Schema();
schema.ChangeFilePath("game.schema.json".As<AbsoluteFilePath>());

// Add enums
var difficultyEnum = schema.AddEnum("Difficulty".As<EnumName>());
difficultyEnum?.AddValue("Easy".As<EnumValueName>());
difficultyEnum?.AddValue("Normal".As<EnumValueName>());
difficultyEnum?.AddValue("Hard".As<EnumValueName>());

// Add classes
var playerClass = schema.AddClass("Player".As<ClassName>());
var nameMember = playerClass?.AddMember("Name".As<MemberName>());
var levelMember = playerClass?.AddMember("Level".As<MemberName>());
var difficultyMember = playerClass?.AddMember("Difficulty".As<MemberName>());

// Set types
if (nameMember != null)
    nameMember.Type = new SchemaTypes.String();
if (levelMember != null)
    levelMember.Type = new SchemaTypes.Int();
if (difficultyMember != null)
    difficultyMember.Type = new SchemaTypes.Enum { EnumName = "Difficulty".As<EnumName>() };

// Add data source
var playersDataSource = schema.AddDataSource("Players".As<DataSourceName>());
if (playersDataSource != null)
{
    playersDataSource.File = "players.json".As<RelativeFilePath>();
    playersDataSource.Class = playerClass;
}

// Save schema
schema.Save();
```

### Loading and Querying a Schema

```csharp
if (Schema.TryLoad("game.schema.json".As<AbsoluteFilePath>(), out Schema? schema))
{
    // List all classes
    foreach (var schemaClass in schema.Classes)
    {
        Console.WriteLine($"Class: {schemaClass.Name}");
        foreach (var member in schemaClass.Members)
        {
            Console.WriteLine($"  {member.Name}: {member.Type.DisplayName}");
        }
    }

    // Get specific class
    if (schema.TryGetClass("Player".As<ClassName>(), out SchemaClass? playerClass))
    {
        Console.WriteLine($"Player class has {playerClass.Members.Count} members");
    }

    // List available types
    var availableTypes = schema.GetTypes().ToList();
    Console.WriteLine($"Available types: {string.Join(", ", availableTypes.Select(t => t.DisplayName))}");
}
```

## Thread Safety

The `Schema` class is not thread-safe. If you need to access a schema from multiple threads, you must provide your own synchronization.

## See Also

-   [SchemaClass](schema-class.md) - Individual classes within a schema
-   [SchemaEnum](schema-enum.md) - Enumerations within a schema
-   [DataSource](data-source.md) - Data source configuration
-   [SchemaTypes](schema-types.md) - Available type system
