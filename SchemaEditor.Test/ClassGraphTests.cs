// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

using ktsu.ImGui.App.Testing;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

using SchemaTypes = ktsu.Schema.Models.Types;

/// <summary>
/// The class graph, which draws the schema's classes and the references between them.
/// </summary>
/// <remarks>
/// Driven through <see cref="WidgetHarness"/> rather than through the editor, because the graph
/// lives behind a tab and the tab bar comes from a widget library that does not record its tabs -
/// so there is no name for a test to click. Drawing the view directly reaches the same code, and
/// is the same thing the editor's tab delegate does.
/// </remarks>
[TestClass]
public sealed class ClassGraphTests
{
	private WidgetHarness harness = null!;

	[TestInitialize]
	public void StartHarness() => harness = WidgetHarness.Start();

	[TestCleanup]
	public void StopHarness() => harness.Dispose();

	/// <summary>
	/// Two classes, one referencing the other, so the graph has both a node and an edge to draw.
	/// </summary>
	private static Schema BuildReferencingSchema()
	{
		Schema schema = new();
		SchemaClass user = schema.AddClass("User".As<ClassName>())!;
		SchemaClass account = schema.AddClass("Account".As<ClassName>())!;
		account.AddMember("Owner".As<MemberName>())!.SetType(new SchemaTypes.Object() { ClassName = user.Name });
		return schema;
	}

	private int DrawnPixels()
	{
		CapturedFrame frame = harness.App.Capture();
		Rgba32 background = harness.App.Options.ClearColor;
		return frame.CountPixels(p => p != background);
	}

	[TestMethod]
	public void TheGraphDrawsASchemaWithReferences()
	{
		ClassGraphView graph = new();
		Schema schema = BuildReferencingSchema();
		harness.Draw = () => graph.Show(schema, 1f / 60f);

		// The layout is force directed, so it settles over frames rather than in one.
		harness.App.Step(30);

		Assert.IsTrue(DrawnPixels() > 0, "The graph rendered nothing at all.");
	}

	/// <summary>
	/// The graph is drawn every frame whether or not a schema is open, so the empty case is the
	/// one that runs most often.
	/// </summary>
	[TestMethod]
	public void TheGraphDrawsWithNoSchemaOpen()
	{
		ClassGraphView graph = new();
		harness.Draw = () => graph.Show(null, 1f / 60f);

		harness.App.Step(10);

		Assert.AreEqual(10 + 1, harness.App.FrameCount, "Frames stopped advancing, so a frame threw.");
	}

	/// <summary>
	/// A schema with nothing in it takes an early return that says so, rather than handing an
	/// empty graph to the node editor.
	/// </summary>
	[TestMethod]
	public void TheGraphSaysSoWhenThereIsNothingToShow()
	{
		ClassGraphView graph = new();
		Schema empty = new();
		harness.Draw = () => graph.Show(empty, 1f / 60f);

		harness.App.Step(10);

		Assert.AreEqual(10 + 1, harness.App.FrameCount, "Frames stopped advancing, so a frame threw.");
		Assert.IsTrue(DrawnPixels() > 0, "The empty-schema message was not drawn.");
	}
}
