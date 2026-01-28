// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models.Names;

using ktsu.Schema.Contracts.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// Represents a data source name as a strong string type.
/// </summary>
public sealed record class DataSourceName : SemanticString<DataSourceName>, ISchemaDataSourceName { }
