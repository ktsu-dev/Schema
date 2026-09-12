// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Reflection;
using System.Runtime.InteropServices;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The integration test #119 asks for: generate C# from a schema, compile it, reimport the
/// compiled types, and check the result is the schema we started with.
/// </summary>
/// <remarks>
/// This is what keeps the generator's type mapping and the reflection importer's type mapping
/// from drifting apart: they are inverses, and if either changes without the other, this fails.
/// </remarks>
[TestClass]
public class CodeGenerationRoundTripTests
{
	[TestMethod]
	public void TestGeneratedSourceCompiles()
	{
		Schema schema = CodeGenerationTests.CreateFullSchema();
		SchemaGenerationResult result = SchemaGenerator.Generate(schema, CodeGenerationTests.ConfigureGenerator(schema));

		Assembly assembly = GeneratedSourceCompiler.Compile(result.Files);
		Assert.IsNotNull(assembly.GetType("Generated.User"));
	}

	[TestMethod]
	public void TestGenerateCompileAndReimportReproducesTheSchema()
	{
		Schema original = CodeGenerationTests.CreateFullSchema();
		SchemaGenerationResult result = SchemaGenerator.Generate(original, CodeGenerationTests.ConfigureGenerator(original));
		Assert.IsTrue(result.IsSuccess, result.Message);

		Assembly assembly = GeneratedSourceCompiler.Compile(result.Files);

		Schema reimported = new();
		reimported.AddClass(assembly.GetType("Generated.Item", throwOnError: true)!);
		reimported.AddClass(assembly.GetType("Generated.Packet", throwOnError: true)!);
		reimported.AddClass(assembly.GetType("Generated.User", throwOnError: true)!);

		AssertClassesMatch(original, reimported, "Item");
		AssertClassesMatch(original, reimported, "Packet");
		AssertClassesMatch(original, reimported, "User");

		// The enum came back too, discovered through User.Role.
		SchemaEnum? role = reimported.GetEnum("Role".As<EnumName>());
		Assert.IsNotNull(role);
		CollectionAssert.AreEquivalent(
			original.GetEnum("Role".As<EnumName>())!.Values.Select(v => v.ToString()).ToArray(),
			role.Values.Select(v => v.ToString()).ToArray());
	}

	private static void AssertClassesMatch(Schema expected, Schema actual, string className)
	{
		SchemaClass expectedClass = expected.GetClass(className.As<ClassName>())!;
		SchemaClass actualClass = actual.GetClass(className.As<ClassName>())!;

		CollectionAssert.AreEquivalent(
			expectedClass.Members.Select(m => m.Name.ToString()).ToArray(),
			actualClass.Members.Select(m => m.Name.ToString()).ToArray(),
			$"Members of '{className}' differ after the round trip.");

		// Nothing about a CLR type says this, so it survives only because the generator writes an
		// attribute and the importer reads it - the two halves this test exists to keep in step.
		Assert.AreEqual(
			expectedClass.TravelsAsBytes,
			actualClass.TravelsAsBytes,
			$"'{className}' came back {(actualClass.TravelsAsBytes ? "promising" : "not promising")} to travel as bytes.");

		foreach (SchemaMember expectedMember in expectedClass.Members)
		{
			SchemaMember actualMember = actualClass.GetMember(expectedMember.Name)!;

			// BaseType equality is structural as of #106, which is what makes this comparison
			// meaningful rather than a reference check that could never fail.
			Assert.AreEqual(
				expectedMember.Type,
				actualMember.Type,
				$"'{className}.{expectedMember.Name}' came back as '{actualMember.Type.DisplayName}' " +
				$"instead of '{expectedMember.Type.DisplayName}'.");

			AssertMetadataMatches(expectedMember, actualMember, $"{className}.{expectedMember.Name}");
		}
	}

