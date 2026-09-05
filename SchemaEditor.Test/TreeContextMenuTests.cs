// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

using System.Linq;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// Renaming and deleting from a tree row's context menu, and what an undo of each puts back.
/// </summary>
[TestClass]
public sealed class TreeContextMenuTests
{
	private EditorHarness harness = null!;
	private Schema schema = null!;

	[TestInitialize]
	public void StartEditor()
	{
		harness = EditorHarness.Start();

		schema = new Schema();
		SchemaClass user = schema.AddClass("User".As<ClassName>())!;
		user.AddMember("Id".As<MemberName>());
		user.AddMember("Age".As<MemberName>());
		user.AddMember("Email".As<MemberName>());
		schema.AddClass("Account".As<ClassName>());
		schema.AddClass("Order".As<ClassName>());
		schema.AddEnum("Colour".As<EnumName>());
		schema.AddEnum("Size".As<EnumName>());

		harness.Editor.CurrentSchema = schema;
	}

	[TestCleanup]
	public void StopEditor() => harness.Dispose();

	private string[] ClassNames => [.. schema.Classes.Select(c => c.Name.ToString())];

	private string[] EnumNames => [.. schema.Enums.Select(e => e.Name.ToString())];

	private void AssertClasses(params string[] expected) =>
		CollectionAssert.AreEqual(expected, ClassNames, $"Classes were [{string.Join(", ", ClassNames)}].");

	private void AssertEnums(params string[] expected) =>
		CollectionAssert.AreEqual(expected, EnumNames, $"Enums were [{string.Join(", ", EnumNames)}].");

	private void ChooseFromContextMenu(string row, string entry)
	{
		harness.RightClick(row);
		harness.Click(entry);
	}

	[TestMethod]
	public void DeletingAClassRemovesIt()
	{
		ChooseFromContextMenu("BtnAccount", "DeleteAccount");

		AssertClasses("User", "Order");
	}

	/// <summary>
	/// Restore appends, so without the position being remembered an undo would bring the class
	/// back at the end - quietly reordering the schema it was asked to put back, and with it the
	/// order the file and any generated code are written in.
	/// </summary>
	[TestMethod]
	public void UndoingAClassDeleteBringsItBackWhereItWas()
	{
		ChooseFromContextMenu("BtnAccount", "DeleteAccount");

		harness.Editor.UndoRedo.Undo();

		AssertClasses("User", "Account", "Order");
	}

	[TestMethod]
	public void RenamingAClassChangesItsName()
	{
		ChooseFromContextMenu("BtnAccount", "RenameAccount");
		harness.TypeInto("input/field", "Ledger");
		harness.Click("input/ok");

		Assert.IsNotNull(schema.GetClass("Ledger".As<ClassName>()));
		Assert.IsNull(schema.GetClass("Account".As<ClassName>()));
	}

	[TestMethod]
	public void DeletingAnEnumRemovesIt()
	{
		ChooseFromContextMenu("BtnColour", "DeleteColour");

		AssertEnums("Size");
	}

	[TestMethod]
	public void UndoingAnEnumDeleteBringsItBackWhereItWas()
	{
		ChooseFromContextMenu("BtnColour", "DeleteColour");

		harness.Editor.UndoRedo.Undo();

		AssertEnums("Colour", "Size");
	}

	private void AddDataSources(params string[] names)
	{
		foreach (string name in names)
		{
			schema.AddDataSource(name.As<DataSourceName>());
		}

		harness.Editor.CurrentSchema = schema;
	}

	private void AssertDataSources(params string[] expected)
	{
		string[] actual = [.. schema.DataSources.Select(d => d.Name.ToString())];
		CollectionAssert.AreEqual(expected, actual, $"Data sources were [{string.Join(", ", actual)}].");
	}

	[TestMethod]
	public void DeletingADataSourceRemovesIt()
	{
		AddDataSources("Users");

		ChooseFromContextMenu("BtnUsers", "DeleteUsers");

		Assert.IsNull(schema.GetDataSource("Users".As<DataSourceName>()));
	}

