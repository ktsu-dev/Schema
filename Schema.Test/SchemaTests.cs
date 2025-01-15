namespace ktsu.Schema.Tests;

using System.Collections.ObjectModel;

using ktsu.Extensions;
using ktsu.Schema;

[TestClass]
public class SchemaTests
{
	[TestMethod]
	public void TestReassosciate()
	{
		Schema schema = new();
		var schemaClass = schema.AddClass((ClassName)"TestClass");

		schema.Reassosciate();
		Assert.IsNotNull(schemaClass);
		Assert.AreEqual(schema, schemaClass.ParentSchema);
	}

	[TestMethod]
	public void TestEnsureDirectoryExists()
	{
		string path = "test_directory/test_file.txt";
		Schema.EnsureDirectoryExists(path);
		Assert.IsTrue(Directory.Exists("test_directory"));
	}

	[TestMethod]
	public void TestTryRemoveChild()
	{
		Collection<SchemaClass> collection = [];
		SchemaClass schemaClass = new();
		collection.Add(schemaClass);

		bool result = Schema.TryRemoveChild(schemaClass, collection);
		Assert.IsTrue(result);
		Assert.AreEqual(0, collection.Count);
	}

	[TestMethod]
	public void TestGetChild()
	{
		Collection<SchemaClass> collection = [];
		SchemaClass schemaClass = new();
		schemaClass.Rename("TestClass".As<ClassName>());
		collection.Add(schemaClass);

		var result = Schema.GetChild("TestClass".As<ClassName>(), collection);
		Assert.IsNotNull(result);
		Assert.AreEqual(schemaClass, result);
	}

	[TestMethod]
	public void TestTryGetChild()
	{
		Collection<SchemaClass> collection = [];
		SchemaClass schemaClass = new();
		schemaClass.Rename((ClassName)"TestClass");
		collection.Add(schemaClass);

		bool result = Schema.TryGetChild("TestClass".As<ClassName>(), collection, out var foundClass);
		Assert.IsTrue(result);
		Assert.AreEqual(schemaClass, foundClass);
	}

	[TestMethod]
	public void TestTryAddChild()
	{
		Schema schema = new();
		bool result = schema.TryAddChild("TestClass".As<ClassName>(), schema.ClassesInternal);

		Assert.IsTrue(result);
		Assert.AreEqual(1, schema.ClassesInternal.Count);
	}

	[TestMethod]
	public void TestAddChild()
	{
		Schema schema = new();
		var schemaClass = schema.AddChild("TestClass".As<ClassName>(), schema.ClassesInternal);

		Assert.IsNotNull(schemaClass);
		Assert.AreEqual(1, schema.ClassesInternal.Count);
	}

	[TestMethod]
	public void TestTryRemoveEnum()
	{
		Schema schema = new();
		var schemaEnum = schema.AddEnum((EnumName)"TestEnum");
		Assert.IsNotNull(schemaEnum);
		bool result = schema.TryRemoveEnum(schemaEnum);
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void TestTryRemoveClass()
	{
		Schema schema = new();
		var schemaClass = schema.AddClass((ClassName)"TestClass");
		Assert.IsNotNull(schemaClass);
		bool result = schema.TryRemoveClass(schemaClass);
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void TestTryRemoveDataSource()
	{
		Schema schema = new();
		var dataSource = schema.AddDataSource((DataSourceName)"TestDataSource");
		Assert.IsNotNull(dataSource);
		bool result = schema.TryRemoveDataSource(dataSource);
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void TestTryAddEnum()
	{
		Schema schema = new();
		bool result = schema.TryAddEnum((EnumName)"TestEnum");

		Assert.IsTrue(result);
	}

	[TestMethod]
	public void TestTryAddClass()
	{
		Schema schema = new();
		bool result = schema.TryAddClass((ClassName)"TestClass");

		Assert.IsTrue(result);
	}

	[TestMethod]
	public void TestTryAddDataSource()
	{
		Schema schema = new();
		bool result = schema.TryAddDataSource((DataSourceName)"TestDataSource");

		Assert.IsTrue(result);
	}

	[TestMethod]
	public void TestAddEnum()
	{
		Schema schema = new();
		var schemaEnum = schema.AddEnum((EnumName)"TestEnum");

		Assert.IsNotNull(schemaEnum);
	}

	[TestMethod]
	public void TestAddClass()
	{
		Schema schema = new();
		var schemaClass = schema.AddClass((ClassName)"TestClass");

		Assert.IsNotNull(schemaClass);
	}

	[TestMethod]
	public void TestAddDataSource()
	{
		Schema schema = new();
		var dataSource = schema.AddDataSource((DataSourceName)"TestDataSource");

		Assert.IsNotNull(dataSource);
	}

	[TestMethod]
	public void TestFirstClass()
	{
		Schema schema = new();
		var schemaClass = schema.AddClass((ClassName)"TestClass");

		Assert.AreEqual(schemaClass, schema.FirstClass);
	}

	[TestMethod]
	public void TestLastClass()
	{
		Schema schema = new();
		var schemaClass = schema.AddClass((ClassName)"TestClass");

		Assert.AreEqual(schemaClass, schema.LastClass);
	}
}
