// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SchemaTypes = Models.Types;

[TestClass]
public class SchemaSerializerTests
{
	[TestMethod]
	public void TestSerializeEmptySchema()
	{
		Schema schema = new();
		string json = SchemaSerializer.Serialize(schema);
		Assert.IsFalse(string.IsNullOrEmpty(json));
	}

	[TestMethod]
	public void TestDeserializeEmptySchema()
	{
		Schema original = new();
		string json = SchemaSerializer.Serialize(original);

		bool result = SchemaSerializer.TryDeserialize(json, out Schema? deserialized);
		Assert.IsTrue(result);
		Assert.IsNotNull(deserialized);
	}

	[TestMethod]
	public void TestRoundtripWithClasses()
	{
		Schema original = new();
		SchemaClass? userClass = original.AddClass("User".As<ClassName>());
		Assert.IsNotNull(userClass);
		SchemaMember? nameMember = userClass.AddMember("Name".As<MemberName>());
		Assert.IsNotNull(nameMember);
		nameMember.SetType(new SchemaTypes.String());

		string json = SchemaSerializer.Serialize(original);
		bool result = SchemaSerializer.TryDeserialize(json, out Schema? deserialized);

		Assert.IsTrue(result);
		Assert.IsNotNull(deserialized);
		Assert.AreEqual(1, deserialized.Classes.Count);
		Assert.IsTrue(deserialized.TryGetClass("User".As<ClassName>(), out SchemaClass? deserializedClass));
		Assert.IsNotNull(deserializedClass);
		Assert.AreEqual(1, deserializedClass.Members.Count);
	}

	[TestMethod]
	public void TestRoundtripWithEnums()
	{
		Schema original = new();
		SchemaEnum? roleEnum = original.AddEnum("Role".As<EnumName>());
		Assert.IsNotNull(roleEnum);
		roleEnum.TryAddValue("Admin".As<EnumValueName>());
		roleEnum.TryAddValue("User".As<EnumValueName>());

		string json = SchemaSerializer.Serialize(original);
		bool result = SchemaSerializer.TryDeserialize(json, out Schema? deserialized);

		Assert.IsTrue(result);
		Assert.IsNotNull(deserialized);
		Assert.AreEqual(1, deserialized.Enums.Count);
	}

	[TestMethod]
	public void TestRoundtripWithAllTypes()
	{
		Schema original = new();
		SchemaClass? testClass = original.AddClass("Test".As<ClassName>());
		Assert.IsNotNull(testClass);

		SchemaTypes.BaseType[] types =
		[
			new SchemaTypes.Int(),
			new SchemaTypes.Long(),
			new SchemaTypes.Float(),
			new SchemaTypes.Double(),
			new SchemaTypes.String(),
			new SchemaTypes.Bool(),
			new SchemaTypes.DateTime(),
			new SchemaTypes.TimeSpan(),
			new SchemaTypes.Vector2(),
			new SchemaTypes.Vector3(),
			new SchemaTypes.Vector4(),
			new SchemaTypes.ColorRGB(),
			new SchemaTypes.ColorRGBA(),
			new SchemaTypes.None(),
		];

		int i = 0;
		foreach (SchemaTypes.BaseType type in types)
		{
			SchemaMember? member = testClass.AddMember($"Field{i++}".As<MemberName>());
			Assert.IsNotNull(member);
			member.SetType(type);
		}

		string json = SchemaSerializer.Serialize(original);
		bool result = SchemaSerializer.TryDeserialize(json, out Schema? deserialized);

		Assert.IsTrue(result);
		Assert.IsNotNull(deserialized);
		Assert.IsTrue(deserialized.TryGetClass("Test".As<ClassName>(), out SchemaClass? deserializedClass));
		Assert.IsNotNull(deserializedClass);
		Assert.AreEqual(types.Length, deserializedClass.Members.Count);
	}

	[TestMethod]
	public void TestDeserializeInvalidJson()
	{
		bool result = SchemaSerializer.TryDeserialize("not valid json", out Schema? schema);
		Assert.IsFalse(result);
		Assert.IsNull(schema);
	}

	[TestMethod]
	public void TestRoundtripReassociatesParents()
	{
		Schema original = new();
		SchemaClass? userClass = original.AddClass("User".As<ClassName>());
		Assert.IsNotNull(userClass);
		SchemaMember? member = userClass.AddMember("Name".As<MemberName>());
		Assert.IsNotNull(member);
		member.SetType(new SchemaTypes.String());

		string json = SchemaSerializer.Serialize(original);
		SchemaSerializer.TryDeserialize(json, out Schema? deserialized);
		Assert.IsNotNull(deserialized);

		// Verify parent references were re-established
		SchemaClass? deserializedClass = deserialized.Classes.First();
		Assert.AreEqual(deserialized, deserializedClass.ParentSchema);
		SchemaMember deserializedMember = deserializedClass.Members.First();
		Assert.AreEqual(deserializedClass, deserializedMember.ParentClass);
	}
}
