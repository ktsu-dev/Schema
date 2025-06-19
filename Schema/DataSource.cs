// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema;

/// <summary>
/// Represents a data source in a schema.
/// This class focuses on schema definition without filesystem concerns.
/// </summary>
public class DataSource : SchemaChild<DataSourceName>
{
	/// <summary>
	/// Gets or sets the root schema member associated with this data source.
	/// </summary>
	public RootSchemaMember RootSchemaMember { get; set; } = new();

	/// <summary>
	/// Tries to remove this data source from the parent schema provider.
	/// </summary>
	/// <returns>True if the data source was successfully removed; otherwise, false.</returns>
	public override bool TryRemove() => ParentSchema?.TryRemoveDataSource(this) ?? throw new InvalidOperationException("You can not remove a DataSource that doesn't belong to a SchemaProvider");
}
