// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Collections.ObjectModel;
using ktsu.Schema.Contracts;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SchemaTypes = Models.Types;

/// <summary>
/// Covers the <see cref="ktsu.Schema.Contracts"/> abstraction seam: that the models implement it,
/// that a schema can be built and read through the contracts alone, and that the covariance the
/// contracts rely on actually holds.
/// </summary>
[TestClass]
public class SchemaContractsTests
{
	private static readonly string[] SeedMembers = ["First", "Second", "Third"];
	private static readonly string[] AfterReorder = ["Third", "First", "Second"];
	private static readonly string[] AfterRemoveAndRestore = ["First", "Third", "Second"];

	/// <summary>
	/// A consumer holding only <see cref="ISchema"/> — what dependency injection hands it — can
	/// define a schema without ever naming a model type. This is the scenario
	/// <c>docs/examples/dependency-injection.md</c> describes.
	/// </summary>
	[TestMethod]
	public void ISchemaAloneCanDefineASchema()
	{
		ISchema schema = new Schema();

		ISchemaClass? user = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(user, "A class can be added through the contract.");

		ISchemaMember? name = user.AddMember("Name".As<MemberName>());
		Assert.IsNotNull(name, "A member can be added through the contract.");
		name.SetType(new SchemaTypes.String());

		ISchemaEnum? role = schema.AddEnum("Role".As<EnumName>());
		Assert.IsNotNull(role, "An enum can be added through the contract.");
		Assert.IsTrue(role.TryAddValue("Admin".As<EnumValueName>()), "An enum value can be added through the contract.");

		Assert.AreEqual(1, schema.Classes.Count);
		Assert.AreEqual(1, schema.Enums.Count);
	}

	/// <summary>
	/// The whole model is reachable through the contracts: schema to class to member to type, and
	/// back up through the parent references.
	/// </summary>
	[TestMethod]
	public void ContractsNavigateTheModelInBothDirections()
	{
		ISchema schema = new Schema();
		ISchemaClass user = schema.AddClass("User".As<ClassName>())!;
		ISchemaMember member = user.AddMember("Name".As<MemberName>())!;
		member.SetType(new SchemaTypes.String());

		ISchemaClass found = schema.Classes.GetByName("User".As<ClassName>())!;
		Assert.AreSame(user, found, "Lookup by name returns the same element.");

		ISchemaMember foundMember = found.Members.GetByName("Name".As<MemberName>())!;
		Assert.AreSame(member, foundMember);
		Assert.AreEqual("String".As<BaseTypeName>(), foundMember.Type.TypeName);

		Assert.AreSame(user, foundMember.ParentClass, "A member knows its class through the contract.");
		Assert.AreSame(schema, foundMember.ParentSchema, "A member knows its schema through the contract.");
		Assert.AreSame(foundMember, foundMember.Type.ParentMember, "A type knows its member through the contract.");
	}

	/// <summary>
	/// The contract's element type is the interface while the model's is the concrete class. The
	/// covariance of <see cref="ISchemaChildSet{TValue, TName}"/> is what lets one be the other,
	/// and it is the same object rather than a copy.
	/// </summary>
	[TestMethod]
	public void TheContractCollectionIsTheModelCollection()
	{
		Schema schema = new();
		SchemaClass user = schema.AddClass("User".As<ClassName>())!;

		ISchema contract = schema;
		ISchemaChildSet<ISchemaClass, ClassName> classes = contract.Classes;

		Assert.AreEqual(1, classes.Count);
		Assert.AreSame(user, classes.GetByName("User".As<ClassName>()), "The covariant view yields the model's own elements.");

		// A class added afterwards through the model is visible through a view taken before it.
		schema.AddClass("Item".As<ClassName>());
		Assert.IsTrue(classes.ContainsByName("Item".As<ClassName>()), "The view reads the live collection rather than a snapshot.");
	}

