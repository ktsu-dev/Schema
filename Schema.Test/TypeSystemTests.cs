// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TypeSystemTests
{
	[TestMethod]
	public void TestIntType()
	{
		Int type = new();
		Assert.IsTrue(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsTrue(type.IsIntegral);
		Assert.IsTrue(type.IsNumeric);
		Assert.IsFalse(type.IsDecimal);
		Assert.IsFalse(type.IsContainer);
		Assert.IsFalse(type.IsObject);
		Assert.IsFalse(type.IsArray);
		Assert.AreEqual("Int", type.ToString());
		Assert.AreEqual("Int", type.DisplayName);
	}

	[TestMethod]
	public void TestLongType()
	{
		Long type = new();
		Assert.IsTrue(type.IsPrimitive, "Long should not be primitive according to the set definition");
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsTrue(type.IsIntegral);
		Assert.IsTrue(type.IsNumeric);
		Assert.IsFalse(type.IsDecimal);
		Assert.AreEqual("Long", type.ToString());
	}

	[TestMethod]
	public void TestFloatType()
	{
		Float type = new();
		Assert.IsTrue(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsTrue(type.IsDecimal);
		Assert.IsTrue(type.IsNumeric);
		Assert.IsFalse(type.IsIntegral);
		Assert.AreEqual("Float", type.ToString());
	}

	[TestMethod]
	public void TestDoubleType()
	{
		Double type = new();
		Assert.IsTrue(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsTrue(type.IsDecimal);
		Assert.IsTrue(type.IsNumeric);
		Assert.IsFalse(type.IsIntegral);
		Assert.AreEqual("Double", type.ToString());
	}

	[TestMethod]
	public void TestStringType()
	{
		String type = new();
		Assert.IsTrue(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsFalse(type.IsNumeric);
		Assert.AreEqual("String", type.ToString());
	}

	[TestMethod]
	public void TestBoolType()
	{
		Bool type = new();
		Assert.IsTrue(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsFalse(type.IsNumeric);
		Assert.AreEqual("Bool", type.ToString());
	}

	[TestMethod]
	public void TestDateTimeType()
	{
		DateTime type = new();
		Assert.IsFalse(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.AreEqual("DateTime", type.ToString());
	}

	[TestMethod]
	public void TestTimeSpanType()
	{
		TimeSpan type = new();
		Assert.IsFalse(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.AreEqual("TimeSpan", type.ToString());
	}

	[TestMethod]
	public void TestVector2Type()
	{
		Vector2 type = new();
		Assert.IsFalse(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsTrue(type.IsObject);
		Assert.IsTrue(type.IsSystemObject);
	}

	[TestMethod]
	public void TestVector3Type()
	{
		Vector3 type = new();
		Assert.IsFalse(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsTrue(type.IsObject);
	}

	[TestMethod]
	public void TestVector4Type()
	{
		Vector4 type = new();
		Assert.IsFalse(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsTrue(type.IsObject);
	}

	[TestMethod]
	public void TestColorRGBType()
	{
		ColorRGB type = new();
		Assert.IsFalse(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsTrue(type.IsObject);
	}

	[TestMethod]
	public void TestColorRGBAType()
	{
		ColorRGBA type = new();
		Assert.IsFalse(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsTrue(type.IsObject);
	}

	[TestMethod]
	public void TestNoneType()
	{
		None type = new();
		Assert.IsFalse(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsFalse(type.IsNumeric);
		Assert.AreEqual("None", type.ToString());
	}

	[TestMethod]
	public void TestCreateFromStringValid()
	{
		object? result = BaseType.CreateFromString("Int");
		Assert.IsNotNull(result);
		Assert.IsInstanceOfType<Int>(result);
	}

	[TestMethod]
	public void TestCreateFromStringInvalid()
	{
		object? result = BaseType.CreateFromString("NonExistentType");
		Assert.IsNull(result);
	}

	[TestMethod]
	public void TestCreateFromStringNull()
	{
		object? result = BaseType.CreateFromString(null);
		Assert.IsNull(result);
	}

	[TestMethod]
	public void TestCreateFromStringEmpty()
	{
		object? result = BaseType.CreateFromString(string.Empty);
		Assert.IsNull(result);
	}

	[TestMethod]
	public void TestTypeEqualitySameReference()
	{
		Int a = new();
		Assert.IsTrue(a.Equals(a));
	}

	[TestMethod]
	public void TestTypeDifferentNotEqual()
	{
		Int intType = new();
		String stringType = new();
		Assert.IsFalse(intType.Equals(stringType));
	}

	[TestMethod]
	public void TestTypeHashCode()
	{
		Int a = new();
		Int b = new();
		Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
	}

	[TestMethod]
	public void TestArrayType()
	{
		Array arrayType = new() { ElementType = new Int() };
		Assert.IsTrue(arrayType.IsArray);
		Assert.IsTrue(arrayType.IsContainer);
		Assert.IsFalse(arrayType.IsBuiltIn);
		Assert.IsFalse(arrayType.IsPrimitive);
		Assert.IsTrue(arrayType.IsPrimitiveArray);
		Assert.IsFalse(arrayType.IsComplexArray);
	}

	[TestMethod]
	public void TestArrayDisplayName()
	{
		Array arrayType = new() { ElementType = new Int() };
		Assert.AreEqual("Array(Int)", arrayType.DisplayName);
	}

	[TestMethod]
	public void TestArrayDefaultElementType()
	{
		Array arrayType = new();
		Assert.IsInstanceOfType<None>(arrayType.ElementType);
	}

	[TestMethod]
	public void TestComplexArray()
	{
		Array arrayType = new()
		{
			ElementType = new Object() { ClassName = "User".As<ClassName>() },
		};
		Assert.IsTrue(arrayType.IsComplexArray);
		Assert.IsFalse(arrayType.IsPrimitiveArray);
	}

	[TestMethod]
	public void TestKeyedArray()
	{
		Schema schema = new();
		SchemaClass? userClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(userClass);
		SchemaMember? idMember = userClass.AddMember("Id".As<MemberName>());
		Assert.IsNotNull(idMember);
		idMember.SetType(new Int());

		Array arrayType = new()
		{
			ElementType = new Object() { ClassName = "User".As<ClassName>() },
			Key = "Id".As<MemberName>(),
			Container = "Users".As<ContainerName>(),
		};
		Assert.IsTrue(arrayType.IsKeyed);
	}

	[TestMethod]
	public void TestNotKeyedArray()
	{
		Array arrayType = new()
		{
			ElementType = new Int(),
		};
		Assert.IsFalse(arrayType.IsKeyed);
	}

	[TestMethod]
	public void TestObjectType()
	{
		Object objectType = new() { ClassName = "User".As<ClassName>() };
		Assert.IsTrue(objectType.IsObject);
		Assert.IsFalse(objectType.IsBuiltIn);
		Assert.AreEqual("User", objectType.ToString());
	}

	[TestMethod]
	public void TestObjectClassResolution()
	{
		Schema schema = new();
		SchemaClass? userClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(userClass);

		SchemaMember? member = userClass.AddMember("Friend".As<MemberName>());
		Assert.IsNotNull(member);

		Object objectType = new() { ClassName = "User".As<ClassName>() };
		member.SetType(objectType);

		Assert.AreEqual(userClass, objectType.Class);
	}

	[TestMethod]
	public void TestEnumType()
	{
		Enum enumType = new() { EnumName = "Color".As<EnumName>() };
		Assert.IsFalse(enumType.IsBuiltIn);
		Assert.IsFalse(enumType.IsPrimitive);
	}

	[TestMethod]
	public void TestEnumDisplayName()
	{
		Enum enumType = new() { EnumName = "Color".As<EnumName>() };
		Assert.AreEqual("Enum(Color)", enumType.DisplayName);
	}

	[TestMethod]
	public void TestTypeParentMemberAssociation()
	{
		Schema schema = new();
		SchemaClass? schemaClass = schema.AddClass("User".As<ClassName>());
		Assert.IsNotNull(schemaClass);

		SchemaMember? member = schemaClass.AddMember("Name".As<MemberName>());
		Assert.IsNotNull(member);

		String stringType = new();
		member.SetType(stringType);

		Assert.AreEqual(member, stringType.ParentMember);
	}
}
