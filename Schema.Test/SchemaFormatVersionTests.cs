// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SchemaTypes = Models.Types;

/// <summary>
/// Covers the .schema.json format version: that it is written, that older files still load, and
/// that a file from the future is refused in a way a caller can act on.
/// </summary>
[TestClass]
public class SchemaFormatVersionTests
{
	private static string BuildJson(string? formatVersionProperty) =>
		$$"""
		{
		  {{formatVersionProperty}}
		  "classes": [
		    {
		      "name": "User",
		      "description": "",
		      "members": [
		        { "name": "Name", "description": "", "type": { "TypeName": "String" } }
		      ]
		    }
		  ],
		  "enums": [],
		  "dataSources": [],
		  "codeGenerators": []
		}
		""";

	[TestMethod]
	public void TestSavedFilesCarryTheFormatVersion()
	{
		Schema schema = new();
		schema.AddClass("User".As<ClassName>());

		string json = SchemaSerializer.Serialize(schema);

		Assert.IsTrue(json.Contains("\"formatVersion\"", StringComparison.Ordinal), json);
		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? reloaded));
		Assert.AreEqual(Schema.CurrentFormatVersion, reloaded!.FormatVersion);
	}

	[TestMethod]
	public void TestANewSchemaIsAtTheCurrentVersion()
	{
		Schema schema = new();
		Assert.AreEqual(Schema.CurrentFormatVersion, schema.FormatVersion);
	}

	[TestMethod]
	public void TestUnversionedFilesStillLoadAndAreMigrated()
	{
		SchemaLoadResult result = SchemaSerializer.Load(BuildJson(formatVersionProperty: null));

		Assert.AreEqual(SchemaLoadStatus.Success, result.Status, result.Message);
		Assert.AreEqual(Schema.PreVersioningFormatVersion, result.FileFormatVersion);
		Assert.IsNotNull(result.Schema);
		Assert.AreEqual(Schema.CurrentFormatVersion, result.Schema.FormatVersion, "The loaded schema is brought up to date.");
		Assert.IsNotNull(result.Schema.GetClass("User".As<ClassName>()));
		Assert.IsTrue(result.Message.Contains("Migrated", StringComparison.Ordinal), result.Message);
	}

	[TestMethod]
	public void TestCurrentVersionFilesLoad()
	{
		SchemaLoadResult result = SchemaSerializer.Load(BuildJson($"\"formatVersion\": {Schema.CurrentFormatVersion},"));

		Assert.AreEqual(SchemaLoadStatus.Success, result.Status, result.Message);
		Assert.AreEqual(Schema.CurrentFormatVersion, result.FileFormatVersion);
	}

	[TestMethod]
	public void TestFutureVersionIsRefusedDistinguishably()
	{
		int futureVersion = Schema.CurrentFormatVersion + 1;
		SchemaLoadResult result = SchemaSerializer.Load(BuildJson($"\"formatVersion\": {futureVersion},"));

		// The point of the whole exercise: this is not reported as a corrupt file.
		Assert.AreEqual(SchemaLoadStatus.UnsupportedFutureVersion, result.Status);
		Assert.AreNotEqual(SchemaLoadStatus.InvalidJson, result.Status);
		Assert.IsNull(result.Schema);
		Assert.AreEqual(futureVersion, result.FileFormatVersion);

		// Actionable: it names both versions and says what to do.
		Assert.IsTrue(result.Message.Contains(futureVersion.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal), result.Message);
		Assert.IsTrue(result.Message.Contains("Update", StringComparison.Ordinal), result.Message);
	}

	[TestMethod]
	public void TestCorruptJsonIsReportedAsInvalid()
	{
		SchemaLoadResult result = SchemaSerializer.Load("{ this is not json");

		Assert.AreEqual(SchemaLoadStatus.InvalidJson, result.Status);
		Assert.IsNull(result.Schema);
	}

	[TestMethod]
	public void TestEmptyInputIsReportedAsInvalid()
	{
		SchemaLoadResult result = SchemaSerializer.Load(string.Empty);

		Assert.AreEqual(SchemaLoadStatus.InvalidJson, result.Status);
		Assert.IsNull(result.Schema);
	}

	[TestMethod]
	public void TestNonObjectRootIsReportedAsInvalid()
	{
		SchemaLoadResult result = SchemaSerializer.Load("[]");

		Assert.AreEqual(SchemaLoadStatus.InvalidJson, result.Status);
		Assert.IsNull(result.Schema);
	}

	[TestMethod]
	public void TestNonIntegerFormatVersionIsReportedAsInvalid()
	{
		SchemaLoadResult result = SchemaSerializer.Load(BuildJson("\"formatVersion\": \"one\","));

		Assert.AreEqual(SchemaLoadStatus.InvalidJson, result.Status);
		Assert.IsNull(result.Schema);
	}

	[TestMethod]
	public void TestMigratedSchemaSavesAtTheCurrentVersion()
	{
		SchemaLoadResult loaded = SchemaSerializer.Load(BuildJson(formatVersionProperty: null));
		Assert.IsTrue(loaded.IsSuccess, loaded.Message);

		string resaved = SchemaSerializer.Serialize(loaded.Schema!);
		SchemaLoadResult reloaded = SchemaSerializer.Load(resaved);

		Assert.AreEqual(Schema.CurrentFormatVersion, reloaded.FileFormatVersion);
	}

	[TestMethod]
	public void TestTryDeserializeStillReportsFailureForAFutureVersion()
	{
		string json = BuildJson($"\"formatVersion\": {Schema.CurrentFormatVersion + 1},");

		Assert.IsFalse(SchemaSerializer.TryDeserialize(json, out Schema? schema));
		Assert.IsNull(schema);
	}

	[TestMethod]
	public void TestDocumentedRootPropertiesAreAllPresent()
	{
		// docs/schema-format.md documents this exact set; if the serializer's root changes, this
		// fails and the doc gets updated with it.
		Schema schema = new();
		schema.AddClass("User".As<ClassName>())?.AddMember("Name".As<MemberName>())?.SetType(new SchemaTypes.String());

		string json = SchemaSerializer.Serialize(schema);

		foreach (string property in new[] { "formatVersion", "classes", "enums", "dataSources", "codeGenerators" })
		{
			Assert.IsTrue(json.Contains($"\"{property}\"", StringComparison.Ordinal), $"Root property '{property}' is missing from {json}");
		}
	}

	/// <summary>
	/// A file whose root the model does not read is refused rather than loaded as an empty schema.
	/// </summary>
	/// <remarks>
	/// Loading the legacy files reported "Migrated from format version 0" with nothing in the
	/// schema, so validation passed and saving from the editor replaced the file with an empty one.
	/// </remarks>
	[TestMethod]
	public void TestALegacyFileIsRefusedRatherThanLoadedEmpty()
	{
		string path = Path.Join(LegacySchemaMigrationTests.SamplesDirectory, "legacy", "dungeoneer", "weapon.schema.json");

		SchemaLoadResult result = SchemaSerializer.Load(File.ReadAllText(path));

		Assert.AreEqual(SchemaLoadStatus.InvalidJson, result.Status, result.Message);
		Assert.IsNull(result.Schema);
		Assert.Contains("'Classes'", result.Message);
		Assert.Contains("'classes'", result.Message);
	}

	/// <summary>
	/// No committed legacy file loads as a success, so none of them can be mistaken for an empty schema.
	/// </summary>
	[TestMethod]
	public void TestNoLegacyFileLoads()
	{
		string[] files = Directory.GetFiles(Path.Join(LegacySchemaMigrationTests.SamplesDirectory, "legacy"), "*.schema.json", SearchOption.AllDirectories);
		Assert.IsNotEmpty(files);

		foreach (string file in files)
		{
			Assert.IsFalse(SchemaSerializer.Load(File.ReadAllText(file)).IsSuccess, file);
		}
	}

	/// <summary>
	/// A root property with no camelCase counterpart is named without a casing hint.
	/// </summary>
	[TestMethod]
	public void TestAnUnknownRootPropertyIsNamed()
	{
		SchemaLoadResult result = SchemaSerializer.Load("""{ "formatVersion": 1, "widgets": [] }""");

		Assert.AreEqual(SchemaLoadStatus.InvalidJson, result.Status);
		Assert.Contains("'widgets'", result.Message);
		Assert.DoesNotContain("camelCase", result.Message);
	}

	/// <summary>
	/// A file from a newer version is still told apart from one this build cannot read, even when
	/// the newer version added a root property this build has never heard of.
	/// </summary>
	[TestMethod]
	public void TestAFutureFileWithANewRootPropertyIsStillReportedAsFuture()
	{
		SchemaLoadResult result = SchemaSerializer.Load($$"""{ "formatVersion": {{Schema.CurrentFormatVersion + 1}}, "somethingNew": [] }""");

		Assert.AreEqual(SchemaLoadStatus.UnsupportedFutureVersion, result.Status);
	}

	/// <summary>
	/// Every committed modern sample still loads, so the root check refuses nothing the format allows.
	/// </summary>
	[TestMethod]
	public void TestEveryModernSampleStillLoads()
	{
		foreach (string file in Directory.GetFiles(LegacySchemaMigrationTests.SamplesDirectory, "*.schema.json"))
		{
			SchemaLoadResult result = SchemaSerializer.Load(File.ReadAllText(file));
			Assert.IsTrue(result.IsSuccess, $"{file}: {result.Message}");
		}
	}

	/// <summary>
	/// A type discriminator written after the type's other properties still loads.
	/// </summary>
	[TestMethod]
	public void TestADiscriminatorThatIsNotFirstStillLoads()
	{
		const string json = """
			{
			  "classes": [
			    {
			      "name": "Body",
			      "description": "",
			      "members": [
			        {
			          "name": "mass",
			          "description": "",
			          "type": { "storage": { "TypeName": "Double" }, "quantityName": "Mass", "TypeName": "Quantity" },
			          "defaultValue": { "value": 1.0, "DefaultKind": "NumberDefault" }
			        }
			      ]
			    }
			  ]
			}
			""";

		SchemaLoadResult result = SchemaSerializer.Load(json);

		Assert.AreEqual(SchemaLoadStatus.Success, result.Status, result.Message);
		SchemaMember? member = result.Schema!.GetClass("Body".As<ClassName>())?.Members[0];
		Assert.IsNotNull(member);
		Assert.IsInstanceOfType<SchemaTypes.Quantity>(member.Type);
		Assert.IsNotNull(member.DefaultValue);
	}

	/// <summary>
	/// A type with no discriminator the serializer recognises is a failed load rather than an exception.
	/// </summary>
	[TestMethod]
	[DataRow("""{ "typeName": "Int" }""", DisplayName = "Discriminator in camelCase")]
	[DataRow("""{}""", DisplayName = "No discriminator")]
	public void TestATypeWithNoRecognisedDiscriminatorIsReportedAsInvalid(string type)
	{
		string json = $$"""
			{
			  "classes": [
			    { "name": "User", "description": "", "members": [ { "name": "Name", "description": "", "type": {{type}} } ] }
			  ]
			}
			""";

		SchemaLoadResult result = SchemaSerializer.Load(json);

		Assert.AreEqual(SchemaLoadStatus.InvalidJson, result.Status, result.Message);
		Assert.IsNull(result.Schema);
		Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message));
	}

	/// <summary>
	/// A null element in any list is refused by its path rather than throwing out of the load.
	/// </summary>
	[TestMethod]
	[DataRow("""{ "classes": [null] }""", "$.classes[0]", DisplayName = "Null class")]
	[DataRow("""{ "codeGenerators": [null] }""", "$.codeGenerators[0]", DisplayName = "Null code generator")]
	[DataRow("""{ "classes": [ { "name": "A", "members": [ null ] } ] }""", "$.classes[0].members[0]", DisplayName = "Null member")]
	[DataRow("""{ "enums": [ { "name": "E", "values": [ "One", null ] } ] }""", "$.enums[0].values[1]", DisplayName = "Null enum value")]
	public void TestANullElementIsReportedByItsPath(string json, string path)
	{
		SchemaLoadResult result = SchemaSerializer.Load(json);

		Assert.AreEqual(SchemaLoadStatus.InvalidJson, result.Status, result.Message);
		Assert.IsNull(result.Schema);
		Assert.Contains(path, result.Message);
	}

	/// <summary>
	/// A null property reads as an omitted one, so the load succeeds and validation, which must not
	/// throw, says what is missing.
	/// </summary>
	[TestMethod]
	[DataRow("""{ "classes": null }""", DisplayName = "Null classes")]
	[DataRow("""{ "enums": null, "interfaces": null, "dataSources": null, "codeGenerators": null }""", DisplayName = "Null collections")]
	[DataRow("""{ "classes": [ { "name": "A", "members": null } ] }""", DisplayName = "Null members")]
	[DataRow("""{ "classes": [ { "name": null } ] }""", DisplayName = "Null class name")]
	[DataRow("""{ "classes": [ { "name": "A", "members": [ { "name": "x", "type": null } ] } ] }""", DisplayName = "Null member type")]
	[DataRow("""{ "classes": [ { "name": "A", "members": [ { "name": null, "type": { "TypeName": "Int" } } ] } ] }""", DisplayName = "Null member name")]
	[DataRow("""{ "interfaces": [ { "name": "I", "functions": [ { "name": "F", "parameters": null, "returnType": null } ] } ] }""", DisplayName = "Null function parts")]
	public void TestANullPropertyLoadsAsIfOmitted(string json)
	{
		SchemaLoadResult result = SchemaSerializer.Load(json);

		Assert.AreEqual(SchemaLoadStatus.Success, result.Status, result.Message);
		Assert.IsNotNull(result.Schema);
		_ = result.Schema.Validate();
	}

	/// <summary>
	/// A null name is reported by validation the same way an omitted one is.
	/// </summary>
	[TestMethod]
	public void TestANullClassNameIsAValidationError()
	{
		SchemaLoadResult result = SchemaSerializer.Load("""{ "classes": [ { "name": null } ] }""");

		Assert.IsTrue(result.IsSuccess, result.Message);
		Assert.IsTrue(result.Schema!.Validate().Any(issue => issue.Message.Contains("empty name", StringComparison.Ordinal)));
	}
}
