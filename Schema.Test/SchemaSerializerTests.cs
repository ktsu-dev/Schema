// Copyright (c) 2023-2026 ktsu-dev contributors

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
	public void TestVectorAndColorMembersSerializeWithoutAClassName()
	{
		Schema original = new();
		SchemaClass? playerClass = original.AddClass("Player".As<ClassName>());
		Assert.IsNotNull(playerClass);
		playerClass.AddMember("Position".As<MemberName>())?.SetType(new SchemaTypes.Vector3());
		playerClass.AddMember("Tint".As<MemberName>())?.SetType(new SchemaTypes.ColorRGB());

		string json = SchemaSerializer.Serialize(original);

		// Before issue #107 these types derived from Object and wrote an empty "className".
		Assert.IsFalse(json.Contains("className", StringComparison.Ordinal), json);
		Assert.IsTrue(json.Contains("Vector3", StringComparison.Ordinal), json);
		Assert.IsTrue(json.Contains("ColorRGB", StringComparison.Ordinal), json);
	}

	[TestMethod]
	public void TestRoundtripWithVectorAndColorMembers()
	{
		Schema original = new();
		SchemaClass? playerClass = original.AddClass("Player".As<ClassName>());
		Assert.IsNotNull(playerClass);
		playerClass.AddMember("Position".As<MemberName>())?.SetType(new SchemaTypes.Vector3());
		playerClass.AddMember("Tint".As<MemberName>())?.SetType(new SchemaTypes.ColorRGBA());

		string json = SchemaSerializer.Serialize(original);
		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? deserialized));
		Assert.IsNotNull(deserialized);

		SchemaClass? roundTripped = deserialized.GetClass("Player".As<ClassName>());
		Assert.IsNotNull(roundTripped);
		Assert.IsTrue(roundTripped.TryGetMember("Position".As<MemberName>(), out SchemaMember? position));
		Assert.IsTrue(roundTripped.TryGetMember("Tint".As<MemberName>(), out SchemaMember? tint));
		Assert.AreEqual(new SchemaTypes.Vector3(), position!.Type);
		Assert.AreEqual(new SchemaTypes.ColorRGBA(), tint!.Type);
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
		Assert.IsTrue(deserializedClass.TryGetMember("Name".As<MemberName>(), out SchemaMember? deserializedMember));
		Assert.AreEqual(new SchemaTypes.String(), deserializedMember!.Type);
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

		// Counting the members is not enough: the member type is what the format exists to
		// carry, so assert each one survived the round trip.
		for (int index = 0; index < types.Length; index++)
		{
			Assert.IsTrue(deserializedClass.TryGetMember($"Field{index}".As<MemberName>(), out SchemaMember? member));
			Assert.AreEqual(types[index], member!.Type);
		}
	}

	[TestMethod]
	public void TestDescriptionsRoundTripForEveryElementKind()
	{
		Schema original = new();
		SchemaClass userClass = original.AddClass("User".As<ClassName>())!;
		userClass.Description = "A person".As<SchemaChildDescription>();
		SchemaMember nameMember = userClass.AddMember("Name".As<MemberName>())!;
		nameMember.SetType(new SchemaTypes.String());
		nameMember.Description = "Their display name".As<SchemaChildDescription>();

		SchemaEnum roleEnum = original.AddEnum("Role".As<EnumName>())!;
		roleEnum.Description = "What they may do".As<SchemaChildDescription>();

		DataSource dataSource = original.AddDataSource("Users".As<DataSourceName>())!;
		dataSource.Description = "Seed data".As<SchemaChildDescription>();

		SchemaCodeGenerator generator = original.AddCodeGenerator("CSharp".As<CodeGeneratorName>())!;
		generator.Description = "Emits POCOs".As<SchemaChildDescription>();

		string json = SchemaSerializer.Serialize(original);
		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? reloaded));
		Assert.IsNotNull(reloaded);

		SchemaClass reloadedClass = reloaded.GetClass("User".As<ClassName>())!;
		Assert.AreEqual("A person", reloadedClass.Description.ToString());
		Assert.AreEqual("Their display name", reloadedClass.GetMember("Name".As<MemberName>())!.Description.ToString());
		Assert.AreEqual("What they may do", reloaded.GetEnum("Role".As<EnumName>())!.Description.ToString());
		Assert.AreEqual("Seed data", reloaded.GetDataSource("Users".As<DataSourceName>())!.Description.ToString());
		Assert.AreEqual("Emits POCOs", reloaded.GetCodeGenerator("CSharp".As<CodeGeneratorName>())!.Description.ToString());
	}

	[TestMethod]
	public void TestLegacyMemberDescriptionMigratesOntoDescription()
	{
		// Files written before the two description fields were collapsed carry the member's text
		// under "memberDescription". It must land on Description rather than being dropped.
		const string json = """
		{
		  "classes": [
		    {
		      "name": "User",
		      "description": "",
		      "members": [
		        { "name": "Name", "description": "", "memberDescription": "Their display name",
		          "type": { "TypeName": "String" } }
		      ]
		    }
		  ],
		  "enums": [],
		  "dataSources": [],
		  "codeGenerators": []
		}
		""";

		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? schema));
		Assert.IsNotNull(schema);

		SchemaMember member = schema.GetClass("User".As<ClassName>())!.GetMember("Name".As<MemberName>())!;
		Assert.AreEqual("Their display name", member.Description.ToString());
	}

	[TestMethod]
	public void TestMemberDescriptionIsNoLongerWritten()
	{
		Schema schema = new();
		SchemaClass userClass = schema.AddClass("User".As<ClassName>())!;
		SchemaMember member = userClass.AddMember("Name".As<MemberName>())!;
		member.SetType(new SchemaTypes.String());
		member.Description = "Their display name".As<SchemaChildDescription>();

		string json = SchemaSerializer.Serialize(schema);

		Assert.IsFalse(json.Contains("memberDescription", StringComparison.Ordinal), json);
		Assert.IsTrue(json.Contains("Their display name", StringComparison.Ordinal), json);
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
