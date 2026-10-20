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
	/// Builds a schema where a semantic type and an interface are referenced from a member, from
	/// inside a wrapper, from a function signature, and - for the semantic type - from another
	/// semantic type that refines it.
	/// </summary>
	private static Schema CreateSemanticAndInterfaceReferencingSchema()
	{
		Schema schema = new();

		schema.AddSemanticType("Mass".As<SemanticTypeName>())?.SetUnderlyingType(new SchemaTypes.Float());
		schema.AddSemanticType("Weight".As<SemanticTypeName>())?.SetUnderlyingType(MassReference());

		SchemaInterface? store = schema.AddInterface("Store".As<InterfaceName>());
		SchemaFunction? weigh = store?.AddFunction("Weigh".As<FunctionName>());
		weigh?.SetReturnType(MassReference());
		weigh?.AddParameter("Into".As<ParameterName>())?.SetType(StoreReference());

		SchemaClass? crate = schema.AddClass("Crate".As<ClassName>());
		crate?.AddMember("Mass".As<MemberName>())?.SetType(MassReference());
		crate?.AddMember("MaybeMass".As<MemberName>())?.SetType(new SchemaTypes.Optional() { ElementType = MassReference() });
		crate?.AddMember("Owner".As<MemberName>())?.SetType(StoreReference());

		return schema;
	}

	private static SchemaTypes.Semantic MassReference() => new() { SemanticTypeName = "Mass".As<SemanticTypeName>() };

	private static SchemaTypes.Interface StoreReference() => new() { InterfaceName = "Store".As<InterfaceName>() };

	private static SchemaTypes.BaseType CrateMember(Schema schema, string member) =>
		schema.GetClass("Crate".As<ClassName>())!.GetMember(member.As<MemberName>())!.Type;

	[TestMethod]
	public void TestRenameSemanticTypeCascadesToEveryReference()
	{
		Schema schema = CreateSemanticAndInterfaceReferencingSchema();
		SchemaSemanticType mass = schema.GetSemanticType("Mass".As<SemanticTypeName>())!;

		Assert.IsTrue(schema.TryRenameSemanticType(mass, "Kilograms".As<SemanticTypeName>()));

		Assert.AreEqual("Kilograms", mass.Name.ToString());
		Assert.AreEqual("Kilograms", ((SchemaTypes.Semantic)CrateMember(schema, "Mass")).SemanticTypeName.ToString());
		Assert.AreEqual("Kilograms", ((SchemaTypes.Semantic)((SchemaTypes.Optional)CrateMember(schema, "MaybeMass")).ElementType).SemanticTypeName.ToString());
		Assert.AreEqual("Kilograms", ((SchemaTypes.Semantic)schema.Interfaces.Single().Functions.Single().ReturnType).SemanticTypeName.ToString());

		SchemaSemanticType weight = schema.GetSemanticType("Weight".As<SemanticTypeName>())!;
		Assert.AreEqual("Kilograms", ((SchemaTypes.Semantic)weight.UnderlyingType).SemanticTypeName.ToString());
		Assert.AreSame(mass, ((SchemaTypes.Semantic)weight.UnderlyingType).Declaration);
		AssertNoIssueNames(schema, "Mass");
	}

	[TestMethod]
	public void TestRenameSemanticTypeRejectsACollision()
	{
		Schema schema = CreateSemanticAndInterfaceReferencingSchema();
		SchemaSemanticType mass = schema.GetSemanticType("Mass".As<SemanticTypeName>())!;

		Assert.IsFalse(schema.TryRenameSemanticType(mass, "Weight".As<SemanticTypeName>()));
		Assert.AreEqual("Mass", mass.Name.ToString());
		Assert.AreEqual("Mass", ((SchemaTypes.Semantic)CrateMember(schema, "Mass")).SemanticTypeName.ToString());
	}

	[TestMethod]
	public void TestRenameInterfaceCascadesToEveryReference()
	{
		Schema schema = CreateSemanticAndInterfaceReferencingSchema();
		SchemaInterface store = schema.Interfaces.Single();

		Assert.IsTrue(schema.TryRenameInterface(store, "Warehouse".As<InterfaceName>()));

		Assert.AreEqual("Warehouse", store.Name.ToString());
		Assert.AreEqual("Warehouse", ((SchemaTypes.Interface)CrateMember(schema, "Owner")).InterfaceName.ToString());
		Assert.AreEqual("Warehouse", ((SchemaTypes.Interface)store.Functions.Single().Parameters.Single().Type).InterfaceName.ToString());
		AssertNoIssueNames(schema, "Store");
	}

	[TestMethod]
	public void TestRenameInterfaceRejectsAnEmptyName()
	{
		Schema schema = CreateSemanticAndInterfaceReferencingSchema();
		SchemaInterface store = schema.Interfaces.Single();

		Assert.IsFalse(schema.TryRenameInterface(store, string.Empty.As<InterfaceName>()));
		Assert.AreEqual("Store", store.Name.ToString());
	}

	[TestMethod]
	public void TestRestoredSemanticTypeIsFoundAgain()
	{
		Schema schema = CreateSemanticAndInterfaceReferencingSchema();
		SchemaSemanticType mass = schema.GetSemanticType("Mass".As<SemanticTypeName>())!;

		Assert.IsTrue(mass.TryRemove());
		Assert.IsFalse(schema.TryGetSemanticType("Mass".As<SemanticTypeName>(), out _));

		Assert.IsTrue(schema.RestoreSemanticType(mass));
		Assert.AreSame(mass, schema.GetSemanticType("Mass".As<SemanticTypeName>()));
	}

	/// <summary>
	/// An array and a vector hold their component type the same way a wrapper does, so a semantic
	/// type named only from inside one has to be found there too.
	/// </summary>
	[TestMethod]
	public void TestRenameSemanticTypeReachesInsideArraysAndVectors()
	{
		Schema schema = CreateSemanticAndInterfaceReferencingSchema();
		SchemaClass crate = schema.GetClass("Crate".As<ClassName>())!;
		crate.AddMember("Masses".As<MemberName>())!.SetType(new SchemaTypes.Array() { ElementType = MassReference() });
		crate.AddMember("Moment".As<MemberName>())!.SetType(new SchemaTypes.Vector3() { ElementType = MassReference() });
		SchemaSemanticType mass = schema.GetSemanticType("Mass".As<SemanticTypeName>())!;

		Assert.IsTrue(schema.TryRenameSemanticType(mass, "Kilograms".As<SemanticTypeName>()));

		Assert.AreEqual("Kilograms", ((SchemaTypes.Semantic)((SchemaTypes.Array)CrateMember(schema, "Masses")).ElementType).SemanticTypeName.ToString());
		Assert.AreEqual("Kilograms", ((SchemaTypes.Semantic)((SchemaTypes.Vector3)CrateMember(schema, "Moment")).ElementType).SemanticTypeName.ToString());
	}

	/// <summary>
	/// A schema only renames what it owns: one holding a semantic type or an interface of the same
	/// name must not rename another schema's element, nor repoint its own references to it.
	/// </summary>
	[TestMethod]
	public void TestRenameRefusesAnElementFromAnotherSchema()
	{
		Schema schema = CreateSemanticAndInterfaceReferencingSchema();
		Schema other = CreateSemanticAndInterfaceReferencingSchema();
		SchemaSemanticType foreignMass = other.GetSemanticType("Mass".As<SemanticTypeName>())!;
		SchemaInterface foreignStore = other.Interfaces.Single();

		Assert.IsFalse(schema.TryRenameSemanticType(foreignMass, "Kilograms".As<SemanticTypeName>()));
		Assert.IsFalse(schema.TryRenameInterface(foreignStore, "Warehouse".As<InterfaceName>()));

		Assert.AreEqual("Mass", foreignMass.Name.ToString());
		Assert.AreEqual("Store", foreignStore.Name.ToString());
		Assert.AreEqual("Mass", ((SchemaTypes.Semantic)CrateMember(schema, "Mass")).SemanticTypeName.ToString());
		Assert.AreEqual("Store", ((SchemaTypes.Interface)CrateMember(schema, "Owner")).InterfaceName.ToString());
	}

	[TestMethod]
	public void TestRestoredInterfaceIsFoundAgainAndRefusesADuplicate()
	{
		Schema schema = CreateSemanticAndInterfaceReferencingSchema();
		SchemaInterface store = schema.Interfaces.Single();

		Assert.IsTrue(store.TryRemove());
		Assert.IsEmpty(schema.Interfaces);

		Assert.IsTrue(schema.RestoreInterface(store));
		Assert.AreSame(store, schema.Interfaces.Single());
		Assert.IsFalse(schema.RestoreInterface(store), "Restoring an interface already present added it twice.");
	}
}