	/// <summary>
	/// A member's semantic metadata has to survive the trip too. None of it is expressible in the
	/// generated property's type, so it survives only as long as the attributes the generator
	/// writes and the importer reads stay a matched pair - which is the thing this asserts.
	/// </summary>
	private static void AssertMetadataMatches(SchemaMember expected, SchemaMember actual, string path)
	{
		Assert.AreEqual(expected.Unit?.ToString(), actual.Unit?.ToString(), $"'{path}' lost its unit.");
		Assert.AreEqual(expected.Interpolation, actual.Interpolation, $"'{path}' lost its interpolation.");
		Assert.AreEqual(expected.Editor?.ToString(), actual.Editor?.ToString(), $"'{path}' lost its editor hint.");

		Assert.AreEqual(expected.Range?.Minimum, actual.Range?.Minimum, $"'{path}' lost its range minimum.");
		Assert.AreEqual(expected.Range?.Maximum, actual.Range?.Maximum, $"'{path}' lost its range maximum.");
		Assert.AreEqual(expected.Range?.Wrap, actual.Range?.Wrap, $"'{path}' lost its wrap flag.");

		Assert.AreEqual(expected.Network?.Quantise, actual.Network?.Quantise, $"'{path}' lost its quantisation step.");
		Assert.AreEqual(expected.Network?.Delta, actual.Network?.Delta, $"'{path}' lost its delta flag.");

		// Compared by type as well as by text, since a NumberDefault of 1 and a TextDefault of "1"
		// print the same and mean different things.
		Assert.AreEqual(expected.DefaultValue?.GetType(), actual.DefaultValue?.GetType(), $"'{path}' changed the kind of its default.");
		Assert.AreEqual(expected.DefaultValue?.ToString(), actual.DefaultValue?.ToString(), $"'{path}' lost its default.");
	}

	/// <summary>
	/// The generated type does not merely record its defaults, it starts at them: an attribute a
	/// consumer has to read is not the same as an instance that is already right.
	/// </summary>
	[TestMethod]
	public void TestGeneratedPropertiesStartAtTheirDefaults()
	{
		Schema schema = CodeGenerationTests.CreateFullSchema();
		SchemaGenerationResult result = SchemaGenerator.Generate(schema, CodeGenerationTests.ConfigureGenerator(schema));
		Assert.IsTrue(result.IsSuccess, result.Message);

		Assembly assembly = GeneratedSourceCompiler.Compile(result.Files);
		Type user = assembly.GetType("Generated.User", throwOnError: true)!;
		object instance = Activator.CreateInstance(user)!;

		Assert.AreEqual(1.5f, user.GetProperty("Ratio")!.GetValue(instance));
		Assert.AreEqual(3, user.GetProperty("Count")!.GetValue(instance));
		Assert.IsTrue((bool)user.GetProperty("Flag")!.GetValue(instance)!);
		Assert.AreEqual("anonymous", user.GetProperty("Name")!.GetValue(instance));
		Assert.AreEqual("Member", user.GetProperty("Role")!.GetValue(instance)!.ToString());
	}

	// ---- what the promise is emitted as --------------------------------------

	/// <summary>
	/// A class that promises to travel as raw bytes is a sequentially laid out struct, which is
	/// the promise being kept rather than merely recorded.
	/// </summary>
	/// <remarks>
	/// Sequential layout is what makes the schema's member order load-bearing on this side too: a
	/// CLR class may order its fields however it likes, so the C# and C++ versions of a class
	/// could not be compared byte for byte until this.
	/// </remarks>
	[TestMethod]
	public void TestAPromisingClassIsGeneratedAsASequentialStruct()
	{
		Type packet = CompileFullSchema().GetType("Generated.Packet", throwOnError: true)!;

		Assert.IsTrue(packet.IsValueType, "A class promising to travel as bytes came back as a reference type.");

		StructLayoutAttribute layout = packet.StructLayoutAttribute!;
		Assert.AreEqual(LayoutKind.Sequential, layout.Value);
	}

	/// <summary>
	/// The promise changes the shape of the classes that make it and no others. This is not a move
	/// to value types for generated C# in general.
	/// </summary>
	[TestMethod]
	public void TestAClassMakingNoSuchPromiseIsStillAReferenceType()
	{
		Assembly assembly = CompileFullSchema();

		Assert.IsFalse(assembly.GetType("Generated.User", throwOnError: true)!.IsValueType);
		Assert.IsFalse(assembly.GetType("Generated.Item", throwOnError: true)!.IsValueType);
	}

