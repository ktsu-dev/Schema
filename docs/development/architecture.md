# Architecture Overview

This document provides a high-level overview of the Schema library architecture, design principles, and key patterns used throughout the codebase.

## Design Principles

### 🎯 **Type Safety**

-   Extensive use of strong string types to prevent parameter confusion
-   Compile-time type checking wherever possible
-   Nullable reference types for clear null-safety contracts

### 🔄 **Immutability & State Management**

-   Schema objects maintain internal state but provide controlled mutation
-   Copy-on-write patterns for performance
-   Clear separation between public and internal APIs

### 📐 **Single Responsibility**

-   Each class has a clear, focused purpose
-   Separation of concerns between data modeling and UI
-   Modular design allowing independent component evolution

### 🔌 **Extensibility**

-   Plugin architecture for custom types
-   Interface-based design for key abstractions
-   Clear extension points for customization

## System Architecture

### High-Level Overview

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Schema.Test   │    │  SchemaEditor   │    │  Client Apps    │
│   (Unit Tests)  │    │  (ImGui App)    │    │  (Consumers)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
                  ┌─────────────────────────┐
                  │       Schema            │
                  │    (Core Library)       │
                  │                         │
                  │  ┌─────────────────┐   │
                  │  │  Schema Types   │   │
                  │  └─────────────────┘   │
                  │  ┌─────────────────┐   │
                  │  │ Strong Strings  │   │
                  │  └─────────────────┘   │
                  │  ┌─────────────────┐   │
                  │  │   Data Model    │   │
                  │  └─────────────────┘   │
                  └─────────────────────────┘
                              │
                ┌─────────────────────────────┐
                │     ktsu.dev Libraries      │
                │                             │
                │ ┌─────────┐ ┌─────────────┐ │
                │ │ Strong  │ │    ImGui    │ │
                │ │ Paths   │ │    Apps     │ │
                │ └─────────┘ └─────────────┘ │
                │ ┌─────────┐ ┌─────────────┐ │
                │ │ Strong  │ │    JSON     │ │
                │ │ Strings │ │ Converters  │ │
                │ └─────────┘ └─────────────┘ │
                └─────────────────────────────┘
```

## Core Components

### Schema (Root Container)

```csharp
public partial class Schema
{
    // Collections of schema elements
    public IReadOnlyCollection<SchemaClass> Classes { get; }
    public IReadOnlyCollection<SchemaEnum> Enums { get; }
    public IReadOnlyCollection<DataSource> DataSources { get; }

    // File management
    public AbsoluteFilePath FilePath { get; }
    public SchemaPaths RelativePaths { get; }

    // CRUD operations
    public SchemaClass? AddClass(ClassName name);
    public bool TryGetClass(ClassName name, out SchemaClass? schemaClass);
    // ... similar for Enums, DataSources
}
```

**Responsibilities:**

-   Container for all schema elements
-   File I/O operations (load/save)
-   Element lifecycle management
-   Cross-reference validation

### Schema Elements Hierarchy

```
SchemaChild<T> (Abstract Base)
├── SchemaClass : SchemaChild<ClassName>
├── SchemaEnum : SchemaChild<EnumName>
├── DataSource : SchemaChild<DataSourceName>
└── SchemaCodeGenerator : SchemaChild<CodeGeneratorName>

SchemaMemberChild<T> (Abstract Base)
└── SchemaTypes.BaseType : SchemaMemberChild<BaseTypeName>
    ├── Primitive Types (Int, String, Bool, etc.)
    ├── Vector Types (Vector2, Vector3, etc.)
    ├── Complex Types (Array, Object, Enum)
    └── System Types (DateTime, TimeSpan)

SchemaMember : SchemaMemberChild<MemberName>
└── Contains: SchemaTypes.BaseType
```

### Type System Architecture

The type system is built around a polymorphic hierarchy:

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "TypeName")]
public abstract class BaseType : SchemaMemberChild<BaseTypeName>
{
    // Core type functionality
    public abstract string DisplayName { get; }
    public bool IsBuiltIn { get; }
    public bool IsPrimitive { get; }
    // ... type classification properties
}
```

**Key Design Decisions:**

-   **Polymorphic JSON Serialization**: Uses `System.Text.Json` with type discriminators
-   **Type Classification**: Properties like `IsBuiltIn`, `IsPrimitive` for runtime decisions
-   **Extensibility**: New types can be added by inheriting from `BaseType`

### Strong String Types

```csharp
public sealed record class ClassName : StrongStringAbstract<ClassName> { }
public sealed record class MemberName : StrongStringAbstract<MemberName> { }
public sealed record class EnumName : StrongStringAbstract<EnumName> { }
// ... etc
```

**Benefits:**

-   Compile-time prevention of parameter confusion
-   Implicit conversion from strings via `.As<T>()` extension
-   JSON serialization as plain strings
-   IntelliSense and refactoring support

## Design Patterns

### Repository Pattern (Schema as Repository)

```csharp
// Schema acts as a repository for schema elements
public SchemaClass? GetClass(ClassName name);
public bool TryGetClass(ClassName name, out SchemaClass? schemaClass);
public SchemaClass? AddClass(ClassName name);
public bool TryRemoveClass(SchemaClass schemaClass);
```

### Builder Pattern (Schema Construction)

