// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp.Test;

using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;

using ktsu.Schema.Cpp;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SchemaEnumType = ktsu.Schema.Models.Types.Enum;

/// <summary>
/// Covers the reflection table: what a generated header can only put in a comment, written as data
/// a program can read.
/// </summary>
/// <remarks>
/// The table is what makes the rest of the schema's vocabulary reach C++ at all. A unit, a range, a
/// wrap flag, a network quantisation and an editor hint are all facts the struct cannot carry, and
/// a program that needs them either reads a table or has them written into it a second time by
/// hand - which is the drift this whole stack exists to stop.
/// </remarks>
[TestClass]
public sealed class ReflectionTableTests
{
	/// <summary>
	/// Two files a target that does not read them did not ask for, so they are asked for.
	/// </summary>
	[TestMethod]
	public void EmitsNothingUnlessAsked()
	{
		IReadOnlyDictionary<string, string> files = Generate(HolotypeOptions with { Reflection = false });

		Assert.DoesNotContain("reflect.gen.hpp", files.Keys);
		Assert.DoesNotContain("reflection.gen.hpp", files.Keys);
		Assert.Contains("RigidBody.gen.hpp", files.Keys, "the ordinary headers are unaffected");
	}

	/// <summary>The vocabulary and the table are two files, and both are new.</summary>
	[TestMethod]
	public void EmitsTheVocabularyAndTheTable()
	{
		IReadOnlyDictionary<string, string> files = Generate();

		Assert.Contains("reflect.gen.hpp", files.Keys);
		Assert.Contains("reflection.gen.hpp", files.Keys);
	}

	/// <summary>
	/// The vocabulary names every type a member can have, because the schema library is where the
	/// list lives.
	/// </summary>
	/// <remarks>
	/// This is the reason the C++ side emits its own vocabulary rather than being written against
	/// one the target already has: a type added to the schema appears here with no edit in either
	/// repository. The two spellings - the enumeration and the names beside it - are generated from
	/// one list, and the count is what says they still are.
	/// </remarks>
	[TestMethod]
	public void TheVocabularyNamesEveryTypeTheSchemaHas()
	{
		string vocabulary = Generate()["reflect.gen.hpp"];

		int declared = typeof(BaseType).GetCustomAttributes(typeof(JsonDerivedTypeAttribute), inherit: false).Length;

		Assert.AreEqual(declared, CppReflection.TypeKinds.Count, "the table's kinds are not the schema's");
		Assert.AreEqual(declared, Enumerators(vocabulary, "TypeKind"), "the enumeration has drifted from the list");
		Assert.AreEqual(declared, Enumerators(vocabulary, "kTypeKindNames"), "the names have drifted from the enumeration");

		foreach (string kind in (string[])["Float", "Semantic", "Vector3", "Enum", "Handle", "Optional"])
		{
			Assert.Contains($"\t\t{kind},", vocabulary, StringComparison.Ordinal);
			Assert.Contains($"\t\t\"{kind}\",", vocabulary, StringComparison.Ordinal);
		}
	}

	/// <summary>
	/// The offsets are the compiler's answer, not the generator's.
	/// </summary>
	/// <remarks>
	/// The single most important property of the table, and the reason it cannot drift from the
	/// layout it describes. A generator that worked out offsets itself would be a second
	/// implementation of the C++ ABI, and would be wrong on some platform eventually - so the test
	/// is not only that <c>offsetof</c> is there but that no number is.
	/// </remarks>
	[TestMethod]
	public void AsksTheCompilerForTheLayout()
	{
		string table = Generate()["reflection.gen.hpp"];

		Assert.Contains(
			".offset = static_cast<std::uint32_t>(offsetof(holo::components::RigidBody, velocity))",
			table,
			StringComparison.Ordinal);
		Assert.Contains(
			".size = static_cast<std::uint32_t>(sizeof(holo::components::RigidBody::velocity))",
			table,
			StringComparison.Ordinal);
		Assert.Contains("alignof(holo::components::RigidBody)", table, StringComparison.Ordinal);

		foreach (string offset in (string[])[".offset = 0", ".offset = 4", ".offset = 12"])
		{
			Assert.DoesNotContain(offset, table, StringComparison.Ordinal);
		}
	}

