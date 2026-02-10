# Basic Schema Creation

This example demonstrates how to create a simple schema from scratch, including classes, members, enums, and data sources.

## Overview

We'll create a schema for a simple user management system with:

-   A `User` class with basic properties
-   A `UserRole` enum for user permissions
-   A data source for user data

## Complete Example

```csharp
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using ktsu.Semantics.Paths;
using SchemaTypes = ktsu.Schema.Models.Types;

class Program
{
    static void Main()
    {
        // Create a new schema
        Schema schema = new();

        // Create the schema structure
        CreateUserRoleEnum(schema);
        CreateUserClass(schema);
        CreateDataSource(schema);

        // Serialize and save the schema
        string json = SchemaSerializer.Serialize(schema);
        File.WriteAllText("user-management.schema.json", json);

        Console.WriteLine("Schema created successfully!");
        Console.WriteLine($"Classes: {schema.Classes.Count}");
        Console.WriteLine($"Enums: {schema.Enums.Count}");
        Console.WriteLine($"Data Sources: {schema.DataSources.Count}");
    }

    static void CreateUserRoleEnum(Schema schema)
    {
        // Add a UserRole enum
        SchemaEnum? userRoleEnum = schema.AddEnum("UserRole".As<EnumName>());
        if (userRoleEnum != null)
        {
            // Add enum values
            userRoleEnum.TryAddValue("Admin".As<EnumValueName>());
            userRoleEnum.TryAddValue("Manager".As<EnumValueName>());
            userRoleEnum.TryAddValue("User".As<EnumValueName>());
            userRoleEnum.TryAddValue("Guest".As<EnumValueName>());
        }
    }

    static void CreateUserClass(Schema schema)
    {
        // Add a User class
        SchemaClass? userClass = schema.AddClass("User".As<ClassName>());
        if (userClass == null) return;

        // Add basic properties and set their types
        userClass.AddMember("Id".As<MemberName>())?.SetType(new SchemaTypes.Int());
        userClass.AddMember("Username".As<MemberName>())?.SetType(new SchemaTypes.String());
        userClass.AddMember("Email".As<MemberName>())?.SetType(new SchemaTypes.String());
        userClass.AddMember("FirstName".As<MemberName>())?.SetType(new SchemaTypes.String());
        userClass.AddMember("LastName".As<MemberName>())?.SetType(new SchemaTypes.String());
        userClass.AddMember("CreatedAt".As<MemberName>())?.SetType(new SchemaTypes.DateTime());
        userClass.AddMember("IsActive".As<MemberName>())?.SetType(new SchemaTypes.Bool());
        userClass.AddMember("Role".As<MemberName>())?.SetType(
            new SchemaTypes.Enum { EnumName = "UserRole".As<EnumName>() });
    }

    static void CreateDataSource(Schema schema)
    {
        // Add a data source for users
        DataSource? usersDataSource = schema.AddDataSource("Users".As<DataSourceName>());
        if (usersDataSource != null)
        {
            usersDataSource.File = "users.json".As<RelativeFilePath>();
            usersDataSource.ClassName = "User".As<ClassName>();
        }
    }
}
```

## Step-by-Step Breakdown

### 1. Creating the Schema

```csharp
Schema schema = new();
```

-   Create a new `Schema` instance
-   The schema is a pure in-memory model; file I/O is handled separately

### 2. Adding an Enum

```csharp
SchemaEnum? userRoleEnum = schema.AddEnum("UserRole".As<EnumName>());
userRoleEnum?.TryAddValue("Admin".As<EnumValueName>());
userRoleEnum?.TryAddValue("Manager".As<EnumValueName>());
userRoleEnum?.TryAddValue("User".As<EnumValueName>());
userRoleEnum?.TryAddValue("Guest".As<EnumValueName>());
```

-   Use `AddEnum()` to create a new enumeration
-   Add values using `TryAddValue()` with strongly-typed enum value names
-   Each value represents a possible choice for the enum

### 3. Adding a Class

```csharp
SchemaClass? userClass = schema.AddClass("User".As<ClassName>());
```

-   Use `AddClass()` to create a new schema class
-   Class names use the `ClassName` semantic string type for type safety

### 4. Adding Members to the Class

```csharp
SchemaMember? idMember = userClass?.AddMember("Id".As<MemberName>());
idMember?.SetType(new SchemaTypes.Int());
```

-   Use `AddMember()` to add properties to a class
-   Use `SetType()` to assign a type (this also establishes the parent association)
-   Each member has a name and a type (defaults to `None`)

### 5. Setting Different Types

