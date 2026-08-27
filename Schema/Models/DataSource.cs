// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Text.Json.Serialization;
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
	[JsonIgnore]
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
	/// Resolves <see cref="File"/> against the directory the schema was loaded from.
	/// </summary>
	/// <param name="resolved">The absolute path to the data file, when one could be produced.</param>
	/// <returns>True if the path was resolved; false if the schema has no anchor or no file is set.</returns>
	public bool TryResolveFile(out AbsoluteFilePath resolved)
	{
		resolved = new();
		return ParentSchema?.TryResolvePath(File, out resolved) ?? false;
	}

	/// <summary>
	/// Tries to remove this data source from its parent schema.
	/// </summary>
	/// <returns>True if the data source was removed; otherwise, false.</returns>
	public override bool TryRemove() => ParentSchema?.TryRemoveDataSource(this) ?? false;
}
