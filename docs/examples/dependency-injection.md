# Dependency Injection

The Schema library focuses solely on schema definition — serialization is handled by the separate `SchemaSerializer` class and there are no filesystem concerns baked into the model. This makes it straightforward to use in dependency injection scenarios.

You can inject either the concrete `Schema` or the `ISchema` contract. Inject `ISchema` when a consumer only needs to read or edit a schema and you want it substitutable in tests; inject `Schema` when the consumer needs the parts that are not on the contract — serialization, validation, path resolution, data sources or code generators.

## Basic Setup

### Using Microsoft.Extensions.DependencyInjection

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

var builder = Host.CreateApplicationBuilder(args);

// Register a pre-configured schema as a singleton
builder.Services.AddSingleton<Schema>(provider =>
{
    Schema schema = new();
    schema.AddClass("User".As<ClassName>());
    schema.AddEnum("Role".As<EnumName>());
    return schema;
});

var host = builder.Build();
```

### Registering the Contract

`Schema` implements `ISchema`, so a consumer can depend on the abstraction instead of the model:

```csharp
using ktsu.Schema.Contracts;

builder.Services.AddSingleton<ISchema>(provider =>
{
    Schema schema = new();
    schema.AddClass("User".As<ClassName>());
    return schema;
});
```

### Loading a Schema from a File

```csharp
builder.Services.AddSingleton<Schema>(provider =>
{
    string json = File.ReadAllText("app.schema.json");
    return SchemaSerializer.TryDeserialize(json, out Schema? schema) && schema is not null
        ? schema
        : throw new InvalidOperationException("Failed to load app.schema.json");
});
```

## Consuming the Schema

### Through the contract

A service that only defines and reads schema elements needs nothing but `ISchema`:

```csharp
using ktsu.Schema.Contracts;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using SchemaTypes = ktsu.Schema.Models.Types;

public class UserSchemaBuilder(ISchema schema)
{
    public void Define()
    {
        ISchemaClass? user = schema.AddClass("User".As<ClassName>());
        user?.AddMember("Name".As<MemberName>())?.SetType(new SchemaTypes.String());
        user?.AddMember("Email".As<MemberName>())?.SetType(new SchemaTypes.String());
    }

    public void Describe()
    {
        foreach (ISchemaClass schemaClass in schema.Classes)
        {
            Console.WriteLine($"{schemaClass.Name} ({schemaClass.Members.Count} members)");
        }
    }
}
```

`Classes` and `Members` are name-indexed and preserve declaration order, so `GetByName` and
`ContainsByName` avoid scanning by hand:

```csharp
if (schema.Classes.GetByName("User".As<ClassName>()) is ISchemaClass user
    && user.Members.ContainsByName("Email".As<MemberName>()))
{
    // ...
}
```

### Through the concrete type

```csharp
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using SchemaTypes = ktsu.Schema.Models.Types;

public class MyService
{
    private readonly Schema _schema;

    public MyService(Schema schema)
    {
        _schema = schema;
    }

    public void DefineUserSchema()
    {
        SchemaClass? userClass = _schema.AddClass("User".As<ClassName>());
        if (userClass != null)
        {
            userClass.AddMember("Name".As<MemberName>())?.SetType(new SchemaTypes.String());
            userClass.AddMember("Email".As<MemberName>())?.SetType(new SchemaTypes.String());
        }
    }

    public void DescribeSchema()
    {
        foreach (SchemaClass schemaClass in _schema.Classes)
        {
            Console.WriteLine($"{schemaClass.Name} ({schemaClass.Members.Count} members)");
        }
    }
}
```

## Wrapping the Schema in Your Own Abstraction

`ISchema` abstracts what a schema *is*. It deliberately says nothing about where a schema comes from or what happens to it — loading, saving, caching and change tracking are your application's concerns, not the model's. If you need those, wrap `Schema` in your own service interface:

```csharp
public interface ISchemaService
{
    Schema Current { get; }
    void Save();
}

public class FileSchemaService : ISchemaService
{
    private readonly string _path;

    public FileSchemaService(string path)
    {
        _path = path;
        string json = File.ReadAllText(path);
        Current = SchemaSerializer.TryDeserialize(json, out Schema? schema) && schema is not null
            ? schema
            : new Schema();
    }

    public Schema Current { get; }

    public void Save() => File.WriteAllText(_path, SchemaSerializer.Serialize(Current));
}

// Registration
builder.Services.AddSingleton<ISchemaService>(_ => new FileSchemaService("app.schema.json"));
```

## Notes

-   `Schema` is not thread-safe; if multiple services mutate a shared schema concurrently, provide your own synchronization.
-   `ISchema` covers classes, enums, members and types. Serialization, validation, path resolution, data sources and code generators are on the concrete `Schema` — a consumer that needs them should take `Schema`, or your own abstraction over it.
-   The collections on the contracts are read-only views. Add and remove through `ISchema`/`ISchemaClass`, which is what enforces name uniqueness and gives a new element the parent reference it needs to resolve its own type references.
-   Register schemas as singletons when they represent application-wide definitions; use factories or scoped services if each scope needs an independent copy.

## Navigation

-   **[Examples](README.md)** - All examples
-   **[Basic Schema Creation](basic-schema.md)** - Building a schema from scratch
-   **[API Reference](../api/schema-core.md)** - Schema and SchemaSerializer details
