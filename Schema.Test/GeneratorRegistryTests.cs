// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;
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
/// The registry is process-wide, so each test here registers a language nothing else uses. It is
/// also an unsynchronised dictionary, documented as meant to be filled once at start-up, so two of
/// these tests registering at once can lose a write while it resizes. That failed CI once on
/// Windows, with a generator registered a line earlier reported as unknown, so the class runs its
/// tests one at a time.
/// </para>
/// </remarks>
[TestClass]
[DoNotParallelize]
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
	/// A registered generator is code this library did not write, and the paths it returns are
	/// whatever it says. One naming a file outside the output directory is refused before anything
	/// is written, so its other files are not left behind either.
	/// </summary>
	[TestMethod]
	[DataRow("../escaped.txt")]
	[DataRow("nested/../../escaped.txt")]
	public void AGeneratorCannotWriteOutsideItsOutputDirectory(string key)
	{
		string directory = Path.Combine(Path.GetTempPath(), $"schema-escape-{Guid.NewGuid():N}");
		string output = Path.Combine(directory, "out");
		Directory.CreateDirectory(output);

		// A language of its own per row: the rows run in parallel, and a shared one let one row's
		// generator replace the other's between registering and generating.
		string language = $"fake-escape-{Guid.NewGuid():N}";
		try
		{
			SchemaGenerator.Register(new FakeGenerator(language, "// escaped", key));

			Schema schema = new();
			schema.SetSourceFile(Path.Combine(directory, "test.schema.json").As<AbsoluteFilePath>());
			SchemaCodeGenerator configuration = schema.AddCodeGenerator("Fake".As<CodeGeneratorName>())!;
			configuration.Language = language.As<LanguageName>();
			configuration.OutputPath = "out".As<RelativeDirectoryPath>();

			SchemaGenerationResult result = SchemaGenerator.GenerateToDisk(schema, configuration);

			Assert.AreEqual(SchemaGenerationStatus.WriteFailed, result.Status, result.Message);
			Assert.Contains(key, result.Message);
			Assert.IsFalse(File.Exists(Path.Combine(directory, "escaped.txt")), "The file was written outside the output directory.");
			Assert.IsFalse(File.Exists(Path.Combine(output, "fake.txt")), "The generator's other file was written although one of its paths was refused.");
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	/// <summary>
	/// A generator that emits a file or two, standing in for one that cannot ship in this project.
	/// </summary>
	/// <param name="language">The language it answers to.</param>
	/// <param name="content">What it writes.</param>
	/// <param name="extraPath">A second path to write the same content to, when there is one.</param>
	private sealed class FakeGenerator(string language, string content = "// fake", string? extraPath = null) : ISchemaCodeGenerator
	{
		public string Language { get; } = language;

		public IReadOnlyDictionary<string, string> Generate(Schema schema, SchemaCodeGenerator configuration)
		{
			Dictionary<string, string> files = new() { ["fake.txt"] = content };
			if (extraPath is not null)
			{
				files[extraPath] = content;
			}

			return files;
		}
	}
}
