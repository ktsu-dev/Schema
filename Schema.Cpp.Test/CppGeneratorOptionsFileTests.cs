// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp.Test;

using ktsu.Schema.Cpp;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the file a target writes to say how it spells the types this generator cannot invent.
/// </summary>
/// <remarks>
/// The record itself was always the answer; this is how a target gives one without being a C#
/// program. A build that shells out to the command line tool has no compilation of its own to put
/// a <c>new CppGeneratorOptions</c> in.
/// </remarks>
[TestClass]
public sealed class CppGeneratorOptionsFileTests
{
	/// <summary>
	/// The six spellings and the three settings a target actually writes.
	/// </summary>
	[TestMethod]
	public void ReadsWhatATargetSays()
	{
		const string json = """
			{
			  "vector3": { "name": "holo::Vector3", "include": "holotype/core/vector.hpp" },
			  "colorRgba": { "name": "holo::ColorRGBA", "include": "holotype/core/color.hpp" },
			  "handle": { "name": "holo::Handle", "include": "holotype/core/handle.hpp" },
			  "result": { "name": "holo::Result", "include": "holotype/core/result.hpp" },
			  "dateTime": { "name": "holo::DateTime", "include": "holotype/core/time.hpp" },
			  "generatedBy": "kschema",
			  "headerExtension": ".gen.hpp",
			  "memberNaming": "SnakeCase",
			  "reflection": true
			}
			""";

		Assert.IsTrue(CppGeneratorOptionsFile.TryParse(json, out CppGeneratorOptions? options, out string? message), message);

		Assert.AreEqual("holo::Vector3", options.Vector3?.Name);
		Assert.AreEqual("holotype/core/vector.hpp", options.Vector3?.Include);
		Assert.AreEqual("holo::ColorRGBA", options.ColorRgba?.Name);
		Assert.AreEqual("holo::Handle", options.Handle?.Name);
		Assert.AreEqual("holo::Result", options.Result?.Name);
		Assert.AreEqual("holo::DateTime", options.DateTime?.Name);
		Assert.AreEqual("kschema", options.GeneratedBy);
		Assert.AreEqual(".gen.hpp", options.HeaderExtension);
		Assert.AreEqual(CppMemberNaming.SnakeCase, options.MemberNaming);
		Assert.IsTrue(options.Reflection);

		// Said nothing about a two-component vector, so it still has none - which refuses the
		// schema's Vector2 by name rather than guessing at one.
		Assert.IsNull(options.Vector2);
	}

	/// <summary>
	/// A semantic type the target already hand-wrote is named rather than generated a second time,
	/// and the schema's name for it is the key.
	/// </summary>
	[TestMethod]
	public void ReadsTheTypesTheTargetAlreadyHas()
	{
		const string json = """
			{
			  "existingTypes": {
			    "Kilograms": { "name": "holo::Kilograms", "include": "holotype/core/units.hpp" },
			    "Radians": { "name": "holo::Radians", "include": "holotype/core/units.hpp" }
			  }
			}
			""";

		Assert.IsTrue(CppGeneratorOptionsFile.TryParse(json, out CppGeneratorOptions? options, out string? message), message);

		Assert.AreEqual(2, options.ExistingTypes.Count);
		Assert.AreEqual("holo::Kilograms", options.ExistingTypes["Kilograms"].Name);
		Assert.AreEqual("holotype/core/units.hpp", options.ExistingTypes["Radians"].Include);
	}

	/// <summary>
	/// An empty object is every default, which is standard C++ and nothing else.
	/// </summary>
	[TestMethod]
	public void AnEmptyObjectIsTheDefaults()
	{
		Assert.IsTrue(CppGeneratorOptionsFile.TryParse("{}", out CppGeneratorOptions? options, out string? message), message);

		Assert.AreEqual(new CppGeneratorOptions().GeneratedBy, options.GeneratedBy);
		Assert.AreEqual(CppMemberNaming.SnakeCase, options.MemberNaming);
		Assert.IsFalse(options.Reflection);
		Assert.IsEmpty(options.ExistingTypes);
		Assert.IsNull(options.Handle);
	}

