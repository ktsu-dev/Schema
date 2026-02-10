# Getting Started

This guide will help you get up and running with the Schema library quickly.

## Installation

### Prerequisites

-   .NET 7.0, 8.0, 9.0, or 10.0
-   Visual Studio 2022 or Visual Studio Code (recommended)

### Adding the Library

Install via NuGet:

```xml
<PackageReference Include="ktsu.Schema" />
```

## Basic Concepts

### Schema

The main container that holds all your data structure definitions, including classes, enums, data sources, and code generators.

### Schema Classes

User-defined classes that contain typed members. These represent the structure of your data objects.

### Schema Members

Properties within a schema class that have a specific type (primitive, enum, array, or object).

### Types

The Schema library supports:

-   **Primitive types**: `int`, `long`, `float`, `double`, `string`, `bool`, `DateTime`, `TimeSpan`
-   **Vector types**: `Vector2`, `Vector3`, `Vector4`, `ColorRGB`, `ColorRGBA`
-   **Complex types**: Custom classes, enums, arrays
-   **Container types**: Arrays with optional keying

## Creating Your First Schema

### 1. Create a New Schema

```csharp
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;
using SchemaTypes = ktsu.Schema.Models.Types;

// Create a new schema
Schema schema = new();
```

### 2. Define Classes

```csharp
// Add a User class
SchemaClass? userClass = schema.AddClass("User".As<ClassName>());

// Add members to the User class
SchemaMember? idMember = userClass?.AddMember("Id".As<MemberName>());
SchemaMember? nameMember = userClass?.AddMember("Name".As<MemberName>());
SchemaMember? emailMember = userClass?.AddMember("Email".As<MemberName>());
SchemaMember? ageMember = userClass?.AddMember("Age".As<MemberName>());

// Set member types (members default to 'None' type)
idMember?.SetType(new SchemaTypes.Int());
nameMember?.SetType(new SchemaTypes.String());
emailMember?.SetType(new SchemaTypes.String());
ageMember?.SetType(new SchemaTypes.Int());
```

### 3. Define Enums

```csharp
// Add an enum for user roles
SchemaEnum? roleEnum = schema.AddEnum("UserRole".As<EnumName>());

// Add enum values
roleEnum?.TryAddValue("Admin".As<EnumValueName>());
roleEnum?.TryAddValue("User".As<EnumValueName>());
roleEnum?.TryAddValue("Guest".As<EnumValueName>());

// Use the enum in a class
SchemaMember? roleMember = userClass?.AddMember("Role".As<MemberName>());
roleMember?.SetType(new SchemaTypes.Enum { EnumName = "UserRole".As<EnumName>() });
```

### 4. Work with Arrays

```csharp
// Add a class for projects
SchemaClass? projectClass = schema.AddClass("Project".As<ClassName>());
SchemaMember? projectIdMember = projectClass?.AddMember("Id".As<MemberName>());
SchemaMember? projectNameMember = projectClass?.AddMember("Name".As<MemberName>());

projectIdMember?.SetType(new SchemaTypes.Int());
projectNameMember?.SetType(new SchemaTypes.String());

// Add an array of projects to the User class
SchemaMember? projectsMember = userClass?.AddMember("Projects".As<MemberName>());
projectsMember?.SetType(new SchemaTypes.Array
{
    ElementType = new SchemaTypes.Object { ClassName = "Project".As<ClassName>() },
    Container = "vector".As<ContainerName>()
});
```

### 5. Serialize the Schema

```csharp
// Serialize to JSON
string json = SchemaSerializer.Serialize(schema);

// Write to a file
File.WriteAllText("MySchema.schema.json", json);
```

## Loading an Existing Schema

```csharp
// Read JSON from file
string json = File.ReadAllText("MySchema.schema.json");

// Deserialize (automatically calls Reassociate() to restore parent references)
if (SchemaSerializer.TryDeserialize(json, out Schema? loadedSchema))
{
    Console.WriteLine($"Schema loaded with {loadedSchema.Classes.Count} classes");

    // Work with the loaded schema
    foreach (SchemaClass schemaClass in loadedSchema.Classes)
    {
        Console.WriteLine($"Class: {schemaClass.Name}");
        foreach (SchemaMember member in schemaClass.Members)
        {
            Console.WriteLine($"  Member: {member.Name} ({member.Type.DisplayName})");
        }
    }
}
```

## Working with Data Sources

Data sources associate a schema class with a data file:

```csharp
using ktsu.Semantics.Paths;

// Add a data source
DataSource? dataSource = schema.AddDataSource("Users".As<DataSourceName>());
if (dataSource != null)
{
    dataSource.File = "users.json".As<RelativeFilePath>();
    dataSource.ClassName = "User".As<ClassName>();
}
```

## Using the Schema Editor

For a visual editing experience, you can use the SchemaEditor application:

1. Build the SchemaEditor project
2. Run the executable
3. Open your `.schema.json` file
4. Edit your schema using the visual interface

The editor provides:

-   Tree view of all schema elements
-   Property panels for editing
-   Type selection dropdowns
-   Real-time validation

## Project Structure Recommendations

When organizing your schema files, consider this structure:

```
MyProject/
├── schemas/
│   ├── user.schema.json
│   ├── project.schema.json
│   └── common.schema.json
├── data/
│   ├── users.json
│   └── projects.json
└── generated/
    ├── UserClass.cs
    └── ProjectClass.cs
```

## Next Steps

-   Check out [examples](examples/) for common use cases
-   Try the [Schema Editor](features/schema-editor.md) for visual editing
-   Read the [Architecture](development/architecture.md) overview

## Common Patterns

### Creating Strongly-Typed Collections

```csharp
// Create a keyed collection
SchemaMember? usersMember = someClass?.AddMember("Users".As<MemberName>());
usersMember?.SetType(new SchemaTypes.Array
{
    ElementType = new SchemaTypes.Object { ClassName = "User".As<ClassName>() },
    Container = "map".As<ContainerName>(),
    Key = "Id".As<MemberName>() // Use the Id member as the key
});
```

### Working with Vector Types

```csharp
// Add position and color members
SchemaMember? positionMember = someClass?.AddMember("Position".As<MemberName>());
SchemaMember? colorMember = someClass?.AddMember("Color".As<MemberName>());

positionMember?.SetType(new SchemaTypes.Vector3());
colorMember?.SetType(new SchemaTypes.ColorRGBA());
```
