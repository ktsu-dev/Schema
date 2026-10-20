// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System;
using System.Collections.ObjectModel;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>
/// The rewrite that keeps every relative path in a schema pointing at the same file once the
/// schema itself is saved somewhere else.
/// </summary>
/// <remarks>
/// A data source's file and a code generator's output directory are relative to the directory
/// holding the <c>.schema.json</c>. Saving the schema into another directory moves that anchor, so
/// left alone each path would quietly start meaning a file the user never moved. The plan records
/// each path as written and as it has to be written from the new directory, so a caller can apply
/// it before saving and put the old text back if the save fails or is undone.
///
/// A path is left out of the plan when it is empty, when the schema has no anchor to resolve it
/// against yet, or when no relative route exists from the new directory - a different drive on
/// Windows - since there is nothing a relative path could say there.
/// </remarks>
public sealed class SchemaPathRebase
{
	private readonly Collection<Action> apply = [];
	private readonly Collection<Action> revert = [];

	private SchemaPathRebase()
	{
	}

	/// <summary>
	/// Gets a value indicating whether saving to the new location changes no path at all.
	/// </summary>
	public bool IsEmpty => apply.Count == 0;

	/// <summary>
	/// Gets how many paths the plan rewrites.
	/// </summary>
	public int Count => apply.Count;

	/// <summary>
	/// Writes every path as it has to read from the new location.
	/// </summary>
	public void Apply()
	{
		foreach (Action action in apply)
		{
			action();
		}
	}

	/// <summary>
	/// Puts every path back as it was written before <see cref="Apply"/>.
	/// </summary>
	public void Revert()
	{
		foreach (Action action in revert)
		{
			action();
		}
	}

	/// <summary>
	/// Plans the rewrite that keeps <paramref name="schema"/>'s relative paths resolving to the same
	/// places once it is saved as <paramref name="newSchemaFilePath"/>.
	/// </summary>
	/// <param name="schema">The schema about to be saved.</param>
	/// <param name="newSchemaFilePath">Where the <c>.schema.json</c> is about to be written.</param>
	/// <returns>The plan, which is empty when the directory does not change or the schema has no anchor.</returns>
	public static SchemaPathRebase Plan(Schema schema, AbsoluteFilePath newSchemaFilePath)
	{
		Ensure.NotNull(schema);
		Ensure.NotNull(newSchemaFilePath);

		SchemaPathRebase plan = new();
		string? newDirectory = Path.GetDirectoryName((string)newSchemaFilePath);
		if (!schema.CanResolvePaths || string.IsNullOrEmpty(newDirectory) || SameDirectory(schema.SourceDirectory, newDirectory))
		{
			return plan;
		}

		foreach (DataSource dataSource in schema.DataSources)
		{
			RelativeFilePath written = dataSource.File;
			if (schema.TryResolvePath(written, out AbsoluteFilePath resolved)
				&& TryRelativeTo(newDirectory, resolved, out string rebased)
				&& rebased != written)
			{
				RelativeFilePath replacement = rebased.As<RelativeFilePath>();
				plan.Add(() => dataSource.File = replacement, () => dataSource.File = written);
			}
		}

		foreach (SchemaCodeGenerator codeGenerator in schema.CodeGenerators)
		{
			RelativeDirectoryPath written = codeGenerator.OutputPath;
			if (schema.TryResolvePath(written, out AbsoluteDirectoryPath resolved)
				&& TryRelativeTo(newDirectory, resolved, out string rebased)
				&& rebased != written)
			{
				RelativeDirectoryPath replacement = rebased.As<RelativeDirectoryPath>();
				plan.Add(() => codeGenerator.OutputPath = replacement, () => codeGenerator.OutputPath = written);
			}
		}

		return plan;
	}

	private void Add(Action applyOne, Action revertOne)
	{
		apply.Add(applyOne);
		revert.Add(revertOne);
	}

	private static bool SameDirectory(string first, string second) =>
		string.Equals(
			Path.TrimEndingDirectorySeparator(Path.GetFullPath(first)),
			Path.TrimEndingDirectorySeparator(Path.GetFullPath(second)),
			OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

	/// <summary>
	/// The route from <paramref name="directory"/> to <paramref name="target"/>, written with
	/// forward slashes so the file reads the same whichever platform saved it.
	/// </summary>
	private static bool TryRelativeTo(string directory, string target, out string relative)
	{
		relative = Path.GetRelativePath(directory, target);
		if (Path.IsPathRooted(relative))
		{
			return false;
		}

		relative = relative.Replace(Path.DirectorySeparatorChar, '/');
		return true;
	}
}
