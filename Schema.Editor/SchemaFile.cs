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
		TrySave(schema, filePath, File.WriteAllText);

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
	/// <returns>True if the schema was saved successfully, false otherwise.</returns>
	internal static bool TrySave(Schema schema, AbsoluteFilePath filePath, Action<string, string> writeFile)
	{
		if (schema is null || string.IsNullOrEmpty(filePath))
		{
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
			return true;
		}
		catch (JsonException)
		{
			// Serialization error
		}
		catch (IOException)
		{
			// File access error
		}

		return false;
	}

	/// <summary>
	/// Writes <paramref name="content"/> to a temporary file in the target's directory, then moves it
	/// over the target, so the target only ever holds a complete previous save or a complete new one.
	/// </summary>
	/// <remarks>
	/// The temporary file is in the same directory so the final step is a rename on one volume rather
	/// than a copy. It is deleted on any failure, which is what the <c>finally</c> is for: an
	/// exception the caller does not catch still must not leave it behind.
	/// </remarks>
	private static void WriteAtomically(string targetPath, string content, Action<string, string> writeFile)
	{
		string temporaryPath = $"{targetPath}.tmp-{Guid.NewGuid():N}";

		try
		{
			writeFile(temporaryPath, content);

			if (File.Exists(targetPath))
			{
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
}
