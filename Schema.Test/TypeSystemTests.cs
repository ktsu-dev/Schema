// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SchemaTypes = ktsu.Schema.Models.Types;

[TestClass]
public class TypeSystemTests
{
	[TestMethod]
	public void TestIntType()
	{
		SchemaTypes.Int type = new();
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
		SchemaTypes.Long type = new();
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
		SchemaTypes.Float type = new();
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
		SchemaTypes.Double type = new();
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
		SchemaTypes.String type = new();
		Assert.IsTrue(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsFalse(type.IsNumeric);
		Assert.AreEqual("String", type.ToString());
	}

	[TestMethod]
	public void TestBoolType()
	{
		SchemaTypes.Bool type = new();
		Assert.IsTrue(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsFalse(type.IsNumeric);
		Assert.AreEqual("Bool", type.ToString());
	}

	[TestMethod]
	public void TestDateTimeType()
	{
		SchemaTypes.DateTime type = new();
		Assert.IsFalse(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.AreEqual("DateTime", type.ToString());
	}

	[TestMethod]
	public void TestTimeSpanType()
	{
		SchemaTypes.TimeSpan type = new();
		Assert.IsFalse(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.AreEqual("TimeSpan", type.ToString());
	}

	[TestMethod]
	public void TestVector2Type()
	{
		SchemaTypes.Vector2 type = new();
		Assert.IsFalse(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsTrue(type.IsObject);
		Assert.IsTrue(type.IsSystemObject);
	}

	[TestMethod]
	public void TestVector3Type()
	{
		SchemaTypes.Vector3 type = new();
		Assert.IsFalse(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsTrue(type.IsObject);
	}

	[TestMethod]
	public void TestVector4Type()
	{
		SchemaTypes.Vector4 type = new();
		Assert.IsFalse(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsTrue(type.IsObject);
	}

	[TestMethod]
	public void TestColorRGBType()
	{
		SchemaTypes.ColorRGB type = new();
		Assert.IsFalse(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsTrue(type.IsObject);
	}

	[TestMethod]
	public void TestColorRGBAType()
	{
		SchemaTypes.ColorRGBA type = new();
		Assert.IsFalse(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsTrue(type.IsObject);
	}

	[TestMethod]
	public void TestNoneType()
	{
		SchemaTypes.None type = new();
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
		Assert.IsInstanceOfType<SchemaTypes.Int>(result);
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
		SchemaTypes.Int a = new();
		Assert.IsTrue(a.Equals(a));
	}

	[TestMethod]
	public void TestTypeDifferentNotEqual()
	{
		SchemaTypes.Int intType = new();
		SchemaTypes.String stringType = new();
		Assert.IsFalse(intType.Equals(stringType));
	}

	[TestMethod]
	public void TestTypeHashCode()
	{
		SchemaTypes.Int a = new();
		SchemaTypes.Int b = new();
		Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
	}

	[TestMethod]
	public void TestArrayType()
	{
		SchemaTypes.Array arrayType = new() { ElementType = new SchemaTypes.Int() };
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
		SchemaTypes.Array arrayType = new() { ElementType = new SchemaTypes.Int() };
		Assert.AreEqual("Array(Int)", arrayType.DisplayName);
	}

	[TestMethod]
	public void TestArrayDefaultElementType()
	{
		SchemaTypes.Array arrayType = new();
		Assert.IsInstanceOfType<SchemaTypes.None>(arrayType.ElementType);
	}

	[TestMethod]
	public void TestComplexArray()
	{
		SchemaTypes.Array arrayType = new()
		{
			ElementType = new SchemaTypes.Object() { ClassName = "User".As<ClassName>() },
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
		idMember.SetType(new SchemaTypes.Int());

		SchemaTypes.Array arrayType = new()
		{
			ElementType = new SchemaTypes.Object() { ClassName = "User".As<ClassName>() },
			Key = "Id".As<MemberName>(),
			Container = "Users".As<ContainerName>(),
		};
		Assert.IsTrue(arrayType.IsKeyed);
	}

	[TestMethod]
	public void TestNotKeyedArray()
	{
		SchemaTypes.Array arrayType = new()
		{
			ElementType = new SchemaTypes.Int(),
		};
		Assert.IsFalse(arrayType.IsKeyed);
	}

	[TestMethod]
	public void TestObjectType()
	{
		SchemaTypes.Object objectType = new() { ClassName = "User".As<ClassName>() };
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

		SchemaTypes.Object objectType = new() { ClassName = "User".As<ClassName>() };
		member.SetType(objectType);

		Assert.AreEqual(userClass, objectType.Class);
	}

	[TestMethod]
	public void TestEnumType()
	{
		SchemaTypes.Enum enumType = new() { EnumName = "Color".As<EnumName>() };
		Assert.IsFalse(enumType.IsBuiltIn);
		Assert.IsFalse(enumType.IsPrimitive);
	}

	[TestMethod]
	public void TestEnumDisplayName()
	{
		SchemaTypes.Enum enumType = new() { EnumName = "Color".As<EnumName>() };
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

		SchemaTypes.String stringType = new();
		member.SetType(stringType);

		Assert.AreEqual(member, stringType.ParentMember);
	}
}
