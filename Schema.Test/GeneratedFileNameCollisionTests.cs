// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Names that are different to the schema but the same to the file system that generated output is
/// written to.
/// </summary>
/// <remarks>
/// Every generator names a file after each element, so two elements whose names differ only in case
/// write one file on Windows or macOS, and an element named after a generator's own support file
/// writes over it everywhere. Either way the output is missing something with nothing said.
/// </remarks>
[TestClass]
public sealed class GeneratedFileNameCollisionTests
{
	/// <summary>
	/// A language no real generator uses, so registering one here cannot change what another test
	/// generates.
	/// </summary>
	private const string ReservingLanguage = "reserving-test-language";

	private sealed class ReservingGenerator : ISchemaCodeGenerator
	{
		public string Language => ReservingLanguage;

		public IReadOnlyDictionary<string, string> Generate(Schema schema, SchemaCodeGenerator configuration) =>
			new Dictionary<string, string> { ["support.txt"] = string.Empty };

		public IReadOnlyCollection<string> ReservedNames(SchemaCodeGenerator configuration) => ["support"];
	}

	/// <summary>
	/// Registers the reserving generator once, rather than from each test, since the registry is
	/// process-wide and tests run in parallel.
	/// </summary>
	/// <param name="context">Unused.</param>
	[ClassInitialize]
	public static void RegisterReservingGenerator(TestContext context) => SchemaGenerator.Register(new ReservingGenerator());

	private static bool IsCaseCollision(SchemaValidationIssue issue) =>
		issue.Severity == SchemaValidationSeverity.Error && issue.Message.Contains("differ only in case", StringComparison.Ordinal);

	[TestMethod]
	public void ClassesDifferingOnlyInCaseAreAnError()
	{
		Schema schema = new();
		schema.AddClass("Player".As<ClassName>());
		schema.AddClass("player".As<ClassName>());

		SchemaValidationIssue issue = schema.Validate().Single(IsCaseCollision);

		Assert.Contains("'Player'", issue.Message);
		Assert.Contains("'player'", issue.Message);
		Assert.AreSame(schema.GetClass("player".As<ClassName>()), issue.Element);
	}

	[TestMethod]
	public void ElementsOfDifferentKindsDifferingOnlyInCaseAreAnError()
	{
		Schema schema = new();
		schema.AddClass("Status".As<ClassName>());
		schema.AddEnum("STATUS".As<EnumName>());
		schema.AddInterface("status".As<InterfaceName>());

		SchemaValidationIssue issue = schema.Validate().Single(IsCaseCollision);

		Assert.Contains("'Status', 'STATUS', 'status'", issue.Message);
	}

	[TestMethod]
	public void SemanticTypesTakePartInTheCheck()
	{
		Schema schema = new();
		schema.AddClass("Metres".As<ClassName>());
		schema.AddSemanticType("metres".As<SemanticTypeName>());

		Assert.ContainsSingle(schema.Validate().Where(IsCaseCollision));
	}

	/// <summary>
	/// Data sources and code generators are not written out as files, so their names may differ
	/// from an element's only in case.
	/// </summary>
	[TestMethod]
	public void NamesThatAreNotGeneratedAreLeftAlone()
	{
		Schema schema = new();
		schema.AddClass("Users".As<ClassName>());
		schema.AddDataSource("users".As<DataSourceName>());
		schema.AddCodeGenerator("USERS".As<CodeGeneratorName>());

		Assert.IsEmpty(schema.Validate().Where(IsCaseCollision));
	}

	/// <summary>
	/// An exact repeat is another check's to report, so it is not reported twice.
	/// </summary>
	[TestMethod]
	public void AnExactRepeatIsNotReportedAsACaseCollision()
	{
		Schema schema = new();
		schema.AddClass("Item".As<ClassName>());
		schema.AddEnum("Item".As<EnumName>());

		Assert.IsEmpty(schema.Validate().Where(IsCaseCollision));
	}

	[TestMethod]
	public void GenerationIsRefusedWhileNamesDifferOnlyInCase()
	{
		Schema schema = new();
		schema.AddClass("Player".As<ClassName>());
		schema.AddClass("player".As<ClassName>());
		SchemaCodeGenerator configuration = schema.AddCodeGenerator("CSharp".As<CodeGeneratorName>())!;
		configuration.Language = CSharpCodeGenerator.LanguageId.As<LanguageName>();

		SchemaGenerationResult result = SchemaGenerator.Generate(schema, configuration);

		Assert.AreEqual(SchemaGenerationStatus.SchemaInvalid, result.Status);
		Assert.IsEmpty(result.Files);
	}

	[TestMethod]
	public void AnElementNamedAfterAGeneratorsOwnFileIsAnError()
	{
		Schema schema = new();
		SchemaClass support = schema.AddClass("Support".As<ClassName>())!;
		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Reserving".As<CodeGeneratorName>())!;
		configuration.Language = ReservingLanguage.As<LanguageName>();

		SchemaValidationIssue issue = schema.Validate().Single(i => i.Severity == SchemaValidationSeverity.Error);

		Assert.Contains("writes for itself", issue.Message);
		Assert.AreSame(support, issue.Element);
		Assert.AreEqual(SchemaGenerationStatus.SchemaInvalid, SchemaGenerator.Generate(schema, configuration).Status);
	}

	/// <summary>
	/// A name is only reserved by a generator the schema actually configures.
	/// </summary>
	[TestMethod]
	public void AReservedNameIsAllowedWhenNoGeneratorReservesIt()
	{
		Schema schema = new();
		schema.AddClass("Support".As<ClassName>());
		SchemaCodeGenerator configuration = schema.AddCodeGenerator("CSharp".As<CodeGeneratorName>())!;
		configuration.Language = CSharpCodeGenerator.LanguageId.As<LanguageName>();

		Assert.IsEmpty(schema.Validate().Where(i => i.Severity == SchemaValidationSeverity.Error));
	}
}
