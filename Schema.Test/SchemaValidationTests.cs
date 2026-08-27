// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Collections.ObjectModel;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SchemaTypes = Models.Types;

[TestClass]
public class SchemaValidationTests
{
	private static Schema CreateValidSchema()
	{
		Schema schema = new();

		SchemaEnum? statusEnum = schema.AddEnum("Status".As<EnumName>());
		statusEnum?.TryAddValue("Active".As<EnumValueName>());
		statusEnum?.TryAddValue("Inactive".As<EnumValueName>());

		SchemaClass? itemClass = schema.AddClass("Item".As<ClassName>());
		SchemaMember? itemId = itemClass?.AddMember("Id".As<MemberName>());
		itemId?.SetType(new SchemaTypes.Int());

		SchemaClass? userClass = schema.AddClass("User".As<ClassName>());
		SchemaMember? status = userClass?.AddMember("Status".As<MemberName>());
		status?.SetType(new SchemaTypes.Enum() { EnumName = "Status".As<EnumName>() });
		SchemaMember? items = userClass?.AddMember("Items".As<MemberName>());
		items?.SetType(new SchemaTypes.Array()
		{
			ElementType = new SchemaTypes.Object() { ClassName = "Item".As<ClassName>() },
			Container = "map".As<ContainerName>(),
			Key = "Id".As<MemberName>(),
		});

		return schema;
	}

	[TestMethod]
	public void TestValidSchemaHasNoIssues()
	{
		Schema schema = CreateValidSchema();
		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
	}

	[TestMethod]
	public void TestVectorAndColorMembersHaveNoIssues()
	{
		// Before issue #107 these types derived from Object, so the validator saw an empty
		// inherited ClassName and reported "Object type does not specify a class name."
		Schema schema = new();
		SchemaClass? playerClass = schema.AddClass("Player".As<ClassName>());
		playerClass?.AddMember("Position".As<MemberName>())?.SetType(new SchemaTypes.Vector3());
		playerClass?.AddMember("Tint".As<MemberName>())?.SetType(new SchemaTypes.ColorRGB());
		playerClass?.AddMember("Velocity".As<MemberName>())?.SetType(new SchemaTypes.Vector2());
		playerClass?.AddMember("Rotation".As<MemberName>())?.SetType(new SchemaTypes.Vector4());
		playerClass?.AddMember("Overlay".As<MemberName>())?.SetType(new SchemaTypes.ColorRGBA());

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
	}

	[TestMethod]
	public void TestIssuesCarryTheElementTheyWereReportedAgainst()
	{
		Schema schema = CreateValidSchema();
		schema.GetEnum("Status".As<EnumName>())?.TryRemove();

		SchemaValidationIssue issue = schema.Validate().First(i => i.Path == "User.Status");

		// A tool can navigate straight to the offending member rather than parsing the path.
		Assert.IsInstanceOfType<SchemaMember>(issue.Element);
		SchemaMember member = (SchemaMember)issue.Element!;
		Assert.AreEqual("Status", member.Name.ToString());
		Assert.AreEqual("User", member.ParentClass?.Name.ToString());
	}

	[TestMethod]
	public void TestDataSourceAndCodeGeneratorIssuesCarryTheirElement()
	{
		Schema schema = new();
		schema.AddDataSource("Users".As<DataSourceName>());
		schema.AddCodeGenerator("CSharp".As<CodeGeneratorName>());

		Collection<SchemaValidationIssue> issues = schema.Validate();

		Assert.IsTrue(issues.Where(i => i.Path == "Users").All(i => i.Element is DataSource));
		Assert.IsTrue(issues.Where(i => i.Path == "CSharp").All(i => i.Element is SchemaCodeGenerator));
	}

