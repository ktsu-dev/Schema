// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Tests;

using System.Collections.ObjectModel;
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

	[TestMethod]
	public void TestAddClassFromTypeCreatesLongMember()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass(typeof(TypeWithWideNumerics));
		Assert.IsNotNull(schemaClass);

		Assert.IsTrue(schemaClass.TryGetMember("BigCount".As<MemberName>(), out SchemaMember? member));
		Assert.IsNotNull(member);
		Assert.IsInstanceOfType<SchemaTypes.Long>(member.Type);
	}

	[TestMethod]
	public void TestAddClassFromTypeCreatesDoubleMember()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass(typeof(TypeWithWideNumerics));
		Assert.IsNotNull(schemaClass);

		Assert.IsTrue(schemaClass.TryGetMember("Precise".As<MemberName>(), out SchemaMember? member));
		Assert.IsNotNull(member);
		Assert.IsInstanceOfType<SchemaTypes.Double>(member.Type);

		Assert.IsTrue(schemaClass.TryGetMember("Money".As<MemberName>(), out SchemaMember? decimalMember));
		Assert.IsNotNull(decimalMember);
		Assert.IsInstanceOfType<SchemaTypes.Double>(decimalMember.Type);
	}

	[TestMethod]
	public void TestAddClassFromTypeCreatesTemporalMembers()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass(typeof(TypeWithTemporals));
		Assert.IsNotNull(schemaClass);

		Assert.IsTrue(schemaClass.TryGetMember("CreatedAt".As<MemberName>(), out SchemaMember? dateTimeMember));
		Assert.IsNotNull(dateTimeMember);
		Assert.IsInstanceOfType<SchemaTypes.DateTime>(dateTimeMember.Type);

		Assert.IsTrue(schemaClass.TryGetMember("Duration".As<MemberName>(), out SchemaMember? timeSpanMember));
		Assert.IsNotNull(timeSpanMember);
		Assert.IsInstanceOfType<SchemaTypes.TimeSpan>(timeSpanMember.Type);
	}

	[TestMethod]
	public void TestAddClassFromTypeUnwrapsNullable()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass(typeof(TypeWithNullable));
		Assert.IsNotNull(schemaClass);

		Assert.IsTrue(schemaClass.TryGetMember("MaybeValue".As<MemberName>(), out SchemaMember? member));
		Assert.IsNotNull(member);
		Assert.IsInstanceOfType<SchemaTypes.Int>(member.Type);
	}

	[TestMethod]
	public void TestAddClassFromTypeCreatesArrayFromArray()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass(typeof(TypeWithCollections));
		Assert.IsNotNull(schemaClass);

		Assert.IsTrue(schemaClass.TryGetMember("Scores".As<MemberName>(), out SchemaMember? member));
		Assert.IsNotNull(member);
		SchemaTypes.Array? arrayType = member.Type as SchemaTypes.Array;
		Assert.IsNotNull(arrayType);
		Assert.IsInstanceOfType<SchemaTypes.Int>(arrayType.ElementType);
		Assert.AreEqual("vector".As<ContainerName>(), arrayType.Container);
	}

	[TestMethod]
	public void TestAddClassFromTypeCreatesArrayFromList()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass(typeof(TypeWithCollections));
		Assert.IsNotNull(schemaClass);

		Assert.IsTrue(schemaClass.TryGetMember("Tags".As<MemberName>(), out SchemaMember? member));
		Assert.IsNotNull(member);
		SchemaTypes.Array? arrayType = member.Type as SchemaTypes.Array;
		Assert.IsNotNull(arrayType);
		Assert.IsInstanceOfType<SchemaTypes.String>(arrayType.ElementType);
		Assert.AreEqual("vector".As<ContainerName>(), arrayType.Container);
	}

	[TestMethod]
	public void TestAddClassFromTypeCreatesObjectArrayAndAddsElementClass()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass(typeof(TypeWithCollections));
		Assert.IsNotNull(schemaClass);

		Assert.IsTrue(schemaClass.TryGetMember("Items".As<MemberName>(), out SchemaMember? member));
		Assert.IsNotNull(member);
		SchemaTypes.Array? arrayType = member.Type as SchemaTypes.Array;
		Assert.IsNotNull(arrayType);
		SchemaTypes.Object? elementType = arrayType.ElementType as SchemaTypes.Object;
		Assert.IsNotNull(elementType);
		Assert.AreEqual("SimpleClass".As<ClassName>(), elementType.ClassName);
		Assert.IsTrue(schema.TryGetClass("SimpleClass".As<ClassName>(), out _));
	}

	[TestMethod]
	public void TestAddClassFromTypeCreatesMapFromDictionary()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass(typeof(TypeWithCollections));
		Assert.IsNotNull(schemaClass);

		Assert.IsTrue(schemaClass.TryGetMember("ItemsById".As<MemberName>(), out SchemaMember? member));
		Assert.IsNotNull(member);
		SchemaTypes.Array? arrayType = member.Type as SchemaTypes.Array;
		Assert.IsNotNull(arrayType);
		Assert.IsInstanceOfType<SchemaTypes.Object>(arrayType.ElementType);
		Assert.AreEqual("map".As<ContainerName>(), arrayType.Container);
	}

	[TestMethod]
	public void TestAddClassFromTypeDoesNotTreatStringAsCollection()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass(typeof(SimpleClass));
		Assert.IsNotNull(schemaClass);

		Assert.IsTrue(schemaClass.TryGetMember("Name".As<MemberName>(), out SchemaMember? member));
		Assert.IsNotNull(member);
		Assert.IsInstanceOfType<SchemaTypes.String>(member.Type);
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

public class TypeWithWideNumerics
{
	public long BigCount { get; set; }
	public double Precise { get; set; }
	public decimal Money { get; set; }
}

public class TypeWithTemporals
{
	public DateTime CreatedAt { get; set; }
	public TimeSpan Duration { get; set; }
}

public class TypeWithNullable
{
	public int? MaybeValue { get; set; }
}

public class TypeWithCollections
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Exercises array import via reflection")]
	public int[] Scores { get; } = [];
	public Collection<string> Tags { get; } = [];
	public Collection<SimpleClass> Items { get; } = [];
	public Dictionary<string, SimpleClass> ItemsById { get; } = [];
}
