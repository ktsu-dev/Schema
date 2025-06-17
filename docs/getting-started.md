# Getting Started

This guide will help you get up and running with the Schema library quickly.

## Installation

### Prerequisites

-   .NET 8.0 or .NET 9.0
-   Visual Studio 2022 or Visual Studio Code (recommended)

### Adding the Library

Add the Schema library to your project:

```xml
<ProjectReference Include="path/to/Schema/Schema.csproj" />
```

Or if using as a NuGet package:

```xml
<PackageReference Include="ktsu.Schema" Version="1.3.2" />
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
using ktsu.Schema;
using ktsu.StrongPaths;

// Create a new schema
var schema = new Schema();

// Set the file path for saving
schema.ChangeFilePath("MySchema.schema.json".As<AbsoluteFilePath>());
```

### 2. Define Classes

```csharp
// Add a User class
var userClass = schema.AddClass("User".As<ClassName>());

// Add members to the User class
var idMember = userClass?.AddMember("Id".As<MemberName>());
var nameMember = userClass?.AddMember("Name".As<MemberName>());
var emailMember = userClass?.AddMember("Email".As<MemberName>());
var ageMember = userClass?.AddMember("Age".As<MemberName>());

// Set member types (members default to 'None' type)
if (idMember != null)
    idMember.Type = new SchemaTypes.Int();
if (nameMember != null)
    nameMember.Type = new SchemaTypes.String();
if (emailMember != null)
    emailMember.Type = new SchemaTypes.String();
if (ageMember != null)
    ageMember.Type = new SchemaTypes.Int();
```

### 3. Define Enums

```csharp
// Add an enum for user roles
var roleEnum = schema.AddEnum("UserRole".As<EnumName>());

// Add enum values
roleEnum?.AddValue("Admin".As<EnumValueName>());
roleEnum?.AddValue("User".As<EnumValueName>());
roleEnum?.AddValue("Guest".As<EnumValueName>());

// Use the enum in a class
var roleMember = userClass?.AddMember("Role".As<MemberName>());
if (roleMember != null)
    roleMember.Type = new SchemaTypes.Enum { EnumName = "UserRole".As<EnumName>() };
```

### 4. Work with Arrays

```csharp
// Add a class for projects
var projectClass = schema.AddClass("Project".As<ClassName>());
var projectIdMember = projectClass?.AddMember("Id".As<MemberName>());
var projectNameMember = projectClass?.AddMember("Name".As<MemberName>());

if (projectIdMember != null)
    projectIdMember.Type = new SchemaTypes.Int();
if (projectNameMember != null)
    projectNameMember.Type = new SchemaTypes.String();

// Add an array of projects to the User class
var projectsMember = userClass?.AddMember("Projects".As<MemberName>());
if (projectsMember != null)
{
    projectsMember.Type = new SchemaTypes.Array
    {
        ElementType = new SchemaTypes.Object { ClassName = "Project".As<ClassName>() },
        Container = "vector".As<ContainerName>()
    };
}
```

### 5. Save the Schema

```csharp
// Save the schema to file
schema.Save();
```

## Loading an Existing Schema

```csharp
// Load a schema from file
if (Schema.TryLoad("MySchema.schema.json".As<AbsoluteFilePath>(), out Schema? loadedSchema))
{
    Console.WriteLine($"Schema loaded with {loadedSchema.Classes.Count} classes");

    // Work with the loaded schema
    foreach (var schemaClass in loadedSchema.Classes)
    {
        Console.WriteLine($"Class: {schemaClass.Name}");
        foreach (var member in schemaClass.Members)
        {
            Console.WriteLine($"  Member: {member.Name} ({member.Type.DisplayName})");
        }
    }
}
```

## Working with Data Sources

Data sources define where your schema data comes from:

```csharp
// Add a data source
var dataSource = schema.AddDataSource("Users".As<DataSourceName>());
if (dataSource != null)
{
    dataSource.File = "users.json".As<RelativeFilePath>();
    dataSource.Class = userClass;
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

-   Explore the [API Reference](api/) for detailed class documentation
-   Learn about [advanced features](features/) like code generation
-   Check out [examples](examples/) for common use cases
-   Try the [Schema Editor](features/schema-editor.md) for visual editing

## Common Patterns

### Creating Strongly-Typed Collections

```csharp
// Create a keyed collection
var usersMember = someClass?.AddMember("Users".As<MemberName>());
if (usersMember != null)
{
    usersMember.Type = new SchemaTypes.Array
    {
        ElementType = new SchemaTypes.Object { ClassName = "User".As<ClassName>() },
        Container = "map".As<ContainerName>(),
        Key = "Id".As<MemberName>() // Use the Id member as the key
    };
}
```

### Working with Vector Types

```csharp
// Add position and color members
var positionMember = someClass?.AddMember("Position".As<MemberName>());
var colorMember = someClass?.AddMember("Color".As<MemberName>());

if (positionMember != null)
    positionMember.Type = new SchemaTypes.Vector3();
if (colorMember != null)
    colorMember.Type = new SchemaTypes.ColorRGBA();
```
