// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ktsu.RoundTripStringJsonConverter;
using ktsu.Semantics.Paths;

/// <summary>
/// Reads semantic strings and paths as <see cref="RoundTripStringJsonConverterFactory"/> does, but
/// reports a value the type refuses as a <see cref="JsonException"/>.
/// </summary>
/// <remarks>
/// A semantic type refuses a value it cannot hold - an absolute path where the schema keeps a
/// relative one - by throwing <see cref="ArgumentException"/>. The serializer passes that through
/// untouched, so it escaped <see cref="Models.SchemaSerializer.Load(string)"/>, which promises a result, and
/// reached <c>kschema</c> and the editor as a crash. As a <see cref="JsonException"/> it carries the
/// property's path in the document and is reported like any other value the file cannot hold.
/// </remarks>
internal sealed class ValidatingStringJsonConverterFactory : JsonConverterFactory
{
	private readonly RoundTripStringJsonConverterFactory inner = new();

	/// <inheritdoc/>
	public override bool CanConvert(Type typeToConvert) => inner.CanConvert(typeToConvert);

	/// <inheritdoc/>
	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		JsonConverter? converter = inner.CreateConverter(typeToConvert, options);
		return converter is null
			? null
			: (JsonConverter?)Activator.CreateInstance(typeof(Validating<>).MakeGenericType(typeToConvert), converter);
	}

	private sealed class Validating<T>(JsonConverter inner) : JsonConverter<T>
		where T : notnull
	{
		private readonly JsonConverter<T> inner = (JsonConverter<T>)inner;

		public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			string? value = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
			try
			{
				return inner.Read(ref reader, typeToConvert, options);
			}
			catch (ArgumentException ex)
			{
				throw new JsonException(Describe(value, ex), ex);
			}
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
			inner.Write(writer, value, options);

		public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			string? value = reader.GetString();
			try
			{
				return inner.ReadAsPropertyName(ref reader, typeToConvert, options);
			}
			catch (ArgumentException ex)
			{
				throw new JsonException(Describe(value, ex), ex);
			}
		}

		public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
			inner.WriteAsPropertyName(writer, value, options);

		private static string Describe(string? value, ArgumentException ex) =>
			typeof(IRelativePath).IsAssignableFrom(typeof(T))
				? $"'{value}' is not a relative path. Paths in a schema are relative to the schema file."
				: $"'{value}' is not a valid {typeof(T).Name}: {ex.Message}";
	}
}