```csharp
// Primitive types
idMember?.SetType(new SchemaTypes.Int());
usernameMember?.SetType(new SchemaTypes.String());
isActiveMember?.SetType(new SchemaTypes.Bool());
createdAtMember?.SetType(new SchemaTypes.DateTime());

// Enum reference
roleMember?.SetType(new SchemaTypes.Enum { EnumName = "UserRole".As<EnumName>() });
```

-   Primitive types are straightforward instances
-   Enum types reference an enum by name
-   The enum must exist in the schema for the reference to be valid

### 6. Adding a Data Source

```csharp
DataSource? usersDataSource = schema.AddDataSource("Users".As<DataSourceName>());
if (usersDataSource != null)
{
    usersDataSource.File = "users.json".As<RelativeFilePath>();
    usersDataSource.ClassName = "User".As<ClassName>();
}
```

-   Data sources link schema classes to actual data files
-   Specify the file path relative to the schema location
-   Associate with a schema class by name using `ClassName`

### 7. Serializing the Schema

```csharp
string json = SchemaSerializer.Serialize(schema);
File.WriteAllText("user-management.schema.json", json);
```

-   Use `SchemaSerializer.Serialize()` to convert to JSON
-   Write the JSON string to a file

## Generated Schema File

The resulting `user-management.schema.json` file will look like this (using camelCase property names):

```json
{
  "classes": [
    {
      "name": "User",
      "description": "",
      "members": [
        {
          "name": "Id",
          "description": "",
          "type": { "TypeName": "Int" },
          "memberDescription": ""
        },
        {
          "name": "Username",
          "description": "",
          "type": { "TypeName": "String" },
          "memberDescription": ""
        },
        {
          "name": "Email",
          "description": "",
          "type": { "TypeName": "String" },
          "memberDescription": ""
        },
        {
          "name": "CreatedAt",
          "description": "",
          "type": { "TypeName": "DateTime" },
          "memberDescription": ""
        },
        {
          "name": "IsActive",
          "description": "",
          "type": { "TypeName": "Bool" },
          "memberDescription": ""
        },
        {
          "name": "Role",
          "description": "",
          "type": { "TypeName": "Enum", "enumName": "UserRole" },
          "memberDescription": ""
        }
      ]
    }
  ],
  "enums": [
    {
      "name": "UserRole",
      "description": "",
      "values": ["Admin", "Manager", "User", "Guest"]
    }
  ],
  "dataSources": [
    {
      "name": "Users",
      "description": "",
      "file": "users.json",
      "className": "User"
    }
  ],
  "codeGenerators": []
}
```

## Loading and Using the Schema

Once created, you can load and use the schema:

```csharp
// Load the schema from a JSON file
string json = File.ReadAllText("user-management.schema.json");
if (SchemaSerializer.TryDeserialize(json, out Schema? loadedSchema))
{
    // Access classes
    if (loadedSchema.TryGetClass("User".As<ClassName>(), out SchemaClass? userClass))
    {
        Console.WriteLine($"User class has {userClass.Members.Count} members");

        // List all members
        foreach (SchemaMember member in userClass.Members)
        {
            Console.WriteLine($"  {member.Name}: {member.Type.DisplayName}");
        }
    }

    // Access enums
    if (loadedSchema.TryGetEnum("UserRole".As<EnumName>(), out SchemaEnum? roleEnum))
    {
        Console.WriteLine($"UserRole enum has {roleEnum.Values.Count} values");

        // List all values
        foreach (EnumValueName value in roleEnum.Values)
        {
            Console.WriteLine($"  {value}");
        }
    }

    // Access data sources
    foreach (DataSource dataSource in loadedSchema.DataSources)
    {
        Console.WriteLine($"Data source: {dataSource.Name} -> {dataSource.File}");
    }
}
```

## Key Concepts

### Strong String Types

-   `ClassName`, `MemberName`, `EnumName` etc. provide compile-time type safety
-   Convert regular strings using `.As<T>()` extension method
-   Prevents mixing up different kinds of names

### Type System

-   All types inherit from `SchemaTypes.BaseType`
-   Primitive types are simple instances
-   Complex types (Array, Object, Enum) have additional properties
-   Types are polymorphic and serializable

### Error Handling

-   Methods return `null` when operations fail (e.g., duplicate names)
-   Use `Try...` methods for explicit success/failure checking
-   Use `SchemaSerializer` for JSON serialization and deserialization

## See Also

-   [Getting Started](../getting-started.md) - Basic setup and concepts
-   [Schema Editor](../features/schema-editor.md) - Visual editing
