// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp.Test;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the seam that lets this generator be found at all.
/// </summary>
/// <remarks>
/// The generator cannot live inside <c>ktsu.Schema</c>: that library publishes <c>net8.0</c> and
/// the AST this is built on does not. Registration is what makes it findable by the language a
/// schema names, so a schema saying <c>cpp</c> reaches it without the library knowing it exists.
/// </remarks>
[TestClass]
public sealed class RegistrationTests
{
	/// <summary>
	/// A registered generator is reached by the language a schema names, through the same call the
	/// built-in one is reached by.
	/// </summary>
	[TestMethod]
	public void ARegisteredGeneratorIsFoundByTheLanguageASchemaNames()
	{
		SchemaGenerator.Register(new CppCodeGenerator());
		Assert.IsTrue(SchemaGenerator.IsRegistered(CppCodeGenerator.LanguageId));

		Models.Schema schema = new();
		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		configuration.Language = CppCodeGenerator.LanguageId.As<LanguageName>();
		configuration.Namespace = "generated".As<CodeNamespace>();
		schema.AddClass("Packet".As<ClassName>())!.AddMember("Sequence".As<MemberName>())!.SetType(new Int());

		SchemaGenerationResult result = SchemaGenerator.Generate(schema, configuration);

		Assert.IsTrue(result.IsSuccess, result.Message);
		Assert.Contains("Packet.gen.hpp", result.Files.Keys);
		Assert.Contains("struct Packet", result.Files["Packet.gen.hpp"], StringComparison.Ordinal);
	}

	/// <summary>
	/// Generation is refused for a schema with errors, so a target with its own vocabulary is
	/// never asked to spell something the schema itself does not mean.
	/// </summary>
	[TestMethod]
	public void AnInvalidSchemaIsRefusedBeforeThisGeneratorSeesIt()
	{
		SchemaGenerator.Register(new CppCodeGenerator());

		Models.Schema schema = new();
		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		configuration.Language = CppCodeGenerator.LanguageId.As<LanguageName>();
		schema.AddClass("Holder".As<ClassName>())!
			.AddMember("Missing".As<MemberName>())!
			.SetType(new Models.Types.Object { ClassName = "Nowhere".As<ClassName>() });

		SchemaGenerationResult result = SchemaGenerator.Generate(schema, configuration);

		Assert.AreEqual(SchemaGenerationStatus.SchemaInvalid, result.Status);
		Assert.IsEmpty(result.Files);
	}

	/// <summary>
	/// A generator with no namespace configured emits into the global one: legal, rarely wanted,
	/// and reported by validation as a warning rather than refused - so the declarations go
	/// straight into the file rather than into nothing.
	/// </summary>
	[TestMethod]
	public void NoNamespaceEmitsIntoTheGlobalOne()
	{
		Models.Schema schema = new();
		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		configuration.Language = CppCodeGenerator.LanguageId.As<LanguageName>();

		SchemaClass packet = schema.AddClass("Packet".As<ClassName>())!;
		packet.TravelsAsBytes = true;
		packet.AddMember("Sequence".As<MemberName>())!.SetType(new Int());

		string code = new CppCodeGenerator().Generate(schema, configuration)["Packet.gen.hpp"];

		Assert.DoesNotContain("namespace", code, StringComparison.Ordinal);
		Assert.Contains("struct Packet", code, StringComparison.Ordinal);

		// The assertions below the struct are members of the file too, so this is also what says
		// they survive the path that has no namespace to put them in.
		Assert.Contains("static_assert(std::is_standard_layout_v<Packet>", code, StringComparison.Ordinal);
	}

	/// <summary>
	/// A schema read from a file names that file in every header it generates, so a reader of
	/// generated code knows what to edit instead.
	/// </summary>
	[TestMethod]
	public void AGeneratedHeaderNamesTheSchemaItCameFrom()
	{
		Models.Schema schema = new();
		schema.SetSourceFile(Path.Combine(Path.GetTempPath(), "physics.schema.json").As<ktsu.Semantics.Paths.AbsoluteFilePath>());

		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		configuration.Language = CppCodeGenerator.LanguageId.As<LanguageName>();
		configuration.Namespace = "generated".As<CodeNamespace>();
		schema.AddClass("Packet".As<ClassName>())!.AddMember("Sequence".As<MemberName>())!.SetType(new Int());

		string code = new CppCodeGenerator().Generate(schema, configuration)["Packet.gen.hpp"];

		Assert.Contains("// Source: physics.schema.json", code, StringComparison.Ordinal);
	}

	/// <summary>
	/// A generator built with no options carries the standard-library-only vocabulary, which is
	/// what the command line registers.
	/// </summary>
	[TestMethod]
	public void TheDefaultGeneratorSuppliesNothingBeyondTheStandardLibrary()
	{
		CppCodeGenerator generator = new();

		Assert.AreEqual(CppCodeGenerator.LanguageId, generator.Language);
		Assert.IsNull(generator.Options.Vector3);
		Assert.IsNull(generator.Options.Handle);
		Assert.IsNull(generator.Options.Result);
		Assert.IsEmpty(generator.Options.ExistingTypes);
	}
}
