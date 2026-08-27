// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Collections.ObjectModel;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SchemaTypes = Models.Types;

/// <summary>
/// Covers resolving a schema's relative paths against the file it was loaded from, and validating
/// the data a <see cref="DataSource"/> is bound to against its class.
/// </summary>
[TestClass]
public class SchemaDataSourceTests
{
	private string workingDirectory = string.Empty;

	[TestInitialize]
	public void CreateWorkingDirectory()
	{
		workingDirectory = Path.Combine(Path.GetTempPath(), $"schema-datasource-{Guid.NewGuid():N}");
		Directory.CreateDirectory(workingDirectory);
	}

	[TestCleanup]
	public void RemoveWorkingDirectory()
	{
		if (Directory.Exists(workingDirectory))
		{
			Directory.Delete(workingDirectory, recursive: true);
		}
	}

	private AbsoluteFilePath SchemaPath => Path.Combine(workingDirectory, "test.schema.json").As<AbsoluteFilePath>();

	private void WriteDataFile(string relativePath, string contents)
	{
		string full = Path.Combine(workingDirectory, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(full)!);
		File.WriteAllText(full, contents);
	}

	/// <summary>
	/// A schema with an Item class and a data source bound to it, anchored at the working
	/// directory so its relative paths resolve.
	/// </summary>
	private Schema CreateAnchoredSchema(string dataFile = "data/items.json")
	{
		Schema schema = new();

		SchemaEnum status = schema.AddEnum("Status".As<EnumName>())!;
		status.TryAddValue("Active".As<EnumValueName>());
		status.TryAddValue("Retired".As<EnumValueName>());

		SchemaClass item = schema.AddClass("Item".As<ClassName>())!;
		item.AddMember("Id".As<MemberName>())!.SetType(new SchemaTypes.String());
		item.AddMember("Count".As<MemberName>())!.SetType(new SchemaTypes.Int());
		item.AddMember("Status".As<MemberName>())!.SetType(new SchemaTypes.Enum { EnumName = "Status".As<EnumName>() });
		item.AddMember("Tint".As<MemberName>())!.SetType(new SchemaTypes.ColorRGB());

		DataSource dataSource = schema.AddDataSource("Items".As<DataSourceName>())!;
		dataSource.ClassName = "Item".As<ClassName>();
		dataSource.File = dataFile.As<RelativeFilePath>();

		schema.SetSourceFile(SchemaPath);
		return schema;
	}

	private const string ConformingItem = """
	{ "Id": "sword", "Count": 3, "Status": "Active", "Tint": [1, 0.5, 0] }
	""";

	// ---- path resolution ----------------------------------------------------

	[TestMethod]
	public void TestSchemaWithoutASourceCannotResolvePaths()
	{
		Schema schema = new();
		Assert.IsFalse(schema.CanResolvePaths);
		Assert.IsFalse(schema.TryResolvePath("data/items.json".As<RelativeFilePath>(), out _));
	}

	[TestMethod]
	public void TestPathsResolveAgainstTheSchemaFilesDirectory()
	{
		Schema schema = CreateAnchoredSchema();
		Assert.IsTrue(schema.CanResolvePaths);

		Assert.IsTrue(schema.GetDataSource("Items".As<DataSourceName>())!.TryResolveFile(out AbsoluteFilePath resolved));
		Assert.AreEqual(
			Path.GetFullPath(Path.Combine(workingDirectory, "data/items.json")),
			resolved.ToString());
	}

	[TestMethod]
	public void TestCodeGeneratorOutputPathResolves()
	{
		Schema schema = CreateAnchoredSchema();
		SchemaCodeGenerator generator = schema.AddCodeGenerator("CSharp".As<CodeGeneratorName>())!;
		generator.OutputPath = "generated".As<RelativeDirectoryPath>();

		Assert.IsTrue(generator.TryResolveOutputPath(out AbsoluteDirectoryPath resolved));
		Assert.AreEqual(Path.GetFullPath(Path.Combine(workingDirectory, "generated")), resolved.ToString());
	}

