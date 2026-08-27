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
  "formatVersion": 1,
  "classes": [],
  "enums": [],
  "codeGenerators": [],
  "dataSources": []
}
```

| Property | Type | Meaning |
| --- | --- | --- |
| `formatVersion` | integer | The format version. Written first so a reader can find it without scanning the document. See [Versioning](#versioning). |
| `classes` | array of [class](#class) | The classes the schema defines. |
| `enums` | array of [enum](#enum) | The enumerations the schema defines. |
| `codeGenerators` | array of [code generator](#code-generator) | Code generator configurations. |
| `dataSources` | array of [data source](#data-source) | Bindings from a data file to a class. |

All four collections are always written, empty or not.

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
  "name": "CSharp",
  "description": ""
}
```

| Property | Type | Meaning |
| --- | --- | --- |
| `outputPath` | string | A relative directory to write generated files into. See [Path resolution](#path-resolution). |

## Types

A type is an object with a `TypeName` discriminator. `TypeName` is deliberately not camel-cased -
it is the polymorphic discriminator, not a data property.

### Types with no further properties

`TypeName` alone fully describes these:

| `TypeName` | Meaning |
| --- | --- |
| `None` | No type chosen yet. A valid intermediate editing state, not a generatable one. |
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

> Resolving these paths against the filesystem, and validating that a data source's file exists,
> is tracked by [issue #120](https://github.com/ktsu-dev/Schema/issues/120). This section defines
> what the paths mean; the resolution API lands with that work.

## Versioning

`formatVersion` is an integer that increases when the shape of the file changes in a way a reader
needs to know about.

| Version | Introduced by | Notes |
| --- | --- | --- |
| *(absent)* | - | Any file written before versioning. Read as version 0 and migrated on load. |
| `1` | The version field itself | A member's description moved from `memberDescription` to the `description` every element shares. |

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

## Compatibility policy

What a release may do to this format:

| Change | Allowed in |
| --- | --- |
| Adding an optional property that older readers can ignore | patch or minor |
| Adding a new `TypeName` | minor - older readers will fail to read files that use it, so `formatVersion` increases with it |
| Adding a container name to the known vocabulary | patch or minor - the vocabulary is open, so an unknown name is only a warning |
| Renaming or removing a property, or changing the meaning of an existing one | major, with a migration step and a `formatVersion` increase |
| Changing the discriminator property name (`TypeName`) | major |

Every `formatVersion` increase ships with a migration step from the previous version, and this
document gains a row in the [version table](#versioning). Files that predate versioning remain
readable.
