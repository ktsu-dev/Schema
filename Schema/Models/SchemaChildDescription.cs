// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using ktsu.Schema.Contracts;
using ktsu.Semantics.Strings;

/// <summary>
/// Represents a description for a schema child.
/// </summary>
public sealed record class SchemaChildDescription : SemanticString<SchemaChildDescription>, ISchemaChildDescription
{
}
