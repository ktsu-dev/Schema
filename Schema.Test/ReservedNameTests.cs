// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Reflection;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// What happens when a schema names something the target language has already taken.
/// </summary>
/// <remarks>
/// A schema names its elements in its own vocabulary and has no reason to know either language's
/// keyword list, so <c>Object</c>, <c>Params</c> and <c>Default</c> are ordinary things for someone
/// to write. C# has <c>@</c> and can carry them; these are the tests that say it does, by compiling
/// what comes out rather than by reading it. The C++ side has no escape and refuses instead, which
/// is tested where that generator lives.
/// </remarks>
[TestClass]
public class ReservedNameTests
{
	/// <summary>
	/// The whole point, checked the only way that settles it: the generated source compiles.
	/// </summary>
	[TestMethod]
	public void AClassNamedAfterAKeywordCompiles()
	{
		SchemaGenerationResult result = GenerateKeywordSchema(out _);

		Assembly assembly = GeneratedSourceCompiler.Compile(result.Files);

		Assert.IsNotNull(assembly.GetType("Generated.object"));
	}

	/// <summary>
	/// The escape is source syntax rather than part of the name, so the compiled type is called
	/// what the schema called it. That is what keeps the importer the generator's exact inverse
	/// without the importer needing to know this list at all.
	/// </summary>
	[TestMethod]
	public void TheEscapeDoesNotBecomePartOfTheName()
	{
		SchemaGenerationResult result = GenerateKeywordSchema(out _);
		Assembly assembly = GeneratedSourceCompiler.Compile(result.Files);

		Type generated = assembly.GetType("Generated.object")!;

		Assert.AreEqual("object", generated.Name);
		Assert.IsNotNull(generated.GetProperty("params"));
	}

	/// <summary>
	/// A use has to be escaped as well as a declaration: escaping only the declaration produces a
	/// file that does not compile, which is what not escaping at all produces with more steps.
	/// </summary>
	[TestMethod]
	public void AKeywordNameIsEscapedWhereItIsUsedToo()
	{
		GenerateKeywordSchema(out IReadOnlyDictionary<string, string> files);

		Assert.Contains("@object", files["Holder.g.cs"]);
	}

	/// <summary>
	/// A contextual keyword is an identifier everywhere it is not in the one position that makes it
	/// a keyword, and a property is never in that position. Escaping it would be noise in every
	/// generated file that has a member called <c>Value</c>.
	/// </summary>
	[TestMethod]
	public void AContextualKeywordIsLeftAlone()
	{
		GenerateKeywordSchema(out IReadOnlyDictionary<string, string> files);

		Assert.Contains(" value { get; set; }", files["Holder.g.cs"]);
		Assert.DoesNotContain("@value", files["Holder.g.cs"]);
	}

	/// <summary>
	/// Builds a schema whose names are keywords, and generates C# from it.
	/// </summary>
	/// <param name="files">The generated files, by name.</param>
	/// <returns>The generation result.</returns>
	private static SchemaGenerationResult GenerateKeywordSchema(out IReadOnlyDictionary<string, string> files)
	{
		Schema schema = new();

		SchemaClass reserved = schema.AddClass("object".As<ClassName>())!;
		reserved.AddMember("params".As<MemberName>())!.SetType(new Models.Types.String());
		reserved.AddMember("value".As<MemberName>())!.SetType(new Int());

		SchemaClass holder = schema.AddClass("Holder".As<ClassName>())!;
		holder.AddMember("Body".As<MemberName>())!
			.SetType(new Models.Types.Object { ClassName = "object".As<ClassName>() });
		holder.AddMember("value".As<MemberName>())!.SetType(new Int());

		SchemaGenerationResult result = SchemaGenerator.Generate(schema, CodeGenerationTests.ConfigureGenerator(schema));
		files = result.Files;
		return result;
	}
}