	/// <summary>
	/// The table is reached from the type rather than by spelling a convention.
	/// </summary>
	[TestMethod]
	public void DescribesTheClassByItsType()
	{
		string table = Generate()["reflection.gen.hpp"];

		Assert.Contains("template <>\nstruct Describe<holo::components::RigidBody>", Normalised(table), StringComparison.Ordinal);
		Assert.Contains("static constexpr ClassInfo info", table, StringComparison.Ordinal);
		Assert.Contains(".members = kRigidBodyMembers", table, StringComparison.Ordinal);
	}

	/// <summary>
	/// The dimension is the unit's own, resolved through the same registry validation uses.
	/// </summary>
	/// <remarks>
	/// Derived rather than restated, so the eight numbers cannot disagree with the unit text beside
	/// them. A member that measures nothing is dimensionless, which is the same shape rather than a
	/// missing one.
	/// </remarks>
	[TestMethod]
	public void CarriesTheDimensionOfTheUnitItNames()
	{
		string table = Generate()["reflection.gen.hpp"];

		// Metres per second: length 1, time -1, and nothing else.
		Assert.Contains(".dimension = { 1, 0, -1, 0, 0, 0, 0, 0 }", table, StringComparison.Ordinal);

		// Kilograms.
		Assert.Contains(".dimension = { 0, 1, 0, 0, 0, 0, 0, 0 }", table, StringComparison.Ordinal);
	}

	/// <summary>
	/// A semantic type is what the schema declares and a float is what the bytes are, and the table
	/// says both.
	/// </summary>
	/// <remarks>
	/// One field could not do it. Anything reading a value out of untyped bytes needs the
	/// representation; anything showing the member to a person wants the declared kind, because
	/// "Kilograms" is the answer and "Float" is not.
	/// </remarks>
	[TestMethod]
	public void TellsTheDeclaredKindFromTheRepresentation()
	{
		string mass = Entry(Generate()["reflection.gen.hpp"], "Mass");

		Assert.Contains(".kind = TypeKind::Semantic", mass, StringComparison.Ordinal);
		Assert.Contains(".representation = TypeKind::Float", mass, StringComparison.Ordinal);
	}

	/// <summary>
	/// Every property is written, including the ones taking their default.
	/// </summary>
	/// <remarks>
	/// What a person writing this by hand would leave out, and it would be wrong: designated
	/// initialisers with gaps trip <c>-Wmissing-field-initializers</c>, and generated code needing
	/// a warning suppressed around it does not survive a build stricter than the one it was written
	/// against.
	/// </remarks>
	[TestMethod]
	public void WritesEveryPropertyEvenWhenItIsTheDefault()
	{
		string restitution = Entry(Generate()["reflection.gen.hpp"], "Restitution");

		foreach (string property in Properties)
		{
			Assert.Contains($".{property} = ", restitution, StringComparison.Ordinal);
		}
	}

	/// <summary>
	/// An enum's values are named, and their numbers are read back out of the generated enum.
	/// </summary>
	/// <remarks>
	/// The number is the index today, because the generated enumeration writes no explicit values.
	/// Casting the enumerator rather than writing the index is what keeps this describing what is
	/// true the moment that stops being so.
	/// </remarks>
	[TestMethod]
	public void NamesAnEnumsValuesThroughTheGeneratedEnum()
	{
		string table = Generate()["reflection.gen.hpp"];

		Assert.Contains(
			"{ .name = \"Dynamic\", .value = static_cast<std::int64_t>(holo::components::BodyKind::Dynamic) }",
			table,
			StringComparison.Ordinal);
		Assert.Contains(".enum_values = kRigidBodyBodyKindValues", table, StringComparison.Ordinal);
		Assert.Contains(".enum_values = {}", table, StringComparison.Ordinal);
	}

	/// <summary>
	/// Declaration order is layout, so the table is in it too.
	/// </summary>
	/// <remarks>
	/// The offsets would still be right if the table were sorted - they come from the compiler -
	/// but anything walking the members to write them out in order would produce a different byte
	/// stream than the struct does.
	/// </remarks>
	[TestMethod]
	public void KeepsMembersInDeclarationOrder()
	{
		string table = Generate()["reflection.gen.hpp"];

		int previous = -1;
		foreach (string name in (string[])["Velocity", "Mass", "Heading", "Restitution", "BodyKind"])
		{
			int position = table.IndexOf($".name = \"{name}\"", StringComparison.Ordinal);

			Assert.IsGreaterThan(previous, position, $"{name} is out of declaration order");
			previous = position;
		}
	}

