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
		var schema = new Schema();
		var schemaClass = new SchemaClass();
		schemaClass.Rename((ClassName)"TestClass");
		schema.AddClass((ClassName)"TestClass");

		schema.Reassosciate();
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
		var collection = new Collection<SchemaClass>();
		var schemaClass = new SchemaClass();
		collection.Add(schemaClass);

		bool result = Schema.TryRemoveChild(schemaClass, collection);
		Assert.IsTrue(result);
		Assert.AreEqual(0, collection.Count);
	}

	[TestMethod]
	public void TestGetChild()
	{
		var collection = new Collection<SchemaClass>();
		var schemaClass = new SchemaClass();
		schemaClass.Rename("TestClass".As<ClassName>());
		collection.Add(schemaClass);

		var result = Schema.GetChild("TestClass".As<ClassName>(), collection);
		Assert.IsNotNull(result);
		Assert.AreEqual(schemaClass, result);
	}

	[TestMethod]
	public void TestTryGetChild()
	{
		var collection = new Collection<SchemaClass>();
		var schemaClass = new SchemaClass();
		schemaClass.Rename((ClassName)"TestClass");
		collection.Add(schemaClass);

		bool result = Schema.TryGetChild("TestClass".As<ClassName>(), collection, out var foundClass);
		Assert.IsTrue(result);
		Assert.AreEqual(schemaClass, foundClass);
	}

	[TestMethod]
	public void TestTryAddChild()
	{
		var collection = new Collection<SchemaClass>();
		bool result = Schema.TryAddChild("TestClass".As<ClassName>(), collection);

		Assert.IsTrue(result);
		Assert.AreEqual(1, collection.Count);
	}

	[TestMethod]
	public void TestAddChild()
	{
		var collection = new Collection<SchemaClass>();
		var schemaClass = Schema.AddChild<SchemaClass, ClassName>("TestClass".As<ClassName>(), collection);

		Assert.IsNotNull(schemaClass);
		Assert.AreEqual(1, collection.Count);
	}

	[TestMethod]
	public void TestTryRemoveEnum()
	{
		var schema = new Schema();
		var schemaEnum = new SchemaEnum();
		schemaEnum.Rename((EnumName)"TestEnum");
		schema.AddEnum((EnumName)"TestEnum");

		bool result = schema.TryRemoveEnum(schemaEnum);
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void TestTryRemoveClass()
	{
		var schema = new Schema();
		var schemaClass = new SchemaClass();
		schemaClass.Rename((ClassName)"TestClass");
		schema.AddClass((ClassName)"TestClass");

		bool result = schema.TryRemoveClass(schemaClass);
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void TestTryRemoveDataSource()
	{
		var schema = new Schema();
		var dataSource = new DataSource();
		dataSource.Rename((DataSourceName)"TestDataSource");
		schema.AddDataSource((DataSourceName)"TestDataSource");

		bool result = schema.TryRemoveDataSource(dataSource);
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void TestTryAddEnum()
	{
		var schema = new Schema();
		bool result = schema.TryAddEnum((EnumName)"TestEnum");

		Assert.IsTrue(result);
	}

	[TestMethod]
	public void TestTryAddClass()
	{
		var schema = new Schema();
		bool result = schema.TryAddClass((ClassName)"TestClass");

		Assert.IsTrue(result);
	}

	[TestMethod]
	public void TestTryAddDataSource()
	{
		var schema = new Schema();
		bool result = schema.TryAddDataSource((DataSourceName)"TestDataSource");

		Assert.IsTrue(result);
	}

	[TestMethod]
	public void TestAddEnum()
	{
		var schema = new Schema();
		var schemaEnum = schema.AddEnum((EnumName)"TestEnum");

		Assert.IsNotNull(schemaEnum);
	}

	[TestMethod]
	public void TestAddClass()
	{
		var schema = new Schema();
		var schemaClass = schema.AddClass((ClassName)"TestClass");

		Assert.IsNotNull(schemaClass);
	}

	[TestMethod]
	public void TestAddDataSource()
	{
		var schema = new Schema();
		var dataSource = schema.AddDataSource((DataSourceName)"TestDataSource");

		Assert.IsNotNull(dataSource);
	}

	[TestMethod]
	public void TestFirstClass()
	{
		var schema = new Schema();
		var schemaClass = schema.AddClass((ClassName)"TestClass");

		Assert.AreEqual(schemaClass, schema.FirstClass);
	}

	[TestMethod]
	public void TestLastClass()
	{
		var schema = new Schema();
		var schemaClass = schema.AddClass((ClassName)"TestClass");

		Assert.AreEqual(schemaClass, schema.LastClass);
	}
}
