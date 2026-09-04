// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// The schema tree, driven by clicking its rows: that each one selects the thing it names.
/// </summary>
/// <remarks>
/// Rows are addressed by the name the editor marks them with rather than by pixel position, so
/// these tests survive the tree being laid out differently. The marking happens once, in
/// <see cref="ButtonTree{TItem}"/>, which is where every row in every tree is drawn.
/// </remarks>
[TestClass]
public sealed class TreeNavigationTests
{
	private EditorHarness harness = null!;

	[TestInitialize]
	public void StartEditor() => harness = EditorHarness.Start();

	[TestCleanup]
	public void StopEditor() => harness.Dispose();

	/// <summary>
	/// A schema with one of everything, so every tree has a row to click.
	/// </summary>
	private Schema OpenPopulatedSchema()
	{
		Schema schema = new();

		SchemaClass user = schema.AddClass("User".As<ClassName>())!;
		user.AddMember("Id".As<MemberName>());
		user.AddMember("Age".As<MemberName>());
		schema.AddClass("Account".As<ClassName>());

		schema.AddEnum("Colour".As<EnumName>())!.TryAddValue("Red".As<EnumValueName>());
		schema.AddDataSource("Users".As<DataSourceName>());
		schema.AddCodeGenerator("CSharp".As<CodeGeneratorName>());

		harness.Editor.CurrentSchema = schema;
		return schema;
	}

	[TestMethod]
	public void ClickingAClassRowSelectsThatClass()
	{
		OpenPopulatedSchema();

		harness.Click("BtnAccount");

		Assert.AreEqual("Account", harness.Editor.CurrentClass?.Name.ToString());
	}

	/// <summary>
	/// A member has no panel of its own; its row is drawn in its class's, so that is what its tree
	/// row selects.
	/// </summary>
	[TestMethod]
	public void ClickingAMemberRowSelectsItsOwningClass()
	{
		OpenPopulatedSchema();
		harness.Click("BtnAccount");

		harness.Click("User/BtnAge");

		Assert.AreEqual("User", harness.Editor.CurrentClass?.Name.ToString());
	}

	[TestMethod]
	public void ClickingAnEnumRowSelectsThatEnum()
	{
		OpenPopulatedSchema();

		harness.Click("BtnColour");

		Assert.AreEqual("Colour", harness.Editor.CurrentEnum?.Name.ToString());
		Assert.IsNull(harness.Editor.CurrentClass, "Selecting an enum should clear the class selection.");
	}

	[TestMethod]
	public void ClickingADataSourceRowSelectsThatDataSource()
	{
		OpenPopulatedSchema();

		harness.Click("BtnUsers");

		Assert.AreEqual("Users", harness.Editor.CurrentDataSource?.Name.ToString());
	}

	[TestMethod]
	public void ClickingACodeGeneratorRowSelectsThatCodeGenerator()
	{
		OpenPopulatedSchema();

		harness.Click("BtnCSharp");

		Assert.AreEqual("CSharp", harness.Editor.CurrentCodeGenerator?.Name.ToString());
	}

	/// <summary>
	/// Only one thing is selected at a time, or two panels would each claim to be showing the
	/// current selection.
	/// </summary>
	[TestMethod]
	public void SelectingOneKindClearsTheOthers()
	{
		OpenPopulatedSchema();

		harness.Click("BtnUser");
		harness.Click("BtnUsers");
		harness.Click("BtnCSharp");

		Assert.IsNull(harness.Editor.CurrentClass);
		Assert.IsNull(harness.Editor.CurrentDataSource);
		Assert.IsNull(harness.Editor.CurrentEnum);
		Assert.IsNotNull(harness.Editor.CurrentCodeGenerator);
	}
}