	[TestMethod]
	public void TestLoadWithASourcePathAnchorsTheSchema()
	{
		Schema schema = CreateAnchoredSchema();
		string json = SchemaSerializer.Serialize(schema);

		SchemaLoadResult result = SchemaSerializer.Load(json, SchemaPath);

		Assert.IsTrue(result.IsSuccess, result.Message);
		Assert.IsTrue(result.Schema!.CanResolvePaths);
		Assert.IsTrue(result.Schema.GetDataSource("Items".As<DataSourceName>())!.TryResolveFile(out _));
	}

	[TestMethod]
	public void TestSourceDirectoryIsNotSerialized()
	{
		Schema schema = CreateAnchoredSchema();
		string json = SchemaSerializer.Serialize(schema);

		Assert.IsFalse(json.Contains("sourceDirectory", StringComparison.OrdinalIgnoreCase), json);
		Assert.IsFalse(json.Contains(workingDirectory, StringComparison.Ordinal), json);
	}

	// ---- Validate() reports a missing file ----------------------------------

	[TestMethod]
	public void TestMissingDataFileIsReportedByValidate()
	{
		Schema schema = CreateAnchoredSchema();

		Collection<SchemaValidationIssue> issues = schema.Validate();

		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Error &&
			i.Path == "Items" &&
			i.Message.Contains("does not exist", StringComparison.Ordinal)),
			string.Join("; ", issues));
	}

	[TestMethod]
	public void TestPresentDataFileIsNotReportedByValidate()
	{
		Schema schema = CreateAnchoredSchema();
		WriteDataFile("data/items.json", $"[{ConformingItem}]");

		Assert.AreEqual(0, schema.Validate().Count, string.Join("; ", schema.Validate()));
	}

	[TestMethod]
	public void TestUnanchoredSchemaDoesNotReportAMissingFile()
	{
		// Without an anchor there is no defensible path to check, so the validator says nothing
		// rather than resolving against the working directory.
		Schema schema = CreateAnchoredSchema();
		schema.SourceDirectory = new();

		Assert.IsFalse(schema.Validate().Any(i => i.Message.Contains("does not exist", StringComparison.Ordinal)));
	}

	// ---- data conformance ---------------------------------------------------

	[TestMethod]
	public void TestConformingDataProducesNoIssues()
	{
		Schema schema = CreateAnchoredSchema();
		WriteDataFile("data/items.json", $"[{ConformingItem}]");

		Collection<SchemaValidationIssue> issues = SchemaDataValidator.ValidateDataSources(schema);
		Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
	}

	[TestMethod]
	public void TestASingleObjectRootIsAccepted()
	{
		Schema schema = CreateAnchoredSchema();
		WriteDataFile("data/items.json", ConformingItem);

		Assert.AreEqual(0, SchemaDataValidator.ValidateDataSources(schema).Count);
	}

	[TestMethod]
	public void TestMissingRequiredMemberIsAnError()
	{
		Schema schema = CreateAnchoredSchema();
		WriteDataFile("data/items.json", """{ "Id": "sword", "Status": "Active", "Tint": [1, 0.5, 0] }""");

		Collection<SchemaValidationIssue> issues = SchemaDataValidator.ValidateDataSources(schema);
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Error &&
			i.Message.Contains("'Count'", StringComparison.Ordinal) &&
			i.Message.Contains("missing", StringComparison.Ordinal)),
			string.Join("; ", issues));
	}

	[TestMethod]
	public void TestUnknownPropertyIsAWarning()
	{
		Schema schema = CreateAnchoredSchema();
		WriteDataFile("data/items.json", """{ "Id": "s", "Count": 1, "Status": "Active", "Tint": [1,1,1], "Extra": 5 }""");

		Collection<SchemaValidationIssue> issues = SchemaDataValidator.ValidateDataSources(schema);
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Warning &&
			i.Message.Contains("'Extra'", StringComparison.Ordinal)),
			string.Join("; ", issues));
	}

	[TestMethod]
	public void TestWrongScalarTypeIsAnError()
	{
		Schema schema = CreateAnchoredSchema();
		WriteDataFile("data/items.json", """{ "Id": 7, "Count": "many", "Status": "Active", "Tint": [1,1,1] }""");

		Collection<SchemaValidationIssue> issues = SchemaDataValidator.ValidateDataSources(schema);

		// One diagnostic per failure, not just the first.
		Assert.IsTrue(issues.Any(i => i.Path.EndsWith(".Id", StringComparison.Ordinal)), string.Join("; ", issues));
		Assert.IsTrue(issues.Any(i => i.Path.EndsWith(".Count", StringComparison.Ordinal)), string.Join("; ", issues));
	}

	[TestMethod]
	public void TestNonIntegralNumberForAnIntIsAnError()
	{
		Schema schema = CreateAnchoredSchema();
		WriteDataFile("data/items.json", """{ "Id": "s", "Count": 1.5, "Status": "Active", "Tint": [1,1,1] }""");

		Assert.IsTrue(SchemaDataValidator.ValidateDataSources(schema).Any(i =>
			i.Message.Contains("whole number", StringComparison.Ordinal)));
	}

	[TestMethod]
	public void TestValueOutsideTheEnumIsAnError()
	{
		Schema schema = CreateAnchoredSchema();
		WriteDataFile("data/items.json", """{ "Id": "s", "Count": 1, "Status": "Lapsed", "Tint": [1,1,1] }""");

		Collection<SchemaValidationIssue> issues = SchemaDataValidator.ValidateDataSources(schema);
		Assert.IsTrue(issues.Any(i =>
			i.Message.Contains("'Lapsed' is not a value of enum 'Status'", StringComparison.Ordinal)),
			string.Join("; ", issues));
	}

	[TestMethod]
	public void TestWrongVectorLengthIsAnError()
	{
		Schema schema = CreateAnchoredSchema();
		WriteDataFile("data/items.json", """{ "Id": "s", "Count": 1, "Status": "Active", "Tint": [1,1] }""");

		Assert.IsTrue(SchemaDataValidator.ValidateDataSources(schema).Any(i =>
			i.Message.Contains("Expected 3 numbers", StringComparison.Ordinal)));
	}

	[TestMethod]
	public void TestVectorContainerExpectsAnArray()
	{
		Schema schema = CreateAnchoredSchema("data/bag.json");
		SchemaClass bag = schema.AddClass("Bag".As<ClassName>())!;
		bag.AddMember("Tags".As<MemberName>())!.SetType(new SchemaTypes.Array
		{
			ElementType = new SchemaTypes.String(),
			Container = SchemaTypes.Array.VectorContainer.As<ContainerName>(),
		});
		schema.GetDataSource("Items".As<DataSourceName>())!.ClassName = "Bag".As<ClassName>();

		WriteDataFile("data/bag.json", """{ "Tags": { "not": "an array" } }""");

		Assert.IsTrue(SchemaDataValidator.ValidateDataSources(schema).Any(i =>
			i.Message.Contains("Expected an array for container 'vector'", StringComparison.Ordinal)));
	}

	[TestMethod]
	public void TestMapContainerExpectsAnObjectKeyedByItsKeyMember()
	{
		Schema schema = CreateAnchoredSchema("data/world.json");
		SchemaClass world = schema.AddClass("World".As<ClassName>())!;
		world.AddMember("Items".As<MemberName>())!.SetType(new SchemaTypes.Array
		{
			ElementType = new SchemaTypes.Object { ClassName = "Item".As<ClassName>() },
			Container = SchemaTypes.Array.MapContainer.As<ContainerName>(),
			Key = "Id".As<MemberName>(),
		});
		schema.GetDataSource("Items".As<DataSourceName>())!.ClassName = "World".As<ClassName>();

		WriteDataFile("data/world.json", """
		{ "Items": [ { "Id": "sword", "Count": 1, "Status": "Active", "Tint": [1,1,1] } ] }
		""");

		Assert.IsTrue(SchemaDataValidator.ValidateDataSources(schema).Any(i =>
			i.Message.Contains("Expected an object keyed by 'Id'", StringComparison.Ordinal)));
	}

	[TestMethod]
	public void TestMapKeyMustAgreeWithTheEntrysKeyMember()
	{
		Schema schema = CreateAnchoredSchema("data/world.json");
		SchemaClass world = schema.AddClass("World".As<ClassName>())!;
		world.AddMember("Items".As<MemberName>())!.SetType(new SchemaTypes.Array
		{
			ElementType = new SchemaTypes.Object { ClassName = "Item".As<ClassName>() },
			Container = SchemaTypes.Array.MapContainer.As<ContainerName>(),
			Key = "Id".As<MemberName>(),
		});
		schema.GetDataSource("Items".As<DataSourceName>())!.ClassName = "World".As<ClassName>();

		WriteDataFile("data/world.json", """
		{ "Items": { "shield": { "Id": "sword", "Count": 1, "Status": "Active", "Tint": [1,1,1] } } }
		""");

		Collection<SchemaValidationIssue> issues = SchemaDataValidator.ValidateDataSources(schema);
		Assert.IsTrue(issues.Any(i =>
			i.Message.Contains("does not match", StringComparison.Ordinal)),
			string.Join("; ", issues));
	}

	[TestMethod]
	public void TestNestedObjectsAreValidated()
	{
		Schema schema = CreateAnchoredSchema("data/holder.json");
		SchemaClass holder = schema.AddClass("Holder".As<ClassName>())!;
		holder.AddMember("Best".As<MemberName>())!.SetType(new SchemaTypes.Object { ClassName = "Item".As<ClassName>() });
		schema.GetDataSource("Items".As<DataSourceName>())!.ClassName = "Holder".As<ClassName>();

		WriteDataFile("data/holder.json", """{ "Best": { "Id": "s", "Status": "Active", "Tint": [1,1,1] } }""");

		Assert.IsTrue(SchemaDataValidator.ValidateDataSources(schema).Any(i =>
			i.Path.Contains("Best", StringComparison.Ordinal) &&
			i.Message.Contains("'Count'", StringComparison.Ordinal)));
	}

	[TestMethod]
	public void TestInvalidJsonInTheDataFileIsReported()
	{
		Schema schema = CreateAnchoredSchema();
		WriteDataFile("data/items.json", "{ not json");

		Assert.IsTrue(SchemaDataValidator.ValidateDataSources(schema).Any(i =>
			i.Message.Contains("not valid JSON", StringComparison.Ordinal)));
	}

	[TestMethod]
	public void TestMissingDataFileIsReportedByTheDataValidatorToo()
	{
		Schema schema = CreateAnchoredSchema();

		Assert.IsTrue(SchemaDataValidator.ValidateDataSources(schema).Any(i =>
			i.Severity == SchemaValidationSeverity.Error &&
			i.Message.Contains("does not exist", StringComparison.Ordinal)));
	}

	[TestMethod]
	public void TestUnanchoredSchemaReportsThatItCannotCheckData()
	{
		Schema schema = new();
		schema.AddDataSource("Items".As<DataSourceName>());

		Collection<SchemaValidationIssue> issues = SchemaDataValidator.ValidateDataSources(schema);
		Assert.AreEqual(1, issues.Count);
		Assert.AreEqual(SchemaValidationSeverity.Warning, issues[0].Severity);
		Assert.IsTrue(issues[0].Message.Contains("location is unknown", StringComparison.Ordinal), issues[0].Message);
	}
}
