// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SchemaTypes = Models.Types;

/// <summary>
/// Covers renaming schema elements and the reference fixups each rename cascades.
/// </summary>
[TestClass]
public class SchemaRenameTests
{
	/// <summary>
	/// Builds a schema where every kind of reference to a class, an enum and a member is present:
	/// an object-typed member, an array of objects keyed by a member, a data source, and an
	/// enum-typed member.
	/// </summary>
	private static Schema CreateReferencingSchema()
	{
		Schema schema = new();

		SchemaEnum? status = schema.AddEnum("Status".As<EnumName>());
		status?.TryAddValue("Active".As<EnumValueName>());

		SchemaClass? item = schema.AddClass("Item".As<ClassName>());
		item?.AddMember("Id".As<MemberName>())?.SetType(new SchemaTypes.Int());

		SchemaClass? user = schema.AddClass("User".As<ClassName>());
		user?.AddMember("Status".As<MemberName>())?.SetType(new SchemaTypes.Enum() { EnumName = "Status".As<EnumName>() });
		user?.AddMember("Favourite".As<MemberName>())?.SetType(new SchemaTypes.Object() { ClassName = "Item".As<ClassName>() });
		user?.AddMember("Items".As<MemberName>())?.SetType(new SchemaTypes.Array()
		{
			ElementType = new SchemaTypes.Object() { ClassName = "Item".As<ClassName>() },
			Container = SchemaTypes.Array.MapContainer.As<ContainerName>(),
			Key = "Id".As<MemberName>(),
		});

		DataSource? dataSource = schema.AddDataSource("Items".As<DataSourceName>());
		if (dataSource is not null)
		{
			dataSource.ClassName = "Item".As<ClassName>();

			// Set so the fixture validates clean and the tests can assert on the issue count.
			dataSource.File = "items.json".As<RelativeFilePath>();
		}

		return schema;
	}

	[TestMethod]
	public void TestRenameClassCascadesToEveryReference()
	{
		Schema schema = CreateReferencingSchema();
		SchemaClass item = schema.GetClass("Item".As<ClassName>())!;

		Assert.IsTrue(schema.TryRenameClass(item, "Product".As<ClassName>()));

		Assert.AreEqual("Product", item.Name.ToString());
		Assert.IsNotNull(schema.GetClass("Product".As<ClassName>()));

		SchemaClass user = schema.GetClass("User".As<ClassName>())!;
		SchemaTypes.Object favourite = (SchemaTypes.Object)user.GetMember("Favourite".As<MemberName>())!.Type;
		Assert.AreEqual("Product", favourite.ClassName.ToString());

		SchemaTypes.Array items = (SchemaTypes.Array)user.GetMember("Items".As<MemberName>())!.Type;
		Assert.AreEqual("Product", ((SchemaTypes.Object)items.ElementType).ClassName.ToString());

		Assert.AreEqual("Product", schema.GetDataSource("Items".As<DataSourceName>())!.ClassName.ToString());

		// The whole point of cascading: the schema is still referentially intact.
		Assert.AreEqual(0, schema.Validate().Count, string.Join("; ", schema.Validate()));
	}

	[TestMethod]
	public void TestRenameEnumCascadesToTypedMembers()
	{
		Schema schema = CreateReferencingSchema();
		SchemaEnum status = schema.GetEnum("Status".As<EnumName>())!;

		Assert.IsTrue(schema.TryRenameEnum(status, "State".As<EnumName>()));

		SchemaClass user = schema.GetClass("User".As<ClassName>())!;
		SchemaTypes.Enum memberType = (SchemaTypes.Enum)user.GetMember("Status".As<MemberName>())!.Type;
		Assert.AreEqual("State", memberType.EnumName.ToString());
		Assert.AreEqual(0, schema.Validate().Count, string.Join("; ", schema.Validate()));
	}

