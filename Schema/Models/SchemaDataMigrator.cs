// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Types;

/// <summary>
/// Moves data bound to one version of a schema onto the next, across a <see cref="SchemaDiff"/>.
/// </summary>
/// <remarks>
/// <para>
/// A renamed class or member carries its values to the new name, and a renamed enum value is
/// rewritten wherever it is held. A removed member's values are dropped and an added member is
/// given its declared default, or its type's when it declares none. A retyped member keeps its
/// value when that value is already one of the new type, is converted when the conversion loses
/// nothing (a number written as text, a whole number into an integral type), and is otherwise
/// replaced by the default. Every value dropped or replaced is reported, so nothing is lost silently.
/// </para>
/// <para>
/// Whether a value can stand is decided by <see cref="SchemaDataValidator"/>, not by rules restated
/// here, which is what makes migrated data validate clean against the current schema. A property
/// the previous schema did not model either is carried across untouched, as validation would have
/// warned about it before the migration and will after.
/// </para>
/// </remarks>
public static class SchemaDataMigrator
{
	private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

	/// <summary>
	/// Migrates data bound to a class of <see cref="SchemaDiff.Previous"/> to its counterpart in
	/// <see cref="SchemaDiff.Current"/>.
	/// </summary>
	/// <param name="diff">The differences between the two versions.</param>
	/// <param name="previousClass">The class of the previous version the data is bound to.</param>
	/// <param name="json">The data: one object of the class, or an array of them.</param>
	/// <param name="path">A prefix for the reported paths, identifying what is being migrated.</param>
	/// <returns>The migrated data and anything the migration gave up.</returns>
	public static SchemaMigrationResult Migrate(SchemaDiff diff, SchemaClass previousClass, string json, string path)
	{
		Ensure.NotNull(diff);
		Ensure.NotNull(previousClass);
		Ensure.NotNull(json);

		Collection<SchemaValidationIssue> issues = [];

		if (diff.CurrentClassFor(previousClass) is null)
		{
			issues.Add(Error(path, $"Class '{previousClass.Name}' was removed, so data bound to it has nothing to migrate to."));
			return new() { Json = json, Issues = issues };
		}

		JsonNode? root;
		try
		{
			root = JsonNode.Parse(json);
		}
		catch (JsonException ex)
		{
			issues.Add(Error(path, $"Data is not valid JSON: {ex.Message}"));
			return new() { Json = json, Issues = issues };
		}

		Migration migration = new(diff, issues);
		JsonNode? migrated;

		switch (root)
		{
			case JsonArray items:
				JsonArray array = [];
				for (int index = 0; index < items.Count; index++)
				{
					array.Add(migration.MigrateRecord(previousClass, items[index], $"{path}[{index}]"));
				}

				migrated = array;
				break;

			case JsonObject:
				migrated = migration.MigrateRecord(previousClass, root, path);
				break;

			default:
				issues.Add(Error(path, $"Expected an object of class '{previousClass.Name}' or an array of them."));
				return new() { Json = json, Issues = issues };
		}

		return new() { Json = migrated?.ToJsonString(Indented) ?? "null", Issues = issues };
	}

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

	/// <summary>
	/// One migration's state: the diff it follows and where it reports.
	/// </summary>
	private sealed class Migration(SchemaDiff diff, Collection<SchemaValidationIssue> issues)
	{
		public JsonObject? MigrateRecord(SchemaClass previousClass, JsonNode? record, string path)
		{
			SchemaClass currentClass = diff.CurrentClassFor(previousClass)!;

			if (record is not JsonObject source)
			{
				issues.Add(Warning(path, $"Expected an object of class '{previousClass.Name}'; replaced with the default."));
				return DefaultObject(currentClass, []);
			}

			JsonObject target = [];
			List<KeyValuePair<string, JsonNode?>> unmodelled = [];
			HashSet<string> filled = new(StringComparer.Ordinal);

			foreach (KeyValuePair<string, JsonNode?> property in source.ToList())
			{
				SchemaMember? previousMember = previousClass.Members.FirstOrDefault(member =>
					string.Equals(member.Name, property.Key, StringComparison.OrdinalIgnoreCase));

				if (previousMember is null)
				{
					unmodelled.Add(property);
					continue;
				}

				string propertyPath = $"{path}.{property.Key}";
				SchemaMember? currentMember = diff.CurrentMemberFor(previousClass, previousMember.Name);
				if (currentMember is null)
				{
					issues.Add(Warning(propertyPath, $"Member '{previousMember.Name}' was removed; its value was dropped."));
					continue;
				}

				source.Remove(property.Key);
				string name = SpellLike(property.Key, previousMember.Name, currentMember.Name);
				target[name] = MigrateValue(previousMember.Type, currentMember.Type, currentMember, property.Value, propertyPath);
				filled.Add(currentMember.Name);
			}

			foreach (SchemaMember member in currentClass.Members.Where(member => !filled.Contains(member.Name)))
			{
				target[member.Name] = DefaultFor(member.Type, member, []);
				issues.Add(Warning($"{path}.{member.Name}", $"Member '{member.Name}' was added; given its default."));
			}

			foreach (KeyValuePair<string, JsonNode?> property in unmodelled)
			{
				if (target.Any(existing => string.Equals(existing.Key, property.Key, StringComparison.OrdinalIgnoreCase)))
				{
					issues.Add(Warning($"{path}.{property.Key}", $"Property '{property.Key}' is now the name of a member; its previous value was dropped."));
					continue;
				}

				source.Remove(property.Key);
				target[property.Key] = property.Value;
			}

			return target;
		}

