// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models;

using ktsu.Schema.Models.Names;

/// <summary>
/// Represents a data source in a schema.
/// </summary>
public class DataSource : SchemaChild<DataSourceName>
{
	/// <summary>
	/// Tries to remove this data source from its parent schema.
	/// </summary>
	/// <returns>True if the data source was removed; otherwise, false.</returns>
	public override bool TryRemove() => ParentSchema?.TryRemoveDataSource(this) ?? false;
}