	[TestMethod]
	public void TestRenameMemberCascadesToArrayKeys()
	{
		Schema schema = CreateReferencingSchema();
		SchemaClass item = schema.GetClass("Item".As<ClassName>())!;
		SchemaMember id = item.GetMember("Id".As<MemberName>())!;

		Assert.IsTrue(item.TryRenameMember(id, "Identifier".As<MemberName>()));

		SchemaClass user = schema.GetClass("User".As<ClassName>())!;
		SchemaTypes.Array items = (SchemaTypes.Array)user.GetMember("Items".As<MemberName>())!.Type;
		Assert.AreEqual("Identifier", items.Key.ToString());
		Assert.AreEqual(0, schema.Validate().Count, string.Join("; ", schema.Validate()));
	}

	[TestMethod]
	public void TestRenameMemberLeavesOtherClassesKeysAlone()
	{
		Schema schema = CreateReferencingSchema();

		// A second class with a same-named member must not be caught by the cascade.
		SchemaClass? other = schema.AddClass("Order".As<ClassName>());
		other?.AddMember("Id".As<MemberName>())?.SetType(new SchemaTypes.Int());
		SchemaClass user = schema.GetClass("User".As<ClassName>())!;
		user.AddMember("Orders".As<MemberName>())?.SetType(new SchemaTypes.Array()
		{
			ElementType = new SchemaTypes.Object() { ClassName = "Order".As<ClassName>() },
			Container = SchemaTypes.Array.MapContainer.As<ContainerName>(),
			Key = "Id".As<MemberName>(),
		});

		SchemaClass item = schema.GetClass("Item".As<ClassName>())!;
		Assert.IsTrue(item.TryRenameMember(item.GetMember("Id".As<MemberName>())!, "Identifier".As<MemberName>()));

		SchemaTypes.Array orders = (SchemaTypes.Array)user.GetMember("Orders".As<MemberName>())!.Type;
		Assert.AreEqual("Id", orders.Key.ToString(), "Order's key must be untouched by Item's rename.");
	}

	[TestMethod]
	public void TestRenameToACollidingNameIsRejected()
	{
		Schema schema = CreateReferencingSchema();
		SchemaClass item = schema.GetClass("Item".As<ClassName>())!;

		Assert.IsFalse(schema.TryRenameClass(item, "User".As<ClassName>()));
		Assert.AreEqual("Item", item.Name.ToString(), "A rejected rename must leave the name untouched.");

		SchemaEnum status = schema.GetEnum("Status".As<EnumName>())!;
		schema.AddEnum("Other".As<EnumName>());
		Assert.IsFalse(schema.TryRenameEnum(status, "Other".As<EnumName>()));
		Assert.AreEqual("Status", status.Name.ToString());

		SchemaClass user = schema.GetClass("User".As<ClassName>())!;
		SchemaMember favourite = user.GetMember("Favourite".As<MemberName>())!;
		Assert.IsFalse(user.TryRenameMember(favourite, "Items".As<MemberName>()));
		Assert.AreEqual("Favourite", favourite.Name.ToString());
	}

	[TestMethod]
	public void TestRenameToAnEmptyNameIsRejected()
	{
		Schema schema = CreateReferencingSchema();
		SchemaClass item = schema.GetClass("Item".As<ClassName>())!;

		Assert.IsFalse(schema.TryRenameClass(item, string.Empty.As<ClassName>()));
		Assert.AreEqual("Item", item.Name.ToString());
	}

	[TestMethod]
	public void TestRenameToTheSameNameSucceedsAsANoOp()
	{
		Schema schema = CreateReferencingSchema();
		SchemaClass item = schema.GetClass("Item".As<ClassName>())!;

		Assert.IsTrue(schema.TryRenameClass(item, "Item".As<ClassName>()));
		Assert.AreEqual("Item", item.Name.ToString());
	}

