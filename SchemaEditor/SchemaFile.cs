// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

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

			return SchemaSerializer.Load(File.ReadAllText(filePath));
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
	public static bool TrySave(Schema schema, AbsoluteFilePath filePath)
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

			File.WriteAllText(filePath, json);
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
}
