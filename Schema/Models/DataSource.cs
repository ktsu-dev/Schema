// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models;

using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;

/// <summary>
/// Represents a data source in a schema.
/// </summary>
public class DataSource : SchemaChild<DataSourceName>
{
	/// <summary>
	/// Gets or sets the relative file path for this data source.
	/// </summary>
	public RelativeFilePath File { get; set; } = new();

	/// <summary>
	/// Gets or sets the class name this data source is associated with.
	/// </summary>
	public ClassName ClassName { get; set; } = new();

	/// <summary>
	/// Gets the schema class associated with this data source.
	/// Resolved lazily from the parent schema using ClassName.
	/// </summary>
	public SchemaClass? Class
	{
		get
		{
			if (!string.IsNullOrEmpty(ClassName) && ParentSchema is not null)
			{
				ParentSchema.TryGetClass(ClassName, out SchemaClass? schemaClass);
				return schemaClass;
			}

			return null;
		}
	}

	/// <summary>
	/// Tries to remove this data source from its parent schema.
	/// </summary>
	/// <returns>True if the data source was removed; otherwise, false.</returns>
	public override bool TryRemove() => ParentSchema?.TryRemoveDataSource(this) ?? false;
}
