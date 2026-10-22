// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ktsu.Semantics.Paths;

/// <summary>
/// Provides JSON serialization and deserialization for Schema objects.
/// </summary>
public static class SchemaSerializer
{
	/// <summary>
	/// How a schema file is parsed before anything reads it.
	/// </summary>
	/// <remarks>
	/// A schema is edited by hand and kept beside the C++ generator's options file, which is read
	/// in this dialect too, so both accept comments and trailing commas. Every parse of a schema
	/// goes through these, because the format version and the root properties are read before the
	/// serializer ever sees the document. Comments are skipped rather than kept, so a schema saved
	/// after loading has none.
	/// </remarks>
	private static readonly JsonDocumentOptions DocumentOptions = new()
	{
		CommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
	};

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

		// A type's "TypeName" and a default's "DefaultKind" are ordinary properties to anyone
		// editing a file by hand, and JSON objects are unordered. Without this, a discriminator
		// that is not the first property threw NotSupportedException out of Load.
		AllowOutOfOrderMetadataProperties = true,
		Converters = { new ValidatingStringJsonConverterFactory() },
		TypeInfoResolver = new DefaultJsonTypeInfoResolver
		{
			Modifiers = { OmitDefaultVectorElement, OmitDefaultQuantityStorage, OmitUnboundDataSource },
		},
	};

	/// <summary>
	/// Leaves a vector's component type out of the file when it is the default.
	/// </summary>
	/// <remarks>
	/// <see cref="Types.Vector.ElementType"/> is not nullable, so no ignore condition describes
	/// "the same as saying nothing". Writing it regardless would put four lines of
	/// <c>{ "TypeName": "Float" }</c> under every vector in every file, and change every file
	/// written before vectors had a component type. The property reads either way, so what is
	/// omitted here still loads.
	/// </remarks>
	private static void OmitDefaultVectorElement(JsonTypeInfo typeInfo)
	{
		if (!typeof(Types.Vector).IsAssignableFrom(typeInfo.Type))
		{
			return;
		}

		IEnumerable<JsonPropertyInfo> componentProperties = typeInfo.Properties
			.Where(property => string.Equals(property.Name, "elementType", StringComparison.OrdinalIgnoreCase));

		foreach (JsonPropertyInfo property in componentProperties)
		{
			property.ShouldSerialize = static (_, value) => value is not Types.Float;
		}
	}

	/// <summary>
	/// Leaves a quantity's storage out of the file when it is the default.
	/// </summary>
	/// <remarks>
	/// The same arrangement as <see cref="OmitDefaultVectorElement"/> and for the same reason:
	/// <see cref="Types.Quantity.Storage"/> is not nullable, so no ignore condition describes
	/// "the same as saying nothing", and writing it regardless would put four lines of
	/// <c>{ "TypeName": "Float" }</c> under every quantity in every file.
	/// </remarks>
	private static void OmitDefaultQuantityStorage(JsonTypeInfo typeInfo)
	{
		if (typeInfo.Type != typeof(Types.Quantity))
		{
			return;
		}

		IEnumerable<JsonPropertyInfo> storageProperties = typeInfo.Properties
			.Where(property => string.Equals(property.Name, "storage", StringComparison.OrdinalIgnoreCase));

		foreach (JsonPropertyInfo property in storageProperties)
		{
			property.ShouldSerialize = static (_, value) => value is not Types.Float;
		}
	}

	/// <summary>
	/// Leaves a code generator's data source out of the file when none is bound.
	/// </summary>
	/// <remarks>
	/// The name is an empty string rather than null when unset, so no ignore condition omits it.
	/// Writing it regardless would add an empty <c>"dataSource"</c> to every code generator in
	/// every file, including every file written before generators could bind one.
	/// </remarks>
	private static void OmitUnboundDataSource(JsonTypeInfo typeInfo)
	{
		if (typeInfo.Type != typeof(SchemaCodeGenerator))
		{
			return;
		}

		IEnumerable<JsonPropertyInfo> dataSourceProperties = typeInfo.Properties
			.Where(property => string.Equals(property.Name, "dataSource", StringComparison.OrdinalIgnoreCase));

		foreach (JsonPropertyInfo property in dataSourceProperties)
		{
			property.ShouldSerialize = static (_, value) => !string.IsNullOrEmpty(value?.ToString());
		}
	}

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
	/// <remarks>
	/// Reports only whether the load worked. Use <see cref="Load(string)"/> when the caller needs to tell
	/// a corrupt file from one written by a newer version of the library, which need different
	/// things said to the user.
	/// </remarks>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="schema">The deserialized schema, or null if deserialization failed.</param>
	/// <returns>True if deserialization succeeded, false otherwise.</returns>
	public static bool TryDeserialize(string json, out Schema? schema)
	{
		SchemaLoadResult result = Load(json);
		schema = result.Schema;
		return result.IsSuccess;
	}

	/// <summary>
	/// Loads a schema from JSON, migrating an older format version and reporting why a load
	/// failed when it does.
	/// </summary>
	/// <remarks>
	/// A file with no <c>formatVersion</c> predates versioning and is migrated as version
	/// <see cref="Schema.PreVersioningFormatVersion"/>. A file declaring a version newer than
	/// <see cref="Schema.CurrentFormatVersion"/> is refused with
	/// <see cref="SchemaLoadStatus.UnsupportedFutureVersion"/> rather than being read on a guess:
	/// a newer writer may have changed the meaning of what is there, so a partial read would
	/// silently drop or misinterpret data. See <c>docs/schema-format.md</c>.
	/// </remarks>
	/// <param name="json">The JSON to load.</param>
	/// <returns>The outcome, including the schema when the load succeeded.</returns>
	/// <param name="sourceFilePath">The path the JSON was read from, used to anchor relative paths.</param>
	public static SchemaLoadResult Load(string json, AbsoluteFilePath sourceFilePath)
	{
		SchemaLoadResult result = Load(json);
		result.Schema?.SetSourceFile(sourceFilePath);
		return result;
	}

	/// <summary>
	/// Loads a schema from JSON, migrating an older format version and reporting why a load
	/// failed when it does.
	/// </summary>
	/// <remarks>
	/// The schema has no anchor for its relative paths. Use
	/// <see cref="Load(string, AbsoluteFilePath)"/> when the file's location is known.
	/// </remarks>
	/// <param name="json">The JSON to load.</param>
	/// <returns>The outcome, including the schema when the load succeeded.</returns>
	public static SchemaLoadResult Load(string json)
	{
		if (string.IsNullOrEmpty(json))
		{
			return new()
			{
				Status = SchemaLoadStatus.InvalidJson,
				Message = "The schema is empty.",
			};
		}

		int declaredVersion;
		try
		{
			declaredVersion = ReadFormatVersion(json);
		}
		catch (JsonException ex)
		{
			return new()
			{
				Status = SchemaLoadStatus.InvalidJson,
				Message = $"The schema is not valid JSON: {ex.Message}",
			};
		}

		if (declaredVersion > Schema.CurrentFormatVersion)
		{
			return new()
			{
				Status = SchemaLoadStatus.UnsupportedFutureVersion,
				FileFormatVersion = declaredVersion,
				Message =
					$"This schema is format version {declaredVersion}, but this build understands " +
					$"up to version {Schema.CurrentFormatVersion}. Update to a newer version of the " +
					"library to open it.",
			};
		}

		string? unrecognised = FindUnrecognisedRootProperty(json);
		if (unrecognised is not null)
		{
			return new()
			{
				Status = SchemaLoadStatus.InvalidJson,
				FileFormatVersion = declaredVersion,
				Message = DescribeUnrecognisedRootProperty(unrecognised),
			};
		}

		JsonNode? document = JsonNode.Parse(json, documentOptions: DocumentOptions);
		string? nullElement = RemoveNulls(document, "$");
		if (nullElement is not null)
		{
			return new()
			{
				Status = SchemaLoadStatus.InvalidJson,
				FileFormatVersion = declaredVersion,
				Message = $"The schema has a null at {nullElement}, where a list holds only elements.",
			};
		}

		try
		{
			Schema? schema = document.Deserialize<Schema>(JsonOptions);
			if (schema is null)
			{
				return new()
				{
					Status = SchemaLoadStatus.InvalidJson,
					FileFormatVersion = declaredVersion,
					Message = "The schema is not valid JSON.",
				};
			}

			schema.Reassociate();
			Migrate(schema, declaredVersion);

			return new()
			{
				Status = SchemaLoadStatus.Success,
				Schema = schema,
				FileFormatVersion = declaredVersion,
				Message = declaredVersion < Schema.CurrentFormatVersion
					? $"Migrated from format version {declaredVersion} to {Schema.CurrentFormatVersion}."
					: "Loaded.",
			};
		}
		catch (JsonException ex) when (ex.InnerException is ArgumentException)
		{
			// Well-formed JSON holding a value its type refuses, such as an absolute output path.
			return new()
			{
				Status = SchemaLoadStatus.InvalidJson,
				FileFormatVersion = declaredVersion,
				Message = $"The schema has a value at {ex.Path} it cannot hold: {ex.Message}",
			};
		}
		catch (JsonException ex)
		{
			return new()
			{
				Status = SchemaLoadStatus.InvalidJson,
				FileFormatVersion = declaredVersion,
				Message = $"The schema is not valid JSON: {ex.Message}",
			};
		}
		catch (NotSupportedException ex)
		{
			// The serializer reports a shape it cannot construct - a polymorphic object with no
			// discriminator, or one it does not recognise - as NotSupportedException rather than
			// JsonException. The file is no less unreadable for that, and Load promises a result.
			return new()
			{
				Status = SchemaLoadStatus.InvalidJson,
				FileFormatVersion = declaredVersion,
				Message = $"The schema could not be read: {ex.Message}",
			};
		}
	}

	/// <summary>
	/// Reads every null property in a document as an omitted one, and finds the first null element.
	/// </summary>
	/// <remarks>
	/// The serializer never writes a null, so a null in a file was written by hand. Read as given, a
	/// null collection, name or type reached the model as a null its non-nullable properties never
	/// expect, and <see cref="Schema.Reassociate"/> or <see cref="Schema.Validate"/> then threw
	/// rather than reporting anything. Omitting a property already means "the default": an empty
	/// collection, an empty name the validator reports, an unfinished type. A null element in a list
	/// has no default to stand for, so it is refused by its path.
	/// </remarks>
	/// <param name="node">The node to clean, which is modified in place.</param>
	/// <param name="path">Where <paramref name="node"/> sits in the document.</param>
	/// <returns>The path of the first null element, or <see langword="null"/> when there is none.</returns>
	private static string? RemoveNulls(JsonNode? node, string path)
	{
		switch (node)
		{
			case JsonObject jsonObject:
				foreach (string name in jsonObject.Where(property => property.Value is null).Select(property => property.Key).ToList())
				{
					jsonObject.Remove(name);
				}

				foreach (KeyValuePair<string, JsonNode?> property in jsonObject)
				{
					string? found = RemoveNulls(property.Value, $"{path}.{property.Key}");
					if (found is not null)
					{
						return found;
					}
				}

				return null;

			case JsonArray jsonArray:
				for (int index = 0; index < jsonArray.Count; index++)
				{
					string elementPath = $"{path}[{index}]";
					string? found = jsonArray[index] is null ? elementPath : RemoveNulls(jsonArray[index], elementPath);
					if (found is not null)
					{
						return found;
					}
				}

				return null;

			default:
				return null;
		}
	}

	/// <summary>
	/// The names a schema document's root may hold, as the serializer itself spells them.
	/// </summary>
	/// <remarks>
	/// Read from the serializer's own contract rather than listed here, so a property added to
	/// <see cref="Schema"/> is accepted the moment it can be read.
	/// </remarks>
	private static readonly Lazy<HashSet<string>> RootPropertyNames = new(() =>
		[.. JsonOptions.GetTypeInfo(typeof(Schema)).Properties.Select(property => property.Name)]);

	/// <summary>
	/// Finds the first property at the document root that the model does not read, if there is one.
	/// </summary>
	/// <remarks>
	/// The serializer skips a property it does not recognise, so a document written with
	/// <c>Classes</c> rather than <c>classes</c> used to load as an empty schema reported as a
	/// successful migration, and saving it from the editor then replaced the file with nothing.
	/// Refusing the document is what keeps a file this build cannot read from being taken for an
	/// empty one. A file from a newer version is refused before this runs, with its own message,
	/// so an unrecognised property here is never one a newer writer added.
	/// </remarks>
	private static string? FindUnrecognisedRootProperty(string json)
	{
		using JsonDocument document = JsonDocument.Parse(json, DocumentOptions);
		HashSet<string> known = RootPropertyNames.Value;

		return document.RootElement.EnumerateObject()
			.Select(property => property.Name)
			.FirstOrDefault(name => !known.Contains(name));
	}

	private static string DescribeUnrecognisedRootProperty(string name)
	{
		string? differentlyCased = RootPropertyNames.Value
			.FirstOrDefault(known => string.Equals(known, name, StringComparison.OrdinalIgnoreCase));

		return differentlyCased is null
			? $"The schema has a property '{name}' that this build does not recognise, so it cannot be read without losing data."
			: $"The schema has a property '{name}' that this build does not recognise: property names are camelCase, so it would be '{differentlyCased}'.";
	}

	/// <summary>
	/// Reads just the format version from the document root.
	/// </summary>
	/// <remarks>
	/// Read separately, and before deserializing, so a file from a newer version is refused with
	/// an accurate message instead of failing somewhere in the middle of a document whose shape
	/// this build does not know.
	/// </remarks>
	private static int ReadFormatVersion(string json)
	{
		using JsonDocument document = JsonDocument.Parse(json, DocumentOptions);

		if (document.RootElement.ValueKind != JsonValueKind.Object)
		{
			throw new JsonException("The root of a schema must be a JSON object.");
		}

		if (!document.RootElement.TryGetProperty("formatVersion", out JsonElement versionElement))
		{
			return Schema.PreVersioningFormatVersion;
		}

		return versionElement.ValueKind == JsonValueKind.Number && versionElement.TryGetInt32(out int version)
			? version
			: throw new JsonException("'formatVersion' must be an integer.");
	}

	/// <summary>
	/// Brings a schema loaded from an older format up to the current one.
	/// </summary>
	/// <remarks>
	/// Migrations are cumulative: each step upgrades one version, so a very old file is carried
	/// forward through every step in turn.
	///
	/// 0 to 1: a member's description moved from "memberDescription" to the "description" it
	/// shares with every other element. <see cref="SchemaMember"/> reads the old property into
	/// the new one as it deserializes, so nothing further is needed here; the step exists so the
	/// version is stamped and the path is documented.
	///
	/// Every step since has been additive - a property or a collection that an older file simply
	/// does not have, which loads as absent or as its default. The version moves anyway, because
	/// an older reader would silently drop what it does not recognise; see
	/// <c>docs/schema-format.md</c>. Nothing further is needed here for those steps either.
	/// </remarks>
	/// <param name="schema">The schema to migrate in place.</param>
	/// <param name="fromVersion">The version the file declared.</param>
	private static void Migrate(Schema schema, int fromVersion)
	{
		if (fromVersion >= Schema.CurrentFormatVersion)
		{
			return;
		}

		schema.FormatVersion = Schema.CurrentFormatVersion;
	}
}
