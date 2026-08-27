// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;

/// <summary>
/// Represents a code generator in a schema.
/// </summary>
public class SchemaCodeGenerator : SchemaChild<CodeGeneratorName>
{
	/// <summary>
	/// Gets or sets the output path for generated code.
	/// </summary>
	public RelativeDirectoryPath OutputPath { get; set; } = new();

	/// <summary>
	/// Resolves <see cref="OutputPath"/> against the directory the schema was loaded from.
	/// </summary>
	/// <param name="resolved">The absolute output directory, when one could be produced.</param>
	/// <returns>True if the path was resolved; false if the schema has no anchor or no output path is set.</returns>
	public bool TryResolveOutputPath(out AbsoluteDirectoryPath resolved)
	{
		resolved = new();
		return ParentSchema?.TryResolvePath(OutputPath, out resolved) ?? false;
	}

	/// <summary>
	/// Tries to remove this code generator from its parent schema.
	/// </summary>
	/// <returns>True if the code generator was removed; otherwise, false.</returns>
	public override bool TryRemove() => ParentSchema?.TryRemoveCodeGenerator(this) ?? false;
}
