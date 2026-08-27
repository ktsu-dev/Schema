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
}