	/// <summary>
	/// Every class is listed, so a tool can walk them without naming any of them.
	/// </summary>
	[TestMethod]
	public void ListsEveryClassTheSchemaDeclares()
	{
		Assert.Contains(
			"inline constexpr const ClassInfo* kAll[] = { &Describe<holo::components::RigidBody>::info };",
			Generate()["reflection.gen.hpp"],
			StringComparison.Ordinal);
	}

	/// <summary>
	/// The table compiles, and says what it was generated to say.
	/// </summary>
	/// <remarks>
	/// Every claim below is a <c>static_assert</c>, so this needs no run: a wrong answer is a
	/// compile error and <c>-fsyntax-only</c> reaches it. That matters here more than anywhere
	/// else, because the two things the table exists to guarantee - that an offset is the
	/// compiler's and that a lookup by type finds the right descriptor - are exactly the things
	/// asserting on the text cannot check.
	/// <para>
	/// Over a schema of plain types rather than Holotype's exemplar, whose headers name an engine
	/// this test does not have.
	/// </para>
	/// </remarks>
	[TestMethod]
	public void TheTableCompilesAndSaysWhatItWasGeneratedToSay()
	{
		string directory = Emit(PlainSchema(), new CppGeneratorOptions { Reflection = true });

		File.WriteAllText(Path.Join(directory, "meaning.cpp"), """
			#include "reflection.gen.hpp"

			using namespace plain::reflect;

			// The descriptor is found by type, which is the whole reason it is a specialisation.
			static_assert(describe<plain::Body>().members.size() == 4);
			static_assert(describe<plain::Body>().travels_as_bytes);
			static_assert(describe<plain::Body>().has("Mass"));
			static_assert(!describe<plain::Body>().has("Nonexistent"));

			// The offset in the table is the offset the compiler chose, which is what stops the
			// table describing a layout other than the one beside it.
			static_assert(describe<plain::Body>().member("Mass").offset == offsetof(plain::Body, mass));
			static_assert(describe<plain::Body>().member("Alive").offset == offsetof(plain::Body, alive));
			static_assert(describe<plain::Body>().member("Mass").size == sizeof(plain::Body::mass));

			// A semantic type declares one thing and stores another, and the table says both.
			static_assert(describe<plain::Body>().member("Mass").kind == TypeKind::Semantic);
			static_assert(describe<plain::Body>().member("Mass").representation == TypeKind::Float);

			// The metadata the struct itself cannot carry.
			static_assert(describe<plain::Body>().member("Mass").has_range);
			static_assert(describe<plain::Body>().member("Mass").range.minimum == 0.5);
			static_assert(describe<plain::Body>().member("Heading").range.wrap);
			static_assert(describe<plain::Body>().member("Heading").interpolation == Interpolation::Spherical);
			static_assert(describe<plain::Body>().member("Heading").editor_hint == "dial");
			static_assert(describe<plain::Body>().member("Kind").enum_values.size() == 2);
			static_assert(describe<plain::Body>().member("Kind").enum_values[1].name == "Dynamic");

			// The two spellings of the kind list agree, which is what generating both from one
			// list is for.
			static_assert(to_string(TypeKind::Float) == "Float");
			static_assert(to_string(Interpolation::Spherical) == "Spherical");

			// Every class, without naming one.
			static_assert(std::size(kAll) == 1);

			int main() { return 0; }
			""");

		(int exitCode, string output) = Compile(directory, "meaning.cpp");

		Assert.AreEqual(0, exitCode, $"the generated table should compile and hold:\n{output}");
	}

	/// <summary>Every property of a member the table carries, in the order it writes them.</summary>
	private static readonly string[] Properties =
	[
		"name", "description", "unit", "kind", "representation", "dimension", "offset", "size",
		"has_range", "range", "interpolation", "editor_hint", "has_network", "network", "enum_values",
	];

	private static string Normalised(string text) => text.ReplaceLineEndings("\n");

