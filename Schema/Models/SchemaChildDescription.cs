// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models;

using ktsu.Schema.Contracts;
using ktsu.Semantics.Strings;

/// <summary>
/// Represents a description for a schema child.
/// </summary>
public sealed record class SchemaChildDescription : SemanticString<SchemaChildDescription>, ISchemaChildDescription
{
}
