// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Reflection;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
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
		reimported.AddClass(assembly.GetType("Generated.User", throwOnError: true)!);

		AssertClassesMatch(original, reimported, "Item");
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
		}
	}
}
