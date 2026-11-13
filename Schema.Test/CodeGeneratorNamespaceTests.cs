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
/// A C# code generator's namespace is written into every file as it is, so validation refuses one
/// that C# cannot write rather than letting the generated code fail to build.
/// </summary>
[TestClass]
public class CodeGeneratorNamespaceTests
{
	[TestMethod]
	[DataRow("holo::components")]
	[DataRow("My Game")]
	[DataRow("1Game")]
	[DataRow("Game.class")]
	[DataRow("Game..Core")]
	[DataRow("Game.")]
	[DataRow("Game-Core")]
	public void ANamespaceCSharpCannotWriteIsAnErrorAtTheGenerator(string codeNamespace)
	{
		Schema schema = SchemaWithNamespace(codeNamespace);

		SchemaValidationIssue[] errors = [.. schema.Validate().Where(i => i.Severity == SchemaValidationSeverity.Error)];

		Assert.HasCount(1, errors, string.Join("; ", errors.Select(e => e.Message)));
		Assert.AreEqual("CSharp", errors[0].Path);
		Assert.Contains($"'{codeNamespace}'", errors[0].Message);
	}

	/// <summary>
	/// The C++ spelling is the likeliest mistake, so the message names the separator C# wants.
	/// </summary>
	[TestMethod]
	public void TheCppSpellingIsToldWhichSeparatorCSharpUses()
	{
		Schema schema = SchemaWithNamespace("holo::components");

		SchemaValidationIssue error = schema.Validate().Single(i => i.Severity == SchemaValidationSeverity.Error);

		Assert.Contains("C# uses '.'", error.Message);
	}

	/// <summary>
	/// What validation accepts compiles: an ordinary namespace, and a keyword escaped with <c>@</c>.
	/// </summary>
	[TestMethod]
	[DataRow("Generated")]
	[DataRow("Game.Core")]
	[DataRow("_Game.V2")]
	[DataRow("Game.@class")]
	public void ANamespaceCSharpCanWriteValidatesAndCompiles(string codeNamespace)
	{
		Schema schema = SchemaWithNamespace(codeNamespace);

		Assert.IsFalse(
			schema.Validate().Any(i => i.Severity == SchemaValidationSeverity.Error),
			string.Join("; ", schema.Validate().Select(i => i.Message)));

		SchemaGenerationResult result = SchemaGenerator.Generate(schema, schema.CodeGenerators.Single());
		Assert.IsTrue(result.IsSuccess, result.Message);

		Assembly assembly = GeneratedSourceCompiler.Compile(result.Files);
		Assert.IsNotNull(assembly.GetType($"{codeNamespace}.Packet".Replace("@", string.Empty, StringComparison.Ordinal)));
	}

	private static Schema SchemaWithNamespace(string codeNamespace)
	{
		Schema schema = new();
		CodeGenerationTests.ConfigureGenerator(schema, codeNamespace);
		schema.AddClass("Packet".As<ClassName>())!.AddMember("Sequence".As<MemberName>())!.SetType(new Int());
		return schema;
	}
}
