// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers <see cref="Schema.DataSourceSet"/> and <see cref="Schema.CodeGeneratorSet"/>, the ordered
/// views that let a caller put a deleted data source or code generator back where it was.
/// <see cref="Schema.RestoreDataSource"/> and <see cref="Schema.RestoreCodeGenerator"/> append, so
/// without a position to move it to, undoing a delete would quietly reorder the schema - and order
/// is part of its meaning, since it round-trips through the file and drives generated code.
/// </summary>
[TestClass]
public class SchemaRootSetTests
{
	private static readonly string[] DataSourceSeed = ["Users", "Orders", "Products"];
	private static readonly string[] CodeGeneratorSeed = ["CSharp", "Cpp", "Docs"];
	private static readonly string[] WithoutOrders = ["Users", "Products"];
	private static readonly string[] OrdersAppended = ["Users", "Products", "Orders"];
	private static readonly string[] WithoutCpp = ["CSharp", "Docs"];
	private static readonly string[] ProductsMovedToFront = ["Products", "Users", "Orders"];

	private static Schema CreateSchema()
	{
		Schema schema = new();
		foreach (string name in DataSourceSeed)
		{
			schema.AddDataSource(name.As<DataSourceName>());
		}

		foreach (string name in CodeGeneratorSeed)
		{
			schema.AddCodeGenerator(name.As<CodeGeneratorName>());
		}

		return schema;
	}

	private static string[] DataSourceNames(Schema schema) =>
		[.. schema.DataSources.Select(d => d.Name.ToString())];

	private static string[] CodeGeneratorNames(Schema schema) =>
		[.. schema.CodeGenerators.Select(g => g.Name.ToString())];

	[TestMethod]
	public void TestTheDataSourceSetIsAViewInDeclarationOrder()
	{
		Schema schema = CreateSchema();

		Assert.AreEqual(DataSourceSeed.Length, schema.DataSourceSet.Count);
		string[] throughTheSet = [.. schema.DataSourceSet.Select(d => d.Name.ToString())];
		CollectionAssert.AreEqual(DataSourceSeed, throughTheSet);
		Assert.AreEqual(1, schema.DataSourceSet.IndexOf(schema.GetDataSource("Orders".As<DataSourceName>())!));
	}

	[TestMethod]
	public void TestTheCodeGeneratorSetIsAViewInDeclarationOrder()
	{
		Schema schema = CreateSchema();

		Assert.AreEqual(CodeGeneratorSeed.Length, schema.CodeGeneratorSet.Count);
		string[] throughTheSet = [.. schema.CodeGeneratorSet.Select(g => g.Name.ToString())];
		CollectionAssert.AreEqual(CodeGeneratorSeed, throughTheSet);
		Assert.AreEqual(1, schema.CodeGeneratorSet.IndexOf(schema.GetCodeGenerator("Cpp".As<CodeGeneratorName>())!));
	}

	/// <summary>
	/// The restore on its own is what an undo would do without the set; asserting the appended
	/// order first is what makes the move afterwards the thing being tested.
	/// </summary>
	[TestMethod]
	public void TestADataSourceCanBeRestoredWhereItWas()
	{
		Schema schema = CreateSchema();
		DataSource orders = schema.GetDataSource("Orders".As<DataSourceName>())!;
		int index = schema.DataSourceSet.IndexOf(orders);

		Assert.IsTrue(orders.TryRemove());
		CollectionAssert.AreEqual(WithoutOrders, DataSourceNames(schema));

		Assert.IsTrue(schema.RestoreDataSource(orders));
		CollectionAssert.AreEqual(OrdersAppended, DataSourceNames(schema));

		Assert.IsTrue(schema.DataSourceSet.Move(orders, index));
		CollectionAssert.AreEqual(DataSourceSeed, DataSourceNames(schema));
	}

	[TestMethod]
	public void TestACodeGeneratorCanBeRestoredWhereItWas()
	{
		Schema schema = CreateSchema();
		SchemaCodeGenerator cpp = schema.GetCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		int index = schema.CodeGeneratorSet.IndexOf(cpp);

		Assert.IsTrue(cpp.TryRemove());
		CollectionAssert.AreEqual(WithoutCpp, CodeGeneratorNames(schema));

		Assert.IsTrue(schema.RestoreCodeGenerator(cpp));
		Assert.IsTrue(schema.CodeGeneratorSet.Move(cpp, index));
		CollectionAssert.AreEqual(CodeGeneratorSeed, CodeGeneratorNames(schema));
	}

	/// <summary>
	/// The sets write through to the collections the schema serializes rather than to a copy, so a
	/// repositioned element stays repositioned in the file.
	/// </summary>
	[TestMethod]
	public void TestARepositionedDataSourceSurvivesARoundTrip()
	{
		Schema schema = CreateSchema();
		Assert.IsTrue(schema.DataSourceSet.Move(schema.GetDataSource("Products".As<DataSourceName>())!, 0));

		string json = SchemaSerializer.Serialize(schema);
		Assert.IsTrue(SchemaSerializer.TryDeserialize(json, out Schema? reloaded));
		Assert.IsNotNull(reloaded);

		CollectionAssert.AreEqual(ProductsMovedToFront, DataSourceNames(reloaded));
	}
}
