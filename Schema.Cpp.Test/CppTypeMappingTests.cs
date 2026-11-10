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
	/// A map keeps its keys, as the C# generator's <c>Dictionary</c> does: a stated key type is
	/// the map's key, and a map keyed by a member of its elements is keyed by that member's type.
	/// </summary>
	[TestMethod]
	public void AMapIsWrittenAsAStdMapKeyedLikeTheCSharpDictionary()
	{
		string stated = Generate(new Models.Types.Array
		{
			ElementType = new Float(),
			Container = ContainerName.Map,
			KeyType = new Models.Types.String(),
		});

		Assert.Contains("std::map<std::string, float> value{};", stated, StringComparison.Ordinal);
		Assert.Contains("#include <map>", stated, StringComparison.Ordinal);
		Assert.DoesNotContain("std::vector", stated, StringComparison.Ordinal);

		Models.Schema schema = new();
		SchemaClass item = schema.AddClass("Item".As<ClassName>())!;
		item.AddMember("Id".As<MemberName>())!.SetType(new Long());
		string keyed = Generate(schema, new Models.Types.Array
		{
			ElementType = new Models.Types.Object { ClassName = "Item".As<ClassName>() },
			Container = ContainerName.Map,
			Key = "Id".As<MemberName>(),
		});

		Assert.Contains("std::map<std::int64_t, Item> value{};", keyed, StringComparison.Ordinal);
	}

	/// <summary>
	/// The header a map is written in compiles, so both type arguments are ones C++ can key and
	/// hold, and every header they need is included.
	/// </summary>
	[TestMethod]
	public void AGeneratedMapCompiles()
	{
		string? compiler = Find("g++") ?? Find("clang++");
		if (compiler is null)
		{
			Assert.Inconclusive("no C++ compiler on PATH, so the generated header was not compiled.");
		}

		string header = Generate(new Models.Types.Array
		{
			ElementType = new Float(),
			Container = ContainerName.Map,
			KeyType = new Models.Types.String(),
		});

		string directory = Path.Join(Path.GetTempPath(), $"schema-cpp-map-{Guid.NewGuid():N}");
		Directory.CreateDirectory(directory);
		try
		{
			File.WriteAllText(Path.Join(directory, "Holder.gen.hpp"), header);
			File.WriteAllText(Path.Join(directory, "use.cpp"), """
				#include "Holder.gen.hpp"
				int main()
				{
					generated::Holder holder{};
					holder.value["a"] = 1.0f;
					return holder.value.at("a") == 1.0f ? 0 : 1;
				}
				""");

			using System.Diagnostics.Process process = new()
			{
				StartInfo = new System.Diagnostics.ProcessStartInfo(compiler!)
				{
					WorkingDirectory = directory,
					RedirectStandardError = true,
					RedirectStandardOutput = true,
				},
			};

			foreach (string argument in (string[])["-std=c++20", "-Wall", "-Wextra", "-fsyntax-only", "use.cpp"])
			{
				process.StartInfo.ArgumentList.Add(argument);
			}

			process.Start();
			string output = process.StandardError.ReadToEnd() + process.StandardOutput.ReadToEnd();
			process.WaitForExit();

			Assert.AreEqual(0, process.ExitCode, header + output);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
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
	/// <c>Void</c> is the one absent type the mapper has an answer for wherever it is asked, so
	/// nothing here refuses it and generation cannot be what catches it.
	/// </summary>
	/// <remarks>
	/// Both halves are asserted together on purpose. The first says what the generator writes -
	/// three spellings that are not types, since <c>void x{};</c> is not a declaration and neither
	/// <c>std::optional&lt;void&gt;</c> nor <c>std::span&lt;void&gt;</c> is instantiable - and the
	/// second says the schema refuses the same three before a generator is ever reached. That is
	/// the difference between an author being told which member is wrong and a consumer's compiler
	/// pointing at generated code.
	/// </remarks>
	[TestMethod]
	public void VoidIsRefusedByTheSchemaRatherThanByTheMapper()
	{
		(BaseType Type, string IllFormed)[] shapes =
		[
			(new Void(), "void value{};"),
			(new Optional { ElementType = new Void() }, "std::optional<void>"),
			(new Span { ElementType = new Void() }, "std::span<void>"),
		];

		foreach ((BaseType type, string illFormed) in shapes)
		{
			Assert.Contains(illFormed, Generate(type), StringComparison.Ordinal);

			Models.Schema schema = new();
			schema.AddClass("Holder".As<ClassName>())!.AddMember("Value".As<MemberName>())!.SetType(type);

			Assert.Contains(
				i => i.Severity == SchemaValidationSeverity.Error
					&& i.Message.Contains("Void carries no value", StringComparison.Ordinal),
				schema.Validate(),
				illFormed);
		}
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

	private static string? Find(string executable) =>
		(Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
			.Select(directory => Path.Join(directory, executable))
			.FirstOrDefault(File.Exists);

	private static string Generate(BaseType type, CppGeneratorOptions? options = null, string member = "Value") =>
		Generate(new Models.Schema(), type, options, member);

	private static string Generate(Models.Schema schema, BaseType type, CppGeneratorOptions? options = null, string member = "Value")
	{
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