	/// <summary>
	/// A type that needs no include says so with an empty one, which is not the same as a missing
	/// name.
	/// </summary>
	[TestMethod]
	public void AnIncludeMayBeEmpty()
	{
		const string json = """{ "handle": { "name": "holo::Handle", "include": "" } }""";

		Assert.IsTrue(CppGeneratorOptionsFile.TryParse(json, out CppGeneratorOptions? options, out string? message), message);
		Assert.AreEqual(string.Empty, options.Handle?.Include);
	}

	/// <summary>
	/// A misspelled property is refused, rather than quietly selecting the other behaviour.
	/// </summary>
	/// <remarks>
	/// This is the reason the reader disallows unmapped members at all. Every option here means
	/// something when absent - no <c>handle</c> refuses the schema's handles by name - so a typo
	/// would not fail here. It would fail three steps later, when the generator refused a schema
	/// for naming a type the target believed it had just declared, and the file would not be
	/// mentioned.
	/// </remarks>
	[TestMethod]
	public void RefusesAPropertyItDoesNotKnow()
	{
		const string json = """{ "handel": { "name": "holo::Handle", "include": "holotype/core/handle.hpp" } }""";

		Assert.IsFalse(CppGeneratorOptionsFile.TryParse(json, out CppGeneratorOptions? options, out string? message));
		Assert.IsNull(options);
		Assert.Contains("handel", message, StringComparison.Ordinal);
	}

	/// <summary>
	/// A spelling with no name is the one malformed shape the deserialiser itself accepts.
	/// </summary>
	[TestMethod]
	public void RefusesASpellingWithNoName()
	{
		const string json = """{ "vector3": { "include": "holotype/core/vector.hpp" } }""";

		Assert.IsFalse(CppGeneratorOptionsFile.TryParse(json, out CppGeneratorOptions? _, out string? message));
		Assert.Contains("vector3", message, StringComparison.Ordinal);
	}

	/// <summary>The same, for a type the target says it already has.</summary>
	[TestMethod]
	public void RefusesAnExistingTypeWithNoName()
	{
		const string json = """{ "existingTypes": { "Kilograms": { "include": "holotype/core/units.hpp" } } }""";

		Assert.IsFalse(CppGeneratorOptionsFile.TryParse(json, out CppGeneratorOptions? _, out string? message));
		Assert.Contains("Kilograms", message, StringComparison.Ordinal);
	}

	/// <summary>
	/// Malformed JSON reports where it went wrong, which is the whole of what makes a message about
	/// a hand-written file useful.
	/// </summary>
	[TestMethod]
	public void RefusesTextThatIsNotJson()
	{
		Assert.IsFalse(CppGeneratorOptionsFile.TryParse("{ \"reflection\": ", out CppGeneratorOptions? _, out string? message));
		Assert.IsNotEmpty(message);
	}

	/// <summary>
	/// A literal <c>null</c> is a readable file that says nothing, which is not the defaults.
	/// </summary>
	[TestMethod]
	public void RefusesAFileThatSaysNothing()
	{
		Assert.IsFalse(CppGeneratorOptionsFile.TryParse("null", out CppGeneratorOptions? _, out string? message));
		Assert.IsNotEmpty(message);
	}

	/// <summary>
	/// Comments and trailing commas are tolerated, because this is edited by hand beside a
	/// <c>.schema.json</c> and a target will want to say why it spells something the way it does.
	/// </summary>
	[TestMethod]
	public void ToleratesCommentsAndTrailingCommas()
	{
		const string json = """
			{
			  // The engine's own vector, not std::array: a schema's vector has named components.
			  "vector3": { "name": "holo::Vector3", "include": "holotype/core/vector.hpp" },
			}
			""";

		Assert.IsTrue(CppGeneratorOptionsFile.TryParse(json, out CppGeneratorOptions? options, out string? message), message);
		Assert.AreEqual("holo::Vector3", options.Vector3?.Name);
	}

	/// <summary>
	/// With no option, the result is the defaults - which is what a host offering no option does.
	/// </summary>
	[TestMethod]
	public void NoOptionIsTheDefaults()
	{
		string[] args = ["generate", "game.schema.json"];

		Assert.IsTrue(CppGeneratorOptionsFile.TryTake(args, out string[] remaining, out CppGeneratorOptions? options, out string? message), message);

		Assert.AreSequenceEqual(args, remaining);
		Assert.IsNull(options.Handle);
	}

