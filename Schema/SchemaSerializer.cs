// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Models;

using System.Text.Json;
using System.Text.Json.Serialization;
using ktsu.RoundTripStringJsonConverter;

/// <summary>
/// Provides JSON serialization and deserialization for Schema objects.
/// </summary>
public static class SchemaSerializer
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters = { new RoundTripStringJsonConverterFactory() },
	};

	/// <summary>
	/// Serializes a Schema to a JSON string.
	/// </summary>
	/// <param name="schema">The schema to serialize.</param>
	/// <returns>The JSON string representation.</returns>
	public static string Serialize(Schema schema)
	{
		Ensure.NotNull(schema);
		return JsonSerializer.Serialize(schema, JsonOptions);
	}

	/// <summary>
	/// Tries to deserialize a JSON string to a Schema.
	/// Automatically calls Reassociate() on success.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="schema">The deserialized schema, or null if deserialization failed.</param>
	/// <returns>True if deserialization succeeded, false otherwise.</returns>
	public static bool TryDeserialize(string json, out Schema? schema)
	{
		schema = null;

		if (string.IsNullOrEmpty(json))
		{
			return false;
		}

		try
		{
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

		return false;
	}
}