```csharp
var schema = new Schema();
var userClass = schema.AddClass("User".As<ClassName>());
var nameMember = userClass?.AddMember("Name".As<MemberName>());
nameMember.Type = new SchemaTypes.String();
```

### Visitor Pattern (Type Operations)

```csharp
// Type system supports visitor-like operations
public bool IsNumeric => this switch
{
    Int or Long or Float or Double => true,
    _ => false
};
```

### Observer Pattern (Parent-Child Relationships)

```csharp
public abstract class SchemaChild<TName>
{
    protected Schema? ParentSchema { get; private set; }

    public void AssociateWith(Schema schema)
    {
        ParentSchema = schema;
    }
}
```

## Data Flow

### Schema Creation Flow

```
1. Create Schema Instance
   ↓
2. Add Classes/Enums
   ↓
3. Add Members to Classes
   ↓
4. Set Member Types
   ↓
5. Add Data Sources
   ↓
6. Save to File
```

### Schema Loading Flow

```
1. Read JSON File
   ↓
2. Deserialize to Schema
   ↓
3. Reassociate Parent References
   ↓
4. Validate Type References
   ↓
5. Ready for Use
```

### Type Resolution Flow

```
Member.Type Reference
   ↓
Check Type Category
   ├─ Primitive → Direct Use
   ├─ Array → Resolve Element Type
   ├─ Object → Lookup Class by Name
   └─ Enum → Lookup Enum by Name
```

## Memory Management

### Object Lifecycle

```
Schema (Root)
├── Collections<T> (Internal)
│   ├── SchemaClass instances
│   ├── SchemaEnum instances
│   └── DataSource instances
└── File I/O (Transient)
```

**Key Points:**

-   Schema holds strong references to all elements
-   Elements hold weak references back to schema (via interface)
-   JSON serialization creates temporary object graphs
-   No circular reference issues due to careful design

### Performance Considerations

-   **Lazy Loading**: Type references resolved on-demand
-   **Caching**: Display names and computed properties cached
-   **Batching**: Multiple operations can be batched before save
-   **Memory**: Large schemas kept in memory, but not duplicated

## Extension Points

### Custom Types

```csharp
public class MyCustomType : SchemaTypes.BaseType
{
    public override string DisplayName => "MyCustom";

    // Implement required abstract members
}

// Register via JSON polymorphic attributes
[JsonDerivedType(typeof(MyCustomType), nameof(MyCustomType))]
```

### Custom Serialization

```csharp
public class CustomConverter : JsonConverter<MyType>
{
    // Implement custom JSON handling
}

// Add to Schema.JsonSerializerOptions
```

### Custom Validation

```csharp
public class SchemaValidator
{
    public ValidationResult Validate(Schema schema)
    {
        // Custom validation logic
    }
}
```

## Error Handling Strategy

### Graceful Degradation

```csharp
// Methods return null instead of throwing for expected failures
public SchemaClass? AddClass(ClassName name)
{
    if (Classes.Any(c => c.Name == name))
        return null; // Duplicate name

    // Add class
}
```

### Try Pattern for Explicit Checking

```csharp
// Try methods for explicit success/failure handling
public bool TryGetClass(ClassName name, out SchemaClass? schemaClass)
{
    schemaClass = Classes.FirstOrDefault(c => c.Name == name);
    return schemaClass != null;
}
```

### Exception Throwing for Programming Errors

```csharp
// Throw exceptions for programming errors
public SchemaMember AddMember(MemberName memberName) =>
    ParentSchema?.AddChild(memberName, MembersInternal) ??
    throw new InvalidOperationException("Cannot add to unassociated class");
```

## Threading Model

### Not Thread-Safe by Design

-   Schema objects are designed for single-threaded use
-   Consumers must provide synchronization if needed
-   Rationale: Simplifies implementation, matches typical usage patterns

### Safe Points for Concurrency

-   **Reading**: Multiple threads can safely read after construction
-   **Immutable Parts**: Type definitions, names, etc. are effectively immutable
-   **File I/O**: Can be safely parallelized across different schemas

## Future Architecture Considerations

### Planned Enhancements

1. **Plugin System**: Runtime loading of custom types and generators
2. **Schema Diffing**: Compare schemas and generate migration scripts
3. **Validation Rules**: Declarative validation system
4. **Multi-Schema Support**: References between schemas

### Backward Compatibility

-   JSON format versioning strategy
-   Migration support for schema file formats
-   API evolution following semantic versioning

## Testing Architecture

### Test Organization

```
Schema.Test/
├── Unit Tests
│   ├── Schema Core Tests
│   ├── Type System Tests
│   └── Strong String Tests
├── Integration Tests
│   ├── File I/O Tests
│   └── Serialization Tests
└── Performance Tests
    └── Large Schema Tests
```

### Test Patterns

-   **Arrange-Act-Assert**: Standard test structure
-   **Builder Pattern**: Test data creation helpers
-   **Parameterized Tests**: Testing type system variations
-   **Property-Based Testing**: For serialization round-trips

## See Also

-   [Project Structure](project-structure.md) - Detailed code organization
-   [API Reference](../api/) - Public API documentation
-   [Contributing](contributing.md) - Development workflow
-   [Performance](performance.md) - Performance optimization guide
