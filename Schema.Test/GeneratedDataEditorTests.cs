// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Runtime;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SchemaTypes = Models.Types;

/// <summary>
/// Covers binding a code generator to a data source, and the editor the C# generator then emits.
/// The editor is compiled and driven through reflection, because the claim worth testing is about
/// the generated code rather than the runtime base it derives from: that it accepts exactly what
/// <see cref="SchemaDataValidator"/> accepts, and that its typed accessors read and write the file.
/// </summary>
[TestClass]
public class GeneratedDataEditorTests
{
	private const string ConformingItems = """
	[
		{ "Id": "sword", "Count": 3, "Weight": 1.5, "Status": "Active", "Tint": [1, 0.5, 0], "Note": "kept" },
		{ "Id": "shield", "Count": 1, "Weight": 4, "Status": "Retired", "Tint": [0, 0, 1] }
	]
	""";

	private string workingDirectory = string.Empty;

	[TestInitialize]
	public void CreateWorkingDirectory()
	{
		workingDirectory = Path.Combine(Path.GetTempPath(), $"schema-editor-{Guid.NewGuid():N}");
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

	private static Schema CreateSchema(out SchemaCodeGenerator generator, string dataSourceName = "Items")
	{
		Schema schema = new();

		SchemaEnum status = schema.AddEnum("Status".As<EnumName>())!;
		status.TryAddValue("Active".As<EnumValueName>());
		status.TryAddValue("Retired".As<EnumValueName>());

		SchemaClass item = schema.AddClass("Item".As<ClassName>())!;
		item.AddMember("Id".As<MemberName>())!.SetType(new SchemaTypes.String());
		item.AddMember("Count".As<MemberName>())!.SetType(new SchemaTypes.Int());
		item.AddMember("Weight".As<MemberName>())!.SetType(new SchemaTypes.Float());
		item.AddMember("Status".As<MemberName>())!.SetType(new SchemaTypes.Enum { EnumName = "Status".As<EnumName>() });
		item.AddMember("Tint".As<MemberName>())!.SetType(new SchemaTypes.ColorRGB());

		DataSource dataSource = schema.AddDataSource("Items".As<DataSourceName>())!;
		dataSource.ClassName = "Item".As<ClassName>();
		dataSource.File = "data/items.json".As<RelativeFilePath>();

		generator = CodeGenerationTests.ConfigureGenerator(schema);
		generator.DataSource = dataSourceName.As<DataSourceName>();
		return schema;
	}

	private static Assembly CompileEditor()
	{
		Schema schema = CreateSchema(out SchemaCodeGenerator generator);
		return GeneratedSourceCompiler.Compile(new CSharpCodeGenerator().Generate(schema, generator));
	}

	private static object CreateEditor(Assembly assembly) =>
		Activator.CreateInstance(assembly.GetType("Generated.ItemsEditor", throwOnError: true)!)!;

	private static object Invoke(object target, string method, params object[] arguments) =>
		target.GetType().GetMethod(method, [.. arguments.Select(a => a.GetType())])!.Invoke(target, arguments)!;

	private static object? Get(object target, string property) =>
		target.GetType().GetProperty(property)!.GetValue(target);

	private static void Set(object target, string property, object value) =>
		target.GetType().GetProperty(property)!.SetValue(target, value);

	private static List<object> Records(object editor) =>
		[.. ((IEnumerable)Get(editor, "Records")!).Cast<object>()];

	private static IReadOnlyList<SchemaValidationIssue> Issues(object editor) =>
		(IReadOnlyList<SchemaValidationIssue>)Get(editor, "Issues")!;

	// ---- the binding ----------------------------------------------------------

	[TestMethod]
	public void TestAGeneratorNamingAnUnknownDataSourceIsAnError()
	{
		Schema schema = CreateSchema(out _, "Missing");

		SchemaValidationIssue issue = schema.Validate().Single(i => i.Message.Contains("unknown data source", StringComparison.Ordinal));
		Assert.AreEqual(SchemaValidationSeverity.Error, issue.Severity);
		Assert.AreEqual("CSharp", issue.Path);
	}

	[TestMethod]
	public void TestAGeneratorNamingAKnownDataSourceValidates()
	{
		Schema schema = CreateSchema(out _);
		Assert.IsFalse(schema.Validate().Any(i => i.Message.Contains("data source", StringComparison.Ordinal)));
	}

	[TestMethod]
	public void TestTheBindingRoundTripsAndIsOmittedWhenUnset()
	{
		Schema bound = CreateSchema(out _);
		string json = SchemaSerializer.Serialize(bound);
		StringAssert.Contains(json, "\"dataSource\": \"Items\"");

		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? reloaded));
		Assert.AreEqual("Items", reloaded!.CodeGenerators.Single().DataSource.ToString());

