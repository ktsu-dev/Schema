// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers registering a generator that cannot ship beside the ones built in.
/// </summary>
/// <remarks>
/// This library targets frameworks that some generators' own dependencies do not, so a generator
/// cannot always live in this project - the C++ one is built on an AST that ships no
/// <c>net8.0</c> assembly. Registration is what lets one live in its own project and still be
/// found by the language a schema names, which is the whole reason the registry is not a fixed
/// table.
/// <para>
/// The registry is process-wide, so each test here registers a language nothing else uses.
/// </para>
/// </remarks>
[TestClass]
public sealed class GeneratorRegistryTests
{
	/// <summary>
	/// A registered generator is found by the language a schema names, and runs.
	/// </summary>
	[TestMethod]
	public void ARegisteredGeneratorRunsForItsLanguage()
	{
		SchemaGenerator.Register(new FakeGenerator("fake-runs"));

		Schema schema = new();
		schema.AddClass("User".As<ClassName>());
		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Fake".As<CodeGeneratorName>())!;
		configuration.Language = "fake-runs".As<LanguageName>();

		SchemaGenerationResult result = SchemaGenerator.Generate(schema, configuration);

		Assert.AreEqual(SchemaGenerationStatus.Success, result.Status);
		Assert.AreEqual("// fake", result.Files["fake.txt"]);
	}

	/// <summary>
	/// A language is matched the way <see cref="SchemaCodeGenerator.Language"/> says it is:
	/// case-insensitively.
	/// </summary>
	[TestMethod]
	public void ALanguageIsMatchedWithoutRegardToCase()
	{
		SchemaGenerator.Register(new FakeGenerator("fake-casing"));

		Assert.IsTrue(SchemaGenerator.IsRegistered("FAKE-CASING"));
		Assert.Contains("fake-casing", SchemaGenerator.SupportedLanguages);
	}

	/// <summary>
	/// Registering twice replaces rather than duplicating, so a host that overrides a built-in
	/// generator gets its own and not both.
	/// </summary>
	[TestMethod]
	public void RegisteringTwiceReplaces()
	{
		SchemaGenerator.Register(new FakeGenerator("fake-replaced", "// first"));
		SchemaGenerator.Register(new FakeGenerator("fake-replaced", "// second"));

		Schema schema = new();
		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Fake".As<CodeGeneratorName>())!;
		configuration.Language = "fake-replaced".As<LanguageName>();

		Assert.AreEqual("// second", SchemaGenerator.Generate(schema, configuration).Files["fake.txt"]);
		Assert.ContainsSingle(l => string.Equals(l, "fake-replaced", StringComparison.OrdinalIgnoreCase), SchemaGenerator.SupportedLanguages);
	}

	/// <summary>
	/// A language nobody registered is not registered, which is what a host checks before telling
	/// a user their schema names a generator it does not have.
	/// </summary>
	[TestMethod]
	public void AnUnregisteredLanguageIsReportedAsSuch()
	{
		Assert.IsFalse(SchemaGenerator.IsRegistered("brainfuck"));
		Assert.IsFalse(SchemaGenerator.IsRegistered(string.Empty));
	}

	/// <summary>
	/// The one built in is registered, so registration adds to the table rather than replacing it.
	/// </summary>
	[TestMethod]
	public void TheBuiltInGeneratorIsRegistered() =>
		Assert.IsTrue(SchemaGenerator.IsRegistered(CSharpCodeGenerator.LanguageId));

	/// <summary>
	/// A generator that names no language could never be found again, so saying so at registration
	/// is better than a silent entry under an empty key.
	/// </summary>
	[TestMethod]
	public void AGeneratorMustNameALanguage() =>
		Assert.ThrowsExactly<ArgumentException>(() => SchemaGenerator.Register(new FakeGenerator(string.Empty)));

	/// <summary>
	/// Nothing is a generator.
	/// </summary>
	[TestMethod]
	public void RegisteringNothingIsRefused() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => SchemaGenerator.Register(null!));

	/// <summary>
	/// A generator that emits one file, standing in for one that cannot ship in this project.
	/// </summary>
	/// <param name="language">The language it answers to.</param>
	/// <param name="content">What it writes.</param>
	private sealed class FakeGenerator(string language, string content = "// fake") : ISchemaCodeGenerator
	{
		public string Language { get; } = language;

		public IReadOnlyDictionary<string, string> Generate(Schema schema, SchemaCodeGenerator configuration) =>
			new Dictionary<string, string> { ["fake.txt"] = content };
	}
}
