# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Schema is a C# library for defining and managing data structure schemas. It consists of seven projects:

- **Schema** - Core library providing schema definition types (classes, enums, members, types)
- **Schema.Test** - MSTest unit tests for the core library
- **Schema.Cpp** - The C++ generator, in its own project because `ktsu.Coder` ships no `net8.0`
- **Schema.Cpp.Test** - Its tests, including the three acceptance tests against Holotype's target document and one that compiles the generated reflection table
- **Schema.Editor** - ImGui-based visual editor application for creating and editing `.schema.json` files
- **Schema.Editor.Test** - Headless UI tests for the editor, driven through `ktsu.ImGui.App.Testing`
- **Schema.Tool** - The `dotnet tool` (`kschema`) that validates schemas and runs their code generators

## Build Commands

```bash
dotnet build              # Build entire solution
dotnet test               # Run all tests
dotnet test --filter "FullyQualifiedName~TestName"  # Run specific test
dotnet run --project Schema.Editor  # Launch the visual editor
dotnet run --project Schema.Tool -- generate my.schema.json  # Run a schema's code generators
dotnet pack Schema.Tool -c Release   # Build the `kschema` tool package
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

**That argument is being overtaken, and by the thing it appeals to.** A consumer generating against
`ktsu.Semantics.Quantities` finds that library already names the 3D forms - `Velocity3D`,
`Displacement3D`, `Force3D` - so a member holding one says `Semantic` naming that type rather than
`Vector3` of a component. Stating it both ways is two spellings of one fact, and the second is the
one both generators can already map, and now map the same way: `CSharpCodeGenerator` writes
`System.Numerics.Vector3` for `Vector3 { ElementType: Float }` and `Runtime.Vector3<Kilograms>` for
a `Semantic` element, so a dimensional vector spelled either way reaches C# as a type rather than as
an `object?`. Expect `ElementType` to narrow to "which numeric type", with `Vector2/3/4` meaning
untyped geometry, once that lands. It is documented as it stands rather than as it is heading,
because the schema has not changed.

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

Both sides read the **representation** rather than the declared type, through `Represented` in
`Schema.Validation.cs`. A semantic type is a distinct name for something already representable, so
a member typed `Semantic(Kilograms)` over a `Float` holds a number and a unit, a range and a default
are as meaningful on it as on the bare `Float`. This is what lets a unit live on the type, stated
once, while the bounds and the default stay on the member, where they are facts about that field
rather than about kilograms.

A chain that reaches nothing real - a name that does not resolve, or a refinement cycle - leaves
`Represented` returning the `Semantic` it was given, and every caller treats that as "already
reported elsewhere" rather than walking further. That is what keeps a cycle a reported error instead
of a recursion with no bottom, and it is why an unresolved semantic type is one message rather than
one per property hanging off it.

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

Generated C# emits a promising class as a `[StructLayout(LayoutKind.Sequential)] struct`, and every
other class stays a `class`. A reference type could never keep the promise however carefully it was
written - an instance is an address, its fields are somewhere else, and the CLR orders them as it
likes - so a consumer needing the two languages' versions of a class to be the same bytes had
nothing to compare, and the promise was recorded but unkeepable. Sequential layout is what makes
member order load-bearing on this side too. It is narrow by design: it reshapes the classes that
make the promise and no others, rather than being a move to value types for generated C# in general.

Nothing about a CLR type says the promise was *made*, though - sequential layout is a fact about a
type that a hand-written struct may have for its own reasons - so generated C# still carries
`SchemaTravelsAsBytesAttribute` and `ClrTypeImporter` still reads it back, the same arrangement the
member metadata uses and covered by the same generate-compile-reimport test. The struct is how the
promise is kept; the attribute is how it is recorded. Reading a value type as a class is gated on
that attribute for the same reason: read as "any struct", a `Guid` would come back as a class with
none of its members, which says the wrong thing rather than nothing.

Two things follow on the C# side. A member holding a promising class is given no initialiser,
because a struct is already the value its declaration described; and a struct whose members do have
initialisers - from a default the schema declared - needs a parameterless constructor of its own
before it will compile, which the generator writes. `default(T)` is still all zeroes, which is what
it means for the bytes to be the whole of the value rather than a defect in the defaults.

The three types that used to defeat the promise are now spelled. A `Handle`, a `Semantic` and a
vector of anything but floats all fell through `MapType` to `object?`, which compiles and says
nothing - and inside a sequential struct says something worse, a reference field in the one kind of
class whose whole promise is that it has none. Validation accepts all three there, because they
travel as bytes in a language that can say them, so the gap was the generator's rather than the
schema's.

- A **handle** is `Handle<T>` from `ktsu.Schema.Runtime`, an index and a generation with `T` carried
  and never stored. That phantom parameter is what makes the schema's rule true in C#: a handle
  travels as bytes *whatever it identifies*, so a promising class may hold a handle to one that
  makes no promise at all, while the type still keeps a handle to a texture out of a slot meant for
  a mesh.
- A **vector** of floats is still `System.Numerics.Vector3`, which holds floats and nothing else; a
  vector of anything else is `ktsu.Schema.Runtime.Vector3<T>`, whose components are fields of `T`.
  Two spellings for one family, because keeping the first is what stops this changing the type of
  every vector member that already existed.
- A **semantic type** is emitted, which it was not before - a `readonly record struct` over its
  representation, one file each, beside the classes and the enums. C# spells `explicit` and
  `implicit` on a conversion directly, so the schema's two conventions are the conversions
  themselves rather than a comment beside them: crossing into or out of the representation is
  explicit both ways, and a type refining another widens implicitly and narrows explicitly.

`SchemaSemanticTypeAttribute` is what the shape cannot say, the same arrangement as
`TravelsAsBytes`. A semantic type and a promising class are both a struct, so without it `Kilograms`
reimports as a class with a member called `Value`; and a type that refines another stores the shared
representation exactly as the one it refines does, so the field says `float` either way and only
`Refines` can say that one arrived by way of the other. The six metadata attributes target a struct
as well as a member for the same reason, since a semantic type carries them too -
`ISchemaMetadataCarrier` is settable so the importer restores both through one path.

The proof is a test that pins an instance: the CLR refuses to pin a type holding a reference
anywhere in it, so a promising class holding all three either is bytes or the test fails. It is the
C# counterpart of the `static_assert(std::is_trivially_copyable_v<T>)` the C++ generator emits
beside the same class.

**Being bytes is not the same as being the same bytes**, and two members went on differing after
the layout was sequential. Sequential layout fixes the *order* of the members and says nothing
about the *width* of each one, so a generated class was the same bytes in both languages only where
every member happened to agree - and two did not. An `enum` is four bytes in C# unless the
declaration says otherwise and one in the C++ emitted here, which writes
`enum class ... : std::uint8_t`; a `bool` is one byte in the managed layout and four when
marshalled, so `Marshal.SizeOf` disagreed with the runtime before either disagreed with C++. Both
are now spelled: a generated enum is `: byte`, and a `bool` in a promising class carries
`[field: MarshalAs(UnmanagedType.U1)]` - `field:` because an auto-property's backing field is what
the marshaller lays out.

Neither width is anything a `.schema.json` states, so there is no third place for the two
generators to agree with; they agree by each naming the same width and each pinning its own half,
in `GeneratedLayoutAgreesWithCppTests` here and `EnumUnderlyingTypeTests` in `Schema.Cpp.Test`. The
first measures the type rather than the text - a bool, a one-byte enum and a bool is three bytes
when every member is what C++ makes it, nine when the enum is an `int`, and twelve when the bools
marshal as four each, so one assertion on the size tells the three cases apart.

The four that were left - a `Span`, a `Result`, an `Optional` and an `Interface` - were never a gap
in the mapping the way those three were. Each is a decision about the generated C# API, and the one
that had to come first is that **an interface is emitted at all**: until it was, an `Interface`
member named a type nothing produced. `Generate` now writes a file per interface as well as per
class, enum and semantic type.

- An **interface** is emitted under the name the schema gave it, with no `I` prefix. The prefix is
  the C# convention and it is not available: the compiled name is what the importer reads back, so
  one would have to be stripped again, and stripping cannot tell a prefix from a first letter - an
  interface called `Item` would come back as `tem`. Same rule as the classes and the enums.
- A **view** is a `ReadOnlySpan<T>` when its elements are `In` and a `Span<T>` otherwise, which is
  the same reading the C++ generator gives it, where the difference is a `const` on the element
  rather than a second type. It is spelled by `MapParameter` rather than `MapType`, because the
  direction that decides which belongs to the parameter and a type has no route back to one. In a
  *member* position a `Span` is still `object?` - a field of a `ref struct` is something C# forbids
  outright, which is a language limit rather than a gap.
- A **fallible return** is `Result<T, TError>`, or `Result<TError>` for `Result<Void>`, since C# has
  no `void` type argument to close the value-carrying form over. The arity is what tells the two
  apart on reimport. `TError` is the schema's error enum, read through the type's `ParentSchema`
  the same way a `Semantic` resolves its declaration.
- An **absent value** is `Optional<T>`. `T?` would be idiomatic and means two different things: over
  a value type it is a type, and over a reference type it is an annotation that is not part of the
  type at all. A generator writing it would round-trip an `Optional<Int>` and lose an
  `Optional<Item>`, which is the one asymmetry the mapping cannot afford.

Two things a signature says that C# cannot. `IsQuery` has no syntax - C++ writes a trailing `const`
- so it is recorded with `SchemaQueryAttribute`. Direction is the parameter modifier, and `In` is no
modifier at all, because an ordinary by-value parameter is already one the caller supplies and the
callee does not modify; `out`, `ref` and by-value are three different compiled signatures, so all
three read back without an attribute. `void` is spelled only as a return type, by `MapReturnType`,
because there is no field, property or parameter of one.

`Schema.AddInterface(Type)` is the counterpart of `AddClass(Type)`, and is what makes an interface
part of the round trip rather than something emitted and never read back.

What is still `object?` is a `Span` outside a signature and a `Void` outside a return type - both
declarations C# has no member for at all, and neither one a promising class may hold.

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

Whether a call changes the thing it is called on is the fifth, and the one the other four left
out. `SchemaFunction.IsQuery` says a query answers rather than acts, held per function rather than
globally because unlike the others it differs from one signature to the next - it is a property of
what the call does rather than a rule the program keeps. C++ writes it as a trailing `const`. A
query returning `Void` cannot be observed at all, which validation reports as a warning.

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
`Schema.Tool/Program.cs` is the worked example of a host registering it.

`CppGeneratorOptions` is what a target says that its schema cannot. Almost everything comes from the
schema - a unit is a semantic type and the generator emits the class, a class is a struct, and the
standard library spells a string, a sequence, a view and an absent value. What is left is the short
list a program supplies for itself: a fixed-shape numeric vector, a colour, an identifier with a
generation, a fallible return, a calendar date. A target that has them says how it spells them and
which header they come from; a target that has not is **refused those schema types by name**, with
the option to set, rather than handed a header that will not compile. `ExistingTypes` is the other
direction - a semantic type the target already hand-wrote is named rather than generated a second
time.

**A target says all of that in a file, not only in C#.** `CppGeneratorOptionsFile` reads a
`CppGeneratorOptions` out of JSON in the same dialect as a `.schema.json` - camelCase, comments and
trailing commas tolerated - and `kschema --cpp-options <file>` is how a build supplies one. The
alternative was a growing list of command line flags spelling one record, or requiring every target
to be a C# program hosting the generator, and a build that runs a tool is neither.

An unrecognised property is **refused rather than ignored**, which is the one place this differs
from ordinary lenience. Every option here means something when absent - no `handle` refuses the
schema's handles by name, no `reflection` emits no table - so a typo would not fail. It would
select the other behaviour, and the generator would then refuse a schema for naming a type the
target believed it had just declared, with nothing pointing at the file. For the same reason the
refusal message names the file's key *and* the C# property, and asks `NameOf` for the first rather
than restating it.

A refusal reaches a caller as a result rather than a stack trace: `CppGenerationException` derives
from `SchemaGenerationException`, and `SchemaGenerator.Generate` catches that base and reports
`SchemaGenerationStatus.TargetCannotExpress`. Only that base - a generator with a bug in it still
throws, because a stack trace is the right answer to that and the wrong answer to being told a
target has no vector type.

### The command line

`Schema.Tool` packs as a `dotnet tool` called **`kschema`**, under `ktsu.Sdk.Tool` rather than
`ktsu.Sdk.ConsoleApp`: the two differ in that the tool SDK clears `RuntimeIdentifiers`, and packing
a tool with the seven identifiers set writes seven runtime-specific packages instead of the one
everybody installs.

The commands themselves live in `SchemaCommandLine` in the library, where they are tested without
spawning a process; `Program.cs` is only the wiring. `SchemaCommandLineHost` is what the library
cannot know - the name the host was installed as, and the options the host adds - and it exists for
the usage text alone. A host parses its own options and hands on what is left, which is why
`CppGeneratorOptionsFile.TryTake` *removes* `--cpp-options` rather than passing it through: the
commands find the schema by taking the first argument that is not an option, stepping over the value
any option consumes, and the list of options that consume one belongs to the commands, which have
never heard of the C++ generator.

One file per element, and **an enum goes to namespace scope in a header of its own** rather than
nested in the class that names it. Holotype's target document nests it, which was right when an enum
belonged to the component that declared it; here an enum is a top-level element any class may name,
so nesting it in the one class using it today would move the type the moment a second class used it.

### The reflection table

`CppGeneratorOptions.Reflection` adds two files: `reflect`, the vocabulary, and `reflection`, the
table. It is off by default, because they are two files a target that does not read them did not
ask for.

**What it is for.** A generated struct carries a name, a type and an order. Everything else the
schema says about a member — its unit, its range, whether it wraps, whether two states of it can be
blended, how it is quantised on the wire, what an editor should draw — the header can only put in a
comment, which is to say it cannot say it at all. The table is where those facts become data, and a
validator, a serialiser, a network codec and an editor all read it rather than each being a
generator with its own copy. That also makes them work on a schema loaded at run time, which
generated code cannot. The rule it suggests: **generate what has to be a *type*, and write by hand
what only needs to *read* a type's description.**

**The offsets are the compiler's.** The table says `offsetof(Class, member)` and
`sizeof(Class::member)`, so the numbers are whatever the compiler chose for that target, that ABI,
those packing rules. Reflection therefore cannot drift from the layout it describes: it is derived
from it, by the only thing that knows. A generator that worked offsets out itself would be a second
implementation of the C++ ABI and would be wrong somewhere eventually.

**A member's kind and its representation are both carried.** `kind` is what the schema declares —
`Semantic` for a member typed `Kilograms` — and `representation` is what the bytes are, with a
semantic type followed down its chain of refinement (`Float`). One field could not do both: reading
a value out of a save file needs the second, and showing the member to a person wants the first.

**The dimension comes from the unit.** A member's unit text is resolved through `UnitRegistry` and
the unit's own `DimensionInfo` supplies the eight exponents, so the numbers in the table cannot
disagree with the unit beside them. A member that measures nothing is dimensionless, which is the
same shape rather than a missing one.

The fourth of those eight is `angle`, and it is the one that only means something if the unit
registry answers honestly. `ktsu.Semantics` carries the axis so an angular displacement is not the
same type as a ratio, but a unit claimed by two dimensions used to report whichever was declared
first - and `Dimensionless` is first in `dimensions.json`, so a member measured in radians wrote the
eight numbers of a unitless count. Nothing here could have caught that, because this table asks the
unit rather than restating it; so the fix is upstream (`ktsu.Semantics` v5.0.1) and the assertion is
here, in `ReflectionTableTests.CarriesTheAngleOfARadianRatherThanNothing`.

`reflect` is **shipped rather than generated**, like `ktsu.Semantics.Cpp`'s prelude and for the same
reason: `template <typename T> struct Describe;` declares a type parameter and `concept Reflected`
is a concept, neither of which the AST models — a generator names a generic type, it never declares
one. Two lists in it are substituted, though: `TypeKind` is every `[JsonDerivedType]` on `BaseType`
and `Interpolation` is the enum of that name, each written twice (the enumeration and the names
beside it) from one list, because the alternative is a `switch` that nothing would keep in step.
That is why the C++ side emits its own vocabulary rather than being written against one a target
already has — a type added to the schema appears in C++ with no edit in either repository.

The table is anchored by `template<> struct Describe<Class>`, which is `ktsu.Coder`'s
`ClassDeclaration.SpecialisationArguments` and was the last thing the AST could not say. The
alternative was a `DescribeRigidBody` every consumer spells for itself, which is what a lookup by
type exists to avoid.

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
- `Schema.Editor/SchemaEditor.cs` - Main editor application using `ktsu.ImGui.App`
- `Schema.Editor/MemberGridPanel.cs` - The grid of member rows: add, reorder, retype, remove, and the two folds each row opens
- `Schema.Editor/MemberSemanticsPanel.cs` - The metadata behind a member's fold, and the only place that decides what a picker writes for a unit
- `Schema.Editor/EditorHost.cs` - Builds the `ImGuiAppConfig`; `CreateConfig` is what the tests drive too
- `Schema.Editor/EditorTheme.cs` - The ktsu.ThemeProvider theme, and the one definition of how a validation issue is coloured
- `Schema.Editor/Program.cs` - The entry point, and the only file excluded from coverage measurement
- `Schema.Editor.Test/EditorHarness.cs` - Runs a real editor headlessly, frames advanced by the test
- `Schema.Editor.Test/WidgetHarness.cs` - A headless frame containing only the widget under test, and an editor for a panel that is one

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

### The breadth suite

`samples/` holds two real programs' whole schema vocabulary - Carbon Monoxide's and Dungeoneer's,
26 files and 45 elements - migrated from the `{Classes, Enums}` format they were written in. Every
other test here builds a schema to exercise one rule; these exist so a generator meets types nobody
chose for their shape. `LegacySampleGenerationTests` compiles the C# generated from both,
`LegacySampleCppTests` compiles the C++, and one test asserts that no member of either reaches C#
as `object?` - which compiling cannot catch, because `object?` compiles.

`LegacySchemaReader` is in the test project rather than the library: the legacy format is two
repositories' history and publishing a reader for it would commit this library to a dialect nobody
else has. The migration is re-run on every test run rather than trusted, so a committed sample
cannot drift from the legacy files beside it. `samples/README.md` records what the migration could
not preserve - chiefly the file boundaries, since a `className` resolves within one schema and 14
of the legacy references cross a file.
