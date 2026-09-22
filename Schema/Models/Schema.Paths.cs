// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using ktsu.Semantics.Paths;

/// <summary>
/// Resolving the relative paths a schema holds.
/// </summary>
/// <remarks>
/// A schema's relative paths - a data source's file, a code generator's output directory - are
/// anchored to the directory containing the <c>.schema.json</c> file. That anchor is what lets a
/// schema and the data beside it be moved or checked out somewhere else and still resolve.
///
/// The anchor is supplied by whoever read the file, so the serializer itself stays free of the
/// filesystem. A schema that was never read from a file has no anchor and cannot resolve
/// anything, which every resolution API reports rather than guessing at the working directory.
///
/// Resolution goes through <c>AsAbsolute</c> rather than the <c>/</c> combine operator: a schema
/// may reach a sibling directory through <c>..</c>, and only the former normalises those segments
/// away. The operator joins, which hands back a route to the file rather than the file.
/// </remarks>
public partial class Schema
{
	/// <summary>
	/// Gets a value indicating whether this schema knows where it was loaded from, and so can
	/// resolve the relative paths it holds.
	/// </summary>
	public bool CanResolvePaths => !string.IsNullOrEmpty(SourceDirectory);

	/// <summary>
	/// Resolves a path held by this schema against the directory the schema was loaded from.
	/// </summary>
	/// <param name="relativePath">The relative path to resolve.</param>
	/// <param name="resolved">The absolute path, when one could be produced.</param>
	/// <returns>True if the path was resolved; false if the schema has no anchor or the path is empty.</returns>
	public bool TryResolvePath(RelativeFilePath relativePath, out AbsoluteFilePath resolved)
	{
		resolved = new();

		if (!CanResolvePaths || string.IsNullOrEmpty(relativePath))
		{
			return false;
		}

		resolved = relativePath.AsAbsolute(SourceDirectory);
		return true;
	}

	/// <summary>
	/// Resolves a directory held by this schema against the directory the schema was loaded from.
	/// </summary>
	/// <param name="relativePath">The relative directory to resolve.</param>
	/// <param name="resolved">The absolute directory, when one could be produced.</param>
	/// <returns>True if the path was resolved; false if the schema has no anchor or the path is empty.</returns>
	public bool TryResolvePath(RelativeDirectoryPath relativePath, out AbsoluteDirectoryPath resolved)
	{
		resolved = new();

		if (!CanResolvePaths || string.IsNullOrEmpty(relativePath))
		{
			return false;
		}

		resolved = relativePath.AsAbsolute(SourceDirectory);
		return true;
	}

	/// <summary>
	/// Records where this schema was loaded from, so its relative paths can be resolved.
	/// </summary>
	/// <remarks>
	/// Called by whoever read the file. Passing the schema file's own path is the common case;
	/// the directory containing it becomes the anchor.
	/// </remarks>
	/// <param name="schemaFilePath">The path of the <c>.schema.json</c> file this schema came from.</param>
	/// <exception cref="ArgumentNullException"><paramref name="schemaFilePath"/> is null.</exception>
	public void SetSourceFile(AbsoluteFilePath schemaFilePath)
	{
		Ensure.NotNull(schemaFilePath);

		SourceDirectory = schemaFilePath.AbsoluteDirectoryPath;
		SourceFileName = Path.GetFileName((string)schemaFilePath);
	}
}