	/// <summary>
	/// The option and its value are taken out, not passed through.
	/// </summary>
	/// <remarks>
	/// This is the whole reason <c>TryTake</c> is not <c>TryRead</c>. The commands find the schema
	/// by looking for the first argument that is not an option, stepping over the value any option
	/// consumes - and the list of options that consume one belongs to the commands, which have
	/// never heard of this. Left in, <c>holotype.cppgen.json</c> would be opened as the schema.
	/// </remarks>
	[TestMethod]
	public void TakesTheOptionOutOfTheArguments()
	{
		string path = WriteOptions("""{ "handle": { "name": "holo::Handle", "include": "holotype/core/handle.hpp" } }""");
		string[] args = ["generate", CppGeneratorOptionsFile.Option, path, "game.schema.json"];

		Assert.IsTrue(CppGeneratorOptionsFile.TryTake(args, out string[] remaining, out CppGeneratorOptions? options, out string? message), message);

		Assert.AreSequenceEqual(["generate", "game.schema.json"], remaining);
		Assert.AreEqual("holo::Handle", options.Handle?.Name);
	}

	/// <summary>A file that is not there is said so by name.</summary>
	[TestMethod]
	public void RefusesAFileThatIsNotThere()
	{
		string path = Path.Join(Path.GetTempPath(), $"absent-{Guid.NewGuid():N}.json");

		Assert.IsFalse(CppGeneratorOptionsFile.TryTake([CppGeneratorOptionsFile.Option, path], out string[] _, out CppGeneratorOptions? _, out string? message));
		Assert.Contains(path, message, StringComparison.Ordinal);
	}

	/// <summary>The option with nothing after it is an error rather than silently no options.</summary>
	[TestMethod]
	public void RefusesTheOptionWithNoValue()
	{
		Assert.IsFalse(CppGeneratorOptionsFile.TryTake(["generate", CppGeneratorOptionsFile.Option], out string[] _, out CppGeneratorOptions? _, out string? message));
		Assert.Contains(CppGeneratorOptionsFile.Option, message, StringComparison.Ordinal);
	}

	/// <summary>A file that will not parse names itself, so the reader knows which file to open.</summary>
	[TestMethod]
	public void NamesTheFileItCouldNotRead()
	{
		string path = WriteOptions("""{ "handel": {} }""");

		Assert.IsFalse(CppGeneratorOptionsFile.TryTake([CppGeneratorOptionsFile.Option, path], out string[] _, out CppGeneratorOptions? _, out string? message));
		Assert.Contains(path, message, StringComparison.Ordinal);
		Assert.Contains("handel", message, StringComparison.Ordinal);
	}

	/// <summary>
	/// The name a message tells someone to set is the name the reader accepts.
	/// </summary>
	/// <remarks>
	/// Two spellings of one property is exactly the arrangement that drifts, so the message asks
	/// the reader rather than restating it: a refusal naming <c>Vector3</c> would send someone to
	/// write a key this file refuses as unrecognised.
	/// </remarks>
	[TestMethod]
	public void TheNameItReportsIsTheNameItReads()
	{
		Assert.AreEqual("vector3", CppGeneratorOptionsFile.NameOf("Vector3"));
		Assert.AreEqual("colorRgba", CppGeneratorOptionsFile.NameOf("ColorRgba"));
		Assert.AreEqual("dateTime", CppGeneratorOptionsFile.NameOf("DateTime"));

		string path = WriteOptions($$"""{ "{{CppGeneratorOptionsFile.NameOf("Vector3")}}": { "name": "holo::Vector3", "include": "v.hpp" } }""");

		Assert.IsTrue(CppGeneratorOptionsFile.TryTake([CppGeneratorOptionsFile.Option, path], out string[] _, out CppGeneratorOptions? options, out string? message), message);
		Assert.AreEqual("holo::Vector3", options.Vector3?.Name);
	}

	private static string WriteOptions(string json)
	{
		string path = Path.Join(Path.GetTempPath(), $"cppgen-{Guid.NewGuid():N}.json");
		File.WriteAllText(path, json);
		return path;
	}
}
