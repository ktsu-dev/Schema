// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Reads <see cref="CppGeneratorOptions"/> out of a file, so a target can say how it spells the
/// types this generator cannot invent without hosting the generator in its own program.
/// </summary>
/// <remarks>
/// <see cref="CppGeneratorOptions"/> was always the answer to "what does the target have that the
/// standard library does not"; what it lacked was a way for a target to answer without being a C#
/// program. A build that runs the command line tool has no compilation of its own to put a
/// <c>new CppGeneratorOptions</c> in, so the alternative to a file is a growing list of command
/// line flags spelling one record.
/// <para>
/// Written in the same dialect as a <c>.schema.json</c> - camelCase properties, comments and
/// trailing commas tolerated - because it sits beside one and is edited by the same person.
/// </para>
/// <para>
/// <b>An unrecognised property is refused rather than ignored.</b> Every one of these options has
/// a defined meaning when absent: a missing <c>handle</c> refuses the schema's handles by name,
/// and a missing <c>reflection</c> emits no table. So a typo would not fail, it would quietly
/// select the other behaviour, and the generator would then refuse a schema for naming a type the
/// target believed it had just declared. That is a worse error message than the one this gives.
/// </para>
/// </remarks>
public static class CppGeneratorOptionsFile
{
	/// <summary>
	/// The option a host offers for naming this file on a command line.
	/// </summary>
	public const string Option = "--cpp-options";

	/// <summary>
	/// The usage line for <see cref="Option"/>, for a host that lists what it adds.
	/// </summary>
	public const string OptionUsage = Option + " <file>                          How this target spells what C++ does not.";

	/// <summary>
	/// What a file calls one of <see cref="CppGeneratorOptions"/>'s properties.
	/// </summary>
	/// <remarks>
	/// Asked of the same naming policy that reads the file, rather than spelled out a second time,
	/// so a message telling someone which property to set cannot name one the reader would then
	/// refuse.
	/// </remarks>
	/// <param name="property">The property's name in C#.</param>
	/// <returns>Its name in a file.</returns>
	public static string NameOf(string property) => JsonNamingPolicy.CamelCase.ConvertName(property);

	/// <summary>
	/// How the file is read: the schema dialect, plus enums by name.
	/// </summary>
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
		UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
		Converters = { new JsonStringEnumConverter() },
	};

	/// <summary>
	/// Parses the options a target wrote.
	/// </summary>
	/// <param name="json">The file's text.</param>
	/// <param name="options">The options, when the text was readable.</param>
	/// <param name="message">Why it was not, when it was not.</param>
	/// <returns>True when <paramref name="options"/> was set.</returns>
	public static bool TryParse(
		string json,
		[NotNullWhen(true)] out CppGeneratorOptions? options,
		[NotNullWhen(false)] out string? message)
	{
		options = null;
		message = null;

		CppGeneratorOptions? parsed;

		try
		{
			parsed = JsonSerializer.Deserialize<CppGeneratorOptions>(json, JsonOptions);
		}
		catch (JsonException exception)
		{
			// The exception's own message carries the line and position, which is the whole of what
			// makes a message about a hand-written file useful.
			message = exception.Message;
			return false;
		}

		if (parsed is null)
		{
			message = "The file is empty.";
			return false;
		}

		if (!IsSpelled(parsed, out message))
		{
			return false;
		}

		options = parsed;
		return true;
	}

	/// <summary>
	/// Checks that every spelling the file gave actually names a type.
	/// </summary>
	/// <remarks>
	/// A spelling with no name is the one malformed shape the deserialiser accepts, because
	/// <see cref="CppTypeSpelling"/> takes its name as a constructor argument and an object that
	/// omits it is a valid JSON object. The generator would then write an empty type name into a
	/// header, which fails at the C++ compiler with no mention of this file. An include may be
	/// empty: a type that needs none says so that way.
	/// </remarks>
	private static bool IsSpelled(CppGeneratorOptions options, [NotNullWhen(false)] out string? message)
	{
		foreach ((string property, CppTypeSpelling? spelling) in Named(options))
		{
			if (spelling is not null && string.IsNullOrWhiteSpace(spelling.Name))
			{
				message = $"'{property}' has no name.";
				return false;
			}
		}

		foreach ((string schemaName, CppTypeSpelling spelling) in options.ExistingTypes)
		{
			if (string.IsNullOrWhiteSpace(spelling.Name))
			{
				message = $"'existingTypes.{schemaName}' has no name.";
				return false;
			}
		}

		message = null;
		return true;
	}

	/// <summary>
	/// Takes <see cref="Option"/> out of a host's arguments, reading the file it names.
	/// </summary>
	/// <remarks>
	/// Taken out rather than passed through, and that is not tidiness. The commands find the
	/// schema by looking for the first argument that is not an option, stepping over the value any
	/// option consumes - and the list of options that consume one belongs to the commands, which
	/// have never heard of this. Left in, the file's path would be read as the schema to open.
	/// <para>
	/// Absent, the result is the defaults, which is standard C++ and nothing else: a schema
	/// reaching for a vector, a handle, a fallible return or a date is refused by name. That is
	/// the same thing a host that offered no option at all would do.
	/// </para>
	/// </remarks>
	/// <param name="args">The host's arguments.</param>
	/// <param name="remaining">The arguments with the option and its value removed.</param>
	/// <param name="options">What the file said, or the defaults when there was no file.</param>
	/// <param name="message">Why the file could not be read, when it could not.</param>
	/// <returns>True when <paramref name="options"/> was set.</returns>
	public static bool TryTake(
		string[] args,
		out string[] remaining,
		[NotNullWhen(true)] out CppGeneratorOptions? options,
		[NotNullWhen(false)] out string? message)
	{
		Ensure.NotNull(args);

		remaining = args;
		options = null;
		message = null;

		int index = Array.FindIndex(args, a => string.Equals(a, Option, StringComparison.OrdinalIgnoreCase));

		if (index < 0)
		{
			options = new CppGeneratorOptions();
			return true;
		}

		if (index + 1 >= args.Length)
		{
			message = $"'{Option}' needs the path of a file.";
			return false;
		}

		string path = args[index + 1];
		remaining = [.. args[..index], .. args[(index + 2)..]];

		if (!File.Exists(path))
		{
			message = $"'{path}' does not exist.";
			return false;
		}

		string json;

		try
		{
			json = File.ReadAllText(path);
		}
		catch (IOException exception)
		{
			message = $"Could not read '{path}': {exception.Message}";
			return false;
		}
		catch (UnauthorizedAccessException exception)
		{
			message = $"Could not read '{path}': {exception.Message}";
			return false;
		}

		if (!TryParse(json, out options, out string? why))
		{
			message = $"Could not read '{path}': {why}";
			return false;
		}

		return true;
	}

	/// <summary>The spellings a file can give, under the names it gives them.</summary>
	private static IEnumerable<(string Property, CppTypeSpelling? Spelling)> Named(CppGeneratorOptions options) =>
	[
		("vector2", options.Vector2),
		("vector3", options.Vector3),
		("vector4", options.Vector4),
		("colorRgb", options.ColorRgb),
		("colorRgba", options.ColorRgba),
		("handle", options.Handle),
		("result", options.Result),
		("dateTime", options.DateTime),
	];
}
