// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

using ktsu.ImGui.App.Testing;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// How wide a tree row is drawn.
/// </summary>
/// <remarks>
/// The rows shared one fixed width, and ImGui clips a button's label to its frame, so the longest
/// label in the tree - "Code Generators (0)" - was drawn without its count. The width is a minimum
/// now: short labels still line up as a column, and a long one grows to fit.
/// </remarks>
[TestClass]
public sealed class TreeRowWidthTests
{
	private EditorHarness harness = null!;

	[TestCleanup]
	public void StopEditor() => harness?.Dispose();

	private int WidthOf(string item)
	{
		harness.StepUntil(() => harness.App.Probe.Matches(item).Count > 0, $"'{item}' appearing");
		Rectangle rect = harness.App.Probe.Rect(item) ?? throw new AssertFailedException($"'{item}' was not recorded.");
		return rect.Width;
	}

	/// <summary>
	/// The four tree headings are drawn from the same code with labels of very different lengths,
	/// which is where the clipping showed up.
	/// </summary>
	[TestMethod]
	public void ALongerHeadingIsDrawnWiderThanSpareColumnWidthAllows()
	{
		// Narrow, because the column is 15% of the display width: at a wide display every heading
		// fits and there is nothing to prove. This is the shape of window the clipping was
		// reported from.
		harness = EditorHarness.Start(new HarnessOptions { Width = 700, Height = 600 });
		harness.Editor.CurrentSchema = new Schema();

		Assert.IsTrue(
			WidthOf("RootCode Generators") > WidthOf("RootEnums"),
			"'Code Generators (0)' is the longest heading in the tree; drawn at the same width as 'Enums (0)' it loses its count.");
	}

	[TestMethod]
	public void ALongerClassNameIsDrawnWider()
	{
		harness = EditorHarness.Start();
		Schema schema = new();
		schema.AddClass("A".As<ClassName>());
		schema.AddClass("AClassNameLongEnoughToNeedMoreRoomThanTheColumnGives".As<ClassName>());
		harness.Editor.CurrentSchema = schema;

		Assert.IsTrue(
			WidthOf("BtnAClassNameLongEnoughToNeedMoreRoomThanTheColumnGives") > WidthOf("BtnA"),
			"A class name longer than the column was clipped instead of widening its row.");
	}

	/// <summary>
	/// The width is a minimum, not a per-row measurement: rows whose labels both fit still line up,
	/// or the tree would be a ragged edge.
	/// </summary>
	[TestMethod]
	public void ShortLabelsShareOneColumnWidth()
	{
		harness = EditorHarness.Start();
		Schema schema = new();
		schema.AddClass("A".As<ClassName>());
		schema.AddClass("Bee".As<ClassName>());
		harness.Editor.CurrentSchema = schema;

		Assert.AreEqual(WidthOf("BtnA"), WidthOf("BtnBee"));
	}
}
