# ktsu.Schema.Cpp

The C++ generator for [ktsu.Schema](https://github.com/ktsu-dev/Schema): a `.schema.json` file in, C++ headers out.

[![NuGet Version](https://img.shields.io/nuget/v/ktsu.Schema.Cpp?label=Stable&logo=nuget)](https://nuget.org/packages/ktsu.Schema.Cpp)
[![NuGet Version](https://img.shields.io/nuget/vpre/ktsu.Schema.Cpp?label=Latest&logo=nuget)](https://nuget.org/packages/ktsu.Schema.Cpp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ktsu.Schema.Cpp?label=Downloads&logo=nuget)](https://nuget.org/packages/ktsu.Schema.Cpp)

## Overview

A schema says what your data *is*. This turns that into the C++ that says it too:

- a **class** becomes a `struct`, its members in the order the schema declares them
- an **enum** becomes an `enum class` at namespace scope, in a header of its own
- a **semantic type** becomes a class that shims its representation - so an `EntityId` and a `TextureId` are both a `long` and neither is the other
- an **interface** becomes a pure virtual base, one signature per declared function

The schema is turned into a [ktsu.Coder](https://github.com/ktsu-dev/Coder) AST, and that AST is what decides how C++ is spelled. Nothing in this package writes a brace.

## Installation

```shell
dotnet add package ktsu.Schema.Cpp
```

### Requirements

- .NET 9.0 or 10.0

One framework short of `ktsu.Schema` itself, which also publishes `net8.0`. The AST this builds on has no `net8.0` assembly, which is the whole reason the generator ships as a separate package rather than inside the library: defining and reading a schema stays available to `net8.0`, and only generating C++ from one does not.

## Quick start

Register the generator, then run whatever names it:

```csharp
using ktsu.Schema.Cpp;
using ktsu.Schema.Generation;

SchemaGenerator.Register(new CppCodeGenerator());
```

A schema names the language as `cpp` in its code generator element; `Register` is how that name finds this implementation. `Schema.Tool/Program.cs` in the repository is the worked example of a host doing it.

## Telling the generator what your program already has

Almost everything comes from the schema. What is left is the short list of types standard C++ has no answer for - there is no fixed-shape numeric vector, no identifier-with-a-generation, no fallible return. A target says how it spells them:

```csharp
CppGeneratorOptions options = new()
{
    Vector3 = new CppTypeSpelling("holo::Vector3", "holotype/core/vector.hpp"),
    Handle = new CppTypeSpelling("holo::Handle", "holotype/core/handle.hpp"),
    Result = new CppTypeSpelling("holo::Result", "holotype/core/result.hpp"),
};

SchemaGenerator.Register(new CppCodeGenerator(options));
```

Each spelling carries its header as well as its name, because they are one fact: a generated file naming `holo::Vector3` without including the header that declares it does not compile, and only whoever supplied the name knows which header that is.

A target that says nothing is **refused those schema types by name**, with the option to set, rather than handed a header that will not compile.

`ExistingTypes` is the other direction - a semantic type your headers already declare is named where it is used rather than generated a second time:

```csharp
ExistingTypes = new Dictionary<string, CppTypeSpelling>(StringComparer.Ordinal)
{
    ["Kilograms"] = new("holo::Kilograms", "holotype/core/units.hpp"),
    ["Seconds"] = new("holo::Seconds", "holotype/core/units.hpp"),
}
```

The rest of `CppGeneratorOptions` is cosmetic: `HeaderExtension` (`.gen.hpp` by default), `MemberNaming` (`snake_case` by default), and `GeneratedBy`, the name a generated file's banner opens with - which a target that vendors this behind its own build step should set to the thing a reader would run again.

## Conventions the generated headers keep

Four conventions carry globally rather than being annotated per signature, so a schema never grows an ownership dialect:

| The schema says | C++ gets |
| --- | --- |
| `Result` in a return type | the call can fail |
| `Handle` | something the caller may keep |
| `Span` | a borrow valid for the call |
| `Direction` on a parameter | `const`, or not |
| `IsQuery` on a function | a trailing `const` |

On a `Span`, direction describes the **elements**: `In Span<Velocity>` is `std::span<const Velocity>`, `Out Span<Position>` is `std::span<Position>`.

## License

Licensed under the MIT License. See [LICENSE.md](https://github.com/ktsu-dev/Schema/blob/main/LICENSE.md).