	/// <summary>
	/// A promising class holding another one round-trips as the class it names, which it could not
	/// do while the importer read only reference types.
	/// </summary>
	/// <remarks>
	/// The two halves of this change meet here: the generator turned the inner class into a struct
	/// and the importer would have read a struct as a member with no type at all, so a class the
	/// schema plainly named came back as <c>None</c>. Nothing else covers it, because the full
	/// schema's promising class holds only primitives.
	/// </remarks>
	[TestMethod]
	public void TestAPromisingClassInsideAnotherRoundTripsAsTheClassItNames()
	{
		Schema original = NestedPromisingSchema();
		SchemaGenerationResult result = SchemaGenerator.Generate(original, CodeGenerationTests.ConfigureGenerator(original));
		Assert.IsTrue(result.IsSuccess, result.Message);

		Assembly assembly = GeneratedSourceCompiler.Compile(result.Files);

		Schema reimported = new();
		reimported.AddClass(assembly.GetType("Generated.Transform", throwOnError: true)!);

		SchemaMember scale = reimported.GetClass("Transform".As<ClassName>())!.GetMember("Scale".As<MemberName>())!;
		Assert.AreEqual(new Models.Types.Object { ClassName = "Triple".As<ClassName>() }, scale.Type);

		// Discovered through the member, and still promising once it arrived.
		SchemaClass? triple = reimported.GetClass("Triple".As<ClassName>());
		Assert.IsNotNull(triple);
		Assert.IsTrue(triple.TravelsAsBytes);
	}

	/// <summary>
	/// A member of a promising class starts at its declared default, the same as one of any other
	/// class.
	/// </summary>
	/// <remarks>
	/// A struct whose members have initialisers does not compile without a constructor of its own,
	/// so the generator writes one. Without it the source is rejected outright; with it, this is
	/// what says the defaults survived being moved onto a value type.
	/// </remarks>
	[TestMethod]
	public void TestAPromisingClassStartsAtItsDefaults()
	{
		Schema original = NestedPromisingSchema();
		original.GetClass("Triple".As<ClassName>())!.GetMember("X".As<MemberName>())!.DefaultValue =
			new NumberDefault { Value = 2.5 };

		SchemaGenerationResult result = SchemaGenerator.Generate(original, CodeGenerationTests.ConfigureGenerator(original));
		Assert.IsTrue(result.IsSuccess, result.Message);

		Type triple = GeneratedSourceCompiler.Compile(result.Files).GetType("Generated.Triple", throwOnError: true)!;
		object instance = Activator.CreateInstance(triple)!;

		Assert.AreEqual(2.5f, triple.GetProperty("X")!.GetValue(instance));
	}

	/// <summary>
	/// A class that travels as bytes, holding one that does the same - which is what the promise
	/// permits and what the C# side could not represent before.
	/// </summary>
	private static Schema NestedPromisingSchema()
	{
		Schema schema = new();

		SchemaClass triple = schema.AddClass("Triple".As<ClassName>())!;
		triple.TravelsAsBytes = true;
		triple.AddMember("X".As<MemberName>())!.SetType(new Models.Types.Float());
		triple.AddMember("Y".As<MemberName>())!.SetType(new Models.Types.Float());
		triple.AddMember("Z".As<MemberName>())!.SetType(new Models.Types.Float());

		SchemaClass transform = schema.AddClass("Transform".As<ClassName>())!;
		transform.TravelsAsBytes = true;
		transform.AddMember("Scale".As<MemberName>())!.SetType(new Models.Types.Object { ClassName = "Triple".As<ClassName>() });

		return schema;
	}

	private static Assembly CompileFullSchema()
	{
		Schema schema = CodeGenerationTests.CreateFullSchema();
		SchemaGenerationResult result = SchemaGenerator.Generate(schema, CodeGenerationTests.ConfigureGenerator(schema));
		Assert.IsTrue(result.IsSuccess, result.Message);

		return GeneratedSourceCompiler.Compile(result.Files);
	}

	/// <summary>
	/// A default of the wrong kind for its member cannot reach the generator through
	/// <see cref="SchemaGenerator"/>, which refuses an invalid schema - but the generator is public
	/// and can be called directly. It falls back to the type's own initialiser rather than emitting
	/// source that does not compile.
	/// </summary>
	[TestMethod]
	public void TestAMismatchedDefaultDoesNotBreakTheGeneratedSource()
	{
		Schema schema = CodeGenerationTests.CreateFullSchema();
		schema.GetClass("User".As<ClassName>())!.GetMember("Name".As<MemberName>())!.DefaultValue = new NumberDefault { Value = 4.0 };

		IReadOnlyDictionary<string, string> files =
			new CSharpCodeGenerator().Generate(schema, CodeGenerationTests.ConfigureGenerator(schema));

		Assembly assembly = GeneratedSourceCompiler.Compile(files);
		object instance = Activator.CreateInstance(assembly.GetType("Generated.User", throwOnError: true)!)!;

		Assert.AreEqual(string.Empty, assembly.GetType("Generated.User")!.GetProperty("Name")!.GetValue(instance));
	}
}
