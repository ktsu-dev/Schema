# The `.schema.json` format

This is the reference for the file `ktsu.Schema` reads and writes. It describes what the
serializer actually emits, the version field and its migration path, and what may change in a
future release.

The format is JSON, written by `System.Text.Json` with camel-cased property names and indentation.
Nulls are omitted. Round-tripping is the contract: loading a file and saving it again must not
lose information.

## Root

```json
{
  "formatVersion": 3,
  "classes": [],
  "enums": [],
  "interfaces": [],
  "codeGenerators": [],
  "dataSources": []
}
```

| Property | Type | Meaning |
| --- | --- | --- |
| `formatVersion` | integer | The format version. Written first so a reader can find it without scanning the document. See [Versioning](#versioning). |
| `classes` | array of [class](#class) | The classes the schema defines. |
| `enums` | array of [enum](#enum) | The enumerations the schema defines. |
| `interfaces` | array of [interface](#interface) | The interfaces the schema defines. See [Declaring behaviour](#declaring-behaviour). |
| `codeGenerators` | array of [code generator](#code-generator) | Code generator configurations. |
| `dataSources` | array of [data source](#data-source) | Bindings from a data file to a class. |

All five collections are always written, empty or not.

Every named element also carries a `description`, a free-text string that is always written even
when empty. It is the natural source for a doc comment in generated code.

## Class

```json
{
  "members": [ ... ],
  "name": "User",
  "description": "A person with an account"
}
```

| Property | Type | Meaning |
| --- | --- | --- |
| `members` | array of [member](#member) | The class's members, **in declaration order**. |
| `name` | string | The class name. Unique among classes. |
| `description` | string | Free text. |

Member order is meaningful: it is preserved through a round trip and is the order generated code
will declare members in.

## Member

```json
{
  "type": { "TypeName": "String" },
  "name": "Name",
  "description": "Display name"
}
```

| Property | Type | Meaning |
| --- | --- | --- |
| `type` | [type](#types) | The member's type. |
| `name` | string | The member name. Unique within its class. |
| `description` | string | Free text. |
| `unit` | string | The unit the member's values are measured in. See [Units](#units). |
| `range` | [range](#range) | The values the member may take. |
| `defaultValue` | [default](#default) | The value the member takes when none is supplied. |
| `interpolation` | string | `None`, `Linear`, `Spherical` or `Step`. Absent means `None`. |
| `network` | [network](#network) | How the member should be encoded when sent to a peer. |
| `editor` | string | A hint about how an editor should present the member. Free text. |

All six are optional and omitted when absent. They were added in format version 2.

### Units

A unit is written as its symbol (`"m/s"`) or its name (`"MeterPerSecond"`), and is resolved
against the unit registry in `ktsu.Semantics.Quantities` when the schema is validated.

The symbol is the natural thing to write, and is what a person reads. Names exist because two
symbols in the registry are ambiguous — `g` is both gram and standard gravity, and `rad` is both
radian and *radiation absorbed dose*, which is not even the same dimension. Validation refuses an
ambiguous symbol and names the candidates rather than picking one, so a schema that means radians
must say `"Radian"`.

The text is what the file stores, not a resolved unit: the file stays readable, and the
conversion factors stay owned by `ktsu.Semantics.Quantities` instead of being copied into every
schema that uses them.

A unit is only meaningful on a numeric or vector member.

### Range

```json
{ "minimum": 0.0, "maximum": 6.2831853, "wrap": true }
```

| Property | Type | Meaning |
| --- | --- | --- |
| `minimum` | number | The smallest allowed value, in the member's own unit. |
| `maximum` | number | The largest allowed value, in the member's own unit. |
| `wrap` | boolean | Whether values outside the range wrap into it rather than being invalid. |

`wrap` changes what the range *means*. Without it the range is a bound and a value outside it is
invalid. With it the range is a **period**: an angle of 7 radians on a `[0, 2π)` member is
un-normalised rather than wrong, and clamping it to the maximum would be the one transformation
that is certainly incorrect. A validator should reduce a wrapping value into range and reject a
non-wrapping one — and for the same reason, a default outside a wrapping range is accepted.

### Default

Polymorphic, discriminated by `DefaultKind`, matching the `TypeName` discriminator on types:

```json
{ "DefaultKind": "NumberDefault", "value": 1.0 }
{ "DefaultKind": "BooleanDefault", "value": true }
{ "DefaultKind": "TextDefault", "value": "Dynamic" }
```

`NumberDefault` carries a number for any numeric or vector member; `BooleanDefault` a boolean;
`TextDefault` a string member's contents, or the **name** of an enum value. The name rather than
the ordinal, so that reordering an enum cannot silently change what a default means.

A default is not the same as a zeroed value. `default(T)` in C# and a value-initialised struct in
C++ are all-zero bytes, which is rarely what a schema means by "the default".

### Network

```json
{ "quantise": 0.01, "delta": true }
```

| Property | Type | Meaning |
| --- | --- | --- |
| `quantise` | number | The smallest change worth transmitting, in the member's own unit. Zero means full precision. |
| `delta` | boolean | Whether to send the member only when it differs from the last acknowledged state. |

Both are advisory: a codec that ignores them is still correct, just larger. A quantised round
trip is accurate to half the step and no better.

## Declaring behaviour

A class says what data *is*. An interface says what the program can *do*: a named set of
functions, which a generator turns into the header an implementation is written against — an
abstract type in C++, an interface in C#. The declaration is the contract, so an implementation
cannot drift from it without failing to compile.

### The four conventions

A signature carries no ownership, lifetime, or error annotations, and does not need them. Four
conventions carry that weight instead, and each one is a rule the *program* keeps rather than a
fact every signature restates:

| Question | Answered by | Not by |
| --- | --- | --- |
| Can this call fail? | The return type is [`Result`](#result). | a `throws` clause |
| May the callee keep this? | [`Handle`](#handle) yes, [`Span`](#span) no. | an ownership annotation |
| Who frees it? | Nobody: a handle is an identifier, a span is a borrow. | a lifetime annotation |
| Is this argument read-only? | `direction`. | `const` |

This is deliberate. Every interface language that let signatures answer these individually grew
annotations until it was a worse version of the language it described. Making them global
conventions keeps the declaration small, and the cost is that a program which wants two error
conventions cannot express the second — which is the point.

## Interface

```json
{
  "functions": [ ... ],
  "name": "Renderer",
  "description": "Submits work to the GPU"
}
```

| Property | Type | Meaning |
| --- | --- | --- |
| `functions` | array of [function](#function) | The interface's functions, **in declaration order**. |
| `name` | string | The interface name. Unique among interfaces. |
| `description` | string | Free text. |

Order is preserved, for the same reason member order is: a generated header that reorders itself
between runs produces a diff nobody can review.

## Function

```json
{
  "parameters": [ ... ],
  "returnType": { "TypeName": "Result", "elementType": { "TypeName": "Void" } },
  "name": "Present",
  "description": "Presents the completed frame"
}
```

| Property | Type | Meaning |
| --- | --- | --- |
| `parameters` | array of [parameter](#parameter) | The parameters, **in declaration order**. Order is the signature. |
| `returnType` | [type](#types) | What the function returns. `Void` for nothing; never `None`. |
| `name` | string | The function name, unique within its interface. |
| `description` | string | Free text. |

**There is no overloading.** A name identifies a function within its interface. Two functions
differing only in their parameters are one name in some target languages and two in others, so the
schema refuses the case rather than generating something different per language.

## Parameter

```json
{
  "type": { "TypeName": "Span", "elementType": { "TypeName": "Float" } },
  "direction": "Out",
  "name": "samples",
  "description": "Filled with the captured audio"
}
```

| Property | Type | Meaning |
| --- | --- | --- |
| `type` | [type](#types) | The parameter's type. |
| `direction` | string | `In`, `Out` or `InOut`. Absent means `In`. |
| `name` | string | The parameter name, unique within its function. |
| `description` | string | Free text. |

`direction` replaces const-ness, which is one language's spelling of a parameter rather than a
property of the call. A C++ generator emits `In` as a const reference or a by-value copy; a C#
generator emits `in`, `out` and `ref`.

**On a `Span`, direction describes the elements, not the view.** A span is always a borrow the
callee may not retain; `In` makes its elements read-only and `Out` or `InOut` makes them writable.
So `In Span<Velocity>` is `std::span<const Velocity>` and `Out Span<Position>` is
`std::span<Position>` — which is exactly the shape of a system that reads one component and writes
another.

Some things are refused in a signature, and the refusals are what keep the conventions honest:

| Refused | Why |
| --- | --- |
| An `Array` parameter | An array is a collection a class owns. A borrowed sequence is a `Span`. |
| A `Result` parameter | Fallibility describes the call, not an argument to it. |
| A `Void` parameter | It carries no value. |
| A `None` return or parameter | No type was chosen; not generatable. Use `Void` to mean "returns nothing". |

## Enum

```json
{
  "values": [ "Admin", "Member" ],
  "name": "Role",
  "description": "What a user may do"
}
```

`values` is an array of strings, in declaration order. Values are unique within the enum and may
not be empty.

## Data source

```json
{
  "file": "data/items.json",
  "className": "Item",
  "name": "Items",
  "description": ""
}
```

| Property | Type | Meaning |
| --- | --- | --- |
| `file` | string | A relative path to the data file. See [Path resolution](#path-resolution). |
| `className` | string | The class the data conforms to. Must name a class in `classes`. |

## Code generator

```json
{
  "outputPath": "generated",
  "language": "csharp",
  "namespace": "My.Game",
  "name": "CSharp",
  "description": ""
}
```

| Property | Type | Meaning |
| --- | --- | --- |
| `outputPath` | string | A relative directory to write generated files into. See [Path resolution](#path-resolution). |
| `language` | string | Which generator handles this configuration. Matched case-insensitively; `csharp` is the only one built in. |
| `namespace` | string | The namespace generated code is emitted into. Optional; omitting it emits into the global namespace. |

See [Code generation](#code-generation) for what a generator does with these.

## Types

A type is an object with a `TypeName` discriminator. `TypeName` is deliberately not camel-cased -
it is the polymorphic discriminator, not a data property.

### Types with no further properties

`TypeName` alone fully describes these:

| `TypeName` | Meaning |
| --- | --- |
| `None` | No type chosen yet. A valid intermediate editing state, not a generatable one. |
| `Void` | Returns nothing. A decision, unlike `None`. Only valid as a return type, or inside a `Result`. |
| `Bool` | Boolean. |
| `Int` | 32-bit signed integer. |
| `Long` | 64-bit signed integer. |
| `Float` | 32-bit floating point. |
| `Double` | 64-bit floating point. |
| `String` | Text. |
| `DateTime` | A date and time. |
| `TimeSpan` | A duration. |
| `Vector2`, `Vector3`, `Vector4` | Fixed-shape numeric vectors. |
| `ColorRGB`, `ColorRGBA` | Colors. |

```json
{ "TypeName": "ColorRGBA" }
```

The vector and color types are structured but built in: their shape is fixed and known to the
library, so unlike `Object` they carry no class reference.

### `Object` - a reference to a class in this schema

```json
{ "TypeName": "Object", "className": "Item" }
```

`className` must name a class in `classes`.

### `Interface` - a reference to an interface in this schema

```json
{ "TypeName": "Interface", "interfaceName": "AudioDevice" }
```

`interfaceName` must name an interface in `interfaces`. The counterpart of `Object` for a class.

### Wrappers - types that wrap one other type

Each holds an `elementType` and differs only in what the wrapping *means*:

```json
{ "TypeName": "Span",     "elementType": { "TypeName": "Float" } }
{ "TypeName": "Handle",   "elementType": { "TypeName": "Object", "className": "Texture" } }
{ "TypeName": "Result",   "elementType": { "TypeName": "Int" } }
{ "TypeName": "Optional", "elementType": { "TypeName": "String" } }
```

| `TypeName` | Means |
| --- | --- |
| `Span` | A **borrowed** view over a contiguous sequence, valid for the call and not retained. Contrast `Array`, which is a stored collection a class owns: a span is never a member's type and an array is never a parameter's. |
| `Handle` | An opaque, generation-counted reference to a resource the engine owns. The caller may keep it indefinitely and has nothing to free; a stale handle is detectable rather than undefined. |
| `Result` | The outcome of a call that can fail: the element, or an error. Valid only as a return type. `Result<Void>` is a call that can fail and produces nothing. |
| `Optional` | A value that may be absent. **Absence is not failure** — `Optional` is a lookup that found nothing, `Result` is a call that did not succeed. Using one for the other turns "not found" into an error path, or an error into a silent nothing. |

A reference inside a wrapper resolves exactly as one named directly does, so `Span<Transform>`
finds its class.

### `Enum` - a reference to an enum in this schema

```json
{ "TypeName": "Enum", "enumName": "Role" }
```

`enumName` must name an enum in `enums`.

### `Array` - a collection

```json
{
  "TypeName": "Array",
  "elementType": { "TypeName": "Object", "className": "Item" },
  "container": "map",
  "key": "Id"
}
```

| Property | Type | Meaning |
| --- | --- | --- |
| `elementType` | [type](#types) | The element type. May itself be an array, so arrays nest. |
| `container` | string | The container kind. See [Containers](#containers). |
| `key` | string | For a keyed container, the member of the element class to key by. Empty when unkeyed. |

## Containers

`container` is an open vocabulary: a consumer may use its own container names, and validation
reports an unrecognised one as a warning rather than an error. The library itself produces and
understands two:

| Container | Meaning | Typical mapping |
| --- | --- | --- |
| `vector` | An ordered sequence. | `List<T>` |
| `map` | A lookup keyed by `key`. | `Dictionary<TKey, T>` |

For `map`, `key` must name a member of the element class, and that member's type must be a
primitive. That member's type is also the dictionary's key type.

## Path resolution

`file` on a data source and `outputPath` on a code generator are **relative to the directory
containing the `.schema.json` file**. A schema at `/work/game/game.schema.json` with a data source
`file` of `data/items.json` refers to `/work/game/data/items.json`.

This anchor is what makes a schema and its data movable together: checked out somewhere else, the
relative paths still resolve.

The anchor is supplied by whoever read the file, so the serializer itself stays free of the
filesystem:

```csharp
SchemaLoadResult result = SchemaSerializer.Load(json, schemaFilePath);
// result.Schema.CanResolvePaths is now true
dataSource.TryResolveFile(out AbsoluteFilePath dataFile);
codeGenerator.TryResolveOutputPath(out AbsoluteDirectoryPath outputDirectory);
```

A schema built in memory, or parsed with the anchorless `Load(json)`, has no anchor. Resolution
then fails rather than falling back to the process's working directory, which would silently
resolve to somewhere unrelated. `Schema.Validate` likewise only reports a missing data file when
the schema knows where it lives.

## Data files

A data source binds a data file to a class. The file's root is either:

- **a single object** conforming to that class, or
- **an array of objects**, each conforming to it.

Both are validated by the same rules, by `SchemaDataValidator`:

| Type | Expected JSON |
| --- | --- |
| `Int`, `Long` | a whole number |
| `Float`, `Double` | a number |
| `String` | a string |
| `Bool` | `true` or `false` |
| `DateTime`, `TimeSpan` | a string that parses as one |
| `Vector2` / `Vector3` / `Vector4` | an array of 2 / 3 / 4 numbers |
| `ColorRGB` / `ColorRGBA` | an array of 3 / 4 numbers |
| `Enum` | a string that is one of the enum's values |
| `Object` | an object conforming to the named class |
| `Array` with `vector` | an array of the element type |
| `Array` with `map` | an object whose values are the element type, each entry's key matching that entry's `key` member |

The schema has no notion of an optional member, so **every member of a class is required**: a
missing one is an error. A property the class has no member for is a warning rather than an error,
since carrying extra data does not by itself contradict the schema.

Member lookup accepts the casing the serializer would have written, so a data file may use either
the member's declared name or its camel-cased form.

## Versioning

`formatVersion` is an integer that increases when the shape of the file changes in a way a reader
needs to know about.

| Version | Introduced by | Notes |
| --- | --- | --- |
| *(absent)* | - | Any file written before versioning. Read as version 0 and migrated on load. |
| `1` | The version field itself | A member's description moved from `memberDescription` to the `description` every element shares. |
| `2` | Semantic member metadata | A member may carry `unit`, `range`, `defaultValue`, `interpolation`, `network` and `editor`. All optional and omitted when absent. |
| `3` | Interfaces | The root gains `interfaces`, and the type vocabulary gains `Void`, `Interface`, `Span`, `Handle`, `Result` and `Optional`. Additive: a version 2 file loads as a schema with no interfaces. |

Version 2 is purely additive: a file that uses none of the new properties is byte-identical to
the version 1 file it would have been. The version still moves, because a version 1 reader
ignores properties it does not recognise — it would load such a file, drop the metadata, and
write it back without it, losing information that the round-trip contract above says must
survive. Refusing to read a version 2 file is the honest outcome, and is what the policy below
already specifies.

### How a reader must behave

- **A version it knows** - read it.
- **An older version** - migrate it forward, then treat it as current. Migrations are cumulative:
  a very old file is carried through each step in turn.
- **No version field** - treat it as version 0 and migrate. Files written before versioning are
  still readable and always will be.
- **A newer version** - refuse it, and say so distinctly. `SchemaSerializer.Load` returns
  `SchemaLoadStatus.UnsupportedFutureVersion` with a message naming both versions, rather than
  reporting a parse failure. A newer writer may have changed the meaning of what is already
  there, so reading it on a guess would silently drop or misinterpret data.

```csharp
SchemaLoadResult result = SchemaSerializer.Load(json);
switch (result.Status)
{
    case SchemaLoadStatus.Success:
        Use(result.Schema!);
        break;
    case SchemaLoadStatus.UnsupportedFutureVersion:
        Report($"Written by a newer version: {result.Message}");
        break;
    case SchemaLoadStatus.InvalidJson:
        Report($"Not a readable schema: {result.Message}");
        break;
}
```

`SchemaSerializer.TryDeserialize` remains for callers that only need to know whether the load
worked.

Saving always writes `formatVersion` at the current version, so opening and saving an old file
upgrades it.

## Code generation

`SchemaGenerator` runs the generators a schema declares:

```csharp
SchemaGenerationResult result = SchemaGenerator.GenerateToDisk(schema, codeGenerator);
```

Generation is **refused for a schema with error-severity validation issues**. Emitting code from a
schema whose references do not resolve would produce either code that does not compile, or code
that compiles into something the schema does not describe - and the resulting error would point at
generated code instead of at the schema mistake behind it. Warnings do not refuse: an incomplete
schema is a legitimate work in progress.

The built-in `csharp` generator emits one file per class and per enum, named `<Name>.g.cs`.
Descriptions become XML doc comments, which is what descriptions are for.

### C# type mapping

| Schema type | C# |
| --- | --- |
| `Bool` | `bool` |
| `Int` | `int` |
| `Long` | `long` |
| `Float` | `float` |
| `Double` | `double` |
| `String` | `string` |
| `DateTime` | `System.DateTime` |
| `TimeSpan` | `System.TimeSpan` |
| `Vector2` / `Vector3` / `Vector4` | `System.Numerics.Vector2` / `3` / `4` |
| `ColorRGB` / `ColorRGBA` | `ktsu.Schema.Runtime.ColorRgb` / `ColorRgba` |
| `Enum` | the generated enum |
| `Object` | the generated class |
| `Array` with `vector` | `List<T>` |
| `Array` with `map` | `Dictionary<TKey, T>`, `TKey` taken from the `key` member's type |

The colours have no counterpart in the base class library, so the library provides the two types
they map to. A generator inventing its own would break the round trip below.

### The round trip

This mapping is the exact inverse of the one `Schema.AddClass(Type)` uses to import CLR types, so
generating C# from a schema and reimporting the compiled result reproduces the schema it started
from. A test compiles the generated source and reimports it, so the two mappings cannot drift
apart unnoticed.

Some things need help to survive that trip, because a C# type says what a value *is* and nothing
about what it means. A `Dictionary<TKey, T>` records the key's *type* but not which member it came
from, and a `float` measured in metres per second is the same `float` as one measured in nothing.
Generated properties therefore carry attributes from `ktsu.Schema.Runtime`, which the importer
reads back:

| Attribute | Carries |
| --- | --- |
| `[SchemaKey("Id")]` | The member a keyed map keys on. |
| `[SchemaUnit("m/s")]` | The member's unit, in the same spelling the file holds. |
| `[SchemaRange(0D, 6.2831853D, Wrap = true)]` | Its bounds, and whether they wrap. |
| `[SchemaDefault(1.5D)]` | Its default. One constructor per kind of value - number, boolean, text - so the overload says which kind it is. |
| `[SchemaInterpolation(Interpolation.Spherical)]` | How two of its states blend. |
| `[SchemaNetwork(0.01D, true)]` | Its quantisation step and delta flag. |
| `[SchemaEditorHint("dial")]` | How an editor should present it. |

A default is also emitted as the property's initialiser, so a generated instance *starts* at the
default rather than only recording what it should have been:

```csharp
[ktsu.Schema.Runtime.SchemaUnit("m/s")]
[ktsu.Schema.Runtime.SchemaRange(0D, 6.2831853D, Wrap = true)]
[ktsu.Schema.Runtime.SchemaDefault(1.5D)]
[ktsu.Schema.Runtime.SchemaEditorHint("dial")]
public float Ratio { get; set; } = 1.5f;
```

The attribute and the initialiser are not redundant: the initialiser is what makes the object
right, and the attribute is what lets the default be read back off the type.

### Running a generator

From the editor, select a code generator and press **Generate**.

From the command line:

```shell
dotnet run --project SchemaTool -- generate path/to/game.schema.json
dotnet run --project SchemaTool -- validate path/to/game.schema.json
```

`validate` exits non-zero when the schema has errors, so it can gate a build. Warnings do not fail
it.

## Compatibility policy

What a release may do to this format:

| Change | Allowed in |
| --- | --- |
| Adding an optional property that older readers can ignore | patch or minor |
| Adding a new `TypeName` | minor - older readers will fail to read files that use it, so `formatVersion` increases with it |
| Adding a top-level collection (as `interfaces` was) | minor, with a `formatVersion` increase - additive, so an older file loads with the collection empty |
| Adding a container name to the known vocabulary | patch or minor - the vocabulary is open, so an unknown name is only a warning |
| Renaming or removing a property, or changing the meaning of an existing one | major, with a migration step and a `formatVersion` increase |
| Changing the discriminator property name (`TypeName`) | major |

Every `formatVersion` increase ships with a migration step from the previous version, and this
document gains a row in the [version table](#versioning). Files that predate versioning remain
readable.
