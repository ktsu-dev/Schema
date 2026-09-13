// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp.Test;

using ktsu.Schema.Cpp;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// A generated enum is one byte wide, and says so.
/// </summary>
/// <remarks>
/// The width is not something the schema states, so it is a decision each generator makes - and
/// the two have to make the same one, because a class that
/// <see cref="SchemaClass.TravelsAsBytes"/> and holds an enum is otherwise a different number of
/// bytes in each language, which is the promise the flag makes being false.
/// <para>
/// One byte rather than four because a component's discriminant is a handful of values and the
/// struct around it is copied whole, onto the wire among other places. C++ can say so in the
/// declaration and does; the C# generator names <c>byte</c> for the same reason, asserted there by
/// <c>GeneratedLayoutAgreesWithCppTests</c>. Neither test can see the other's output, so each pins
/// its own half and names the other.
/// </para>
/// </remarks>
[TestClass]
public class EnumUnderlyingTypeTests
{
	/// <summary>
	/// The declaration carries the width explicitly, rather than leaving it to the implementation.
	/// </summary>
	[TestMethod]
	public void AGeneratedEnumIsStoredInAUint8() =>
		Assert.Contains("enum class Kind : std::uint8_t", Generate()["Kind.gen.hpp"]);

	/// <summary>
	/// And the header that says <c>std::uint8_t</c> includes what declares it.
	/// </summary>
	/// <remarks>
	/// Worth its own assertion because it is the failure the first test cannot see: a generated
	/// file naming a type it has not included compiles wherever something else happened to include
	/// <c>&lt;cstdint&gt;</c> first, which makes it a header-order bug rather than a build error.
	/// </remarks>
	[TestMethod]
	public void AndIncludesTheHeaderThatDeclaresIt() =>
		Assert.Contains("#include <cstdint>", Generate()["Kind.gen.hpp"]);

	/// <summary>
	/// Builds a schema with one enum in it and generates it.
	/// </summary>
	/// <returns>The generated files.</returns>
	private static IReadOnlyDictionary<string, string> Generate()
	{
		Models.Schema schema = new();

		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		configuration.Language = CppCodeGenerator.LanguageId.As<LanguageName>();
		configuration.Namespace = "holo::components".As<CodeNamespace>();

		SchemaEnum kind = schema.AddEnum("Kind".As<EnumName>())!;
		kind.TryAddValue("First".As<EnumValueName>());
		kind.TryAddValue("Second".As<EnumValueName>());

		return new CppCodeGenerator(new CppGeneratorOptions()).Generate(schema, configuration);
	}
}
