// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

using System.Linq;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// Adding elements from the tree: the "+ New" button, the name it asks for, and the undoable
/// command that results.
/// </summary>
[TestClass]
public sealed class TreeEditingTests
{
	private EditorHarness harness = null!;
	private Schema schema = null!;

	[TestInitialize]
	public void StartEditor()
	{
		harness = EditorHarness.Start();
		schema = new Schema();
		harness.Editor.CurrentSchema = schema;
	}

	[TestCleanup]
	public void StopEditor() => harness.Dispose();

	/// <summary>
	/// Clicks a "+ New" button and answers the name it asks for.
	/// </summary>
	private void AddNamed(string button, string name)
	{
		harness.Click(button);
		harness.TypeInto("input/field", name);
		harness.Click("input/ok");
	}

	[TestMethod]
	public void AddingAClassNamesItAndSelectsIt()
	{
		AddNamed("NewClass", "Order");

		Assert.IsNotNull(schema.GetClass("Order".As<ClassName>()));
		Assert.AreEqual("Order", harness.Editor.CurrentClass?.Name.ToString());
	}

	[TestMethod]
	public void AddingAClassIsUndoable()
	{
		AddNamed("NewClass", "Order");
		Assert.IsNotNull(schema.GetClass("Order".As<ClassName>()));

		harness.Editor.UndoRedo.Undo();

		Assert.IsNull(schema.GetClass("Order".As<ClassName>()));
	}

	/// <summary>
	/// A name already in use is refused with a message rather than silently replacing the class
	/// that has it.
	/// </summary>
	[TestMethod]
	public void AddingAClassWithANameAlreadyInUseIsRefused()
	{
		schema.AddClass("Order".As<ClassName>());

		AddNamed("NewClass", "Order");

		Assert.AreEqual(1, schema.Classes.Count(c => c.Name.ToString() == "Order"));
	}

	[TestMethod]
	public void AddingAMemberPutsItOnItsClass()
	{
		SchemaClass user = schema.AddClass("User".As<ClassName>())!;
		harness.Editor.EditClass(user);

		AddNamed("User/NewMember", "Age");

		Assert.IsNotNull(user.GetMember("Age".As<MemberName>()));
	}

	[TestMethod]
	public void AddingAnEnumPutsItOnTheSchema()
	{
		AddNamed("NewEnum", "Colour");

		Assert.IsNotNull(schema.GetEnum("Colour".As<EnumName>()));
	}

	[TestMethod]
	public void AddingAnEnumValuePutsItOnItsEnum()
	{
		SchemaEnum colour = schema.AddEnum("Colour".As<EnumName>())!;

		AddNamed("NewValue", "Red");

		Assert.IsTrue(colour.Values.Any(v => v.ToString() == "Red"));
	}

	[TestMethod]
	public void AddingADataSourcePutsItOnTheSchemaAndSelectsIt()
	{
		AddNamed("NewDataSource", "Users");

		Assert.IsNotNull(schema.GetDataSource("Users".As<DataSourceName>()));
		Assert.AreEqual("Users", harness.Editor.CurrentDataSource?.Name.ToString());
	}

	[TestMethod]
	public void AddingACodeGeneratorPutsItOnTheSchema()
	{
		AddNamed("NewCodeGenerator", "CSharp");

		Assert.IsNotNull(schema.GetCodeGenerator("CSharp".As<CodeGeneratorName>()));
	}
}
