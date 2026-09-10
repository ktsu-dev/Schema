// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp.Test;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the complete type mapping - §4 of Holotype's <c>docs/generated-cpp-target.md</c> - a row
/// at a time, and what happens at the rows the target has said nothing about.
/// </summary>
[TestClass]
public sealed class CppTypeMappingTests
{
	/// <summary>
	/// The types standard C++ already has an answer for, which the generator writes itself.
	/// </summary>
	/// <param name="schemaType">The schema type's discriminator.</param>
	/// <param name="expected">The C++ the member should be declared with.</param>
	[TestMethod]
	[DataRow("Bool", "bool value{};")]
	[DataRow("Int", "std::int32_t value{};")]
	[DataRow("Long", "std::int64_t value{};")]
	[DataRow("Float", "float value{};")]
	[DataRow("Double", "double value{};")]
	[DataRow("String", "std::string value{};")]
	[DataRow("TimeSpan", "std::chrono::duration<double> value{};")]
	public void StandardTypesAreWrittenWithoutTheTargetSayingAnything(string schemaType, string expected) =>
		Assert.Contains(expected, Generate(TypeNamed(schemaType)), StringComparison.Ordinal);

	/// <summary>
	/// The header a type needs comes with it, because a file naming a type it has not included
	/// does not compile and nothing but the mapping knows which header that is.
	/// </summary>
	/// <param name="schemaType">The schema type's discriminator.</param>
	/// <param name="expected">The include the member should have brought with it.</param>
	[TestMethod]
	[DataRow("Int", "#include <cstdint>")]
	[DataRow("String", "#include <string>")]
	[DataRow("TimeSpan", "#include <chrono>")]
	public void AStandardTypeBringsItsHeader(string schemaType, string expected) =>
		Assert.Contains(expected, Generate(TypeNamed(schemaType)), StringComparison.Ordinal);

	/// <summary>
	/// A collection is owned, a view is borrowed and an absent value is optional - all three of
	/// which the standard library spells.
	/// </summary>
	[TestMethod]
	public void TheStandardLibraryCarriersAreWrittenDirectly()
	{
		Assert.Contains(
			"std::vector<std::int32_t> value{};",
			Generate(new Models.Types.Array { ElementType = new Int(), Container = "vector".As<ContainerName>() }),
			StringComparison.Ordinal);

		Assert.Contains(
			"std::optional<std::int32_t> value{};",
			Generate(new Optional { ElementType = new Int() }),
			StringComparison.Ordinal);
	}

	/// <summary>
	/// A type the target supplies is spelled the way the target says, and brings the target's own
	/// header.
	/// </summary>
	[TestMethod]
	public void ATypeTheTargetSuppliesIsSpelledItsWay()
	{
		string code = Generate(
			new Vector3 { ElementType = new Float() },
			new CppGeneratorOptions { Vector3 = new CppTypeSpelling("holo::Vector3", "holotype/core/vector.hpp") });

		Assert.Contains("holo::Vector3<float> value{};", code, StringComparison.Ordinal);
		Assert.Contains("#include \"holotype/core/vector.hpp\"", code, StringComparison.Ordinal);
	}

	/// <summary>
	/// A colour's components are floats, which the schema enforces, so its spelling takes no type
	/// argument.
	/// </summary>
	[TestMethod]
	public void AColourIsUnparameterised()
	{
		string code = Generate(
			new ColorRGBA(),
			new CppGeneratorOptions { ColorRgba = new CppTypeSpelling("holo::Rgba", "holotype/core/colour.hpp") });

		Assert.Contains("holo::Rgba value{};", code, StringComparison.Ordinal);
	}

