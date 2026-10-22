// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor;

using System.Text.Json;

using ktsu.Schema.Models;
using ktsu.Semantics.Paths;

/// <summary>
/// Provides file persistence functionality for Schema objects.
/// Delegates serialization to the core SchemaSerializer.
/// </summary>
internal static class SchemaFile
{
	/// <summary>
	/// Tries to load a Schema from a file path.
	/// </summary>
	/// <param name="filePath">The path to the schema file.</param>
	/// <param name="schema">The loaded schema, or null if loading failed.</param>
	/// <returns>True if the schema was loaded successfully, false otherwise.</returns>
	public static bool TryLoad(AbsoluteFilePath filePath, out Schema? schema)
	{
		SchemaLoadResult result = Load(filePath);
		schema = result.Schema;
		return result.IsSuccess;
	}

	/// <summary>
	/// Loads a schema from a file, reporting why the load failed when it does.
	/// </summary>
	/// <remarks>
	/// Keeps the reason intact so the editor can tell the user that a file was written by a newer
	/// version of the library, rather than reporting every failure as a broken file.
	/// </remarks>
	/// <param name="filePath">The path to the schema file.</param>
	/// <returns>The outcome, including the schema when the load succeeded.</returns>
	public static SchemaLoadResult Load(AbsoluteFilePath filePath)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			return new()
			{
				Status = SchemaLoadStatus.InvalidJson,
				Message = "No file path was given.",
			};
		}

		try
		{
			if (!File.Exists(filePath))
			{
				return new()
				{
					Status = SchemaLoadStatus.InvalidJson,
					Message = $"'{filePath}' does not exist.",
				};
			}

			// Passing the path anchors the schema's relative paths - a data source's file, a code
			// generator's output directory - to the directory the schema file lives in.
			return SchemaSerializer.Load(File.ReadAllText(filePath), filePath);
		}
		catch (IOException ex)
		{
			return new()
			{
				Status = SchemaLoadStatus.InvalidJson,
				Message = $"'{filePath}' could not be read: {ex.Message}",
			};
		}
		catch (UnauthorizedAccessException ex)
		{
			return new()
			{
				Status = SchemaLoadStatus.InvalidJson,
				Message = $"'{filePath}' could not be read: {ex.Message}",
			};
		}
	}

	/// <summary>
	/// Saves a Schema to a file path.
	/// </summary>
	/// <param name="schema">The schema to save.</param>
	/// <param name="filePath">The path to save the schema to.</param>
	/// <returns>True if the schema was saved successfully, false otherwise.</returns>
	public static bool TrySave(Schema schema, AbsoluteFilePath filePath) =>
		TrySave(schema, filePath, File.WriteAllText, out _);

	/// <summary>
	/// Saves a Schema to a file path, and says why it could not when it could not.
	/// </summary>
	/// <param name="schema">The schema to save.</param>
	/// <param name="filePath">The path to save the schema to.</param>
	/// <param name="failure">Why the save failed, or null when it succeeded.</param>
	/// <returns>True if the schema was saved successfully, false otherwise.</returns>
	public static bool TrySave(Schema schema, AbsoluteFilePath filePath, out string? failure) =>
		TrySave(schema, filePath, File.WriteAllText, out failure);

	/// <inheritdoc cref="TrySave(Schema, AbsoluteFilePath, Action{string, string}, out string?)"/>
	internal static bool TrySave(Schema schema, AbsoluteFilePath filePath, Action<string, string> writeFile) =>
		TrySave(schema, filePath, writeFile, out _);

	/// <summary>
	/// Saves a Schema to a file path, writing the content through <paramref name="writeFile"/>.
	/// </summary>
	/// <remarks>
	/// The content goes to a temporary file beside the target and is swapped into place only once it
	/// has been written whole, so a write that fails partway - a full disk, a dropped network share -
	/// leaves the previous save intact rather than truncated. <paramref name="writeFile"/> is the seam
	/// a test uses to make that write fail; it is only ever given the temporary path.
	/// </remarks>
	/// <param name="schema">The schema to save.</param>
	/// <param name="filePath">The path to save the schema to.</param>
	/// <param name="writeFile">Writes the given text to the given path.</param>
	/// <param name="failure">Why the save failed, or null when it succeeded.</param>
	/// <returns>True if the schema was saved successfully, false otherwise.</returns>
	internal static bool TrySave(Schema schema, AbsoluteFilePath filePath, Action<string, string> writeFile, out string? failure)
	{
		if (schema is null || string.IsNullOrEmpty(filePath))
		{
			failure = "No file was chosen to save to.";
			return false;
		}

		try
		{
			string json = SchemaSerializer.Serialize(schema);
			string? directory = Path.GetDirectoryName((string)filePath);

			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			WriteAtomically(filePath, json, writeFile);
			failure = null;
			return true;
		}
		catch (JsonException ex)
		{
			failure = ex.Message;
		}
		catch (IOException ex)
		{
			failure = ex.Message;
		}
		catch (UnauthorizedAccessException ex)
		{
			// A read-only file or a directory the user cannot write to. Not an IOException, so
			// without this it escaped the save and took the editor down with the unsaved work.
			failure = ex.Message;
		}

		return false;
	}

	/// <summary>
	/// Writes <paramref name="content"/> to a temporary file in the target's directory, then moves it
	/// over the target, so the target only ever holds a complete previous save or a complete new one.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The temporary file is in the same directory so the final step is a rename on one volume rather
	/// than a copy. It is deleted on any failure, which is what the <c>finally</c> is for: an
	/// exception the caller does not catch still must not leave it behind.
	/// </para>
	/// <para>
	/// A rename replaces a directory entry, not a file, so two things the target's entry carried
	/// would be lost to it. A symbolic link would become a plain file holding the new content while
	/// the file it pointed at - a schema shared between projects - kept the old one, so the path is
	/// followed to the file it finally names and that file is the one replaced. And the new file
	/// would have the process's default mode, so on Unix the existing file's permission bits are
	/// copied onto the temporary file before the swap.
	/// </para>
	/// </remarks>
	private static void WriteAtomically(string targetPath, string content, Action<string, string> writeFile)
	{
		targetPath = ResolveFinalTarget(targetPath);
		string temporaryPath = $"{targetPath}.tmp-{Guid.NewGuid():N}";

		try
		{
			writeFile(temporaryPath, content);

			if (File.Exists(targetPath))
			{
				if (!OperatingSystem.IsWindows())
				{
					File.SetUnixFileMode(temporaryPath, File.GetUnixFileMode(targetPath));
				}

				File.Replace(temporaryPath, targetPath, destinationBackupFileName: null);
			}
			else
			{
				File.Move(temporaryPath, targetPath);
			}
		}
		finally
		{
			if (File.Exists(temporaryPath))
			{
				File.Delete(temporaryPath);
			}
		}
	}

	/// <summary>
	/// Follows <paramref name="path"/> through any symbolic links to the path of the file it finally
	/// names, or returns it unchanged when it is not a link.
	/// </summary>
	/// <remarks>
	/// A link whose target does not exist yet still resolves to that target, so the first save through
	/// it creates the file the link names rather than replacing the link.
	/// </remarks>
	private static string ResolveFinalTarget(string path)
	{
		FileInfo file = new(path);

		// Resolving a path that names nothing throws rather than answering null, and a first save
		// always names nothing, so only a path that is a link is resolved.
		return file.LinkTarget is null ? path : file.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? path;
	}
}
