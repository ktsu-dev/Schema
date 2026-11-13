// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp.Test;

using ktsu.Schema.Cpp;
using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// A C++ code generator's namespace is written into every header as it is, so validation refuses
/// one that C++ cannot write rather than letting the header fail to compile.
/// </summary>
[TestClass]
public sealed class NamespaceValidationTests
{
	[TestMethod]
	[DataRow("Game.Core")]
	[DataRow("My Game")]
	[DataRow("1Game")]
	[DataRow("game::class")]
	[DataRow("game::::core")]
	[DataRow("game::")]
	[DataRow("::game")]
	public void ANamespaceCppCannotWriteIsAnErrorAtTheGenerator(string codeNamespace)
	{
		Models.Schema schema = SchemaWithNamespace(codeNamespace);

		SchemaValidationIssue[] errors = [.. schema.Validate().Where(i => i.Severity == SchemaValidationSeverity.Error)];

		Assert.HasCount(1, errors, string.Join("; ", errors.Select(e => e.Message)));
		Assert.AreEqual("Cpp", errors[0].Path);
		Assert.Contains($"'{codeNamespace}'", errors[0].Message);
	}

	/// <summary>
	/// The C# spelling is the likeliest mistake, so the message names the separator C++ wants.
	/// </summary>
	[TestMethod]
	public void TheCSharpSpellingIsToldWhichSeparatorCppUses()
	{
		Models.Schema schema = SchemaWithNamespace("Game.Core");

		SchemaValidationIssue error = schema.Validate().Single(i => i.Severity == SchemaValidationSeverity.Error);

		Assert.Contains("C++ uses '::'", error.Message);
	}

	/// <summary>
	/// A C# keyword is an ordinary C++ name, so the check is the target language's and not a union
	/// of both.
	/// </summary>
	[TestMethod]
	[DataRow("sample")]
	[DataRow("holo::components")]
	[DataRow("_game::v2")]
	[DataRow("game::object")]
	public void ANamespaceCppCanWriteValidates(string codeNamespace)
	{
		Models.Schema schema = SchemaWithNamespace(codeNamespace);

		Assert.IsFalse(
			schema.Validate().Any(i => i.Severity == SchemaValidationSeverity.Error),
			string.Join("; ", schema.Validate().Select(i => i.Message)));
	}

	private static Models.Schema SchemaWithNamespace(string codeNamespace)
	{
		SchemaGenerator.Register(new CppCodeGenerator());

		Models.Schema schema = new();
		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		configuration.Language = CppCodeGenerator.LanguageId.As<LanguageName>();
		configuration.Namespace = codeNamespace.As<CodeNamespace>();
		configuration.OutputPath = "generated".As<RelativeDirectoryPath>();
		schema.AddClass("Packet".As<ClassName>())!.AddMember("Sequence".As<MemberName>())!.SetType(new Int());
		return schema;
	}
}
