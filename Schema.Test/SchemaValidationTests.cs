// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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