	[TestMethod]
	public void TestRenameOfAnElementNotInTheSchemaIsRejected()
	{
		Schema schema = CreateReferencingSchema();
		Schema other = new();
		SchemaClass stranger = other.AddClass("Stranger".As<ClassName>())!;

		Assert.IsFalse(schema.TryRenameClass(stranger, "Renamed".As<ClassName>()));
		Assert.AreEqual("Stranger", stranger.Name.ToString());
	}

	[TestMethod]
	public void TestRenameDataSourceAndCodeGenerator()
	{
		Schema schema = CreateReferencingSchema();
		DataSource dataSource = schema.GetDataSource("Items".As<DataSourceName>())!;
		Assert.IsTrue(schema.TryRenameDataSource(dataSource, "Catalogue".As<DataSourceName>()));
		Assert.AreEqual("Catalogue", dataSource.Name.ToString());

		SchemaCodeGenerator generator = schema.AddCodeGenerator("CSharp".As<CodeGeneratorName>())!;
		schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>());
		Assert.IsFalse(schema.TryRenameCodeGenerator(generator, "Cpp".As<CodeGeneratorName>()));
		Assert.IsTrue(schema.TryRenameCodeGenerator(generator, "CSharpGen".As<CodeGeneratorName>()));
		Assert.AreEqual("CSharpGen", generator.Name.ToString());
	}

	[TestMethod]
	public void TestRenameEnumValue()
	{
		Schema schema = new();
		SchemaEnum status = schema.AddEnum("Status".As<EnumName>())!;
		status.TryAddValue("Active".As<EnumValueName>());
		status.TryAddValue("Inactive".As<EnumValueName>());

		Assert.IsTrue(status.TryRenameValue("Active".As<EnumValueName>(), "Enabled".As<EnumValueName>()));
		Assert.AreEqual("Enabled", status.Values[0].ToString(), "The renamed value keeps its position.");

		Assert.IsFalse(status.TryRenameValue("Enabled".As<EnumValueName>(), "Inactive".As<EnumValueName>()), "Collides.");
		Assert.IsFalse(status.TryRenameValue("Enabled".As<EnumValueName>(), string.Empty.As<EnumValueName>()), "Empty.");
		Assert.IsFalse(status.TryRenameValue("Missing".As<EnumValueName>(), "Whatever".As<EnumValueName>()), "Not present.");
	}

	[TestMethod]
	public void TestRenamingBackUndoesTheWholeCascade()
	{
		// This is what the editor's undo entry does: it renames back. If the cascade were not
		// symmetric, undoing a rename would leave references pointing at the new name.
		Schema schema = CreateReferencingSchema();
		SchemaClass item = schema.GetClass("Item".As<ClassName>())!;
		SchemaClass user = schema.GetClass("User".As<ClassName>())!;

		Assert.IsTrue(schema.TryRenameClass(item, "Product".As<ClassName>()));
		Assert.IsTrue(schema.TryRenameClass(item, "Item".As<ClassName>()));

		Assert.AreEqual("Item", item.Name.ToString());
		Assert.AreEqual("Item", ((SchemaTypes.Object)user.GetMember("Favourite".As<MemberName>())!.Type).ClassName.ToString());

		SchemaTypes.Array items = (SchemaTypes.Array)user.GetMember("Items".As<MemberName>())!.Type;
		Assert.AreEqual("Item", ((SchemaTypes.Object)items.ElementType).ClassName.ToString());
		Assert.AreEqual("Item", schema.GetDataSource("Items".As<DataSourceName>())!.ClassName.ToString());
		Assert.AreEqual(0, schema.Validate().Count, string.Join("; ", schema.Validate()));
	}

	[TestMethod]
	public void TestRenamingAMemberBackRestoresTheArrayKey()
	{
		Schema schema = CreateReferencingSchema();
		SchemaClass item = schema.GetClass("Item".As<ClassName>())!;
		SchemaMember id = item.GetMember("Id".As<MemberName>())!;

		Assert.IsTrue(item.TryRenameMember(id, "Identifier".As<MemberName>()));
		Assert.IsTrue(item.TryRenameMember(id, "Id".As<MemberName>()));

		SchemaClass user = schema.GetClass("User".As<ClassName>())!;
		SchemaTypes.Array items = (SchemaTypes.Array)user.GetMember("Items".As<MemberName>())!.Type;
		Assert.AreEqual("Id", items.Key.ToString());
		Assert.AreEqual(0, schema.Validate().Count, string.Join("; ", schema.Validate()));
	}

	[TestMethod]
	public void TestRenamedSchemaSurvivesARoundTrip()
	{
		Schema schema = CreateReferencingSchema();
		SchemaClass item = schema.GetClass("Item".As<ClassName>())!;
		Assert.IsTrue(schema.TryRenameClass(item, "Product".As<ClassName>()));

		string json = SchemaSerializer.Serialize(schema);
		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? reloaded));
		Assert.IsNotNull(reloaded);

		Assert.IsNotNull(reloaded.GetClass("Product".As<ClassName>()));
		Assert.AreEqual(0, reloaded.Validate().Count, string.Join("; ", reloaded.Validate()));
	}

	/// <summary>
	/// Builds a schema whose references to <c>Item</c> and <c>Status</c> sit only in nested or
	/// signature positions: inside Optional, Handle and Result wrappers, inside an array of
	/// optionals, and in an interface function's return and parameter types.
	/// </summary>
	private static Schema CreateNestedReferencingSchema()
	{
		Schema schema = new();

		SchemaEnum? status = schema.AddEnum("Status".As<EnumName>());
		status?.TryAddValue("Active".As<EnumValueName>());
		schema.ErrorType = "Status".As<EnumName>();

		schema.AddClass("Item".As<ClassName>())?.AddMember("Id".As<MemberName>())?.SetType(new SchemaTypes.Int());

		SchemaClass? holder = schema.AddClass("Holder".As<ClassName>());
		holder?.AddMember("MaybeItem".As<MemberName>())?.SetType(new SchemaTypes.Optional() { ElementType = ItemReference() });
		holder?.AddMember("ItemHandle".As<MemberName>())?.SetType(new SchemaTypes.Handle() { ElementType = ItemReference() });
		holder?.AddMember("MaybeStatus".As<MemberName>())?.SetType(new SchemaTypes.Optional() { ElementType = StatusReference() });
		holder?.AddMember("MaybeItems".As<MemberName>())?.SetType(new SchemaTypes.Array()
		{
			ElementType = new SchemaTypes.Optional() { ElementType = ItemReference() },
		});

		SchemaFunction? lookup = schema.AddInterface("Store".As<InterfaceName>())?.AddFunction("Lookup".As<FunctionName>());
		lookup?.SetReturnType(new SchemaTypes.Result() { ElementType = ItemReference() });
		lookup?.AddParameter("Filter".As<ParameterName>())?.SetType(StatusReference());
		lookup?.AddParameter("Like".As<ParameterName>())?.SetType(ItemReference());

		return schema;
	}

	private static SchemaTypes.Object ItemReference() => new() { ClassName = "Item".As<ClassName>() };

	private static SchemaTypes.Enum StatusReference() => new() { EnumName = "Status".As<EnumName>() };

	private static SchemaFunction Lookup(Schema schema) =>
		schema.Interfaces.Single().Functions.Single();

	private static SchemaTypes.BaseType Inner(Schema schema, string member) =>
		((SchemaTypes.WrapperType)schema.GetClass("Holder".As<ClassName>())!.GetMember(member.As<MemberName>())!.Type).ElementType;

	private static void AssertNoIssueNames(Schema schema, string oldName)
	{
		string[] dangling = [.. schema.Validate()
			.Select(issue => issue.ToString())
			.Where(issue => issue.Contains($"'{oldName}'", StringComparison.Ordinal))];
		Assert.IsEmpty(dangling, string.Join("; ", dangling));
	}

	[TestMethod]
	public void TestRenameClassRepointsReferencesInsideWrappers()
	{
		Schema schema = CreateNestedReferencingSchema();

		Assert.IsTrue(schema.TryRenameClass(schema.GetClass("Item".As<ClassName>())!, "Product".As<ClassName>()));

		Assert.AreEqual("Product", ((SchemaTypes.Object)Inner(schema, "MaybeItem")).ClassName.ToString());
		Assert.AreEqual("Product", ((SchemaTypes.Object)Inner(schema, "ItemHandle")).ClassName.ToString());
		AssertNoIssueNames(schema, "Item");
	}

	[TestMethod]
	public void TestRenameClassRepointsReferencesInsideAnArrayOfOptionals()
	{
		Schema schema = CreateNestedReferencingSchema();

		Assert.IsTrue(schema.TryRenameClass(schema.GetClass("Item".As<ClassName>())!, "Product".As<ClassName>()));

		SchemaTypes.Array items = (SchemaTypes.Array)schema.GetClass("Holder".As<ClassName>())!.GetMember("MaybeItems".As<MemberName>())!.Type;
		SchemaTypes.Optional optional = (SchemaTypes.Optional)items.ElementType;
		Assert.AreEqual("Product", ((SchemaTypes.Object)optional.ElementType).ClassName.ToString());
		AssertNoIssueNames(schema, "Item");
	}

	[TestMethod]
	public void TestRenameClassRepointsFunctionReturnAndParameterTypes()
	{
		Schema schema = CreateNestedReferencingSchema();

		Assert.IsTrue(schema.TryRenameClass(schema.GetClass("Item".As<ClassName>())!, "Product".As<ClassName>()));

		SchemaFunction lookup = Lookup(schema);
		SchemaTypes.Result result = (SchemaTypes.Result)lookup.ReturnType;
		Assert.AreEqual("Product", ((SchemaTypes.Object)result.ElementType).ClassName.ToString());
		Assert.AreEqual("Product", ((SchemaTypes.Object)lookup.Parameters.Single(p => p.Name.ToString() == "Like").Type).ClassName.ToString());
		AssertNoIssueNames(schema, "Item");
	}

	[TestMethod]
	public void TestRenameEnumRepointsReferencesInsideWrappersAndSignatures()
	{
		Schema schema = CreateNestedReferencingSchema();

		Assert.IsTrue(schema.TryRenameEnum(schema.GetEnum("Status".As<EnumName>())!, "State".As<EnumName>()));

		Assert.AreEqual("State", ((SchemaTypes.Enum)Inner(schema, "MaybeStatus")).EnumName.ToString());
		Assert.AreEqual("State", ((SchemaTypes.Enum)Lookup(schema).Parameters.Single(p => p.Name.ToString() == "Filter").Type).EnumName.ToString());
		AssertNoIssueNames(schema, "Status");
	}

	[TestMethod]
	public void TestRenameEnumRepointsTheSchemaErrorType()
	{
		Schema schema = CreateNestedReferencingSchema();

		Assert.IsTrue(schema.TryRenameEnum(schema.GetEnum("Status".As<EnumName>())!, "State".As<EnumName>()));

		Assert.AreEqual("State", schema.ErrorType.ToString());
	}

	/// <summary>
	/// The schema the issue was filed against: an error type, and an enum-typed member whose
	/// default names one of the enum's values.
	/// </summary>
	private static Schema CreateSchemaNamingEnumsByText()
	{
		Schema schema = new();
		SchemaEnum failure = schema.AddEnum("Failure".As<EnumName>())!;
		failure.TryAddValue("NotFound".As<EnumValueName>());
		schema.ErrorType = "Failure".As<EnumName>();

		SchemaEnum mood = schema.AddEnum("Mood".As<EnumName>())!;
		mood.TryAddValue("Calm".As<EnumValueName>());
		mood.TryAddValue("Angry".As<EnumValueName>());

		SchemaMember member = schema.AddClass("Npc".As<ClassName>())!.AddMember("Mood".As<MemberName>())!;
		member.SetType(new SchemaTypes.Enum() { EnumName = "Mood".As<EnumName>() });
		member.DefaultValue = new TextDefault { Value = "Calm" };

		return schema;
	}

	[TestMethod]
	public void TestRenamingTheErrorEnumRepointsTheErrorType()
	{
		Schema schema = CreateSchemaNamingEnumsByText();
		SchemaEnum failure = schema.GetEnum("Failure".As<EnumName>())!;

		Assert.IsTrue(schema.TryRenameEnum(failure, "ErrorCode".As<EnumName>()));
		Assert.AreEqual("ErrorCode", schema.ErrorType.ToString());
		Assert.AreEqual(0, schema.Validate().Count, string.Join("; ", schema.Validate()));

		Assert.IsTrue(schema.TryRenameEnum(failure, "Failure".As<EnumName>()));
		Assert.AreEqual("Failure", schema.ErrorType.ToString(), "Renaming back did not restore the error type.");
	}

	[TestMethod]
	public void TestRenamingAnotherEnumLeavesTheErrorTypeAlone()
	{
		Schema schema = CreateSchemaNamingEnumsByText();

		Assert.IsTrue(schema.TryRenameEnum(schema.GetEnum("Mood".As<EnumName>())!, "Temper".As<EnumName>()));
		Assert.AreEqual("Failure", schema.ErrorType.ToString());
	}

	[TestMethod]
	public void TestRenamingAnEnumValueRepointsTheDefaultsNamingIt()
	{
		Schema schema = CreateSchemaNamingEnumsByText();
		SchemaEnum mood = schema.GetEnum("Mood".As<EnumName>())!;
		SchemaMember member = schema.GetClass("Npc".As<ClassName>())!.GetMember("Mood".As<MemberName>())!;

		Assert.IsTrue(mood.TryRenameValue("Calm".As<EnumValueName>(), "Serene".As<EnumValueName>()));
		Assert.AreEqual("Serene", ((TextDefault)member.DefaultValue!).Value);
		Assert.AreEqual(0, schema.Validate().Count, string.Join("; ", schema.Validate()));

		Assert.IsTrue(mood.TryRenameValue("Serene".As<EnumValueName>(), "Calm".As<EnumValueName>()));
		Assert.AreEqual("Calm", ((TextDefault)member.DefaultValue!).Value, "Renaming back did not restore the default.");
	}

	/// <summary>
	/// Only a default of the renamed enum moves: a string member, or a member of another enum,
	/// whose default happens to spell the same word is a different value.
	/// </summary>
	[TestMethod]
	public void TestRenamingAnEnumValueLeavesUnrelatedDefaultsAlone()
	{
		Schema schema = CreateSchemaNamingEnumsByText();
		SchemaClass npc = schema.GetClass("Npc".As<ClassName>())!;
		SchemaMember text = npc.AddMember("Note".As<MemberName>())!;
		text.SetType(new SchemaTypes.String());
		text.DefaultValue = new TextDefault { Value = "Calm" };

		SchemaEnum weather = schema.AddEnum("Weather".As<EnumName>())!;
		weather.TryAddValue("Calm".As<EnumValueName>());
		SchemaMember sky = npc.AddMember("Sky".As<MemberName>())!;
		sky.SetType(new SchemaTypes.Enum() { EnumName = "Weather".As<EnumName>() });
		sky.DefaultValue = new TextDefault { Value = "Calm" };

		Assert.IsTrue(schema.GetEnum("Mood".As<EnumName>())!.TryRenameValue("Calm".As<EnumValueName>(), "Serene".As<EnumValueName>()));
		Assert.AreEqual("Calm", ((TextDefault)text.DefaultValue!).Value);
		Assert.AreEqual("Calm", ((TextDefault)sky.DefaultValue!).Value);
	}
}