	/// <summary>How many entries one of the vocabulary's generated lists has.</summary>
	private static int Enumerators(string vocabulary, string named)
	{
		string text = Normalised(vocabulary);
		int at = text.IndexOf(named, StringComparison.Ordinal);
		int end = text.IndexOf("\n\t};", at, StringComparison.Ordinal);

		return text[at..end].Split('\n').Count(line => line.EndsWith(','));
	}

	/// <summary>
	/// The one member entry naming this member, ending at the closing brace of the entry itself
	/// rather than at the first one inside it.
	/// </summary>
	private static string Entry(string table, string member)
	{
		string text = Normalised(table);
		int at = text.IndexOf($".name = \"{member}\"", StringComparison.Ordinal);
		Assert.IsGreaterThanOrEqualTo(0, at, $"the table should describe {member}");

		int end = text.IndexOf("\n    },", at, StringComparison.Ordinal);
		return text[at..(end < 0 ? text.Length : end)];
	}

	private static IReadOnlyDictionary<string, string> Generate() => Generate(HolotypeOptions);

	private static IReadOnlyDictionary<string, string> Generate(CppGeneratorOptions options)
	{
		Models.Schema schema = ExemplarSchema();
		return new CppCodeGenerator(options).Generate(schema, schema.GetCodeGenerator("Cpp".As<CodeGeneratorName>())!);
	}

	private static string Emit(Models.Schema schema, CppGeneratorOptions options)
	{
		string directory = Path.Join(Path.GetTempPath(), $"schema-reflect-{Guid.NewGuid():N}");
		Directory.CreateDirectory(directory);

		IReadOnlyDictionary<string, string> files =
			new CppCodeGenerator(options).Generate(schema, schema.GetCodeGenerator("Cpp".As<CodeGeneratorName>())!);

		foreach ((string name, string text) in files)
		{
			File.WriteAllText(Path.Join(directory, name), text);
		}

		return directory;
	}

	private static (int ExitCode, string Output) Compile(string directory, string file)
	{
		string? compiler = Find("g++") ?? Find("clang++");
		if (compiler is null)
		{
			Assert.Inconclusive("no C++ compiler on PATH, so the generated table was not compiled.");
		}

		using Process process = new()
		{
			StartInfo = new ProcessStartInfo(compiler!)
			{
				WorkingDirectory = directory,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
			},
		};

		foreach (string argument in (string[])["-std=c++20", "-Wall", "-Wextra", "-fsyntax-only", "-I.", file])
		{
			process.StartInfo.ArgumentList.Add(argument);
		}

		process.Start();
		string output = process.StandardError.ReadToEnd() + process.StandardOutput.ReadToEnd();
		process.WaitForExit();

		return (process.ExitCode, output);
	}

