// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Names;

using ktsu.Schema.Contracts.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// Represents an enum value name as a strong string type.
/// </summary>
public sealed record class EnumValueName : SemanticString<EnumValueName>, ISchemaChildName { }
