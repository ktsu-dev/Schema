// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp.Test;

using ktsu.Schema.Cpp;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// What happens when a schema names something C++ has already taken.
/// </summary>
/// <remarks>
/// C# escapes these with <c>@</c> and carries on; C++ has no equivalent, so the name has to change
/// and the only question is who says so. Refusing at the schema element is how the generator says
/// it there rather than leaving a compiler error for a reader to work backwards from — the same
/// thing it already does for a type the target has not declared a spelling for.
/// </remarks>
[TestClass]
public class ReservedNameTests
{
	/// <summary>
	/// The case nobody would write on purpose, and the one that makes the rule obvious.
	/// </summary>
	[TestMethod]
	public void AMemberNamedAfterAKeywordIsRefused()
	{
		CppGenerationException refused = Assert.ThrowsExactly<CppGenerationException>(
			() => Generate("Body", "operator", CppMemberNaming.AsWritten));

		Assert.Contains("operator", refused.Message);
		Assert.Contains("keyword", refused.Message);
	}

	/// <summary>
	/// The case somebody would: <c>Delete</c> is an ordinary name for a member and a perfectly good
	/// one in the schema. It is only a keyword after the target's convention has lower-cased it,
	/// which is why this check cannot live anywhere that does not know the convention.
	/// </summary>
	[TestMethod]
	public void ANameTheConventionTurnsIntoAKeywordIsRefused()
	{
		CppGenerationException refused = Assert.ThrowsExactly<CppGenerationException>(
			() => Generate("Body", "Delete", CppMemberNaming.SnakeCase));

		Assert.Contains("'delete'", refused.Message);
		Assert.Contains("written 'Delete'", refused.Message);
	}

	/// <summary>
	/// And the other side of that: the same name is fine when the convention leaves it alone, since
	/// <c>Delete</c> is not a keyword. Refusing it anyway would be refusing a name that works.
	/// </summary>
	[TestMethod]
	public void TheSameNameIsFineWhenTheConventionLeavesItAlone() =>
		Assert.Contains("Delete", Generate("Body", "Delete", CppMemberNaming.AsWritten)["Body.gen.hpp"]);

	/// <summary>
	/// A type keeps the schema's spelling, so it is the words C++ has taken in that spelling that
	/// are refused — and a class called <c>union</c> is one.
	/// </summary>
	[TestMethod]
	public void AClassNamedAfterAKeywordIsRefused()
	{
		CppGenerationException refused = Assert.ThrowsExactly<CppGenerationException>(
			() => Generate("union", "Speed", CppMemberNaming.AsWritten));

		Assert.Contains("Class 'union'", refused.Message);
	}

	/// <summary>
	/// Builds a one-member schema with the given names and generates it.
	/// </summary>
	/// <param name="className">The class's name.</param>
	/// <param name="memberName">The member's name.</param>
	/// <param name="naming">The target's convention for a member.</param>
	/// <returns>The generated files.</returns>
	private static IReadOnlyDictionary<string, string> Generate(string className, string memberName, CppMemberNaming naming)
	{
		Models.Schema schema = new();

		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		configuration.Language = CppCodeGenerator.LanguageId.As<LanguageName>();
		configuration.Namespace = "holo::components".As<CodeNamespace>();

		SchemaClass body = schema.AddClass(className.As<ClassName>())!;
		body.AddMember(memberName.As<MemberName>())!.SetType(new Float());

		CppGeneratorOptions options = new() { MemberNaming = naming };
		return new CppCodeGenerator(options).Generate(schema, configuration);
	}
}
