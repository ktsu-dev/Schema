// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Types;

/// <summary>
/// Represents a built-in structured type supplied by the schema library itself, such as a
/// vector or a color.
/// </summary>
/// <remarks>
/// A system object is structured, but unlike <see cref="Object"/> it does not reference a
/// user-defined <see cref="SchemaClass"/>: its shape is fixed and known to the library. It is
/// therefore rooted at <see cref="BaseType"/> rather than at <see cref="Object"/>, which keeps
/// <see cref="BaseType.IsObject"/> meaning "references a user-defined class".
/// </remarks>
public abstract class SystemObject : BaseType { }