	private static string? Find(string executable)
	{
		string name = Path.GetFileName(executable);

		return (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
			.Select(directory => Path.Join(directory, name))
			.FirstOrDefault(File.Exists);
	}

	/// <summary>What Holotype tells the generator, with the table asked for.</summary>
	private static CppGeneratorOptions HolotypeOptions => new()
	{
		Reflection = true,
		Vector3 = new CppTypeSpelling("holo::Vector3", "holotype/core/vector.hpp"),
		ExistingTypes = new Dictionary<string, CppTypeSpelling>(StringComparer.Ordinal)
		{
			["MetresPerSecond"] = new("holo::MetresPerSecond", "holotype/core/units.hpp"),
			["Kilograms"] = new("holo::Kilograms", "holotype/core/units.hpp"),
			["Radians"] = new("holo::Radians", "holotype/core/units.hpp"),
			["Scalar"] = new("holo::Scalar", "holotype/core/units.hpp"),
		},
	};

	/// <summary>Holotype's exemplar component, as the schema a person would write.</summary>
	private static Models.Schema ExemplarSchema()
	{
		Models.Schema schema = Configured("holo::components");

		foreach (string unit in (string[])["MetresPerSecond", "Kilograms", "Radians", "Scalar"])
		{
			schema.AddSemanticType(unit.As<SemanticTypeName>())!.SetUnderlyingType(new Float());
		}

		SchemaEnum bodyKind = schema.AddEnum("BodyKind".As<EnumName>())!;
		bodyKind.TryAddValue("Static".As<EnumValueName>());
		bodyKind.TryAddValue("Kinematic".As<EnumValueName>());
		bodyKind.TryAddValue("Dynamic".As<EnumValueName>());

		SchemaClass rigidBody = schema.AddClass("RigidBody".As<ClassName>())!;
		rigidBody.Description = "Physical state integrated by the physics system each frame.".As<SchemaChildDescription>();
		rigidBody.TravelsAsBytes = true;

		SchemaMember velocity = rigidBody.AddMember("Velocity".As<MemberName>())!;
		velocity.Description = "Linear velocity in world space.".As<SchemaChildDescription>();
		velocity.SetType(new Vector3 { ElementType = Named("MetresPerSecond") });
		velocity.Unit = "m/s".As<UnitSymbol>();
		velocity.Interpolation = Interpolation.Linear;
		velocity.Network = new MemberNetwork { Quantise = 0.01, Delta = true };

		SchemaMember mass = rigidBody.AddMember("Mass".As<MemberName>())!;
		mass.SetType(Named("Kilograms"));
		mass.Unit = "kg".As<UnitSymbol>();
		mass.Range = new MemberRange { Minimum = 0.001, Maximum = 1000000 };
		mass.DefaultValue = new NumberDefault { Value = 1.0 };

		SchemaMember heading = rigidBody.AddMember("Heading".As<MemberName>())!;
		heading.SetType(Named("Radians"));
		heading.Unit = "Radian".As<UnitSymbol>();
		heading.Range = new MemberRange { Minimum = 0, Maximum = 6.2831853, Wrap = true };
		heading.Network = new MemberNetwork { Quantise = 0.001 };
		heading.Editor = "dial".As<EditorHint>();

		SchemaMember restitution = rigidBody.AddMember("Restitution".As<MemberName>())!;
		restitution.SetType(Named("Scalar"));
		restitution.Unit = "1".As<UnitSymbol>();
		restitution.Range = new MemberRange { Minimum = 0, Maximum = 1 };
		restitution.DefaultValue = new NumberDefault { Value = 0.5 };

		SchemaMember kind = rigidBody.AddMember("BodyKind".As<MemberName>())!;
		kind.SetType(new SchemaEnumType { EnumName = "BodyKind".As<EnumName>() });
		kind.DefaultValue = new TextDefault { Value = "Dynamic" };

		return schema;
	}

	/// <summary>
	/// A schema whose C++ needs nothing the standard library does not have, so it can be compiled
	/// here.
	/// </summary>
	private static Models.Schema PlainSchema()
	{
		Models.Schema schema = Configured("plain");

		schema.AddSemanticType("Kilograms".As<SemanticTypeName>())!.SetUnderlyingType(new Float());

		SchemaEnum kinds = schema.AddEnum("BodyKind".As<EnumName>())!;
		kinds.TryAddValue("Static".As<EnumValueName>());
		kinds.TryAddValue("Dynamic".As<EnumValueName>());

		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		body.Description = "A body of plain types.".As<SchemaChildDescription>();
		body.TravelsAsBytes = true;

		SchemaMember mass = body.AddMember("Mass".As<MemberName>())!;
		mass.SetType(Named("Kilograms"));
		mass.Unit = "kg".As<UnitSymbol>();
		mass.Range = new MemberRange { Minimum = 0.5, Maximum = 100 };

		SchemaMember heading = body.AddMember("Heading".As<MemberName>())!;
		heading.SetType(new Float());
		heading.Range = new MemberRange { Minimum = 0, Maximum = 6.2831853, Wrap = true };
		heading.Interpolation = Interpolation.Spherical;
		heading.Editor = "dial".As<EditorHint>();

		SchemaMember kind = body.AddMember("Kind".As<MemberName>())!;
		kind.SetType(new SchemaEnumType { EnumName = "BodyKind".As<EnumName>() });

		SchemaMember alive = body.AddMember("Alive".As<MemberName>())!;
		alive.SetType(new Bool());

		return schema;
	}

	private static Models.Schema Configured(string containing)
	{
		Models.Schema schema = new();

		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		configuration.Language = CppCodeGenerator.LanguageId.As<LanguageName>();
		configuration.Namespace = containing.As<CodeNamespace>();

		return schema;
	}

	private static Semantic Named(string semanticType) =>
		new() { SemanticTypeName = semanticType.As<SemanticTypeName>() };
}
