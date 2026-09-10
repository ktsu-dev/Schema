# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Schema is a C# library for defining and managing data structure schemas. It consists of seven projects:

- **Schema** - Core library providing schema definition types (classes, enums, members, types)
- **Schema.Test** - MSTest unit tests for the core library
- **Schema.Cpp** - The C++ generator, in its own project because `ktsu.Coder` ships no `net8.0`
- **Schema.Cpp.Test** - Its tests, including the three acceptance tests against Holotype's target document
- **SchemaEditor** - ImGui-based visual editor application for creating and editing `.schema.json` files
- **SchemaEditor.Test** - Headless UI tests for the editor, driven through `ktsu.ImGui.App.Testing`
- **SchemaTool** - Command line entry point for validating schemas and running their code generators

## Build Commands

```bash
dotnet build              # Build entire solution
dotnet test               # Run all tests
dotnet test --filter "FullyQualifiedName~TestName"  # Run specific test
dotnet run --project SchemaEditor  # Launch the visual editor
dotnet run --project SchemaTool -- generate my.schema.json  # Run a schema's code generators
```

## Architecture

### Core Type Hierarchy

The type system uses polymorphic JSON serialization with `System.Text.Json`:

```
SchemaChild<TName> (base for named elements)
├── SchemaClass : SchemaChild<ClassName>
├── SchemaEnum : SchemaChild<EnumName>
├── SchemaInterface : SchemaChild<InterfaceName>
├── SchemaSemanticType : SchemaChild<SemanticTypeName>
├── DataSource : SchemaChild<DataSourceName>
├── SchemaCodeGenerator : SchemaChild<CodeGeneratorName>
├── SchemaClassChild<TName> : SchemaChild<TName>
│   └── SchemaMember : SchemaClassChild<MemberName>
├── SchemaInterfaceChild<TName> : SchemaChild<TName>
│   └── SchemaFunction : SchemaInterfaceChild<FunctionName>
└── SchemaFunctionChild<TName> : SchemaChild<TName>
    └── SchemaParameter : SchemaFunctionChild<ParameterName>

BaseType (types, in ktsu.Schema.Models.Types)
├── Primitives: Int, Long, Float, Double, String, Bool, DateTime, TimeSpan
├── Vectors: Vector2, Vector3, Vector4, ColorRGB, ColorRGBA
├── Complex: Array, Object, Enum, Interface, Semantic, None, Void
└── Wrappers: Span, Handle, Result, Optional
```

### Vector components

A vector says how many components it has; `Vector.ElementType` says what each one is. A velocity is
three metres per second and a position is three metres, and a schema that says only "three floats"
leaves the one fact worth knowing about either of them to a comment - which is the argument
`Semantic` makes for a single value, applied to three of them.

The component must be a number or a `Semantic` over one; anything else is a collection of things
rather than one value with components, which is what `Array` is for. It defaults to `Float` and is
omitted from the file when it is, so a file whose vectors are vectors of floats is written exactly
as it was before the property existed - the omission is `SchemaSerializer.OmitDefaultVectorElement`,
because a non-nullable property has no ignore condition that means "the same as saying nothing".
`ColorRGB` and `ColorRGBA` inherit the property because they derive from `Vector3` and `Vector4`,
and validation holds them to `Float`: a colour's components are its channels.

The editor's type picker does not offer a component yet, for the same reason it does not offer a
`Semantic`, a `Span` or a `Handle` - `Schema.GetSelectableElementTypes` still lists only the
built-ins, the enums and the classes.

### Semantic types

An entity id is a number, and so is a texture id, and adding one to the other is nonsense that
compiles. `SchemaSemanticType` is how the schema says they are different things: both stored as a
`Long`, neither interchangeable with the other or with a bare number. Refer to one with the
`Semantic` type.

Two conventions, held by the schema rather than restated per declaration: crossing into or out of
the underlying type is always **explicit**, and a semantic type refining another **widens
implicitly and narrows explicitly** (a `Weight` is a `ForceMagnitude`; not every force is a weight).

A semantic type may only shim something whose values would otherwise be interchangeable. An
`Object`, `Interface` or `Enum` is already a distinct type, and an `Array`, `Span`, `Handle`,
`Result` or `Optional` describes how a value is carried rather than what it is; both are refused,
as are refinement cycles.

`ISchemaMetadataCarrier` is the shared shape for the six semantic properties. A member carries them
because a use is often where a unit or range is decided; a semantic type carries them because for
some values those facts belong to the type (`Metres` is metres everywhere). The rules are identical
either way, so validation reads the interface rather than each carrier growing its own copy.

### What a class promises about its representation

