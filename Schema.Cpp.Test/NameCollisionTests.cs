// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp.Test;

using ktsu.Schema.Cpp;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// What happens when two names the schema keeps apart are spelled alike in C++.
/// </summary>
/// <remarks>
/// The schema refuses two members with one name, but snake case can still map two different names
/// onto one spelling, and the header would then declare that spelling twice. Refusing at generation
/// names both schema elements, where the compiler would only point at the duplicate.
/// </remarks>
[TestClass]
public class NameCollisionTests
{
	[TestMethod]
	public void MembersThatDifferOnlyInAcronymCaseAreRefused()
	{
		CppGenerationException refused = Assert.ThrowsExactly<CppGenerationException>(
			() => GenerateClass(CppMemberNaming.SnakeCase, "PlayerID", "PlayerId"));

		Assert.Contains("'PlayerID'", refused.Message);
		Assert.Contains("'PlayerId'", refused.Message);
		Assert.Contains("'player_id'", refused.Message);
	}

	[TestMethod]
	public void MembersThatDifferOnlyInUnderscoresAreRefused()
	{
		CppGenerationException refused = Assert.ThrowsExactly<CppGenerationException>(
			() => GenerateClass(CppMemberNaming.SnakeCase, "fooBar", "foo_bar"));

		Assert.Contains("'fooBar'", refused.Message);
		Assert.Contains("'foo_bar'", refused.Message);
		Assert.Contains("'Body'", refused.Message);
	}

	/// <summary>
	/// The same names are distinct when the convention leaves them alone, so refusing them there
	/// would be refusing a header that compiles.
	/// </summary>
	[TestMethod]
	public void TheSameMembersAreFineWhenTheConventionLeavesThemAlone()
	{
		string header = GenerateClass(CppMemberNaming.AsWritten, "PlayerID", "PlayerId")["Body.gen.hpp"];

		Assert.Contains("PlayerID", header);
		Assert.Contains("PlayerId", header);
	}

	[TestMethod]
	public void FunctionsThatShareASpellingAreRefused()
	{
		CppGenerationException refused = Assert.ThrowsExactly<CppGenerationException>(
			() => GenerateInterface(world =>
			{
				world.AddFunction("LoadHDR".As<FunctionName>())!.SetReturnType(new Void());
				world.AddFunction("LoadHdr".As<FunctionName>())!.SetReturnType(new Void());
			}));

		Assert.Contains("Functions 'LoadHDR' and 'LoadHdr'", refused.Message);
		Assert.Contains("'load_hdr'", refused.Message);
	}

	[TestMethod]
	public void ParametersThatShareASpellingAreRefused()
	{
		CppGenerationException refused = Assert.ThrowsExactly<CppGenerationException>(
			() => GenerateInterface(world =>
			{
				SchemaFunction move = world.AddFunction("Move".As<FunctionName>())!;
				move.SetReturnType(new Void());
				move.AddParameter("deltaX".As<ParameterName>())!.SetType(new Float());
				move.AddParameter("delta_x".As<ParameterName>())!.SetType(new Float());
			}));

		Assert.Contains("Parameters 'deltaX' and 'delta_x' of 'Move'", refused.Message);
		Assert.Contains("'delta_x'", refused.Message);
	}

	private static IReadOnlyDictionary<string, string> GenerateClass(CppMemberNaming naming, params string[] memberNames)
	{
		Models.Schema schema = new();
		SchemaCodeGenerator configuration = Configure(schema);

		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		foreach (string memberName in memberNames)
		{
			body.AddMember(memberName.As<MemberName>())!.SetType(new Float());
		}

		return new CppCodeGenerator(new CppGeneratorOptions { MemberNaming = naming }).Generate(schema, configuration);
	}

	private static IReadOnlyDictionary<string, string> GenerateInterface(Action<SchemaInterface> declare)
	{
		Models.Schema schema = new();
		SchemaCodeGenerator configuration = Configure(schema);

		declare(schema.AddInterface("IWorld".As<InterfaceName>())!);

		return new CppCodeGenerator(new CppGeneratorOptions()).Generate(schema, configuration);
	}

	private static SchemaCodeGenerator Configure(Models.Schema schema)
	{
		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		configuration.Language = CppCodeGenerator.LanguageId.As<LanguageName>();
		configuration.Namespace = "holo::components".As<CodeNamespace>();
		return configuration;
	}
}