	/// <summary>
	/// Deleted from the middle, because appending on restore would put it back in the right set
	/// but the wrong place - and at either end that is indistinguishable from doing it correctly.
	/// </summary>
	[TestMethod]
	public void UndoingADataSourceDeleteBringsItBackWhereItWas()
	{
		AddDataSources("Users", "Orders", "Products");

		ChooseFromContextMenu("BtnOrders", "DeleteOrders");
		AssertDataSources("Users", "Products");

		harness.Editor.UndoRedo.Undo();

		AssertDataSources("Users", "Orders", "Products");
	}

	[TestMethod]
	public void RenamingADataSourceChangesItsName()
	{
		AddDataSources("Users");

		ChooseFromContextMenu("BtnUsers", "RenameUsers");
		harness.TypeInto("input/field", "People");
		harness.Click("input/ok");

		Assert.IsNotNull(schema.GetDataSource("People".As<DataSourceName>()));
	}

	private void AddCodeGenerators(params string[] names)
	{
		foreach (string name in names)
		{
			schema.AddCodeGenerator(name.As<CodeGeneratorName>());
		}

		harness.Editor.CurrentSchema = schema;
	}

	private void AssertCodeGenerators(params string[] expected)
	{
		string[] actual = [.. schema.CodeGenerators.Select(g => g.Name.ToString())];
		CollectionAssert.AreEqual(expected, actual, $"Code generators were [{string.Join(", ", actual)}].");
	}

	[TestMethod]
	public void DeletingACodeGeneratorRemovesIt()
	{
		AddCodeGenerators("CSharp");

		ChooseFromContextMenu("BtnCSharp", "DeleteCSharp");

		Assert.IsNull(schema.GetCodeGenerator("CSharp".As<CodeGeneratorName>()));
	}

	[TestMethod]
	public void UndoingACodeGeneratorDeleteBringsItBackWhereItWas()
	{
		AddCodeGenerators("CSharp", "Cpp", "Docs");

		ChooseFromContextMenu("BtnCpp", "DeleteCpp");
		AssertCodeGenerators("CSharp", "Docs");

		harness.Editor.UndoRedo.Undo();

		AssertCodeGenerators("CSharp", "Cpp", "Docs");
	}

	[TestMethod]
	public void RenamingACodeGeneratorChangesItsName()
	{
		AddCodeGenerators("CSharp");

		ChooseFromContextMenu("BtnCSharp", "RenameCSharp");
		harness.TypeInto("input/field", "CSharpPocos");
		harness.Click("input/ok");

		Assert.IsNotNull(schema.GetCodeGenerator("CSharpPocos".As<CodeGeneratorName>()));
	}

	[TestMethod]
	public void RenamingAnEnumChangesItsName()
	{
		ChooseFromContextMenu("BtnColour", "RenameColour");
		harness.TypeInto("input/field", "Hue");
		harness.Click("input/ok");

		Assert.IsNotNull(schema.GetEnum("Hue".As<EnumName>()));
	}

	[TestMethod]
	public void RenamingAMemberChangesItsName()
	{
		SchemaClass user = schema.GetClass("User".As<ClassName>())!;

		ChooseFromContextMenu("User/BtnId", "RenameId");
		harness.TypeInto("input/field", "Identifier");
		harness.Click("input/ok");

		Assert.IsNotNull(user.GetMember("Identifier".As<MemberName>()));
	}

	private void AssertMembersOfUser(params string[] expected)
	{
		string[] actual = [.. schema.GetClass("User".As<ClassName>())!.Members.Select(m => m.Name.ToString())];
		CollectionAssert.AreEqual(expected, actual, $"Members were [{string.Join(", ", actual)}].");
	}

	[TestMethod]
	public void DeletingAMemberFromTheTreeRemovesIt()
	{
		ChooseFromContextMenu("User/BtnAge", "DeleteAge");

		AssertMembersOfUser("Id", "Email");
	}

	/// <summary>
	/// The tree deletes a member through its own command rather than the panel's, so it needs the
	/// same position-preserving restore - proved here by deleting from the middle.
	/// </summary>
	[TestMethod]
	public void UndoingAMemberDeleteFromTheTreeBringsItBackWhereItWas()
	{
		ChooseFromContextMenu("User/BtnAge", "DeleteAge");
		AssertMembersOfUser("Id", "Email");

		harness.Editor.UndoRedo.Undo();

		AssertMembersOfUser("Id", "Age", "Email");
	}
}