	[TestMethod]
	public void TestDuplicateNameIssuesCarryNoElement()
	{
		// A duplicate names no single element, so there is nothing to navigate to.
		const string json = """
		{
		  "classes": [
		    { "name": "User", "members": [] },
		    { "name": "User", "members": [] }
		  ],
		  "enums": [],
		  "dataSources": [],
		  "codeGenerators": []
		}
		""";

		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? schema));
		Assert.IsNotNull(schema);

		SchemaValidationIssue duplicate = schema.Validate().First(i => i.Message.Contains("Duplicate"));
		Assert.IsNull(duplicate.Element);
	}

	[TestMethod]
	public void TestEmptySchemaHasNoIssues()
	{
		Schema schema = new();
		Assert.AreEqual(0, schema.Validate().Count);
	}

	[TestMethod]
	public void TestDanglingEnumReferenceIsError()
	{
		Schema schema = CreateValidSchema();
		schema.GetEnum("Status".As<EnumName>())?.TryRemove();

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Error &&
			i.Path == "User.Status" &&
			i.Message.Contains("unknown enum")));
	}

	[TestMethod]
	public void TestDanglingClassReferenceIsError()
	{
		Schema schema = CreateValidSchema();
		schema.GetClass("Item".As<ClassName>())?.TryRemove();

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Error &&
			i.Path == "User.Items" &&
			i.Message.Contains("unknown class")));
	}

	[TestMethod]
	public void TestEmptyEnumNameIsError()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("Holder".As<ClassName>());
		SchemaMember? member = schemaClass?.AddMember("Value".As<MemberName>());
		member?.SetType(new SchemaTypes.Enum());

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Error &&
			i.Path == "Holder.Value" &&
			i.Message.Contains("does not specify an enum name")));
	}

	[TestMethod]
	public void TestArrayKeyOnNonObjectElementIsError()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("Holder".As<ClassName>());
		SchemaMember? member = schemaClass?.AddMember("Values".As<MemberName>());
		member?.SetType(new SchemaTypes.Array()
		{
			ElementType = new SchemaTypes.Int(),
			Key = "Id".As<MemberName>(),
		});

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Error &&
			i.Path == "Holder.Values" &&
			i.Message.Contains("element type is not an object")));
	}

	[TestMethod]
	public void TestArrayKeyMissingFromElementClassIsError()
	{
		Schema schema = CreateValidSchema();
		SchemaClass? userClass = schema.GetClass("User".As<ClassName>());
		Assert.IsNotNull(userClass);
		Assert.IsTrue(userClass.TryGetMember("Items".As<MemberName>(), out SchemaMember? items));
		((SchemaTypes.Array)items!.Type).Key = "Missing".As<MemberName>();

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Error &&
			i.Path == "User.Items" &&
			i.Message.Contains("is not a member of class")));
	}

	[TestMethod]
	public void TestArrayKeyOnNonPrimitiveMemberIsError()
	{
		Schema schema = CreateValidSchema();
		SchemaClass? itemClass = schema.GetClass("Item".As<ClassName>());
		SchemaMember? nested = itemClass?.AddMember("Nested".As<MemberName>());
		nested?.SetType(new SchemaTypes.Object() { ClassName = "Item".As<ClassName>() });

		SchemaClass? userClass = schema.GetClass("User".As<ClassName>());
		Assert.IsNotNull(userClass);
		Assert.IsTrue(userClass.TryGetMember("Items".As<MemberName>(), out SchemaMember? items));
		((SchemaTypes.Array)items!.Type).Key = "Nested".As<MemberName>();

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Error &&
			i.Path == "User.Items" &&
			i.Message.Contains("must be a primitive type")));
	}

	[TestMethod]
	public void TestDataSourceWithUnknownClassIsError()
	{
		Schema schema = new();
		DataSource? dataSource = schema.AddDataSource("Users".As<DataSourceName>());
		Assert.IsNotNull(dataSource);
		dataSource.ClassName = "Missing".As<ClassName>();
		dataSource.File = "users.json".As<RelativeFilePath>();

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Error &&
			i.Path == "Users" &&
			i.Message.Contains("unknown class")));
	}

	[TestMethod]
	public void TestEmptyClassNameIsError()
	{
		Schema schema = new();
		schema.AddClass(string.Empty.As<ClassName>());

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Error &&
			i.Path == "(unnamed)" &&
			i.Message.Contains("Class has an empty name")));
	}

	[TestMethod]
	public void TestEmptyEnumDeclarationNameIsError()
	{
		Schema schema = new();
		schema.AddEnum(string.Empty.As<EnumName>());

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Error &&
			i.Path == "(unnamed)" &&
			i.Message.Contains("Enum has an empty name")));
	}

	[TestMethod]
	public void TestEmptyMemberNameIsError()
	{
		Schema schema = new();
		SchemaClass? userClass = schema.AddClass("User".As<ClassName>());
		SchemaMember? member = userClass?.AddMember(string.Empty.As<MemberName>());
		member?.SetType(new SchemaTypes.Int());

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Error &&
			i.Path == "User.(unnamed)" &&
			i.Message.Contains("Member has an empty name")));
	}

	[TestMethod]
	public void TestEmptyEnumValueNameIsError()
	{
		// TryAddValue rejects an empty value, so this state is only reachable by loading a
		// hand-edited schema file - which is exactly what the validator is there to catch.
		const string json = """
		{
		  "classes": [],
		  "enums": [ { "name": "Status", "values": [ "Active", "" ] } ],
		  "dataSources": [],
		  "codeGenerators": []
		}
		""";

		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? schema));
		Assert.IsNotNull(schema);

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(
			issues.Any(i =>
				i.Severity == SchemaValidationSeverity.Error &&
				i.Path == "Status.(unnamed)" &&
				i.Message.Contains("Enum value has an empty name")),
			string.Join("; ", issues));
	}

	[TestMethod]
	public void TestMemberWithoutATypeIsWarning()
	{
		Schema schema = new();
		SchemaClass? userClass = schema.AddClass("User".As<ClassName>());
		userClass?.AddMember("Name".As<MemberName>());

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Warning &&
			i.Path == "User.Name" &&
			i.Message.Contains("does not have a type set")));
	}

	[TestMethod]
	public void TestArrayWithoutAContainerIsWarning()
	{
		Schema schema = new();
		SchemaClass? userClass = schema.AddClass("User".As<ClassName>());
		SchemaMember? tags = userClass?.AddMember("Tags".As<MemberName>());
		tags?.SetType(new SchemaTypes.Array() { ElementType = new SchemaTypes.String() });

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Warning &&
			i.Path == "User.Tags" &&
			i.Message.Contains("does not specify a container")));
	}

	[TestMethod]
	public void TestArrayWithUnrecognizedContainerIsWarning()
	{
		Schema schema = new();
		SchemaClass? userClass = schema.AddClass("User".As<ClassName>());
		SchemaMember? tags = userClass?.AddMember("Tags".As<MemberName>());
		tags?.SetType(new SchemaTypes.Array()
		{
			ElementType = new SchemaTypes.String(),
			Container = "linked-list".As<ContainerName>(),
		});

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Warning &&
			i.Path == "User.Tags" &&
			i.Message.Contains("linked-list")));
	}

	[TestMethod]
	public void TestKnownContainersValidateClean()
	{
		Schema schema = new();
		SchemaClass? userClass = schema.AddClass("User".As<ClassName>());
		SchemaMember? tags = userClass?.AddMember("Tags".As<MemberName>());
		tags?.SetType(new SchemaTypes.Array()
		{
			ElementType = new SchemaTypes.String(),
			Container = SchemaTypes.Array.VectorContainer.As<ContainerName>(),
		});

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.AreEqual(0, issues.Count, string.Join("; ", issues));
	}

	[TestMethod]
	public void TestMapWithoutAKeyIsError()
	{
		Schema schema = new();
		SchemaClass? itemClass = schema.AddClass("Item".As<ClassName>());
		itemClass?.AddMember("Id".As<MemberName>())?.SetType(new SchemaTypes.Int());

		SchemaClass? userClass = schema.AddClass("User".As<ClassName>());
		SchemaMember? items = userClass?.AddMember("Items".As<MemberName>());
		items?.SetType(new SchemaTypes.Array()
		{
			ElementType = new SchemaTypes.Object() { ClassName = "Item".As<ClassName>() },
			Container = SchemaTypes.Array.MapContainer.As<ContainerName>(),
		});

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Error &&
			i.Path == "User.Items" &&
			i.Message.Contains("does not specify a key")));
	}

	[TestMethod]
	public void TestUnconfiguredDataSourceIsWarning()
	{
		Schema schema = new();
		schema.AddDataSource("Users".As<DataSourceName>());

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.AreEqual(2, issues.Count);
		Assert.IsTrue(issues.All(i => i.Severity == SchemaValidationSeverity.Warning));
	}

	[TestMethod]
	public void TestCodeGeneratorWithoutOutputPathIsWarning()
	{
		Schema schema = new();
		schema.AddCodeGenerator("CSharp".As<CodeGeneratorName>());

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Warning &&
			i.Path == "CSharp" &&
			i.Message.Contains("output path")));
	}

	[TestMethod]
	public void TestDuplicateClassNamesFromJsonIsError()
	{
		string json = """
			{
				"classes": [
					{ "name": "User", "description": "", "members": [] },
					{ "name": "User", "description": "", "members": [] }
				],
				"enums": [],
				"dataSources": [],
				"codeGenerators": []
			}
			""";

		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? schema));
		Assert.IsNotNull(schema);

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(issues.Any(i =>
			i.Severity == SchemaValidationSeverity.Error &&
			i.Message.Contains("Duplicate class name 'User'")));
	}

	[TestMethod]
	public void TestIssueToStringContainsSeverityPathAndMessage()
	{
		SchemaValidationIssue issue = new()
		{
			Severity = SchemaValidationSeverity.Error,
			Path = "User.Status",
			Message = "Broken reference.",
		};

		string text = issue.ToString();
		Assert.IsTrue(text.Contains("Error"));
		Assert.IsTrue(text.Contains("User.Status"));
		Assert.IsTrue(text.Contains("Broken reference."));
	}
}
