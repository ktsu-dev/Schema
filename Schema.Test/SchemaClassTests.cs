// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SchemaTypes = ktsu.Schema.Models.Types;

[TestClass]
public class SchemaClassTests
{
	[TestMethod]
	public void TestAddClass()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());

		Assert.IsNotNull(schemaClass);
		Assert.AreEqual("User".As<ClassName>(), schemaClass.Name);
		Assert.AreEqual(1, schema.Classes.Count);
	}

	[TestMethod]
	public void TestAddDuplicateClassReturnsNull()
	{
		Schema schema = new();
		schema.AddClass("User".As<ClassName>());
		SchemaClass? duplicate = schema.AddClass("User".As<ClassName>());

		Assert.IsNull(duplicate);
		Assert.AreEqual(1, schema.Classes.Count);
	}

	[TestMethod]
	public void TestTryAddClass()
	{
		Schema schema = new();
		bool result = schema.TryAddClass("User".As<ClassName>());

		Assert.IsTrue(result);
		Assert.AreEqual(1, schema.Classes.Count);
	}

	[TestMethod]
	public void TestTryAddDuplicateClassReturnsFalse()
	{
		Schema schema = new();
		schema.TryAddClass("User".As<ClassName>());
		bool result = schema.TryAddClass("User".As<ClassName>());

		Assert.IsFalse(result);
	}

	[TestMethod]
	public void TestTryGetClass()
	{
		Schema schema = new();
		SchemaClass? added = schema.AddClass("User".As<ClassName>());

		bool result = schema.TryGetClass("User".As<ClassName>(), out SchemaClass? found);
		Assert.IsTrue(result);
		Assert.AreEqual(added, found);
	}

	[TestMethod]
	public void TestTryGetClassNotFound()
	{
		Schema schema = new();

		bool result = schema.TryGetClass("Missing".As<ClassName>(), out SchemaClass? found);
		Assert.IsFalse(result);
		Assert.IsNull(found);
	}

	[TestMethod]
	public void TestGetClass()
	{
		Schema schema = new();
		SchemaClass? added = schema.AddClass("User".As<ClassName>());

		SchemaClass? found = schema.GetClass("User".As<ClassName>());
		Assert.AreEqual(added, found);
	}

	[TestMethod]
	public void TestClassTryRemove()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		bool result = schemaClass.TryRemove();
		Assert.IsTrue(result);
		Assert.AreEqual(0, schema.Classes.Count);
	}

	[TestMethod]
	public void TestClassTryRemoveWithoutParent()
	{
		SchemaClass schemaClass = new();
		bool result = schemaClass.TryRemove();

		Assert.IsFalse(result);
	}

	[TestMethod]
	public void TestClassSummary()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		schemaClass.AddMember("Name".As<MemberName>());
		schemaClass.AddMember("Age".As<MemberName>());

		Assert.AreEqual("User (2)", schemaClass.ClassSummary);
	}

	[TestMethod]
	public void TestClassParentAssociation()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		Assert.AreEqual(schema, schemaClass.ParentSchema);
	}

	[TestMethod]
	public void TestFirstClass()
	{
		Schema schema = new();
		SchemaClass? first = schema.AddClass("First".As<ClassName>());
		schema.AddClass("Second".As<ClassName>());

		Assert.AreEqual(first, schema.FirstClass);
	}

	[TestMethod]
	public void TestLastClass()
	{
		Schema schema = new();
		schema.AddClass("First".As<ClassName>());
		SchemaClass? last = schema.AddClass("Second".As<ClassName>());

		Assert.AreEqual(last, schema.LastClass);
	}

	[TestMethod]
	public void TestFirstClassEmpty()
	{
		Schema schema = new();
		Assert.IsNull(schema.FirstClass);
	}

	[TestMethod]
	public void TestAddMember()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		SchemaMember? member = schemaClass.AddMember("Name".As<MemberName>());
		Assert.IsNotNull(member);
		Assert.AreEqual("Name".As<MemberName>(), member.Name);
		Assert.AreEqual(1, schemaClass.Members.Count);
	}

	[TestMethod]
	public void TestAddDuplicateMemberReturnsNull()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		schemaClass.AddMember("Name".As<MemberName>());
		SchemaMember? duplicate = schemaClass.AddMember("Name".As<MemberName>());

		Assert.IsNull(duplicate);
		Assert.AreEqual(1, schemaClass.Members.Count);
	}

	[TestMethod]
	public void TestTryGetMember()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		SchemaMember? added = schemaClass.AddMember("Name".As<MemberName>());

		bool result = schemaClass.TryGetMember("Name".As<MemberName>(), out SchemaMember? found);
		Assert.IsTrue(result);
		Assert.AreEqual(added, found);
	}

	[TestMethod]
	public void TestTryGetMemberNotFound()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		bool result = schemaClass.TryGetMember("Missing".As<MemberName>(), out SchemaMember? found);
		Assert.IsFalse(result);
		Assert.IsNull(found);
	}

	[TestMethod]
	public void TestGetMember()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		SchemaMember? added = schemaClass.AddMember("Name".As<MemberName>());

		SchemaMember? found = schemaClass.GetMember("Name".As<MemberName>());
		Assert.AreEqual(added, found);
	}

	[TestMethod]
	public void TestMemberTryRemove()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		SchemaMember? member = schemaClass.AddMember("Name".As<MemberName>());
		Assert.IsNotNull(member);

		bool result = member.TryRemove();
		Assert.IsTrue(result);
		Assert.AreEqual(0, schemaClass.Members.Count);
	}

	[TestMethod]
	public void TestMemberTryRemoveWithoutParent()
	{
		SchemaMember member = new();
		bool result = member.TryRemove();

		Assert.IsFalse(result);
	}

	[TestMethod]
	public void TestMemberSetType()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		SchemaMember? member = schemaClass.AddMember("Name".As<MemberName>());
		Assert.IsNotNull(member);

		member.SetType(new SchemaTypes.String());
		Assert.IsInstanceOfType<SchemaTypes.String>(member.Type);
	}

	[TestMethod]
	public void TestMemberDefaultTypeIsNone()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		SchemaMember? member = schemaClass.AddMember("Name".As<MemberName>());
		Assert.IsNotNull(member);

		Assert.IsInstanceOfType<SchemaTypes.None>(member.Type);
	}

	[TestMethod]
	public void TestMemberParentClassAssociation()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		SchemaMember? member = schemaClass.AddMember("Name".As<MemberName>());
		Assert.IsNotNull(member);

		Assert.AreEqual(schemaClass, member.ParentClass);
	}

	[TestMethod]
	public void TestMemberParentSchemaAssociation()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		SchemaMember? member = schemaClass.AddMember("Name".As<MemberName>());
		Assert.IsNotNull(member);

		Assert.AreEqual(schema, member.ParentSchema);
	}

	[TestMethod]
	public void TestMemberDescription()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		SchemaMember? member = schemaClass.AddMember("Name".As<MemberName>());
		Assert.IsNotNull(member);

		member.MemberDescription = "The user's name";
		Assert.AreEqual("The user's name", member.MemberDescription);
	}

	[TestMethod]
	public void TestMultipleMembers()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		schemaClass.AddMember("Name".As<MemberName>());
		schemaClass.AddMember("Age".As<MemberName>());
		schemaClass.AddMember("Email".As<MemberName>());

		Assert.AreEqual(3, schemaClass.Members.Count);
	}

	[TestMethod]
	public void TestGetTypes()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		SchemaMember? nameMember = schemaClass.AddMember("Name".As<MemberName>());
		Assert.IsNotNull(nameMember);
		nameMember.SetType(new SchemaTypes.String());

		SchemaMember? ageMember = schemaClass.AddMember("Age".As<MemberName>());
		Assert.IsNotNull(ageMember);
		ageMember.SetType(new SchemaTypes.Int());

		List<SchemaTypes.BaseType> types = [.. schema.GetTypes()];
		Assert.AreEqual(2, types.Count);
		Assert.IsTrue(types.Any(t => t is SchemaTypes.String));
		Assert.IsTrue(types.Any(t => t is SchemaTypes.Int));
	}

	[TestMethod]
	public void TestGetTypesDeduplicates()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		SchemaMember? firstName = schemaClass.AddMember("FirstName".As<MemberName>());
		Assert.IsNotNull(firstName);
		firstName.SetType(new SchemaTypes.String());

		SchemaMember? lastName = schemaClass.AddMember("LastName".As<MemberName>());
		Assert.IsNotNull(lastName);
		lastName.SetType(new SchemaTypes.String());

		List<SchemaTypes.BaseType> types = [.. schema.GetTypes()];
		Assert.AreEqual(1, types.Count);
		Assert.IsInstanceOfType<SchemaTypes.String>(types[0]);
	}
}
