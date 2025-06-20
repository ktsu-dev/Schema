// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models;

using ktsu.Schema.Contracts;
using ktsu.Semantics;

/// <summary>
/// Represents a summary of a schema child element.
/// </summary>
public sealed record class SchemaChildSummary : SemanticString<SchemaChildSummary>, ISchemaChildSummary
{
}