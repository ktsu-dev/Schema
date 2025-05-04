// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema;

using ktsu.StrongPaths;

/// <summary>
/// Represents the paths used in the schema.
/// </summary>
public class SchemaPaths
{
	/// <summary>
	/// Gets or sets the relative path to the project root directory.
	/// </summary>
	public RelativeDirectoryPath RelativeProjectRootPath { get; set; } = new();

	/// <summary>
	/// Gets or sets the relative path to the data source directory.
	/// </summary>
	public RelativeDirectoryPath RelativeDataSourcePath { get; set; } = new();
}
