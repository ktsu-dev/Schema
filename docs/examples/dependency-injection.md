---
title: Dependency Injection with SchemaProvider
description: How to use SchemaProvider in a dependency injection container
status: draft
---

# Dependency Injection with SchemaProvider

The Schema library has been refactored to focus solely on schema definition, removing serialization and filesystem concerns. This makes it ideal for dependency injection scenarios.

## Basic Setup

### Using Microsoft.Extensions.DependencyInjection

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ktsu.Schema;

// Configure services
var builder = Host.CreateApplicationBuilder(args);

// Register SchemaProvider as a singleton
builder.Services.AddSingleton<ISchemaProvider, SchemaProvider>();

// Or register a pre-configured schema provider
builder.Services.AddSingleton<ISchemaProvider>(provider =>
{
    var schemaProvider = new SchemaProvider();
    
    // Pre-configure with some schema definitions
    schemaProvider.AddClass((ClassName)"User");
    schemaProvider.AddEnum((EnumName)"Role");
    
    return schemaProvider;
});

var host = builder.Build();
```

### Using the SchemaProvider

```csharp
public class MyService
{
    private readonly ISchemaProvider _schemaProvider;
    
    public MyService(ISchemaProvider schemaProvider)
    {
        _schemaProvider = schemaProvider;
    }
    
    public void DefineUserSchema()
    {
        // Add a User class to the schema
        var userClass = _schemaProvider.AddClass((ClassName)"User");
        if (userClass != null)
        {
            // Add members to the User class
            var nameProperty = userClass.AddMember((MemberName)"Name");
            nameProperty?.SetType(new SchemaTypes.String());
            
            var ageProperty = userClass.AddMember((MemberName)"Age");
            ageProperty?.SetType(new SchemaTypes.Integer());
        }
        
        // Add an enum for user roles
        var roleEnum = _schemaProvider.AddEnum((EnumName)"UserRole");
        if (roleEnum != null)
        {
            roleEnum.TryAddValue((EnumValueName)"Admin");
            roleEnum.TryAddValue((EnumValueName)"User");
            roleEnum.TryAddValue((EnumValueName)"Guest");
        }
    }
    
    public void QuerySchema()
    {
        // Get all classes
        foreach (var schemaClass in _schemaProvider.Classes)
        {
            Console.WriteLine($"Class: {schemaClass.Name}");
            foreach (var member in schemaClass.Members)
            {
                Console.WriteLine($"  {member.Name}: {member.Type}");
            }
        }
        
        // Get all enums
        foreach (var schemaEnum in _schemaProvider.Enums)
        {
            Console.WriteLine($"Enum: {schemaEnum.Name}");
            foreach (var value in schemaEnum.Values)
            {
                Console.WriteLine($"  {value}");
            }
        }
    }
}
```

### Adding Schemas from .NET Types

```csharp
public class UserDto
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public UserRole Role { get; set; }
}

public enum UserRole
{
    Admin,
    User,
    Guest
}

public class SchemaService
{
    private readonly ISchemaProvider _schemaProvider;
    
    public SchemaService(ISchemaProvider schemaProvider)
    {
        _schemaProvider = schemaProvider;
    }
    
    public void RegisterTypes()
    {
        // Automatically create schema from .NET types
        _schemaProvider.AddClass(typeof(UserDto));
        
        // The enum will be automatically added when processing UserDto
        // But you can also add it explicitly:
        // _schemaProvider.AddClass(typeof(UserRole));
    }
}
```

## Separation of Concerns

The refactored SchemaProvider focuses solely on:

- **Schema Definition**: Defining classes, enums, and their relationships
- **Schema Querying**: Retrieving schema information
- **Type Management**: Managing schema types and their metadata

It does **NOT** handle:

- **Serialization**: Use separate libraries like System.Text.Json, Newtonsoft.Json, etc.
- **File I/O**: Use separate services for loading/saving schema definitions
- **Validation**: Use separate validation libraries
- **Code Generation**: Use separate code generation tools

## Integration with Other Services

```csharp
// Separate service for serialization
public class SchemaSerializationService
{
    private readonly ISchemaProvider _schemaProvider;
    private readonly IJsonSerializer _jsonSerializer;
    
    public SchemaSerializationService(
        ISchemaProvider schemaProvider, 
        IJsonSerializer jsonSerializer)
    {
        _schemaProvider = schemaProvider;
        _jsonSerializer = jsonSerializer;
    }
    
    public string SerializeSchema()
    {
        // Use your preferred serialization library
        return _jsonSerializer.Serialize(_schemaProvider);
    }
}

// Separate service for file operations
public class SchemaFileService
{
    private readonly ISchemaProvider _schemaProvider;
    private readonly IFileService _fileService;
    
    public SchemaFileService(
        ISchemaProvider schemaProvider,
        IFileService fileService)
    {
        _schemaProvider = schemaProvider;
        _fileService = fileService;
    }
    
    public async Task SaveSchemaAsync(string filePath)
    {
        var serializedSchema = SerializeSchema();
        await _fileService.WriteAllTextAsync(filePath, serializedSchema);
    }
    
    private string SerializeSchema()
    {
        // Implement your serialization logic here
        return System.Text.Json.JsonSerializer.Serialize(_schemaProvider);
    }
}
```

## Benefits of This Approach

1. **Single Responsibility**: SchemaProvider only handles schema definition
2. **Testability**: Easy to mock and unit test
3. **Flexibility**: Use any serialization or file system library you prefer
4. **Dependency Injection**: Works seamlessly with DI containers
5. **Separation of Concerns**: Clean architecture with clearly defined responsibilities

## Migration from Legacy Schema Class

If you're migrating from the legacy `Schema` class:

```csharp
// Old approach (deprecated)
[Obsolete]
var schema = new Schema();
schema.AddClass((ClassName)"User");

// New approach (recommended)
ISchemaProvider schemaProvider = new SchemaProvider();
schemaProvider.AddClass((ClassName)"User");

// Or via DI
public MyService(ISchemaProvider schemaProvider)
{
    _schemaProvider = schemaProvider;
}
``` 