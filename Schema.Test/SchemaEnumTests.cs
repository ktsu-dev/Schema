// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class SchemaEnumTests
{
	[TestMethod]
	public void TestAddEnum()
	{
		Schema schema = new();
		SchemaEnum? schemaEnum = schema.AddEnum("Color".As<EnumName>());

		Assert.IsNotNull(schemaEnum);
		Assert.AreEqual("Color".As<EnumName>(), schemaEnum.Name);
		Assert.AreEqual(1, schema.Enums.Count);
	}

	[TestMethod]
	public void TestAddDuplicateEnumReturnsNull()
	{
		Schema schema = new();
		schema.AddEnum("Color".As<EnumName>());
		SchemaEnum? duplicate = schema.AddEnum("Color".As<EnumName>());

		Assert.IsNull(duplicate);
		Assert.AreEqual(1, schema.Enums.Count);
	}

	[TestMethod]
	public void TestTryAddEnum()
	{
		Schema schema = new();
		bool result = schema.TryAddEnum("Color".As<EnumName>());

		Assert.IsTrue(result);
		Assert.AreEqual(1, schema.Enums.Count);
	}

	[TestMethod]
	public void TestTryAddDuplicateEnumReturnsFalse()
	{
		Schema schema = new();
		schema.TryAddEnum("Color".As<EnumName>());
		bool result = schema.TryAddEnum("Color".As<EnumName>());

		Assert.IsFalse(result);
		Assert.AreEqual(1, schema.Enums.Count);
	}

	[TestMethod]
	public void TestTryGetEnum()
	{
		Schema schema = new();
		SchemaEnum? added = schema.AddEnum("Color".As<EnumName>());

		bool result = schema.TryGetEnum("Color".As<EnumName>(), out SchemaEnum? found);
		Assert.IsTrue(result);
		Assert.AreEqual(added, found);
	}

	[TestMethod]
	public void TestTryGetEnumNotFound()
	{
		Schema schema = new();

		bool result = schema.TryGetEnum("Missing".As<EnumName>(), out SchemaEnum? found);
		Assert.IsFalse(result);
		Assert.IsNull(found);
	}

	[TestMethod]
	public void TestGetEnum()
	{
		Schema schema = new();
		SchemaEnum? added = schema.AddEnum("Color".As<EnumName>());

		SchemaEnum? found = schema.GetEnum("Color".As<EnumName>());
		Assert.AreEqual(added, found);
	}

	[TestMethod]
	public void TestGetEnumNotFound()
	{
		Schema schema = new();
		SchemaEnum? found = schema.GetEnum("Missing".As<EnumName>());

		Assert.IsNull(found);
	}

	[TestMethod]
	public void TestTryAddValue()
	{
		Schema schema = new();
		SchemaEnum? schemaEnum = schema.AddEnum("Color".As<EnumName>());
		Assert.IsNotNull(schemaEnum);

		bool result = schemaEnum.TryAddValue("Red".As<EnumValueName>());
		Assert.IsTrue(result);
		Assert.AreEqual(1, schemaEnum.Values.Count);
	}

	[TestMethod]
	public void TestTryAddDuplicateValueReturnsFalse()
	{
		Schema schema = new();
		SchemaEnum? schemaEnum = schema.AddEnum("Color".As<EnumName>());
		Assert.IsNotNull(schemaEnum);

		schemaEnum.TryAddValue("Red".As<EnumValueName>());
		bool result = schemaEnum.TryAddValue("Red".As<EnumValueName>());
		Assert.IsFalse(result);
		Assert.AreEqual(1, schemaEnum.Values.Count);
	}

	[TestMethod]
	public void TestTryAddMultipleValues()
	{
		Schema schema = new();
		SchemaEnum? schemaEnum = schema.AddEnum("Color".As<EnumName>());
		Assert.IsNotNull(schemaEnum);

		schemaEnum.TryAddValue("Red".As<EnumValueName>());
		schemaEnum.TryAddValue("Green".As<EnumValueName>());
		schemaEnum.TryAddValue("Blue".As<EnumValueName>());

		Assert.AreEqual(3, schemaEnum.Values.Count);
	}

	[TestMethod]
	public void TestTryRemoveValue()
	{
		Schema schema = new();
		SchemaEnum? schemaEnum = schema.AddEnum("Color".As<EnumName>());
		Assert.IsNotNull(schemaEnum);

		schemaEnum.TryAddValue("Red".As<EnumValueName>());
		bool result = schemaEnum.TryRemoveValue("Red".As<EnumValueName>());

		Assert.IsTrue(result);
		Assert.AreEqual(0, schemaEnum.Values.Count);
	}

	[TestMethod]
	public void TestTryRemoveNonexistentValueReturnsFalse()
	{
		Schema schema = new();
		SchemaEnum? schemaEnum = schema.AddEnum("Color".As<EnumName>());
		Assert.IsNotNull(schemaEnum);

		bool result = schemaEnum.TryRemoveValue("Red".As<EnumValueName>());
		Assert.IsFalse(result);
	}

	[TestMethod]
	public void TestEnumTryRemove()
	{
		Schema schema = new();
		SchemaEnum? schemaEnum = schema.AddEnum("Color".As<EnumName>());
		Assert.IsNotNull(schemaEnum);

		bool result = schemaEnum.TryRemove();
		Assert.IsTrue(result);
		Assert.AreEqual(0, schema.Enums.Count);
	}

	[TestMethod]
	public void TestEnumTryRemoveWithoutParent()
	{
		SchemaEnum schemaEnum = new();
		bool result = schemaEnum.TryRemove();

		Assert.IsFalse(result);
	}

	[TestMethod]
	public void TestEnumSummary()
	{
		Schema schema = new();
		SchemaEnum? schemaEnum = schema.AddEnum("Color".As<EnumName>());
		Assert.IsNotNull(schemaEnum);

		schemaEnum.TryAddValue("Red".As<EnumValueName>());
		schemaEnum.TryAddValue("Green".As<EnumValueName>());

		Assert.AreEqual("Color (2)", schemaEnum.EnumSummary);
	}

	[TestMethod]
	public void TestEnumParentAssociation()
	{
		Schema schema = new();
		SchemaEnum? schemaEnum = schema.AddEnum("Color".As<EnumName>());
		Assert.IsNotNull(schemaEnum);

		Assert.AreEqual(schema, schemaEnum.ParentSchema);
	}

	[TestMethod]
	public void TestEnumRename()
	{
		Schema schema = new();
		SchemaEnum? schemaEnum = schema.AddEnum("Color".As<EnumName>());
		Assert.IsNotNull(schemaEnum);

		schemaEnum.Rename("Colour".As<EnumName>());
		Assert.AreEqual("Colour".As<EnumName>(), schemaEnum.Name);
	}

	[TestMethod]
	public void TestEnumToString()
	{
		Schema schema = new();
		SchemaEnum? schemaEnum = schema.AddEnum("Color".As<EnumName>());
		Assert.IsNotNull(schemaEnum);

		Assert.AreEqual("Color", schemaEnum.ToString());
	}
}
