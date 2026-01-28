// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.SchemaEditor;

using System.Text.Json;
using System.Text.Json.Serialization;

using ktsu.Schema.Models;
using ktsu.Semantics.Paths;

/// <summary>
/// Provides file persistence functionality for Schema objects.
/// </summary>
internal static class SchemaFile
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	/// <summary>
	/// Tries to load a Schema from a file path.
	/// </summary>
	/// <param name="filePath">The path to the schema file.</param>
	/// <param name="schema">The loaded schema, or null if loading failed.</param>
	/// <returns>True if the schema was loaded successfully, false otherwise.</returns>
	public static bool TryLoad(AbsoluteFilePath filePath, out Schema? schema)
	{
		schema = null;

		if (string.IsNullOrEmpty(filePath))
		{
			return false;
		}

		try
		{
			if (!File.Exists(filePath))
			{
				return false;
			}

			string json = File.ReadAllText(filePath);
			schema = JsonSerializer.Deserialize<Schema>(json, JsonOptions);

			if (schema is not null)
			{
				schema.Reassociate();
				return true;
			}
		}
		catch (JsonException)
		{
			// Invalid JSON
		}
		catch (IOException)
		{
			// File access error
		}

		return false;
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
			string json = JsonSerializer.Serialize(schema, JsonOptions);
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
