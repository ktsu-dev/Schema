// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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
	/// Tries to remove this code generator from its parent schema.
	/// </summary>
	/// <returns>True if the code generator was removed; otherwise, false.</returns>
	public override bool TryRemove() => ParentSchema?.TryRemoveCodeGenerator(this) ?? false;
}