		private JsonNode? MigrateValue(BaseType previousType, BaseType currentType, SchemaMember? currentMember, JsonNode? value, string path)
		{
			BaseType previous = SchemaDiff.Represented(previousType);
			BaseType current = SchemaDiff.Represented(currentType);

			switch (previous, current)
			{
				case (Optional p, Optional c):
					return value is null ? null : MigrateValue(p.ElementType, c.ElementType, null, value, path);

				case (_, Optional c):
					return value is null ? null : MigrateValue(previous, c.ElementType, null, value, path);

				case (Optional p, _) when value is not null:
					return MigrateValue(p.ElementType, current, currentMember, value, path);

				case (Types.Object p, Types.Object c)
					when value is JsonObject && p.Class is SchemaClass previousClass &&
						string.Equals(diff.CurrentClassFor(previousClass)?.Name, c.ClassName, StringComparison.Ordinal):
					return MigrateRecord(previousClass, value, path);

				case (Types.Enum p, Types.Enum c) when value is JsonValue text && text.TryGetValue(out string? name):
					string? mapped = string.Equals(diff.CurrentEnumFor(p.EnumName)?.Name, c.EnumName, StringComparison.Ordinal)
						? diff.CurrentEnumValueFor(p.EnumName, name)
						: null;
					return Settle(current, currentMember, mapped is null ? value : JsonValue.Create(mapped), path);

				case (Types.Array p, Types.Array c):
					return MigrateArray(p, c, value, path);

				default:
					return Settle(current, currentMember, value, path);
			}
		}

		private JsonNode? MigrateArray(Types.Array previous, Types.Array current, JsonNode? value, string path)
		{
			List<(string Path, JsonNode? Item)> items = value switch
			{
				JsonArray array => [.. array.Select((item, index) => ($"{path}[{index}]", item))],
				JsonObject map when previous.IsKeyed || string.Equals(previous.Container, Types.Array.MapContainer, StringComparison.Ordinal) =>
					[.. map.Select(entry => ($"{path}['{entry.Key}']", entry.Value))],
				_ => [],
			};

			if (items.Count == 0 && value is not JsonArray and not JsonObject)
			{
				return Settle(current, null, value, path);
			}

			List<(string Path, JsonNode? Item)> migrated = [.. items.Select(entry =>
				(entry.Path, MigrateValue(previous.ElementType, current.ElementType, null, entry.Item?.DeepClone(), entry.Path)))];

			if (!string.Equals(current.Container, Types.Array.MapContainer, StringComparison.Ordinal))
			{
				return new JsonArray([.. migrated.Select(entry => entry.Item)]);
			}

			JsonObject keyed = [];
			foreach ((string itemPath, JsonNode? item) in migrated)
			{
				if (KeyOf(current, item) is not string key)
				{
					issues.Add(Warning(itemPath, $"Entry has no '{current.Key}' to key it by; dropped."));
				}
				else if (keyed.ContainsKey(key))
				{
					issues.Add(Warning(itemPath, $"A second entry keyed '{key}'; dropped."));
				}
				else
				{
					keyed[key] = item;
				}
			}

			return keyed;
		}

		private static string? KeyOf(Types.Array map, JsonNode? item)
		{
			if (string.IsNullOrEmpty(map.Key) || item is not JsonObject record)
			{
				return null;
			}

			JsonNode? key = record.FirstOrDefault(property => string.Equals(property.Key, map.Key, StringComparison.OrdinalIgnoreCase)).Value;
			return key switch
			{
				JsonValue text when text.TryGetValue(out string? s) => s,
				JsonValue other => other.ToJsonString(),
				_ => null,
			};
		}

		/// <summary>
		/// Keeps a value that is already one of <paramref name="type"/>, converts one that converts
		/// without loss, and otherwise reports it and puts the default in its place.
		/// </summary>
		private JsonNode? Settle(BaseType type, SchemaMember? member, JsonNode? value, string path)
		{
			if (Conforms(type, value))
			{
				return value;
			}

			if (Convert(type, value) is JsonNode converted && Conforms(type, converted))
			{
				return converted;
			}

			issues.Add(Warning(path, $"Value {value?.ToJsonString() ?? "null"} is not a '{type.DisplayName}'; replaced with the default."));
			return DefaultFor(type, member, []);
		}

