// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Generation;

using System.IO;

/// <summary>
/// The record a code generator leaves in its output directory of which files it wrote, so the next
/// run can remove the ones it no longer produces.
/// </summary>
/// <remarks>
/// A generator names one file per element, so a class that is renamed or deleted leaves its old
/// file behind, and that file goes on compiling a type the schema no longer declares. Nothing in
/// the output directory says which files a generator wrote, and the directory may hold hand-written
/// ones beside them, so the manifest is the only safe answer to "which of these may be deleted":
/// a file is removed only when an earlier run recorded writing it.
/// <para>
/// There is one manifest per generator, named for it, because two generators may share an output
/// directory and neither may delete the other's files. Renaming a generator therefore starts it
/// with no record, and the files the old name wrote are left alone rather than guessed at.
/// </para>
/// </remarks>
internal static class GeneratedFileManifest
{
	private const string Header = "# Files written by the schema code generator. Generated; do not edit.";

	/// <summary>
	/// Gets the manifest's file name for a code generator.
	/// </summary>
	/// <param name="generatorName">The code generator's name.</param>
	/// <returns>A file name with any character a file system would refuse replaced.</returns>
	internal static string FileNameFor(string generatorName)
	{
		char[] invalid = Path.GetInvalidFileNameChars();
		string safe = new([.. generatorName.Select(c => invalid.Contains(c) ? '_' : c)]);
		return $".{safe}.schema-generated";
	}

	/// <summary>
	/// Reads the files an earlier run recorded writing.
	/// </summary>
	/// <param name="manifestPath">Where the manifest is.</param>
	/// <returns>The recorded paths, relative to the output directory; empty when there is no manifest.</returns>
	internal static IReadOnlyList<string> Read(string manifestPath)
	{
		if (!File.Exists(manifestPath))
		{
			return [];
		}

		return [.. File.ReadAllLines(manifestPath)
			.Select(line => line.Trim())
			.Where(line => line.Length > 0 && !line.StartsWith('#'))];
	}

	/// <summary>
	/// Records the files a run is responsible for.
	/// </summary>
	/// <param name="manifestPath">Where to write the manifest.</param>
	/// <param name="files">The paths, relative to the output directory.</param>
	internal static void Write(string manifestPath, IEnumerable<string> files) =>
		File.WriteAllLines(manifestPath, [Header, .. files.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)]);

	/// <summary>
	/// Resolves a recorded path against the output directory, refusing one that would land outside
	/// it.
	/// </summary>
	/// <remarks>
	/// The manifest is a text file anyone can edit, and deleting is the one thing this does with
	/// what it reads. A rooted path or one climbing out with <c>..</c> is never a file a generator
	/// wrote here, so it is ignored rather than followed.
	/// </remarks>
	/// <param name="outputDirectory">The generator's output directory.</param>
	/// <param name="relativePath">A path read from the manifest.</param>
	/// <param name="fullPath">The resolved path, when it is inside the output directory.</param>
	/// <returns>True when the path is inside the output directory.</returns>
	internal static bool TryResolve(string outputDirectory, string relativePath, out string fullPath)
	{
		fullPath = string.Empty;
		if (Path.IsPathRooted(relativePath))
		{
			return false;
		}

		string root = Path.GetFullPath(outputDirectory);
		string candidate = Path.GetFullPath(Path.Combine(root, relativePath));
		string prefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
		if (!candidate.StartsWith(prefix, StringComparison.Ordinal))
		{
			return false;
		}

		fullPath = candidate;
		return true;
	}
}
