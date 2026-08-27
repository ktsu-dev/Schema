// Copyright (c) 2023-2026 ktsu-dev contributors

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
	[DataRow(typeof(Vector2), "Vector2")]
	[DataRow(typeof(Vector3), "Vector3")]
	[DataRow(typeof(Vector4), "Vector4")]
	[DataRow(typeof(ColorRGB), "ColorRGB")]
	[DataRow(typeof(ColorRGBA), "ColorRGBA")]
	public void TestSystemObjectTypeClassification(Type clrType, string expectedName)
	{
		BaseType type = (BaseType)Activator.CreateInstance(clrType)!;
		Assert.IsFalse(type.IsPrimitive);
		Assert.IsTrue(type.IsBuiltIn);
		Assert.IsTrue(type.IsSystemObject);

		// A vector or color is structured, but it is not a reference to a user-defined class,
		// which is what IsObject means. See issue #107.
		Assert.IsFalse(type.IsObject);

		Assert.AreEqual(expectedName, type.ToString());
		Assert.AreEqual(expectedName, type.DisplayName);
	}

	[TestMethod]
	[DataRow("Vector2")]
	[DataRow("Vector3")]
	[DataRow("Vector4")]
	[DataRow("ColorRGB")]
	[DataRow("ColorRGBA")]
	public void TestSystemObjectTypeRoundTripsThroughCreateFromString(string typeName)
	{
		object? recreated = BaseType.CreateFromString(typeName);
		Assert.IsNotNull(recreated);
		Assert.AreEqual(typeName, recreated.ToString());
	}

	[TestMethod]
	public void TestSystemObjectAndVectorAreAbstract()
	{
		// Neither is registered as a [JsonDerivedType] on BaseType, so an instance of either
		// would fail to serialize. They exist only as intermediate bases.
		Assert.IsTrue(typeof(SystemObject).IsAbstract);
		Assert.IsTrue(typeof(Vector).IsAbstract);
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
	public void TestDistinctInstancesOfSameTypeAreEqual()
	{
		Assert.IsTrue(new Int().Equals(new Int()));
		Assert.IsTrue(new String().Equals(new String()));
		Assert.IsTrue(new Vector3().Equals(new Vector3()));
	}

	[TestMethod]
	public void TestDerivedSystemObjectTypesAreNotEqualToTheirBase()
	{
		// ColorRGB derives from Vector3, but they are distinct schema types.
		Assert.IsFalse(new ColorRGB().Equals(new Vector3()));
		Assert.IsFalse(new Vector3().Equals(new ColorRGB()));
	}

	[TestMethod]
	public void TestObjectEqualityComparesClassName()
	{
		Object a = new() { ClassName = "A".As<ClassName>() };
		Object b = new() { ClassName = "B".As<ClassName>() };
		Object anotherA = new() { ClassName = "A".As<ClassName>() };

		Assert.IsFalse(a.Equals(b));
		Assert.IsTrue(a.Equals(anotherA));
		Assert.AreEqual(a.GetHashCode(), anotherA.GetHashCode());
	}

	[TestMethod]
	public void TestEnumEqualityComparesEnumName()
	{
		Enum a = new() { EnumName = "Color".As<EnumName>() };
		Enum b = new() { EnumName = "Shape".As<EnumName>() };
		Enum anotherA = new() { EnumName = "Color".As<EnumName>() };

		Assert.IsFalse(a.Equals(b));
		Assert.IsTrue(a.Equals(anotherA));
		Assert.AreEqual(a.GetHashCode(), anotherA.GetHashCode());
	}

	[TestMethod]
	public void TestArrayEqualityComparesElementTypeContainerAndKey()
	{
		Array intVector = new() { ElementType = new Int(), Container = "vector".As<ContainerName>() };
		Array stringVector = new() { ElementType = new String(), Container = "vector".As<ContainerName>() };
		Array intMap = new() { ElementType = new Int(), Container = "map".As<ContainerName>() };
		Array keyedIntVector = new()
		{
			ElementType = new Int(),
			Container = "vector".As<ContainerName>(),
			Key = "Id".As<MemberName>(),
		};
		Array anotherIntVector = new() { ElementType = new Int(), Container = "vector".As<ContainerName>() };

		Assert.IsFalse(intVector.Equals(stringVector));
		Assert.IsFalse(intVector.Equals(intMap));
		Assert.IsFalse(intVector.Equals(keyedIntVector));
		Assert.IsTrue(intVector.Equals(anotherIntVector));
		Assert.AreEqual(intVector.GetHashCode(), anotherIntVector.GetHashCode());
	}

	[TestMethod]
	public void TestEqualInstancesDedupeInAHashSet()
	{
		HashSet<BaseType> types =
		[
			new Int(),
			new Int(),
			new String(),
			new Object() { ClassName = "A".As<ClassName>() },
			new Object() { ClassName = "A".As<ClassName>() },
			new Object() { ClassName = "B".As<ClassName>() },
		];

		Assert.AreEqual(4, types.Count);
	}

	[TestMethod]
	public void TestEqualityOperators()
	{
		Int a = new();
		Int b = new();
		String s = new();

		Assert.IsTrue(a == b);
		Assert.IsFalse(a != b);
		Assert.IsTrue(a != s);
		Assert.IsFalse(a == s);
	}

	[TestMethod]
	public void TestEqualityOperatorsWithNull()
	{
		// Indirected through an array so the operands are not constant-folded away.
		BaseType?[] operands = [new Int(), null];
		BaseType? value = operands[0];
		BaseType? nothing = operands[1];

		Assert.IsFalse(value == nothing);
		Assert.IsTrue(value != nothing);
		Assert.IsFalse(nothing == value);
		Assert.IsTrue(nothing != value);
		Assert.IsTrue(nothing == operands[1]);
	}

	[TestMethod]
	public void TestEqualsObjectOverload()
	{
		object?[] operands = [new Int(), new String(), null, "Int"];
		Int a = new();

		Assert.IsTrue(a.Equals(operands[0]));
		Assert.IsFalse(a.Equals(operands[1]));
		Assert.IsFalse(a.Equals(operands[2]));
		Assert.IsFalse(a.Equals(operands[3]));
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
