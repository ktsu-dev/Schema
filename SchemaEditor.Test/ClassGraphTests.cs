// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

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

	/// <summary>
	/// A class or enum that references nothing is drawn as a node like any other.
	/// </summary>
	/// <remarks>
	/// Such an element becomes a node with no pins, and ImNodes ends a node's title bar by moving
	/// the cursor to where the node's content starts. With no pin submitted after that, the node's
	/// group closed on a cursor that had been moved and never used, which cost two things at once:
	/// ImGui reported "code uses SetCursorPos() to extend window/parent boundaries" over the editor
	/// every frame the graph was open, and the node's measured width grew by eight pixels a frame,
	/// without limit, for as long as it was on screen.
	///
	/// The node is drawn by ktsu.ImGui.NodeEditor, so that is where the fix is: it submits an item
	/// of its own for a node with no pins, from 3.16.8.
	/// </remarks>
	[TestMethod]
	public void ANodeWithNoPinsIsDrawnWithoutErrorAndKeepsItsSize()
	{
		Schema schema = new();
		schema.AddClass("Loner".As<ClassName>());

		ClassGraphView graph = new();
		harness.Draw = () => graph.Show(schema, 1f / 60f);

		harness.App.Step(3);
		Vector2 settled = graph.NodeSizes.First();

		harness.App.Step(10);

		// Read between frames: ImGui counts errors per frame and clears the count in the next one.
		Assert.AreEqual(
			0,
			ImGui.GetCurrentContext().ErrorCountCurrentFrame,
			"ImGui reported a usage error while the graph was drawing.");

		Assert.AreEqual(
			settled,
			graph.NodeSizes.First(),
			$"The node grew from {settled} while it sat there with nothing happening to it.");
	}

	[TestMethod]
	public void TheGraphOpensAroundTheMiddleOfTheCanvas()
	{
		ClassGraphView graph = new();
		Schema schema = BuildReferencingSchema();

		Vector2 canvas = Vector2.Zero;
		harness.Draw = () =>
		{
			canvas = ImGui.GetContentRegionAvail();
			graph.Show(schema, 1f / 60f);
		};

		harness.App.Step(10);

		Vector2[] positions = [.. graph.NodePositions];
		Assert.AreEqual(2, positions.Length, "The schema has two classes, so the graph has two nodes.");

		foreach (Vector2 position in positions)
		{
			Assert.IsTrue(
				position.X > 0f && position.Y > 0f,
				$"A node was laid out at {position}, off the top-left corner of the canvas.");
		}

		Vector2 centre = canvas * 0.5f;
		Vector2 centroid = (positions[0] + positions[1]) * 0.5f;
		Assert.IsTrue(
			Vector2.Distance(centroid, centre) < centre.Y,
			$"The graph settled around {centroid}, which is not the canvas's middle at {centre}.");
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
