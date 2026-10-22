// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;

using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Paths;

/// <summary>
/// Checks that a data file conforms to the schema class a <see cref="DataSource"/> binds it to.
/// </summary>
/// <remarks>
/// <para>
/// A data file's root may be either a single object of the bound class, or an array of them. Both
/// are validated by the same rules; which one a given file uses is its own business.
/// </para>
/// <para>
/// The schema has no notion of an optional member, so every member of a class is required: a
/// missing one is an error. A property in the data that no member accounts for is a warning
/// rather than an error, because carrying extra data is not by itself a contradiction of the
/// schema.
/// </para>
/// <para>
/// Every failure is reported with the path to the offending value, so a file with several
/// problems yields a diagnostic per problem rather than stopping at the first.
/// </para>
/// </remarks>
public static class SchemaDataValidator
{
	/// <summary>
	/// Validates every data source in a schema against the class it is bound to.
	/// </summary>
	/// <remarks>
	/// Reads each data source's file, so it needs the schema to know where it was loaded from.
	/// A schema with no anchor yields a single warning rather than a diagnostic per data source.
	/// </remarks>
	/// <param name="schema">The schema whose data sources should be checked.</param>
	/// <returns>The issues found; empty if every bound data file conforms.</returns>
	public static Collection<SchemaValidationIssue> ValidateDataSources(Schema schema)
	{
		Ensure.NotNull(schema);

		Collection<SchemaValidationIssue> issues = [];

		if (!schema.CanResolvePaths)
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Warning,
				Path = string.Empty,
				Message = "Data sources cannot be checked because the schema's own location is unknown. " +
					"Load it with SchemaSerializer.Load(json, path), or call SetSourceFile.",
			});

			return issues;
		}

		foreach (DataSource dataSource in schema.DataSources)
		{
			ValidateDataSource(dataSource, issues);
		}

		return issues;
	}

	private static void ValidateDataSource(DataSource dataSource, Collection<SchemaValidationIssue> issues)
	{
		if (string.IsNullOrEmpty(dataSource.File) || string.IsNullOrEmpty(dataSource.ClassName))
		{
			// Already reported as unconfigured by Schema.Validate.
			return;
		}

		if (dataSource.Class is not SchemaClass boundClass)
		{
			// The dangling class reference is already reported by Schema.Validate.
			return;
		}

		if (!dataSource.TryResolveFile(out AbsoluteFilePath resolved))
		{
			return;
		}

		string json;
		try
		{
			if (!File.Exists(resolved))
			{
				issues.Add(FileIssue(dataSource, $"Data file '{dataSource.File}' does not exist (resolved to '{resolved}')."));
				return;
			}

			json = File.ReadAllText(resolved);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			issues.Add(FileIssue(dataSource, $"Data file '{dataSource.File}' could not be read: {ex.Message}"));
			return;
		}

		foreach (SchemaValidationIssue issue in ValidateData(boundClass, json, dataSource.Name))
		{
			issues.Add(issue);
		}
	}

	private static SchemaValidationIssue FileIssue(DataSource dataSource, string message) => new()
	{
		Severity = SchemaValidationSeverity.Error,
		Path = dataSource.Name,
		Message = message,
		Element = dataSource,
	};

	/// <summary>
	/// Validates JSON data against a schema class.
	/// </summary>
	/// <remarks>
	/// Takes the data as text rather than a path, so this is usable without a filesystem.
	/// </remarks>
	/// <param name="schemaClass">The class the data should conform to.</param>
	/// <param name="json">The data to check.</param>
	/// <param name="path">A prefix for the reported paths, identifying what is being checked.</param>
	/// <returns>The issues found; empty if the data conforms.</returns>
	public static Collection<SchemaValidationIssue> ValidateData(SchemaClass schemaClass, string json, string path)
	{
		Ensure.NotNull(schemaClass);

		Collection<SchemaValidationIssue> issues = [];

		JsonDocument document;
		try
		{
			document = JsonDocument.Parse(json);
		}
		catch (JsonException ex)
		{
			issues.Add(Error(path, $"Data is not valid JSON: {ex.Message}"));
			return issues;
		}

		using (document)
		{
			JsonElement root = document.RootElement;

			if (root.ValueKind == JsonValueKind.Array)
			{
				int index = 0;
				foreach (JsonElement item in root.EnumerateArray())
				{
					ValidateObject(schemaClass, item, $"{path}[{index}]", issues);
					index++;
				}
			}
			else
			{
				ValidateObject(schemaClass, root, path, issues);
			}
		}

		return issues;
	}

	private static void ValidateObject(SchemaClass schemaClass, JsonElement element, string path, Collection<SchemaValidationIssue> issues)
	{
		if (element.ValueKind != JsonValueKind.Object)
		{
			issues.Add(Error(path, $"Expected an object for class '{schemaClass.Name}' but found {Describe(element)}."));
			return;
		}

		// The names of the properties a member matched, in the casing the data spelled them. A member
		// is found case-insensitively, so recording the member's own name instead would report every
		// property of camelCase data as unknown straight after validating it.
		HashSet<string> accountedFor = [];

		foreach (SchemaMember member in schemaClass.Members)
		{
			if (!TryGetMemberProperty(element, member.Name, out string matchedName, out JsonElement value))
			{
				issues.Add(Error($"{path}.{member.Name}", $"Required member '{member.Name}' of class '{schemaClass.Name}' is missing."));
				continue;
			}

			accountedFor.Add(matchedName);
			ValidateValue(member.Type, value, $"{path}.{member.Name}", issues);
		}

		foreach (string propertyName in element.EnumerateObject()
			.Select(candidate => candidate.Name)
			.Where(name => !accountedFor.Contains(name)))
		{
			issues.Add(Warning($"{path}.{propertyName}", $"Class '{schemaClass.Name}' has no member '{propertyName}'."));
		}
	}

	/// <summary>
	/// Looks a member up in the data, accepting the casing the serializer would have written, and
	/// reports the name of the property it matched.
	/// </summary>
	/// <remarks>
	/// The loop stops at the first match, which is the whole point of a lookup. Enumerating a
	/// JsonElement's properties yields a struct enumerator, so taking the first match without a
	/// loop would mean materialising the matches into a collection - an allocation on a path that
	/// runs once per member per object in a data file.
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1751:Loops with at most one iteration should be refactored", Justification = "A first-match lookup terminates on the first iteration by design; see the remarks.")]
	private static bool TryGetMemberProperty(JsonElement element, string memberName, out string matchedName, out JsonElement value)
	{
		if (element.TryGetProperty(memberName, out value))
		{
			matchedName = memberName;
			return true;
		}

		foreach (JsonProperty property in element.EnumerateObject()
			.Where(candidate => string.Equals(candidate.Name, memberName, StringComparison.OrdinalIgnoreCase)))
		{
			matchedName = property.Name;
			value = property.Value;
			return true;
		}

		matchedName = memberName;
		return false;
	}

	private static void ValidateValue(BaseType type, JsonElement value, string path, Collection<SchemaValidationIssue> issues)
	{
		switch (type)
		{
			case None:
				issues.Add(Warning(path, "Member has no type set, so its value cannot be checked."));
				break;

			case Int or Long:
				ExpectIntegral(value, path, type, issues);
				break;

			case Float or Double:
				Expect(value, JsonValueKind.Number, path, type, issues);
				break;

			case Types.String:
				Expect(value, JsonValueKind.String, path, type, issues);
				break;

			case Bool:
				ExpectBoolean(value, path, type, issues);
				break;

			case Types.DateTime:
				ExpectParsable(value, path, type, issues, text => System.DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _));
				break;

			case Types.TimeSpan:
				ExpectParsable(value, path, type, issues, text => System.TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out _));
				break;

			// ColorRGB derives from Vector3 and ColorRGBA from Vector4, so the colors have to be
			// matched first or the compiler rightly calls their cases unreachable.
			case ColorRGB:
				ExpectNumericTuple(value, 3, new Float(), path, type, issues);
				break;

			case ColorRGBA:
				ExpectNumericTuple(value, 4, new Float(), path, type, issues);
				break;

			case Vector2 vector:
				ExpectNumericTuple(value, 2, vector.ElementType, path, type, issues);
				break;

			case Vector3 vector:
				ExpectNumericTuple(value, 3, vector.ElementType, path, type, issues);
				break;

			case Vector4 vector:
				ExpectNumericTuple(value, 4, vector.ElementType, path, type, issues);
				break;

			case Types.Enum enumType:
				ValidateEnumValue(enumType, value, path, issues);
				break;

			case Types.Object objectType:
				ValidateObjectReference(objectType, value, path, issues);
				break;

			case Types.Array arrayType:
				ValidateArray(arrayType, value, path, issues);
				break;

			case Semantic semantic:
				ValidateSemanticValue(semantic, value, path, issues);
				break;

			case Quantity quantity:
				ValidateQuantityValue(quantity, value, path, issues);
				break;

			// An absent value is spelled null; anything else is the value it wraps.
			case Optional optional:
				if (value.ValueKind != JsonValueKind.Null)
				{
					ValidateValue(optional.ElementType, value, path, issues);
				}

				break;

			// A handle names an entry in a store the data file cannot see, an interface is
			// behaviour rather than data, and a result or a span exists only in a signature. None
			// of them has a stored form to compare a value against, so say so rather than pass it.
			case Handle or Result or Span or Interface or Void:
				issues.Add(Warning(path, $"Values of type '{type.DisplayName}' cannot be checked."));
				break;

			default:
				break;
		}
	}

	/// <summary>
	/// Validates a value of a semantic type as the representation it is stored in.
	/// </summary>
	/// <remarks>
	/// A semantic type whose name does not resolve, or whose refinements form a cycle, is already
	/// reported by schema validation. There is nothing real to judge the value against, so it is
	/// left alone rather than reported twice.
	/// </remarks>
	private static void ValidateSemanticValue(Semantic semantic, JsonElement value, string path, Collection<SchemaValidationIssue> issues)
	{
		if (semantic.Declaration?.Representation() is BaseType representation and not Semantic)
		{
			ValidateValue(representation, value, path, issues);
		}
	}

	/// <summary>
	/// Validates a value of a physical quantity: one number for a magnitude or a signed scalar, and
	/// an array of numbers for a vector form, each judged by the quantity's storage.
	/// </summary>
	/// <remarks>
	/// A quantity name that does not resolve is already reported by schema validation, which is
	/// also the only thing that knows how many components it should have.
	/// </remarks>
	private static void ValidateQuantityValue(Quantity quantity, JsonElement value, string path, Collection<SchemaValidationIssue> issues)
	{
		if (!QuantityRegistry.TryResolve(quantity.QuantityName, out QuantityRegistry.QuantityInfo? info) || info is null)
		{
			return;
		}

		if (info.Components >= 2)
		{
			ExpectNumericTuple(value, info.Components, quantity.Storage, path, quantity, issues);
			return;
		}

		ExpectNumber(value, quantity.Storage, path, quantity, issues);
	}

	/// <summary>
	/// Expects one number, held to whole numbers when the type it is stored in is integral.
	/// </summary>
	private static void ExpectNumber(JsonElement value, BaseType storage, string path, BaseType type, Collection<SchemaValidationIssue> issues)
	{
		BaseType represented = storage is Semantic { Declaration: SchemaSemanticType declaration }
			? declaration.Representation()
			: storage;

		if (represented is Int or Long)
		{
			ExpectIntegralAs(value, represented, path, type, issues);
			return;
		}

		Expect(value, JsonValueKind.Number, path, type, issues);
	}

	private static void ValidateEnumValue(Types.Enum enumType, JsonElement value, string path, Collection<SchemaValidationIssue> issues)
	{
		if (value.ValueKind != JsonValueKind.String)
		{
			issues.Add(Error(path, $"Expected a string naming a value of enum '{enumType.EnumName}' but found {Describe(value)}."));
			return;
		}

		SchemaEnum? schemaEnum = enumType.ParentMember?.ParentSchema?.GetEnum(enumType.EnumName);
		if (schemaEnum is null)
		{
			// The dangling enum reference is already reported by Schema.Validate.
			return;
		}

		string text = value.GetString() ?? string.Empty;
		if (!schemaEnum.Values.Any(v => string.Equals(v, text, StringComparison.Ordinal)))
		{
			issues.Add(Error(path, $"'{text}' is not a value of enum '{enumType.EnumName}'. Expected one of: {string.Join(", ", schemaEnum.Values)}."));
		}
	}

	private static void ValidateObjectReference(Types.Object objectType, JsonElement value, string path, Collection<SchemaValidationIssue> issues)
	{
		SchemaClass? referenced = objectType.Class;
		if (referenced is null)
		{
			// The dangling class reference is already reported by Schema.Validate.
			return;
		}

		ValidateObject(referenced, value, path, issues);
	}

	private static void ValidateArray(Types.Array arrayType, JsonElement value, string path, Collection<SchemaValidationIssue> issues)
	{
		bool isMap = string.Equals(arrayType.Container, Types.Array.MapContainer, StringComparison.Ordinal);

		if (isMap)
		{
			ValidateMap(arrayType, value, path, issues);
			return;
		}

		// Every other container, the known 'vector' included, is a sequence.
		if (value.ValueKind != JsonValueKind.Array)
		{
			issues.Add(Error(path, $"Expected an array for container '{arrayType.Container}' but found {Describe(value)}."));
			return;
		}

		int index = 0;
		foreach (JsonElement item in value.EnumerateArray())
		{
			ValidateValue(arrayType.ElementType, item, $"{path}[{index}]", issues);
			index++;
		}
	}

	private static void ValidateMap(Types.Array arrayType, JsonElement value, string path, Collection<SchemaValidationIssue> issues)
	{
		if (value.ValueKind != JsonValueKind.Object)
		{
			issues.Add(Error(path, $"Expected an object keyed by '{arrayType.Key}' for container '{Types.Array.MapContainer}' but found {Describe(value)}."));
			return;
		}

		foreach (JsonProperty entry in value.EnumerateObject())
		{
			string entryPath = $"{path}['{entry.Name}']";
			ValidateValue(arrayType.ElementType, entry.Value, entryPath, issues);
			ValidateMapKeyMatches(arrayType, entry, entryPath, issues);
		}
	}

	/// <summary>
	/// Checks that a map entry's key agrees with the key member of the entry itself, so the two
	/// cannot disagree about which record this is.
	/// </summary>
	private static void ValidateMapKeyMatches(Types.Array arrayType, JsonProperty entry, string entryPath, Collection<SchemaValidationIssue> issues)
	{
		if (string.IsNullOrEmpty(arrayType.Key) ||
			entry.Value.ValueKind != JsonValueKind.Object ||
			!TryGetMemberProperty(entry.Value, arrayType.Key, out _, out JsonElement keyValue))
		{
			return;
		}

		string keyText = keyValue.ValueKind == JsonValueKind.String
			? keyValue.GetString() ?? string.Empty
			: keyValue.GetRawText();

		if (!string.Equals(keyText, entry.Name, StringComparison.Ordinal))
		{
			issues.Add(Error(entryPath, $"Map key '{entry.Name}' does not match the entry's '{arrayType.Key}' value '{keyText}'."));
		}
	}

	private static void Expect(JsonElement value, JsonValueKind expected, string path, BaseType type, Collection<SchemaValidationIssue> issues)
	{
		if (value.ValueKind != expected)
		{
			issues.Add(Error(path, $"Expected {expected.ToString().ToLowerInvariant()} for type '{type.DisplayName}' but found {Describe(value)}."));
		}
	}

	private static void ExpectBoolean(JsonElement value, string path, BaseType type, Collection<SchemaValidationIssue> issues)
	{
		if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
		{
			issues.Add(Error(path, $"Expected a boolean for type '{type.DisplayName}' but found {Describe(value)}."));
		}
	}

	private static void ExpectIntegral(JsonElement value, string path, BaseType type, Collection<SchemaValidationIssue> issues) =>
		ExpectIntegralAs(value, type, path, type, issues);

	/// <summary>
	/// Expects a whole number in the range of <paramref name="storage"/>, reported under the name
	/// of <paramref name="type"/> - which is the storage itself for a bare integer and the vector,
	/// quantity or semantic type holding it otherwise.
	/// </summary>
	private static void ExpectIntegralAs(JsonElement value, BaseType storage, string path, BaseType type, Collection<SchemaValidationIssue> issues)
	{
		if (value.ValueKind != JsonValueKind.Number)
		{
			issues.Add(Error(path, $"Expected a number for type '{type.DisplayName}' but found {Describe(value)}."));
			return;
		}

		if (storage is Int)
		{
			if (value.TryGetInt32(out _))
			{
				return;
			}

			if (value.TryGetInt64(out long integral))
			{
				issues.Add(Error(path, $"Expected a 32-bit whole number for type '{type.DisplayName}' but found {integral} (valid range: {int.MinValue}..{int.MaxValue})."));
				return;
			}
		}
		else if (value.TryGetInt64(out _))
		{
			return;
		}

		if (storage is Long)
		{
			issues.Add(Error(path, $"Expected a whole number for type '{type.DisplayName}' but found {value.GetRawText()}."));
			return;
		}

		issues.Add(Error(path, $"Expected a 32-bit whole number for type '{type.DisplayName}' but found {value.GetRawText()}."));
	}

	private static void ExpectParsable(JsonElement value, string path, BaseType type, Collection<SchemaValidationIssue> issues, Func<string, bool> canParse)
	{
		if (value.ValueKind != JsonValueKind.String)
		{
			issues.Add(Error(path, $"Expected a string for type '{type.DisplayName}' but found {Describe(value)}."));
			return;
		}

		if (!canParse(value.GetString() ?? string.Empty))
		{
			issues.Add(Error(path, $"'{value.GetString()}' is not a valid '{type.DisplayName}'."));
		}
	}

	private static void ExpectNumericTuple(JsonElement value, int length, BaseType component, string path, BaseType type, Collection<SchemaValidationIssue> issues)
	{
		if (value.ValueKind != JsonValueKind.Array)
		{
			issues.Add(Error(path, $"Expected an array of {length} numbers for type '{type.DisplayName}' but found {Describe(value)}."));
			return;
		}

		int count = 0;
		foreach (JsonElement element in value.EnumerateArray())
		{
			ExpectNumber(element, component, $"{path}[{count}]", type, issues);
			count++;
		}

		if (count != length)
		{
			issues.Add(Error(path, $"Expected {length} numbers for type '{type.DisplayName}' but found {count}."));
		}
	}

	private static string Describe(JsonElement element) => element.ValueKind switch
	{
		JsonValueKind.Object => "an object",
		JsonValueKind.Array => "an array",
		JsonValueKind.String => "a string",
		JsonValueKind.Number => "a number",
		JsonValueKind.True or JsonValueKind.False => "a boolean",
		JsonValueKind.Null => "null",
		_ => "nothing",
	};

	private static SchemaValidationIssue Error(string path, string message) => new()
	{
		Severity = SchemaValidationSeverity.Error,
		Path = path,
		Message = message,
	};

	private static SchemaValidationIssue Warning(string path, string message) => new()
	{
		Severity = SchemaValidationSeverity.Warning,
		Path = path,
		Message = message,
	};
}
