// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor;

using System;
using System.IO;

using ktsu.Schema.Models;
using ktsu.Semantics.Strings;

/// <summary>
/// Turns what a user typed or picked into a path the schema can hold.
/// </summary>
/// <remarks>
/// <para>
/// A schema holds its output and data source paths relative to the file it was saved in, and the
/// path types refuse an absolute value outright by throwing. A directory browser answers with an
/// absolute path and a user pasting one into a field is ordinary, so converting either directly
/// threw out of the frame. This is the one place that decides what such a value becomes.
/// </para>
/// <para>
/// An absolute path is made relative to the schema's directory, which needs a saved schema; an
/// unsaved one has nothing to be relative to, so the value is refused with a reason rather than
/// guessed at. A value that is already relative passes through unchanged.
/// </para>
/// </remarks>
internal static class SchemaRelativePath
{
	/// <summary>
	/// Converts <paramref name="input"/> to a path relative to <paramref name="schema"/>'s directory.
	/// </summary>
	/// <typeparam name="TPath">The relative path type to produce.</typeparam>
	/// <param name="schema">The schema the path belongs to.</param>
	/// <param name="input">What the user typed or chose, relative or absolute.</param>
	/// <param name="path">The relative path, when one could be produced.</param>
	/// <param name="problem">Why no path could be produced, when one could not.</param>
	/// <returns>True if <paramref name="path"/> holds the converted value.</returns>
	internal static bool TryCreate<TPath>(Schema schema, string input, out TPath path, out string problem)
		where TPath : SemanticString<TPath>
	{
		path = null!;
		problem = string.Empty;
		string relative = input ?? string.Empty;

		if (Path.IsPathRooted(relative))
		{
			if (!schema.CanResolvePaths)
			{
				problem = $"'{relative}' is an absolute path. Save the schema first: paths are stored relative to the schema file.";
				return false;
			}

			relative = Path.GetRelativePath(schema.SourceDirectory, relative);

			// On Windows a path on another drive has no relative form, and comes back unchanged.
			if (Path.IsPathRooted(relative))
			{
				problem = $"'{relative}' cannot be expressed relative to the schema file at '{schema.SourceDirectory}'.";
				return false;
			}
		}

		try
		{
			path = relative.As<TPath>();
			return true;
		}
		catch (ArgumentException exception)
		{
			problem = $"'{relative}' is not a valid relative path: {exception.Message}";
			return false;
		}
	}
}
