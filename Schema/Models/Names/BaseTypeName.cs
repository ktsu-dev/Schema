// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models.Names;

using ktsu.Schema.Contracts.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// Represents a base type name as a strong string type.
/// </summary>
public sealed record class BaseTypeName : SemanticString<BaseTypeName>, ISchemaTypeName<BaseTypeName>, ISchemaClassChildName<BaseTypeName> { }
