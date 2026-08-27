// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
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
		Assert.AreEqual("Enabled", status.Values.First().ToString(), "The renamed value keeps its position.");

		Assert.IsFalse(status.TryRenameValue("Enabled".As<EnumValueName>(), "Inactive".As<EnumValueName>()), "Collides.");
		Assert.IsFalse(status.TryRenameValue("Enabled".As<EnumValueName>(), string.Empty.As<EnumValueName>()), "Empty.");
		Assert.IsFalse(status.TryRenameValue("Missing".As<EnumValueName>(), "Whatever".As<EnumValueName>()), "Not present.");
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
}
