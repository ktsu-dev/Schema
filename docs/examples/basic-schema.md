# Basic Schema Creation

This example demonstrates how to create a simple schema from scratch, including classes, members, enums, and data sources.

## Overview

We'll create a schema for a simple user management system with:

-   A `User` class with basic properties
-   A `UserRole` enum for user permissions
-   A data source for user data

## Complete Example

```csharp
using ktsu.Schema;
using ktsu.StrongPaths;

class Program
{
    static void Main()
    {
        // Create a new schema
        var schema = new Schema();

        // Set the file path for the schema
        schema.ChangeFilePath("user-management.schema.json".As<AbsoluteFilePath>());

        // Create the schema structure
        CreateUserRoleEnum(schema);
        CreateUserClass(schema);
        CreateDataSource(schema);

        // Save the schema
        schema.Save();

        Console.WriteLine("Schema created successfully!");
        Console.WriteLine($"Classes: {schema.Classes.Count}");
        Console.WriteLine($"Enums: {schema.Enums.Count}");
        Console.WriteLine($"Data Sources: {schema.DataSources.Count}");
    }

    static void CreateUserRoleEnum(Schema schema)
    {
        // Add a UserRole enum
        var userRoleEnum = schema.AddEnum("UserRole".As<EnumName>());
        if (userRoleEnum != null)
        {
            // Add enum values
            userRoleEnum.AddValue("Admin".As<EnumValueName>());
            userRoleEnum.AddValue("Manager".As<EnumValueName>());
            userRoleEnum.AddValue("User".As<EnumValueName>());
            userRoleEnum.AddValue("Guest".As<EnumValueName>());
        }
    }

    static void CreateUserClass(Schema schema)
    {
        // Add a User class
        var userClass = schema.AddClass("User".As<ClassName>());
        if (userClass == null) return;

        // Add basic properties
        var idMember = userClass.AddMember("Id".As<MemberName>());
        var usernameMember = userClass.AddMember("Username".As<MemberName>());
        var emailMember = userClass.AddMember("Email".As<MemberName>());
        var firstNameMember = userClass.AddMember("FirstName".As<MemberName>());
        var lastNameMember = userClass.AddMember("LastName".As<MemberName>());
        var createdAtMember = userClass.AddMember("CreatedAt".As<MemberName>());
        var isActiveMember = userClass.AddMember("IsActive".As<MemberName>());
        var roleMember = userClass.AddMember("Role".As<MemberName>());

        // Set member types
        idMember.Type = new SchemaTypes.Int();
        usernameMember.Type = new SchemaTypes.String();
        emailMember.Type = new SchemaTypes.String();
        firstNameMember.Type = new SchemaTypes.String();
        lastNameMember.Type = new SchemaTypes.String();
        createdAtMember.Type = new SchemaTypes.DateTime();
        isActiveMember.Type = new SchemaTypes.Bool();
        roleMember.Type = new SchemaTypes.Enum { EnumName = "UserRole".As<EnumName>() };
    }

    static void CreateDataSource(Schema schema)
    {
        // Add a data source for users
        var usersDataSource = schema.AddDataSource("Users".As<DataSourceName>());
        if (usersDataSource != null && schema.TryGetClass("User".As<ClassName>(), out var userClass))
        {
            usersDataSource.File = "users.json".As<RelativeFilePath>();
            usersDataSource.Class = userClass;
        }
    }
}
```

## Step-by-Step Breakdown

### 1. Creating the Schema

```csharp
var schema = new Schema();
schema.ChangeFilePath("user-management.schema.json".As<AbsoluteFilePath>());
```

-   Create a new `Schema` instance
-   Set the file path where the schema will be saved
-   The `.As<AbsoluteFilePath>()` extension converts the string to a strongly-typed path

### 2. Adding an Enum

```csharp
var userRoleEnum = schema.AddEnum("UserRole".As<EnumName>());
userRoleEnum?.AddValue("Admin".As<EnumValueName>());
userRoleEnum?.AddValue("Manager".As<EnumValueName>());
userRoleEnum?.AddValue("User".As<EnumValueName>());
userRoleEnum?.AddValue("Guest".As<EnumValueName>());
```

-   Use `AddEnum()` to create a new enumeration
-   Add values using `AddValue()` with strongly-typed enum value names
-   Each value represents a possible choice for the enum

### 3. Adding a Class

```csharp
var userClass = schema.AddClass("User".As<ClassName>());
```

-   Use `AddClass()` to create a new schema class
-   Class names use the `ClassName` strong string type for type safety

### 4. Adding Members to the Class

```csharp
var idMember = userClass.AddMember("Id".As<MemberName>());
idMember.Type = new SchemaTypes.Int();
```