		private static JsonValue? Convert(BaseType type, JsonNode? value)
		{
			if (value is not JsonValue scalar)
			{
				return null;
			}

			JsonElement element = scalar.GetValue<JsonElement>();

			return (type, element.ValueKind) switch
			{
				(Types.String, JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False) => JsonValue.Create(element.GetRawText()),
				(Bool, JsonValueKind.String) when bool.TryParse(element.GetString(), out bool flag) => JsonValue.Create(flag),
				(Int or Long, JsonValueKind.String) when long.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long whole) => JsonValue.Create(whole),
				(Int or Long or Float or Double or Quantity, JsonValueKind.String) when double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double number) => Number(type, number),
				(Int or Long or Quantity, JsonValueKind.Number) when element.TryGetDouble(out double number) => Number(type, number),
				_ => null,
			};
		}

		private static JsonValue Number(BaseType type, double value) =>
			IsIntegral(type) && value == Math.Floor(value) && Math.Abs(value) <= long.MaxValue
				? JsonValue.Create((long)value)
				: JsonValue.Create(value);

		private static bool IsIntegral(BaseType type) => type switch
		{
			Int or Long => true,
			Quantity quantity => quantity.Storage is Int or Long,
			Vector vector => vector.ElementType is Int or Long,
			_ => false,
		};

		private static bool Conforms(BaseType type, JsonNode? value)
		{
			using JsonDocument document = JsonDocument.Parse(value?.ToJsonString() ?? "null");
			return SchemaDataValidator.ConformsTo(type, document.RootElement);
		}

		/// <summary>
		/// The value a member starts at: its declared default when that is one of its type, and
		/// otherwise the type's own.
		/// </summary>
		private static JsonNode? DefaultFor(BaseType type, SchemaMember? member, HashSet<string> building)
		{
			if (member?.DefaultValue is MemberDefault declared && Declared(type, declared) is JsonNode node && Conforms(type, node))
			{
				return node;
			}

			return TypeDefault(type, building);
		}

		private static JsonNode? Declared(BaseType type, MemberDefault declared)
		{
			BaseType represented = SchemaDiff.Represented(type);
			return declared switch
			{
				NumberDefault number when Components(represented) is int count && count >= 2 =>
					new JsonArray([.. Enumerable.Repeat(0, count).Select(_ => Number(represented, number.Value))]),
				NumberDefault number => Number(represented, number.Value),
				BooleanDefault flag => JsonValue.Create(flag.Value),
				TextDefault text => JsonValue.Create(text.Value),
				_ => null,
			};
		}

		private static int? Components(BaseType type) => type switch
		{
			ColorRGB or Vector3 => 3,
			ColorRGBA or Vector4 => 4,
			Vector2 => 2,
			Quantity quantity when QuantityRegistry.TryResolve(quantity.QuantityName, out QuantityRegistry.QuantityInfo? info) && info is not null && info.Components >= 2 => info.Components,
			_ => null,
		};

		private static JsonNode? TypeDefault(BaseType type, HashSet<string> building)
		{
			BaseType represented = SchemaDiff.Represented(type);

			if (Components(represented) is int count)
			{
				return new JsonArray([.. Enumerable.Repeat(0, count).Select(_ => Number(represented, 0))]);
			}

			return represented switch
			{
				Int or Long or Float or Double or Quantity => Number(represented, 0),
				Bool => JsonValue.Create(false),
				Types.String => JsonValue.Create(string.Empty),
				Types.DateTime => JsonValue.Create("0001-01-01T00:00:00"),
				Types.TimeSpan => JsonValue.Create("00:00:00"),
				Types.Enum enumType => enumType.ParentMember?.ParentSchema?.GetEnum(enumType.EnumName)?.Values is { Count: > 0 } values
					? JsonValue.Create<string>(values[0])
					: null,
				Types.Object objectType when objectType.Class is SchemaClass referenced => DefaultObject(referenced, building),
				Types.Array array => string.Equals(array.Container, Types.Array.MapContainer, StringComparison.Ordinal) ? new JsonObject() : new JsonArray(),
				_ => null,
			};
		}

		/// <summary>
		/// An object of <paramref name="schemaClass"/> with every member at its default. A class that
		/// holds itself, directly or not, has no finite default, so the innermost repetition is null.
		/// </summary>
		private static JsonObject? DefaultObject(SchemaClass schemaClass, HashSet<string> building)
		{
			if (!building.Add(schemaClass.Name))
			{
				return null;
			}

			JsonObject result = [];
			foreach (SchemaMember member in schemaClass.Members)
			{
				result[member.Name] = DefaultFor(member.Type, member, building);
			}

			building.Remove(schemaClass.Name);
			return result;
		}

		/// <summary>
		/// Spells a renamed member's property the way the data spelled the old one: exactly as the
		/// member is named, or with its first letter lowered as a camelCase serializer writes it.
		/// </summary>
		private static string SpellLike(string spelled, string previousName, string currentName)
		{
			if (string.Equals(spelled, previousName, StringComparison.Ordinal) || currentName.Length == 0)
			{
				return currentName;
			}

			string camel = char.ToLowerInvariant(previousName[0]) + previousName[1..];
			return string.Equals(spelled, camel, StringComparison.Ordinal)
				? char.ToLowerInvariant(currentName[0]) + currentName[1..]
				: currentName;
		}
	}
}
