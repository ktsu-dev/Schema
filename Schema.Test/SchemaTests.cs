// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Tests;

using System.Collections.ObjectModel;

using ktsu.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class SchemaTests
{
	[TestMethod]
	public void TestReassociate()
	{
		SchemaProvider schemaProvider = new();
		ClassName className = new();
		SchemaClass? schemaClass = schemaProvider.AddClass(className);

		schemaProvider.Reassociate();
		Assert.IsNotNull(schemaClass);
		Assert.AreEqual(schemaProvider, schemaClass.ParentSchema);
	}

	[TestMethod]
	public void TestTryRemoveChild()
	{
		Collection<SchemaClass> collection = [];
		SchemaClass schemaClass = new();
		collection.Add(schemaClass);

		bool result = SchemaProvider.TryRemoveChild(schemaClass, collection);
		Assert.IsTrue(result);
		Assert.AreEqual(0, collection.Count);
	}

	[TestMethod]
	public void TestGetChild()
	{
		Collection<SchemaClass> collection = [];
		SchemaClass schemaClass = new();
		ClassName className = new();
		schemaClass.Rename(className);
		collection.Add(schemaClass);

		SchemaClass? result = SchemaProvider.GetChild(className, collection);
		Assert.IsNotNull(result);
		Assert.AreEqual(schemaClass, result);
	}

	[TestMethod]
	public void TestTryGetChild()
	{
		Collection<SchemaClass> collection = [];
		SchemaClass schemaClass = new();
		ClassName className = new();
		schemaClass.Rename(className);
		collection.Add(schemaClass);

		bool result = SchemaProvider.TryGetChild(className, collection, out SchemaClass? foundClass);
		Assert.IsTrue(result);
		Assert.AreEqual(schemaClass, foundClass);
	}

	[TestMethod]
	public void TestAddChild()
	{
		SchemaProvider schemaProvider = new();
		ClassName className = new();
		SchemaClass? schemaClass = schemaProvider.AddChild(className, schemaProvider.ClassesInternal);

		Assert.IsNotNull(schemaClass);
		Assert.AreEqual(1, schemaProvider.ClassesInternal.Count);
	}

	[TestMethod]
	public void TestTryAddEnum()
	{
		SchemaProvider schemaProvider = new();
		EnumName enumName = new();
		bool result = schemaProvider.TryAddEnum(enumName);

		Assert.IsTrue(result);
	}

	[TestMethod]
	public void TestTryAddClass()
	{
		SchemaProvider schemaProvider = new();
		ClassName className = new();
		bool result = schemaProvider.TryAddClass(className);

		Assert.IsTrue(result);
	}

	[TestMethod]
	public void TestAddEnum()
	{
		SchemaProvider schemaProvider = new();
		EnumName enumName = new();
		SchemaEnum? schemaEnum = schemaProvider.AddEnum(enumName);

		Assert.IsNotNull(schemaEnum);
	}

	[TestMethod]
	public void TestAddClass()
	{
		SchemaProvider schemaProvider = new();
		ClassName className = new();
		SchemaClass? schemaClass = schemaProvider.AddClass(className);

		Assert.IsNotNull(schemaClass);
	}

	[TestMethod]
	public void TestAddClassFromType()
	{
		SchemaProvider schemaProvider = new();
		SchemaClass? schemaClass = schemaProvider.AddClass(typeof(TestData));

		Assert.IsNotNull(schemaClass);
		// We can't easily test the name without knowing how to create a comparable ClassName
	}

	[TestMethod]
	public void TestTryAddClassFromType()
	{
		SchemaProvider schemaProvider = new();
		bool result = schemaProvider.TryAddClass(typeof(TestData));

		Assert.IsTrue(result);
	}

	[TestMethod]
	public void TestFirstClass()
	{
		SchemaProvider schemaProvider = new();
		ClassName className = new();
		SchemaClass? schemaClass = schemaProvider.AddClass(className);

		Assert.AreEqual(schemaClass, schemaProvider.FirstClass);
	}

	[TestMethod]
	public void TestLastClass()
	{
		SchemaProvider schemaProvider = new();
		ClassName className = new();
		SchemaClass? schemaClass = schemaProvider.AddClass(className);

		Assert.AreEqual(schemaClass, schemaProvider.LastClass);
	}

	[TestMethod]
	public void TestGetTypes()
	{
		SchemaProvider schemaProvider = new();
		ClassName className = new();
		SchemaClass? schemaClass = schemaProvider.AddClass(className);
		Assert.IsNotNull(schemaClass);

		MemberName memberName = new();
		SchemaMember? member = schemaClass.AddMember(memberName);
		Assert.IsNotNull(member);
		member.SetType(new SchemaTypes.String());

		List<SchemaTypes.BaseType> types = [.. schemaProvider.GetTypes()];
		Assert.IsTrue(types.Any(t => t is SchemaTypes.String));
	}

	[TestMethod]
	public void TestTryGetEnum()
	{
		SchemaProvider schemaProvider = new();
		EnumName enumName = new();
		schemaProvider.AddEnum(enumName);

		bool result = schemaProvider.TryGetEnum(enumName, out SchemaEnum? foundEnum);
		Assert.IsTrue(result);
		Assert.IsNotNull(foundEnum);
	}

	[TestMethod]
	public void TestTryGetClass()
	{
		SchemaProvider schemaProvider = new();
		ClassName className = new();
		schemaProvider.AddClass(className);

		bool result = schemaProvider.TryGetClass(className, out SchemaClass? foundClass);
		Assert.IsTrue(result);
		Assert.IsNotNull(foundClass);
	}

	[TestMethod]
	public void TestGetEnum()
	{
		SchemaProvider schemaProvider = new();
		EnumName enumName = new();
		SchemaEnum? addedEnum = schemaProvider.AddEnum(enumName);

		SchemaEnum? foundEnum = schemaProvider.GetEnum(enumName);
		Assert.AreEqual(addedEnum, foundEnum);
	}

	[TestMethod]
	public void TestGetClass()
	{
		SchemaProvider schemaProvider = new();
		ClassName className = new();
		SchemaClass? addedClass = schemaProvider.AddClass(className);

		SchemaClass? foundClass = schemaProvider.GetClass(className);
		Assert.AreEqual(addedClass, foundClass);
	}
}

// Test data class for AddClass(Type) tests
public class TestData
{
	public string Name { get; set; } = string.Empty;
	public int Value { get; set; }
}