`SchemaClass.TravelsAsBytes` says an instance is copied whole - across a language boundary, into a
save file, onto the wire - without anyone reading a field on the way. That makes member order
load-bearing rather than merely meaningful, and it constrains the members: a `String`, an `Array`, a
`Span`, an `Optional`, a `Result`, an `Interface` or an `Object` naming a class that makes no such
promise are all refused. A `Handle` is accepted, because an index and a generation is bytes whatever
it identifies.

`Schema.Validation.cs` reads the flag off a named class rather than walking into it, so two classes
holding each other validates rather than hanging the validator. It is off by default and omitted
from the file when it is, so a class that says nothing about how it travels is what a class has
always been.

Nothing about a CLR type says this, so generated C# carries `SchemaTravelsAsBytesAttribute` and
`ClrTypeImporter` reads it back - the same arrangement the member metadata uses, and covered by the
same generate-compile-reimport test. The attribute records the promise rather than enforcing it:
emitting a sequential-layout `struct` would enforce it and would also change every generated
component from a class to a value type, which is a decision about the C# API rather than about the
schema.

### Declaring behaviour

A class says what data is; an interface says what the program can do. `SchemaInterface` holds
`SchemaFunction`s, each holding ordered `SchemaParameter`s and a return type, and a generator turns
one into the header an implementation is written against.

A signature carries no ownership, lifetime or error annotations because four conventions carry that
weight globally instead: fallibility is `Result` in the return type, something the caller may keep
is a `Handle`, a borrow valid for the call is a `Span`, and const-ness is
`SchemaParameter.Direction`. Every interface language that let signatures answer those individually
grew annotations until it was a worse version of the language it described; keeping them global is
what stops that. `Schema.Validation.cs` enforces the corollaries - no `Array`, `Result` or `Void`
parameters, no `None` anywhere generatable.

What a failure *says* is global for the same reason: `Schema.ErrorType` names the enum a failed
`Result` carries, once, rather than every fallible signature choosing its own. An enum rather than
any type, because an error is one of a closed set of reasons. A schema that never returns a `Result`
needs none; one that does is reported at the signature, which is the declaration whose meaning is
incomplete.

On a `Span`, direction describes the **elements**, not the view: `In Span<Velocity>` is
`std::span<const Velocity>` and `Out Span<Position>` is `std::span<Position>`.

A type is not a named child of the schema: it has no name or description of its own and exists only
as the type of the member holding it. `BaseType.TypeName` reports which type it is, and is the same
value written as the file's `TypeName` discriminator.

### The C++ generator

`Schema.Cpp` turns a schema into a `ktsu.Coder` AST and hands that to `CppGenerator`, which owns
every question about how C++ is spelled. Nothing in `Schema.Cpp` writes a brace.

It lives outside the core library because it cannot ship there: this library publishes `net8.0` and
`ktsu.Coder` does not, which is the whole reason `SchemaGenerator.Register` exists.
`SchemaTool/Program.cs` is the worked example of a host registering it.

`CppGeneratorOptions` is what a target says that its schema cannot. Almost everything comes from the
schema - a unit is a semantic type and the generator emits the class, a class is a struct, and the
standard library spells a string, a sequence, a view and an absent value. What is left is the short
list a program supplies for itself: a fixed-shape numeric vector, a colour, an identifier with a
generation, a fallible return, a calendar date. A target that has them says how it spells them and
which header they come from; a target that has not is **refused those schema types by name**, with
the option to set, rather than handed a header that will not compile. `ExistingTypes` is the other
direction - a semantic type the target already hand-wrote is named rather than generated a second
time.

One file per element, and **an enum goes to namespace scope in a header of its own** rather than
nested in the class that names it. Holotype's target document nests it, which was right when an enum
belonged to the component that declared it; here an enum is a top-level element any class may name,
so nesting it in the one class using it today would move the type the moment a second class used it.

### Contracts

`ktsu.Schema.Contracts` is the abstraction seam the models implement: `Schema : ISchema`,
`SchemaClass : ISchemaClass`, `SchemaMember : ISchemaMember`, `SchemaEnum : ISchemaEnum`,
`BaseType : ISchemaType`. Inject `ISchema` where a consumer only defines and reads schema elements.

Entities are abstracted; values are not. Name types (`ClassName`, `MemberName`, …) and
`SchemaChildDescription` appear in the contracts as themselves — a semantic string is already an
abstraction over `string`, and wrapping it again would make `ISchemaChildSet<out TValue, TName>`
unusable, since a covariant element type cannot coexist with a varying name type.

Collections on the contracts are read-only views (`ISchemaChildSet`). Mutation lives on the owning
element (`ISchema.AddClass`, `ISchemaClass.AddMember`), which is what enforces name uniqueness and
establishes parent association.

### Semantic String Types

The library uses `ktsu.Semantics.Strings` for type-safe identifiers. Convert strings using `.As<T>()`:
- `ClassName`, `MemberName`, `EnumName`, `EnumValueName`, `BaseTypeName`, `ContainerName`, `DataSourceName`, `CodeGeneratorName`