	/// <summary>
	/// Adding through the contract enforces the same name uniqueness as adding through the model.
	/// </summary>
	[TestMethod]
	public void AddingADuplicateNameThroughTheContractFails()
	{
		ISchema schema = new Schema();
		Assert.IsNotNull(schema.AddClass("User".As<ClassName>()));
		Assert.IsNull(schema.AddClass("User".As<ClassName>()), "A second class of the same name is refused.");

		ISchemaClass user = schema.Classes.GetByName("User".As<ClassName>())!;
		Assert.IsNotNull(user.AddMember("Name".As<MemberName>()));
		Assert.IsNull(user.AddMember("Name".As<MemberName>()), "A second member of the same name is refused.");
	}

	/// <summary>
	/// Removing through the contract removes the element from the schema itself.
	/// </summary>
	[TestMethod]
	public void RemovingThroughTheContractRemovesFromTheSchema()
	{
		Schema schema = new();
		schema.AddClass("User".As<ClassName>());

		Assert.IsTrue(((ISchema)schema).RemoveClass("User".As<ClassName>()));
		Assert.AreEqual(0, schema.Classes.Count, "The model no longer holds the class.");
		Assert.IsFalse(((ISchema)schema).RemoveClass("User".As<ClassName>()), "Removing it again reports nothing was removed.");
	}

	/// <summary>
	/// A type from outside the model hierarchy cannot be stored: the polymorphic serializer knows
	/// only <see cref="SchemaTypes.BaseType"/> and its declared subtypes, so accepting anything
	/// else would produce a member the library could not write or read back.
	/// </summary>
	[TestMethod]
	public void SettingATypeFromOutsideTheModelHierarchyIsRejected()
	{
		ISchema schema = new Schema();
		ISchemaMember member = schema.AddClass("User".As<ClassName>())!.AddMember("Name".As<MemberName>())!;

		Assert.ThrowsExactly<ArgumentException>(() => member.SetType(new ForeignType()));
	}

	/// <summary>
	/// Every type's <see cref="SchemaTypes.BaseType.TypeName"/> is the discriminator written to the
	/// file, so the two cannot drift apart.
	/// </summary>
	[TestMethod]
	public void TypeNameMatchesTheSerializedDiscriminator()
	{
		SchemaTypes.BaseType[] types =
		[
			new SchemaTypes.None(), new SchemaTypes.Int(), new SchemaTypes.Long(),
			new SchemaTypes.Float(), new SchemaTypes.Double(), new SchemaTypes.String(),
			new SchemaTypes.Bool(), new SchemaTypes.DateTime(), new SchemaTypes.TimeSpan(),
			new SchemaTypes.Enum(), new SchemaTypes.Array(), new SchemaTypes.Object(),
			new SchemaTypes.Vector2(), new SchemaTypes.Vector3(), new SchemaTypes.Vector4(),
			new SchemaTypes.ColorRGB(), new SchemaTypes.ColorRGBA(),
		];

		foreach (SchemaTypes.BaseType type in types)
		{
			Schema schema = new();
			SchemaClass schemaClass = schema.AddClass("Holder".As<ClassName>())!;
			schemaClass.AddMember("Value".As<MemberName>())!.SetType(type);

			string json = SchemaSerializer.Serialize(schema);
			Assert.IsTrue(
				json.Contains($"\"TypeName\": \"{type.TypeName}\"", StringComparison.Ordinal),
				$"{type.GetType().Name} reports a TypeName matching what is written to the file.");
		}
	}

	/// <summary>
	/// The set preserves the order elements were added in, which is what makes member order part of
	/// the schema's meaning rather than an accident of storage.
	/// </summary>
	[TestMethod]
	public void TheSetPreservesInsertionOrder()
	{
		SchemaChildSet<SchemaMember, MemberName> members = CreateMemberSet();
		CollectionAssert.AreEqual(SeedMembers, members.Select(m => m.Name.ToString()).ToArray());
	}

