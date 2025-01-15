namespace ktsu.Schema;

using System.Diagnostics;
using ktsu.Extensions;
using ktsu.StrongPaths;

/// <summary>
/// Represents a data source in a schema.
/// </summary>
public class DataSource : SchemaChild<DataSourceName>
{
	/// <summary>
	/// The file suffix for data source files.
	/// </summary>
	public const string FileSuffix = ".data.json";

	/// <summary>
	/// Gets or sets the root schema member associated with this data source.
	/// </summary>
	public RootSchemaMember RootSchemaMember { get; set; } = new();

	/// <summary>
	/// Gets the file path of the data source.
	/// </summary>
	public AbsoluteFilePath FilePath
	{
		get
		{
			Debug.Assert(ParentSchema is not null);
			return ParentSchema.DataSourcePath / $"{Name}{FileSuffix}".As<FileName>();
		}
	}

	/// <summary>
	/// Tries to remove this data source from the parent schema.
	/// </summary>
	/// <returns>True if the data source was successfully removed; otherwise, false.</returns>
	public override bool TryRemove() => ParentSchema?.TryRemoveDataSource(this) ?? throw new InvalidOperationException("You can not remove a DataSource that doesn't belong to a Schema");
}
