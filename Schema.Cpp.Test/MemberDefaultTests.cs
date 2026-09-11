// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp.Test;

using ktsu.Schema.Cpp;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// What a member's default becomes in the generated header.
/// </summary>
/// <remarks>
/// A default is held as a <see cref="double"/> whatever the member is stored as, so writing one out
/// is a conversion rather than a copy, and the ways a conversion can be wrong are the ways a
/// generated header can hold a number the schema did not say. These are those ways: a value too
/// small to survive the writing, a value with more significant digits than the writing kept, a
/// whole number that has to read as a float rather than an int, and a value C++ cannot write at
/// all.
/// </remarks>
[TestClass]
public class MemberDefaultTests
{
	/// <summary>
	/// A fixed number of decimal places measures from the point rather than from the first digit
	/// that matters, so a small enough value rounds to nothing. A quantisation step is exactly the
	/// kind of number that is that small, and a default that silently becomes zero is a default
	/// nobody can see is wrong.
	/// </summary>
	[TestMethod]
	public void ASmallDefaultSurvives()
	{
		string code = GenerateWithDefault(1e-17);

		Assert.DoesNotContain("= 0.0f", code);
		Assert.Contains("1E-17f", code);
	}

	/// <summary>
	/// The same measurement, one step less extreme: the value survives but its digits do not. The
	/// header used to say 0.0000000000012346 for this, which is a different number.
	/// </summary>
	[TestMethod]
	public void ADefaultKeepsItsSignificantDigits() =>
		Assert.Contains("1.2345678E-12f", GenerateWithDefault(1.2345678e-12));

	/// <summary>
	/// The round-trip form of a whole number has neither a point nor an exponent, and <c>3f</c> is
	/// not a float literal in C++ — it is an integer carrying a suffix no integer takes. So the
	/// point has to be put back.
	/// </summary>
	[TestMethod]
	public void AWholeDefaultIsStillWrittenAsAFloat()
	{
		string code = GenerateWithDefault(3);

		Assert.Contains("3.0f", code);
		Assert.DoesNotContain("= 3f", code);
	}

	/// <summary>
	/// Two of the values a double can hold have no C++ literal. JSON cannot write either, so one
	/// can only arrive from a CLR type whose initialiser held it — and a header that reaches for a
	/// new include to write a default nobody meant is worse than being told.
	/// </summary>
	[TestMethod]
	public void ADefaultCppCannotWriteIsRefusedByName()
	{
		CppGenerationException refused = Assert.ThrowsExactly<CppGenerationException>(
			() => GenerateWithDefault(double.NaN));

		Assert.Contains("Speed", refused.Message);
	}

	/// <summary>
	/// Builds a one-member schema whose member defaults to the given value, and generates it.
	/// </summary>
	/// <param name="value">The default.</param>
	/// <returns>The generated header.</returns>
	private static string GenerateWithDefault(double value)
	{
		Models.Schema schema = new();

		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		configuration.Language = CppCodeGenerator.LanguageId.As<LanguageName>();
		configuration.Namespace = "holo::components".As<CodeNamespace>();

		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		SchemaMember speed = body.AddMember("Speed".As<MemberName>())!;
		speed.SetType(new Float());
		speed.DefaultValue = new NumberDefault { Value = value };

		return new CppCodeGenerator().Generate(schema, configuration)["Body.gen.hpp"];
	}
}