	/// <summary>
	/// A remove followed by an add — what undoing a deletion does — must not disturb the order of
	/// the elements that stayed. A name-keyed hash set would give no such guarantee.
	/// </summary>
	[TestMethod]
	public void RemovingAndRestoringLeavesTheOtherElementsInOrder()
	{
		SchemaChildSet<SchemaMember, MemberName> members = CreateMemberSet();
		SchemaMember second = members.GetByName("Second".As<MemberName>())!;

		Assert.IsTrue(members.Remove(second));
		Assert.IsTrue(members.Add(second), "The element can be restored.");

		CollectionAssert.AreEqual(
			AfterRemoveAndRestore,
			members.Select(m => m.Name.ToString()).ToArray(),
			"The surviving elements keep their relative order; the restored one goes to the end.");
	}

	/// <summary>
	/// The set owns the name-uniqueness rule that each call site would otherwise re-implement.
	/// </summary>
	[TestMethod]
	public void TheSetRefusesADuplicateName()
	{
		SchemaChildSet<SchemaMember, MemberName> members = CreateMemberSet();

		SchemaMember duplicate = new();
		duplicate.Rename("First".As<MemberName>());

		Assert.IsFalse(members.Add(duplicate), "A different element with a name already present is refused.");
		Assert.AreEqual(SeedMembers.Length, members.Count);
	}

	/// <summary>
	/// Moving is bounds-checked, and an out-of-range move changes nothing.
	/// </summary>
	[TestMethod]
	public void MovingReordersAndRejectsAnOutOfRangeIndex()
	{
		SchemaChildSet<SchemaMember, MemberName> members = CreateMemberSet();
		SchemaMember third = members.GetByName("Third".As<MemberName>())!;

		Assert.IsTrue(members.Move(third, 0));
		CollectionAssert.AreEqual(AfterReorder, members.Select(m => m.Name.ToString()).ToArray());

		Assert.IsFalse(members.Move(third, members.Count), "An index past the end is refused.");
		Assert.IsFalse(members.Move(third, -1), "A negative index is refused.");
		CollectionAssert.AreEqual(AfterReorder, members.Select(m => m.Name.ToString()).ToArray(), "A refused move changes nothing.");

		SchemaMember stranger = new();
		stranger.Rename("Stranger".As<MemberName>());
		Assert.IsFalse(members.Move(stranger, 0), "An element not in the set cannot be moved.");
	}

	/// <summary>
	/// Uniqueness is enforced on the way in, not on the way through. A hand-edited file containing
	/// duplicate names still loads with both elements present, so <see cref="Schema.Validate"/> can
	/// report it. Dropping one silently at load would turn a diagnosable mistake into data loss.
	/// </summary>
	[TestMethod]
	public void DuplicateNamesInAFileStillLoadAndAreReported()
	{
		string json = """
			{
			  "formatVersion": 1,
			  "classes": [
			    { "name": "User", "members": [] },
			    { "name": "User", "members": [] }
			  ]
			}
			""";

		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? schema));
		Assert.IsNotNull(schema);
		Assert.AreEqual(2, schema.Classes.Count, "Both classes are loaded rather than one being dropped.");

		Collection<SchemaValidationIssue> issues = schema.Validate();
		Assert.IsTrue(
			issues.Any(i => i.Message.Contains("Duplicate class name 'User'", StringComparison.Ordinal)),
			"The duplicate is reported as a validation issue.");
	}

	private static SchemaChildSet<SchemaMember, MemberName> CreateMemberSet()
	{
		Schema schema = new();
		SchemaClass schemaClass = schema.AddClass("User".As<ClassName>())!;
		foreach (string name in SeedMembers)
		{
			schemaClass.AddMember(name.As<MemberName>())?.SetType(new SchemaTypes.Int());
		}

		return schemaClass.Members;
	}

	/// <summary>
	/// An <see cref="ISchemaType"/> implemented outside the model hierarchy, used to check that it
	/// is refused rather than stored.
	/// </summary>
	private sealed class ForeignType : ISchemaType
	{
		public BaseTypeName TypeName => "Foreign".As<BaseTypeName>();

		public ISchemaMember? ParentMember => null;
	}
}
