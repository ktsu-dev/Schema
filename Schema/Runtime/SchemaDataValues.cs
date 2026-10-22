// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Runtime;

using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// Reads and writes a member of a data record held as JSON, in the form a data file stores it.
/// </summary>
/// <remarks>
/// This is the half of a generated data editor that has rules in it, kept here so the generated
/// accessors stay one line each and the rules are tested once. Every form matches what
/// <see cref="Models.SchemaDataValidator"/> accepts: a member is found by its declared name, then by
/// any casing of it; a date and a duration are invariant strings; an enum is its value's name; a
/// vector and a colour are arrays of numbers.
/// <para>
/// A read of a member that is absent or has the wrong shape throws
/// <see cref="InvalidOperationException"/> rather than inventing a value. The editor's issues say
/// what is wrong with the data; an accessor that quietly answered zero would contradict them.
/// </para>
/// </remarks>
public static class SchemaDataValues
{
	/// <summary>
	/// Finds the property holding a member, by its declared name and then by any casing of it.
	/// </summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <param name="key">The key the record actually uses, when found.</param>
	/// <returns>Whether the record has the member.</returns>
	public static bool TryFind(JsonObject record, string name, out string key)
	{
		Ensure.NotNull(record);
		Ensure.NotNull(name);

		if (record.ContainsKey(name))
		{
			key = name;
			return true;
		}

		foreach (KeyValuePair<string, JsonNode?> property in record)
		{
			if (string.Equals(property.Key, name, StringComparison.OrdinalIgnoreCase))
			{
				key = property.Key;
				return true;
			}
		}

		key = string.Empty;
		return false;
	}

	/// <summary>
	/// Writes a member, replacing the property already holding it or adding one under the declared name.
	/// </summary>
	/// <remarks>
	/// Keeping the existing key is what keeps a hand-written file's casing, and stops an edit from
	/// leaving two properties the validator would both read as the one member.
	/// </remarks>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <param name="value">The value to store.</param>
	public static void Set(JsonObject record, string name, JsonNode? value)
	{
		Ensure.NotNull(record);
		record[TryFind(record, name, out string key) ? key : name] = value;
	}

	/// <summary>Reads a <c>Bool</c> member.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <returns>The value.</returns>
	public static bool GetBool(JsonObject record, string name) =>
		Require(record, name, JsonValueKind.True, JsonValueKind.False).GetValueKind() == JsonValueKind.True;

	/// <summary>Writes a <c>Bool</c> member.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <param name="value">The value.</param>
	public static void SetBool(JsonObject record, string name, bool value) => Set(record, name, JsonValue.Create(value));

	/// <summary>Reads an <c>Int</c> member.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <returns>The value.</returns>
	public static int GetInt(JsonObject record, string name) =>
		ReadNumber(record, name, static (JsonElement e, out int v) => e.TryGetInt32(out v));

	/// <summary>Writes an <c>Int</c> member.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <param name="value">The value.</param>
	public static void SetInt(JsonObject record, string name, int value) => Set(record, name, JsonValue.Create(value));

	/// <summary>Reads a <c>Long</c> member.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <returns>The value.</returns>
	public static long GetLong(JsonObject record, string name) =>
		ReadNumber(record, name, static (JsonElement e, out long v) => e.TryGetInt64(out v));

	/// <summary>Writes a <c>Long</c> member.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <param name="value">The value.</param>
	public static void SetLong(JsonObject record, string name, long value) => Set(record, name, JsonValue.Create(value));

	/// <summary>Reads a <c>Float</c> member.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <returns>The value.</returns>
	public static float GetFloat(JsonObject record, string name) =>
		ReadNumber(record, name, static (JsonElement e, out float v) => e.TryGetSingle(out v));

	/// <summary>Writes a <c>Float</c> member.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <param name="value">The value.</param>
	public static void SetFloat(JsonObject record, string name, float value) => Set(record, name, JsonValue.Create(value));

