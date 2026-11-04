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
			RefuseRootedPath(value);
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
			RefuseRootedPath(value);
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

		/// <summary>
		/// Refuses a rooted path whichever system reads the file. The semantic type asks the host, and
		/// a host only recognises its own roots: Windows reads <c>/tmp/out</c> as relative to the
		/// current drive and Linux reads <c>C:\out</c> as a file name, yet either one escapes the
		/// schema's directory on the system it was written for, and a schema file travels.
		/// </summary>
		private static void RefuseRootedPath(string? value)
		{
			if (value is not null && typeof(IRelativePath).IsAssignableFrom(typeof(T)) && IsRootedOnAnySystem(value))
			{
				// The inner exception is not decoration: the serializer adds the property's path to the
				// message only for a JsonException that has one.
				string message = Describe(value, null);
				throw new JsonException(message, new ArgumentException(message));
			}
		}

		private static bool IsRootedOnAnySystem(string value) =>
			value.Length > 0 && (value[0] is '/' or '\\' || (value.Length >= 2 && value[1] == ':' && char.IsAsciiLetter(value[0])));

		private static string Describe(string? value, ArgumentException? ex) =>
			typeof(IRelativePath).IsAssignableFrom(typeof(T))
				? $"'{value}' is not a relative path. Paths in a schema are relative to the schema file."
				: $"'{value}' is not a valid {typeof(T).Name}: {ex?.Message}";
	}
}