	/// <summary>
	/// A type standard C++ has no answer for, and the target has not supplied, is refused by name
	/// - naming the option that would let it through.
	/// </summary>
	/// <param name="schemaType">The schema type's discriminator.</param>
	/// <param name="option">The option that would supply it.</param>
	[TestMethod]
	[DataRow("Vector2", nameof(CppGeneratorOptions.Vector2))]
	[DataRow("Vector3", nameof(CppGeneratorOptions.Vector3))]
	[DataRow("Vector4", nameof(CppGeneratorOptions.Vector4))]
	[DataRow("ColorRGB", nameof(CppGeneratorOptions.ColorRgb))]
	[DataRow("ColorRGBA", nameof(CppGeneratorOptions.ColorRgba))]
	[DataRow("DateTime", nameof(CppGeneratorOptions.DateTime))]
	public void AGapIsRefusedByNameRatherThanGuessedAt(string schemaType, string option)
	{
		CppGenerationException refused = Assert.ThrowsExactly<CppGenerationException>(
			() => Generate(TypeNamed(schemaType)));

		Assert.Contains(option, refused.Message, StringComparison.Ordinal);
		Assert.Contains(nameof(CppGeneratorOptions), refused.Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// A member left with no type is a schema someone is still editing, and is refused with the
	/// thing to do about it rather than emitted as something.
	/// </summary>
	[TestMethod]
	public void AMemberWithNoTypeIsRefused()
	{
		CppGenerationException refused = Assert.ThrowsExactly<CppGenerationException>(
			() => Generate(new None()));

		Assert.Contains("no type chosen", refused.Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// A member's name is spelled the target's way; a type's keeps the schema's, because a type
	/// name is the same word in both places.
	/// </summary>
	[TestMethod]
	public void MembersFollowTheTargetsNamingAndTypesKeepTheSchemas()
	{
		Assert.Contains("float body_kind{};", Generate(new Float(), member: "BodyKind"), StringComparison.Ordinal);

		Assert.Contains(
			"float bodyKind{};",
			Generate(new Float(), member: "bodyKind", options: new CppGeneratorOptions { MemberNaming = CppMemberNaming.AsWritten }),
			StringComparison.Ordinal);
	}

	/// <summary>
	/// An acronym is one word, so a name does not gain an underscore between each of its letters.
	/// </summary>
	/// <param name="written">The name as the schema writes it.</param>
	/// <param name="spelled">The name as C++ should spell it.</param>
	[TestMethod]
	[DataRow("BodyKind", "body_kind")]
	[DataRow("LoadHDRTexture", "load_hdr_texture")]
	[DataRow("ID", "id")]
	[DataRow("already_snake", "already_snake")]
	[DataRow("X", "x")]
	public void SnakeCaseFindsWordBoundariesRatherThanCapitals(string written, string spelled) =>
		Assert.AreEqual(spelled, CppNaming.SnakeCase(written));

	private static string Generate(BaseType type, CppGeneratorOptions? options = null, string member = "Value")
	{
		Models.Schema schema = new();
		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		configuration.Language = CppCodeGenerator.LanguageId.As<LanguageName>();
		configuration.Namespace = "generated".As<CodeNamespace>();

		schema.AddClass("Holder".As<ClassName>())!.AddMember(member.As<MemberName>())!.SetType(type);

		return new CppCodeGenerator(options ?? new CppGeneratorOptions())
			.Generate(schema, configuration)["Holder.gen.hpp"];
	}

	/// <summary>
	/// Builds a type from the discriminator the schema file writes, so a row of the table is
	/// named here the way it is named there.
	/// </summary>
	private static BaseType TypeNamed(string typeName) => typeName switch
	{
		"Bool" => new Bool(),
		"Int" => new Int(),
		"Long" => new Long(),
		"Float" => new Float(),
		"Double" => new Double(),
		"String" => new Models.Types.String(),
		"DateTime" => new Models.Types.DateTime(),
		"TimeSpan" => new Models.Types.TimeSpan(),
		"Vector2" => new Vector2(),
		"Vector3" => new Vector3(),
		"Vector4" => new Vector4(),
		"ColorRGB" => new ColorRGB(),
		"ColorRGBA" => new ColorRGBA(),
		_ => throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "Not a row of the table."),
	};
}
