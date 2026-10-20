// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor.UITests;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

using SchemaTypes = ktsu.Schema.Models.Types;

/// <summary>
/// The semantic type panel and tree: naming one, describing it, and choosing what it shims.
/// </summary>
/// <remarks>
/// Until these existed a semantic type could only be written into the file by hand, so the editor
/// could open a schema containing one and do nothing with it.
/// </remarks>
[TestClass]
public sealed class SemanticTypePanelTests
{
	private EditorHarness harness = null!;
	private Schema schema = null!;
	private SchemaSemanticType kilograms = null!;

	[TestInitialize]
	public void StartEditor()
	{
		harness = EditorHarness.Start();
		schema = new Schema();
		kilograms = schema.AddSemanticType("Kilograms".As<SemanticTypeName>())!;
		kilograms.SetUnderlyingType(new SchemaTypes.Float());
		harness.Editor.CurrentSchema = schema;
		harness.Editor.EditSemanticType(kilograms);
	}

	[TestCleanup]
	public void StopEditor() => harness.Dispose();

	[TestMethod]
	public void RenamingFromThePanelRenamesEveryReference()
	{
		SchemaMember mass = schema.AddClass("Body".As<ClassName>())!.AddMember("Mass".As<MemberName>())!;
		mass.SetType(new SchemaTypes.Semantic { SemanticTypeName = kilograms.Name });

		harness.Commit("field/SemanticTypeNameKilograms", "Mass");

		Assert.AreEqual("Mass", kilograms.Name.ToString());
		Assert.AreEqual("Mass", ((SchemaTypes.Semantic)mass.Type).SemanticTypeName.ToString());
	}

	[TestMethod]
	public void RenamingFromThePanelIsUndoable()
	{
		harness.Commit("field/SemanticTypeNameKilograms", "Mass");

		harness.Editor.UndoRedo.Undo();

		Assert.AreEqual("Kilograms", kilograms.Name.ToString());
	}

	[TestMethod]
	public void EditingTheDescriptionRecordsIt()
	{
		harness.TypeInto("field/SemanticTypeDescriptionKilograms", "A mass in kilograms.");
		harness.Click("field/SemanticTypeNameKilograms");

		Assert.AreEqual("A mass in kilograms.", kilograms.Description.ToString());
	}

	[TestMethod]
	public void ChoosingTheUnderlyingTypeSetsItUndoably()
	{
		harness.Click("UnderlyingType");
		harness.Click("searchable-list/Double");
		harness.StepUntil(() => !harness.IsOnScreen("searchable-list/search"), "the type picker closing once a type was picked");

		Assert.IsInstanceOfType<SchemaTypes.Double>(kilograms.UnderlyingType);

		harness.Editor.UndoRedo.Undo();

		Assert.IsInstanceOfType<SchemaTypes.Float>(kilograms.UnderlyingType);
	}

	[TestMethod]
	public void TheTreeOffersANewSemanticType()
	{
		harness.App.Step(2);

		Assert.IsTrue(harness.IsOnScreen("NewSemanticType"), "The semantic types tree was not shown.");
	}
}
