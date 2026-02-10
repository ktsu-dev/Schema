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
public class AddClassFromTypeTests
{
	[TestMethod]
	public void TestAddClassFromTypeCreatesClass()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass(typeof(SimpleClass));

		Assert.IsNotNull(schemaClass);
		Assert.AreEqual("SimpleClass".As<ClassName>(), schemaClass.Name);
	}

	[TestMethod]
	public void TestAddClassFromTypeCreatesStringMember()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass(typeof(SimpleClass));
		Assert.IsNotNull(schemaClass);

		Assert.IsTrue(schemaClass.TryGetMember("Name".As<MemberName>(), out SchemaMember? member));
		Assert.IsNotNull(member);
		Assert.IsInstanceOfType<SchemaTypes.String>(member.Type);
	}

	[TestMethod]
	public void TestAddClassFromTypeCreatesIntMember()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass(typeof(SimpleClass));
		Assert.IsNotNull(schemaClass);

		Assert.IsTrue(schemaClass.TryGetMember("Value".As<MemberName>(), out SchemaMember? member));
		Assert.IsNotNull(member);
		Assert.IsInstanceOfType<SchemaTypes.Int>(member.Type);
	}

	[TestMethod]
	public void TestAddClassFromTypeCreatesBoolMember()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass(typeof(TypeWithBool));
		Assert.IsNotNull(schemaClass);

		Assert.IsTrue(schemaClass.TryGetMember("IsActive".As<MemberName>(), out SchemaMember? member));
		Assert.IsNotNull(member);
		Assert.IsInstanceOfType<SchemaTypes.Bool>(member.Type);
	}

	[TestMethod]
	public void TestAddClassFromTypeCreatesFloatMember()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass(typeof(TypeWithFloat));
		Assert.IsNotNull(schemaClass);

		Assert.IsTrue(schemaClass.TryGetMember("Score".As<MemberName>(), out SchemaMember? member));
		Assert.IsNotNull(member);
		Assert.IsInstanceOfType<SchemaTypes.Float>(member.Type);
	}

	[TestMethod]
	public void TestAddClassFromTypeHandlesEnum()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass(typeof(TypeWithEnumProperty));
		Assert.IsNotNull(schemaClass);

		Assert.IsTrue(schemaClass.TryGetMember("Status".As<MemberName>(), out SchemaMember? member));
		Assert.IsNotNull(member);
		Assert.IsInstanceOfType<SchemaTypes.Enum>(member.Type);

		// Check that the enum was also added to the schema
		Assert.IsTrue(schema.TryGetEnum("TestStatus".As<EnumName>(), out SchemaEnum? schemaEnum));
		Assert.IsNotNull(schemaEnum);
		Assert.AreEqual(3, schemaEnum.Values.Count);
	}

	[TestMethod]
	public void TestTryAddClassFromType()
	{
		Schema schema = new();
		bool result = schema.TryAddClass(typeof(SimpleClass));
		Assert.IsTrue(result);
		Assert.AreEqual(1, schema.Classes.Count);
	}

	[TestMethod]
	public void TestAddDuplicateClassFromType()
	{
		Schema schema = new();
		schema.AddClass(typeof(SimpleClass));
		SchemaClass? duplicate = schema.AddClass(typeof(SimpleClass));
		Assert.IsNull(duplicate);
	}
}

// Test data classes
public class SimpleClass
{
	public string Name { get; set; } = string.Empty;
	public int Value { get; set; }
}

public class TypeWithBool
{
	public bool IsActive { get; set; }
}

public class TypeWithFloat
{
	public float Score { get; set; }
}

public enum TestStatus
{
	Active,
	Inactive,
	Pending,
}

public class TypeWithEnumProperty
{
	public TestStatus Status { get; set; }
}