		Schema unbound = CreateSchema(out SchemaCodeGenerator generator);
		generator.DataSource = new();
		Assert.IsFalse(SchemaSerializer.Serialize(unbound).Contains("\"dataSource\"", StringComparison.Ordinal));
	}

	[TestMethod]
	public void TestNoEditorIsEmittedWithoutABinding()
	{
		Schema schema = CreateSchema(out SchemaCodeGenerator generator);
		generator.DataSource = new();

		IReadOnlyDictionary<string, string> files = new CSharpCodeGenerator().Generate(schema, generator);
		Assert.IsFalse(files.ContainsKey("ItemsEditor.g.cs"));
		Assert.IsFalse(files.ContainsKey("ItemRecord.g.cs"));
	}

	[TestMethod]
	public void TestABindingEmitsARecordAndAnEditorThatCompile()
	{
		Assembly assembly = CompileEditor();
		Type editor = assembly.GetType("Generated.ItemsEditor", throwOnError: true)!;
		Type record = assembly.GetType("Generated.ItemRecord", throwOnError: true)!;

		Assert.AreEqual(typeof(SchemaDataEditor<>).MakeGenericType(record), editor.BaseType);
		Assert.AreEqual("data/items.json", editor.GetField("RelativeFilePath")!.GetValue(null));
		Assert.AreEqual(typeof(int), record.GetProperty("Count")!.PropertyType);
		Assert.AreEqual(typeof(ColorRgb), record.GetProperty("Tint")!.PropertyType);
		Assert.AreEqual(assembly.GetType("Generated.Status"), record.GetProperty("Status")!.PropertyType);
	}

	// ---- accepts exactly what the validator accepts ---------------------------

	private static readonly string[] Payloads =
	[
		ConformingItems,
		"""{ "Id": "single", "Count": 2, "Weight": 1, "Status": "Active", "Tint": [0, 0, 0] }""",
		"""[ { "Id": "sword", "Count": "three", "Weight": 1, "Status": "Active", "Tint": [0, 0, 0] } ]""",
		"""[ { "Id": "sword", "Count": 3, "Weight": 1, "Status": "Broken", "Tint": [0, 0, 0] } ]""",
		"""[ { "Count": 3 } ]""",
		"""[ 42 ]""",
		"{ \"Id\": ",
		"// a comment\n[]",
		"""{ "Id": "a", "Id": "b" }""",
	];

	[TestMethod]
	public void TestTheEditorReportsExactlyWhatTheValidatorReports()
	{
		Assembly assembly = CompileEditor();
		Schema schema = CreateSchema(out _);
		SchemaClass item = schema.GetClass("Item".As<ClassName>())!;

		foreach (string payload in Payloads)
		{
			object editor = CreateEditor(assembly);
			Invoke(editor, "Parse", payload);

			string[] expected = [.. SchemaDataValidator.ValidateData(item, payload, "Items").Select(i => i.ToString())];
			string[] actual = [.. Issues(editor).Select(i => i.ToString())];
			CollectionAssert.AreEqual(expected, actual, payload);
			Assert.AreEqual(expected.Length == 0 || !expected.Any(e => e.StartsWith("Error", StringComparison.Ordinal)), Get(editor, "IsValid"), payload);
		}
	}

	[TestMethod]
	public void TestTheEditorStaysInStepWithTheValidatorAfterAnEdit()
	{
		Assembly assembly = CompileEditor();
		SchemaClass item = CreateSchema(out _).GetClass("Item".As<ClassName>())!;
		object editor = CreateEditor(assembly);
		Invoke(editor, "Parse", ConformingItems);

		// A record added and never filled in is missing its members, which the validator reports.
		_ = Invoke(editor, "Add");
		string json = (string)Invoke(editor, "ToJson");
		Assert.AreNotEqual(0, SchemaDataValidator.ValidateData(item, json, "Items").Count);
		CollectionAssert.AreEqual(
			SchemaDataValidator.ValidateData(item, json, "Items").Select(i => i.ToString()).ToArray(),
			Issues(editor).Select(i => i.ToString()).ToArray());
	}

	// ---- typed accessors ------------------------------------------------------

	[TestMethod]
	public void TestTheAccessorsReadTheRecords()
	{
		object editor = CreateEditor(CompileEditor());
		Invoke(editor, "Parse", ConformingItems);

		List<object> records = Records(editor);
		Assert.HasCount(2, records);
		Assert.AreEqual("sword", Get(records[0], "Id"));
		Assert.AreEqual(3, Get(records[0], "Count"));
		Assert.AreEqual(1.5f, Get(records[0], "Weight"));
		Assert.AreEqual("Active", Get(records[0], "Status")!.ToString());
		Assert.AreEqual(new ColorRgb(1, 0.5f, 0), Get(records[0], "Tint"));
		Assert.AreEqual("Retired", Get(records[1], "Status")!.ToString());
	}

	[TestMethod]
	public void TestAnEditSurvivesASaveAndKeepsUnmodelledMembers()
	{
		Assembly assembly = CompileEditor();
		object editor = CreateEditor(assembly);
		Invoke(editor, "Parse", ConformingItems);

		Assert.AreEqual(ConformingItems, Invoke(editor, "ToJson"), "Nothing edited, so the text is what was loaded.");

		object sword = Records(editor)[0];
		Set(sword, "Count", 7);
		Set(sword, "Status", Enum.Parse(sword.GetType().GetProperty("Status")!.PropertyType, "Retired"));
		Set(sword, "Tint", new ColorRgb(0.25f, 0.5f, 0.75f));

		string path = Path.Combine(workingDirectory, "items.json");
		Invoke(editor, "Save", path);

		object reloaded = CreateEditor(assembly);
		Invoke(reloaded, "Load", path);
		object first = Records(reloaded)[0];
		Assert.AreEqual(7, Get(first, "Count"));
		Assert.AreEqual("Retired", Get(first, "Status")!.ToString());
		Assert.AreEqual(new ColorRgb(0.25f, 0.5f, 0.75f), Get(first, "Tint"));
		Assert.AreEqual("kept", ((SchemaDataRecord)first).Json["Note"]!.GetValue<string>());
		Assert.IsTrue((bool)Get(reloaded, "IsValid")!);
	}

	[TestMethod]
	public void TestAnAddedRecordFilledInIsValidAndARemovedOneIsGone()
	{
		object editor = CreateEditor(CompileEditor());
		Invoke(editor, "Parse", """{ "Id": "single", "Count": 2, "Weight": 1, "Status": "Active", "Tint": [0, 0, 0] }""");

		object added = Invoke(editor, "Add");
		Set(added, "Id", "bow");
		Set(added, "Count", 1);
		Set(added, "Weight", 0.5f);
		Set(added, "Status", Enum.Parse(added.GetType().GetProperty("Status")!.PropertyType, "Active"));
		Set(added, "Tint", new ColorRgb(0, 1, 0));

		List<object> records = Records(editor);
		Assert.HasCount(2, records, "A single-object file becomes an array holding it and the new record.");
		Assert.IsTrue((bool)Get(editor, "IsValid")!);

		Assert.IsTrue((bool)Invoke(editor, "Remove", records[0]));
		Assert.AreEqual("bow", Get(Records(editor).Single(), "Id"));
	}

	[TestMethod]
	public void TestAnUnreadableFileListsNoRecordsAndRefusesEdits()
	{
		object editor = CreateEditor(CompileEditor());
		Invoke(editor, "Parse", """{ "Id": """);

		Assert.IsFalse((bool)Get(editor, "IsReadable")!);
		Assert.IsEmpty(Records(editor));
		TargetInvocationException thrown = Assert.ThrowsExactly<TargetInvocationException>(() => Invoke(editor, "Add"));
		Assert.IsInstanceOfType<InvalidOperationException>(thrown.InnerException);
	}

	// ---- SchemaDataValues -----------------------------------------------------

	[TestMethod]
	public void TestValuesMatchNamesCaseInsensitivelyAndKeepTheFilesSpelling()
	{
		JsonObject record = new() { ["count"] = 3 };
		Assert.AreEqual(3, SchemaDataValues.GetInt(record, "Count"));

		SchemaDataValues.SetInt(record, "Count", 4);
		Assert.AreEqual(4, record["count"]!.GetValue<int>());
		Assert.IsFalse(record.ContainsKey("Count"));
	}

	[TestMethod]
	public void TestAMissingOrMisshapenValueThrows()
	{
		JsonObject record = new() { ["Count"] = "three" };
		Assert.ThrowsExactly<InvalidOperationException>(() => SchemaDataValues.GetInt(record, "Count"));
		Assert.ThrowsExactly<InvalidOperationException>(() => SchemaDataValues.GetString(record, "Missing"));
	}

	[TestMethod]
	public void TestVectorsAndColoursRoundTrip()
	{
		JsonObject record = [];
		SchemaDataValues.SetVector3(record, "Position", new System.Numerics.Vector3(1, 2, 3));
		SchemaDataValues.SetColorRgba(record, "Tint", new ColorRgba(0.1f, 0.2f, 0.3f, 0.4f));

		Assert.AreEqual(new System.Numerics.Vector3(1, 2, 3), SchemaDataValues.GetVector3(record, "Position"));
		Assert.AreEqual(new ColorRgba(0.1f, 0.2f, 0.3f, 0.4f), SchemaDataValues.GetColorRgba(record, "Tint"));
		Assert.ThrowsExactly<InvalidOperationException>(() => SchemaDataValues.GetVector4(record, "Position"));
	}
}
