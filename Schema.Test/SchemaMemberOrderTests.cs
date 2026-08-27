// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SchemaTypes = Models.Types;

/// <summary>
/// Covers reordering members, which is part of the schema's meaning because declaration order is
/// preserved through serialization and drives the order of any generated code.
/// </summary>
[TestClass]
public class SchemaMemberOrderTests
{
	private static readonly string[] OriginalOrder = ["First", "Second", "Third"];
	private static readonly string[] ThirdMovedToFront = ["Third", "First", "Second"];
	private static readonly string[] FirstMovedToBack = ["Second", "Third", "First"];
	private static readonly string[] MemberSeed = ["First", "Second", "Third"];

	private static SchemaClass CreateClassWithMembers(out Schema schema)
	{
		schema = new Schema();
		SchemaClass schemaClass = schema.AddClass("User".As<ClassName>())!;
		foreach (string name in MemberSeed)
		{
			schemaClass.AddMember(name.As<MemberName>())?.SetType(new SchemaTypes.Int());
		}

		return schemaClass;
	}

	private static string[] MemberNames(SchemaClass schemaClass) =>
		[.. schemaClass.Members.Select(m => m.Name.ToString())];

	[TestMethod]
	public void TestIndexOfMember()
	{
		SchemaClass schemaClass = CreateClassWithMembers(out _);
		SchemaMember second = schemaClass.GetMember("Second".As<MemberName>())!;
		Assert.AreEqual(1, schemaClass.IndexOfMember(second));

		SchemaMember stranger = new();
		Assert.AreEqual(-1, schemaClass.IndexOfMember(stranger));
	}

	[TestMethod]
	public void TestMoveMemberEarlier()
	{
		SchemaClass schemaClass = CreateClassWithMembers(out _);
		SchemaMember third = schemaClass.GetMember("Third".As<MemberName>())!;

		Assert.IsTrue(schemaClass.TryMoveMember(third, 0));
		CollectionAssert.AreEqual(ThirdMovedToFront, MemberNames(schemaClass));
	}

	[TestMethod]
	public void TestMoveMemberLater()
	{
		SchemaClass schemaClass = CreateClassWithMembers(out _);
		SchemaMember first = schemaClass.GetMember("First".As<MemberName>())!;

		Assert.IsTrue(schemaClass.TryMoveMember(first, 2));
		CollectionAssert.AreEqual(FirstMovedToBack, MemberNames(schemaClass));
	}

	[TestMethod]
	public void TestMoveMemberToItsCurrentIndexIsANoOp()
	{
		SchemaClass schemaClass = CreateClassWithMembers(out _);
		SchemaMember second = schemaClass.GetMember("Second".As<MemberName>())!;

		Assert.IsTrue(schemaClass.TryMoveMember(second, 1));
		CollectionAssert.AreEqual(OriginalOrder, MemberNames(schemaClass));
	}

	[TestMethod]
	[DataRow(-1)]
	[DataRow(3)]
	[DataRow(int.MaxValue)]
	[DataRow(int.MinValue)]
	public void TestMoveMemberOutOfRangeIsRejected(int index)
	{
		SchemaClass schemaClass = CreateClassWithMembers(out _);
		SchemaMember first = schemaClass.GetMember("First".As<MemberName>())!;

		Assert.IsFalse(schemaClass.TryMoveMember(first, index));
		CollectionAssert.AreEqual(OriginalOrder, MemberNames(schemaClass));
	}

	[TestMethod]
	public void TestMoveMemberFromAnotherClassIsRejected()
	{
		SchemaClass schemaClass = CreateClassWithMembers(out Schema schema);
		SchemaClass other = schema.AddClass("Order".As<ClassName>())!;
		SchemaMember stranger = other.AddMember("Total".As<MemberName>())!;

		Assert.IsFalse(schemaClass.TryMoveMember(stranger, 0));
		CollectionAssert.AreEqual(OriginalOrder, MemberNames(schemaClass));
	}

	[TestMethod]
	public void TestMemberOrderSurvivesARoundTrip()
	{
		SchemaClass schemaClass = CreateClassWithMembers(out Schema schema);
		Assert.IsTrue(schemaClass.TryMoveMember(schemaClass.GetMember("Third".As<MemberName>())!, 0));

		string json = SchemaSerializer.Serialize(schema);
		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? reloaded));
		Assert.IsNotNull(reloaded);

		CollectionAssert.AreEqual(
			ThirdMovedToFront,
			MemberNames(reloaded.GetClass("User".As<ClassName>())!));
	}
}