	/// <summary>Reads a <c>Double</c> member.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <returns>The value.</returns>
	public static double GetDouble(JsonObject record, string name) =>
		ReadNumber(record, name, static (JsonElement e, out double v) => e.TryGetDouble(out v));

	/// <summary>Writes a <c>Double</c> member.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <param name="value">The value.</param>
	public static void SetDouble(JsonObject record, string name, double value) => Set(record, name, JsonValue.Create(value));

	/// <summary>Reads a <c>String</c> member.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <returns>The value.</returns>
	public static string GetString(JsonObject record, string name) =>
		Require(record, name, JsonValueKind.String).GetValue<string>();

	/// <summary>Writes a <c>String</c> member.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <param name="value">The value.</param>
	public static void SetString(JsonObject record, string name, string value)
	{
		Ensure.NotNull(value);
		Set(record, name, JsonValue.Create(value));
	}

	/// <summary>Reads a <c>DateTime</c> member, stored as an invariant round-trip string.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <returns>The value.</returns>
	public static DateTime GetDateTime(JsonObject record, string name)
	{
		string text = GetString(record, name);
		return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime value)
			? value
			: throw Malformed(name, $"'{text}' is not a date and time");
	}

	/// <summary>Writes a <c>DateTime</c> member as an invariant round-trip string.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <param name="value">The value.</param>
	public static void SetDateTime(JsonObject record, string name, DateTime value) =>
		SetString(record, name, value.ToString("O", CultureInfo.InvariantCulture));

	/// <summary>Reads a <c>TimeSpan</c> member, stored as an invariant string.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <returns>The value.</returns>
	public static TimeSpan GetTimeSpan(JsonObject record, string name)
	{
		string text = GetString(record, name);
		return TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out TimeSpan value)
			? value
			: throw Malformed(name, $"'{text}' is not a duration");
	}

	/// <summary>Writes a <c>TimeSpan</c> member as an invariant string.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <param name="value">The value.</param>
	public static void SetTimeSpan(JsonObject record, string name, TimeSpan value) =>
		SetString(record, name, value.ToString("c", CultureInfo.InvariantCulture));

	/// <summary>Reads an <c>Enum</c> member, stored as the name of one of its values.</summary>
	/// <typeparam name="TEnum">The enum generated for the member's type.</typeparam>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <returns>The value.</returns>
	public static TEnum GetEnum<TEnum>(JsonObject record, string name)
		where TEnum : struct, System.Enum
	{
		string text = GetString(record, name);

		// Ordinal, as the validator matches: a value spelled in another case is not that value.
		foreach (TEnum value in System.Enum.GetValues<TEnum>())
		{
			if (string.Equals(value.ToString(), text, StringComparison.Ordinal))
			{
				return value;
			}
		}

		throw Malformed(name, $"'{text}' is not a value of {typeof(TEnum).Name}");
	}

	/// <summary>Writes an <c>Enum</c> member as the name of its value.</summary>
	/// <typeparam name="TEnum">The enum generated for the member's type.</typeparam>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <param name="value">The value.</param>
	public static void SetEnum<TEnum>(JsonObject record, string name, TEnum value)
		where TEnum : struct, System.Enum =>
		SetString(record, name, value.ToString());

	/// <summary>Reads a <c>Vector2</c> of floats.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <returns>The value.</returns>
	public static Vector2 GetVector2(JsonObject record, string name)
	{
		float[] c = GetComponents(record, name, 2);
		return new(c[0], c[1]);
	}

	/// <summary>Writes a <c>Vector2</c> of floats.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <param name="value">The value.</param>
	public static void SetVector2(JsonObject record, string name, Vector2 value) => SetComponents(record, name, value.X, value.Y);

	/// <summary>Reads a <c>Vector3</c> of floats.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <returns>The value.</returns>
	public static Vector3 GetVector3(JsonObject record, string name)
	{
		float[] c = GetComponents(record, name, 3);
		return new(c[0], c[1], c[2]);
	}

	/// <summary>Writes a <c>Vector3</c> of floats.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <param name="value">The value.</param>
	public static void SetVector3(JsonObject record, string name, Vector3 value) => SetComponents(record, name, value.X, value.Y, value.Z);

	/// <summary>Reads a <c>Vector4</c> of floats.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <returns>The value.</returns>
	public static Vector4 GetVector4(JsonObject record, string name)
	{
		float[] c = GetComponents(record, name, 4);
		return new(c[0], c[1], c[2], c[3]);
	}

	/// <summary>Writes a <c>Vector4</c> of floats.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <param name="value">The value.</param>
	public static void SetVector4(JsonObject record, string name, Vector4 value) => SetComponents(record, name, value.X, value.Y, value.Z, value.W);

	/// <summary>Reads a <c>ColorRGB</c> member.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <returns>The value.</returns>
	public static ColorRgb GetColorRgb(JsonObject record, string name)
	{
		float[] c = GetComponents(record, name, 3);
		return new(c[0], c[1], c[2]);
	}

	/// <summary>Writes a <c>ColorRGB</c> member.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <param name="value">The value.</param>
	public static void SetColorRgb(JsonObject record, string name, ColorRgb value) => SetComponents(record, name, value.R, value.G, value.B);

	/// <summary>Reads a <c>ColorRGBA</c> member.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <returns>The value.</returns>
	public static ColorRgba GetColorRgba(JsonObject record, string name)
	{
		float[] c = GetComponents(record, name, 4);
		return new(c[0], c[1], c[2], c[3]);
	}

	/// <summary>Writes a <c>ColorRGBA</c> member.</summary>
	/// <param name="record">The record.</param>
	/// <param name="name">The member's declared name.</param>
	/// <param name="value">The value.</param>
	public static void SetColorRgba(JsonObject record, string name, ColorRgba value) => SetComponents(record, name, value.R, value.G, value.B, value.A);

	private delegate bool NumberReader<T>(JsonElement element, out T value);

	private static JsonNode Require(JsonObject record, string name, params JsonValueKind[] kinds)
	{
		Ensure.NotNull(record);
		Ensure.NotNull(name);

		if (!TryFind(record, name, out string key))
		{
			throw new InvalidOperationException($"The record has no member '{name}'.");
		}

		JsonNode? node = record[key];
		JsonValueKind kind = node?.GetValueKind() ?? JsonValueKind.Null;
		return node is not null && kinds.Contains(kind)
			? node
			: throw Malformed(name, $"found {kind}");
	}

	private static T ReadNumber<T>(JsonObject record, string name, NumberReader<T> read)
	{
		JsonElement element = JsonSerializer.SerializeToElement(Require(record, name, JsonValueKind.Number));
		return read(element, out T value)
			? value
			: throw Malformed(name, $"{element.GetRawText()} does not fit {typeof(T).Name}");
	}

	private static float[] GetComponents(JsonObject record, string name, int count)
	{
		JsonArray array = Require(record, name, JsonValueKind.Array).AsArray();
		if (array.Count != count)
		{
			throw Malformed(name, $"expected {count} components, found {array.Count}");
		}

		float[] components = new float[count];
		for (int i = 0; i < count; i++)
		{
			JsonNode? component = array[i];
			if (component?.GetValueKind() != JsonValueKind.Number
				|| !JsonSerializer.SerializeToElement(component).TryGetSingle(out components[i]))
			{
				throw Malformed(name, $"component {i} is not a number");
			}
		}

		return components;
	}

	private static void SetComponents(JsonObject record, string name, params float[] components)
	{
		JsonArray array = [];
		foreach (float component in components)
		{
			array.Add(JsonValue.Create(component));
		}

		Set(record, name, array);
	}

	private static InvalidOperationException Malformed(string name, string reason) =>
		new($"The record's member '{name}' does not hold the form its type is stored in: {reason}.");
}
