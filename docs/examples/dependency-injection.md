# Dependency Injection

The Schema library focuses solely on schema definition — serialization is handled by the separate `SchemaSerializer` class and there are no filesystem concerns baked into the model. This makes it straightforward to use in dependency injection scenarios.

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

If your application needs schema management behavior (loading, saving, caching, change tracking), wrap `Schema` in your own service interface so the rest of your code depends on your abstraction rather than the library type:

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
-   Register schemas as singletons when they represent application-wide definitions; use factories or scoped services if each scope needs an independent copy.

## Navigation

-   **[Examples](README.md)** - All examples
-   **[Basic Schema Creation](basic-schema.md)** - Building a schema from scratch
-   **[API Reference](../api/schema-core.md)** - Schema and SchemaSerializer details