Example: `"User".As<ClassName>()`

### Parent-Child Association Pattern

Schema elements maintain parent references via `AssociateWith()` methods. After deserialization, `Schema.Reassociate()` re-establishes these relationships.

### Member metadata

A member carries six optional properties beside its type - `Unit`, `Range`, `DefaultValue`,
`Interpolation`, `Network` and `Editor` - modelled in `Schema/Models/Metadata/` and validated
together in `Schema/Models/Schema.Validation.cs`, since most of them are only wrong in combination.
Units are not modelled here: a member stores the text and `UnitRegistry` resolves it against
`ktsu.Semantics.Quantities`, reflecting over that assembly rather than keeping a list that would
fall behind it. Two symbols there are ambiguous (`g`, `rad`), so resolution refuses an ambiguous
symbol and `UnitRegistry.PreferredText` is what a picker writes: the symbol when it identifies one
unit, the name when it does not. `docs/schema-format.md` documents the file's side of all of this.

Generated C# carries the metadata as attributes from `ktsu.Schema.Runtime`, and `ClrTypeImporter`
reads them back, because none of it is expressible in a CLR type. The two sides are inverses and
the generate-compile-reimport round-trip test fails if either changes alone. A default is emitted
as the property initialiser as well, so a generated instance starts at it.

### Key Files

- `Schema/Contracts/` - The `ISchema` abstraction seam implemented by the models
- `Schema/Models/Schema.cs` - Root container with CRUD operations for classes/enums
- `Schema/Models/SchemaChildSet.cs` - Order-preserving, name-unique view owning the uniqueness rule
- `Schema/Models/Types/BaseType.cs` - Abstract base with `[JsonDerivedType]` attributes for polymorphic serialization
- `Schema/Models/SchemaClass.cs` - Class definitions containing `SchemaMember` collections
- `Schema/Models/ClrTypeImporter.cs` - Reads a .NET type into a schema; the exact inverse of the C# generator
- `Schema/Runtime/SchemaMetadataAttributes.cs` - What generated code carries that its C# types cannot say
- `SchemaEditor/SchemaEditor.cs` - Main editor application using `ktsu.ImGui.App`
- `SchemaEditor/MemberGridPanel.cs` - The grid of member rows: add, reorder, retype, remove, and the two folds each row opens
- `SchemaEditor/MemberSemanticsPanel.cs` - The metadata behind a member's fold, and the only place that decides what a picker writes for a unit
- `SchemaEditor/EditorHost.cs` - Builds the `ImGuiAppConfig`; `CreateConfig` is what the tests drive too
- `SchemaEditor/EditorTheme.cs` - The ktsu.ThemeProvider theme, and the one definition of how a validation issue is coloured
- `SchemaEditor/Program.cs` - The entry point, and the only file excluded from coverage measurement
- `SchemaEditor.Test/EditorHarness.cs` - Runs a real editor headlessly, frames advanced by the test
- `SchemaEditor.Test/WidgetHarness.cs` - A headless frame containing only the widget under test, and an editor for a panel that is one

### Addressing the editor from a test

The editor's own draw code records where it put things, through `ImGuiProbes.MarkItem` from
`ktsu.ImGui.Probes`. That is what lets a test click a widget by name rather than by pixel position,
and it costs nothing when no probe is installed - which is every run that is not a test. Marking is
therefore part of drawing a control, not an afterthought: a new button, menu item or picker option
that a test will need is marked where it is submitted.

The names are qualified by the ImGui window and any pushed scope, and a test matches on the trailing
part: `menu/New`, `field/ClassNameUser`, `memberId/Delete`, `diagnostic/Error:Users`. Rows that share
a label push a probe scope alongside `ImGui.PushID`, so two members' fields do not collide.

One thing has no name to click: the right-hand tab bar comes from a widget library that neither
records its tabs nor takes a selection from outside. A panel behind it - the class graph, the
diagnostics list - is tested by drawing it directly in a `WidgetHarness`, which is what the tab's
own delegate does.

## Dependencies

- **ktsu.Semantics.Strings/Paths** - Type-safe string and path wrappers
- **ktsu.Semantics.Quantities** - The units a member's values can be measured in
- **ktsu.ImGui.App/Widgets/Popups** - ImGui application framework (editor only)
- **ktsu.AppDataStorage** - Persistent settings storage (editor only)
- **Polyfill** - .NET compatibility shims for multi-targeting
- **ktsu.Coder** - The language-agnostic AST and the C++ writer (`Schema.Cpp` only)

## Schema Files

Schema definitions are stored as `.schema.json` files using `System.Text.Json` with polymorphic type discriminators (`TypeName` property).