-   Use `AddMember()` to add properties to a class
-   Set the type using the appropriate `SchemaTypes` class
-   Each member has a name and a type

### 5. Setting Different Types

```csharp
// Primitive types
idMember.Type = new SchemaTypes.Int();
usernameMember.Type = new SchemaTypes.String();
isActiveMember.Type = new SchemaTypes.Bool();
createdAtMember.Type = new SchemaTypes.DateTime();

// Enum reference
roleMember.Type = new SchemaTypes.Enum { EnumName = "UserRole".As<EnumName>() };
```

-   Primitive types are straightforward instances
-   Enum types reference an enum by name
-   The enum must exist in the schema for the reference to be valid

### 6. Adding a Data Source

```csharp
var usersDataSource = schema.AddDataSource("Users".As<DataSourceName>());
usersDataSource.File = "users.json".As<RelativeFilePath>();
usersDataSource.Class = userClass;
```

-   Data sources link schema classes to actual data files
-   Specify the file path relative to the schema location
-   Associate with a schema class to define the data structure

### 7. Saving the Schema

```csharp
schema.Save();
```

-   Saves the schema to the file path specified earlier
-   Creates the directory if it doesn't exist
-   Uses atomic file operations for safety

## Generated Schema File

The resulting `user-management.schema.json` file will look like this:

```json
{
    "RelativePaths": {
        "RelativeProjectRootPath": "",
        "RelativeDataSourcePath": ""
    },
    "Classes": [
        {
            "Name": "User",
            "Members": [
                {
                    "Name": "Id",
                    "Type": {
                        "TypeName": "Int"
                    }
                },
                {
                    "Name": "Username",
                    "Type": {
                        "TypeName": "String"
                    }
                },
                {
                    "Name": "Email",
                    "Type": {
                        "TypeName": "String"
                    }
                },
                {
                    "Name": "FirstName",
                    "Type": {
                        "TypeName": "String"
                    }
                },
                {
                    "Name": "LastName",
                    "Type": {
                        "TypeName": "String"
                    }
                },
                {
                    "Name": "CreatedAt",
                    "Type": {
                        "TypeName": "DateTime"
                    }
                },
                {
                    "Name": "IsActive",
                    "Type": {
                        "TypeName": "Bool"
                    }
                },
                {
                    "Name": "Role",
                    "Type": {
                        "TypeName": "Enum",
                        "EnumName": "UserRole"
                    }
                }
            ]
        }
    ],
    "Enums": [
        {
            "Name": "UserRole",
            "Values": [
                {
                    "Name": "Admin"
                },
                {
                    "Name": "Manager"
                },
                {
                    "Name": "User"
                },
                {
                    "Name": "Guest"
                }
            ]
        }
    ],
    "DataSources": [
        {
            "Name": "Users",
            "File": "users.json",
            "Class": {
                "ClassName": "User"
            }
        }
    ],
    "CodeGenerators": []
}
```

## Loading and Using the Schema

Once created, you can load and use the schema:

```csharp
// Load the schema
if (Schema.TryLoad("user-management.schema.json".As<AbsoluteFilePath>(), out Schema? loadedSchema))
{
    // Access classes
    if (loadedSchema.TryGetClass("User".As<ClassName>(), out SchemaClass? userClass))
    {
        Console.WriteLine($"User class has {userClass.Members.Count} members");

        // List all members
        foreach (var member in userClass.Members)
        {
            Console.WriteLine($"  {member.Name}: {member.Type.DisplayName}");
        }
    }

    // Access enums
    if (loadedSchema.TryGetEnum("UserRole".As<EnumName>(), out SchemaEnum? roleEnum))
    {
        Console.WriteLine($"UserRole enum has {roleEnum.Values.Count} values");

        // List all values
        foreach (var value in roleEnum.Values)
        {
            Console.WriteLine($"  {value.Name}");
        }
    }

    // Access data sources
    foreach (var dataSource in loadedSchema.DataSources)
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
-   Schema validation occurs during save operations

## Next Steps

-   Try the [Schema Editor](../features/schema-editor.md) for visual editing
-   Learn about [Working with Types](working-with-types.md) for complex scenarios
-   Explore [Collections and Arrays](collections-examples.md) for advanced data structures
-   See [Code Generation Examples](code-generation-examples.md) for generating code from schemas

## See Also

-   [Getting Started](../getting-started.md) - Basic setup and concepts
-   [API Reference](../api/schema-core.md) - Detailed Schema class documentation
-   [Type System](../features/type-system.md) - Understanding the type system
-   [Schema Definition](../features/schema-definition.md) - Advanced schema creation
