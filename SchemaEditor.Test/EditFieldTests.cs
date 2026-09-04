// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

/// <summary>
/// That a text field reports one value per editing session rather than one per frame, and that two
/// fields sharing a label do not share a buffer.
/// </summary>
/// <remarks>
/// This is the widget the editor's every name and description field is built on. ImGui hands back
/// its buffer on every frame the widget exists, so the naive binding writes to the model sixty
/// times a second and, now that edits are undoable, pushes an undo entry each time.
/// </remarks>
[TestClass]
public sealed class EditFieldTests
{
	private WidgetHarness harness = null!;

	[TestInitialize]
	public void StartHarness()
	{
		harness = WidgetHarness.Start();
		EditField.Reset();
	}

	[TestCleanup]
	public void StopHarness()
	{
		EditField.Reset();
		harness.Dispose();
	}

	/// <summary>
	/// One text field, with everything a test needs to drive it and to see what it reported.
	/// </summary>
	private sealed class Field
	{
		internal string Model { get; set; } = string.Empty;
		internal string Id { get; init; } = "##Field";
		internal string? Scope { get; init; }
		internal int Commits { get; private set; }
		internal string? LastCommit { get; private set; }
		internal Vector2 Centre { get; private set; }
		internal Rectangle Bounds { get; private set; }

		internal void Draw()
		{
			if (Scope is not null)
			{
				ImGui.PushID(Scope);
			}

			if (EditField.Text(Id, 200f, Model, out string committed))
			{
				Commits++;
				LastCommit = committed;
				Model = committed;
			}

			Vector2 min = ImGui.GetItemRectMin();
			Vector2 max = ImGui.GetItemRectMax();
			Centre = (min + max) * 0.5f;
			Bounds = new Rectangle((int)min.X, (int)min.Y, (int)MathF.Ceiling(max.X), (int)MathF.Ceiling(max.Y));

			if (Scope is not null)
			{
				ImGui.PopID();
			}
		}
	}

	/// <summary>
	/// Clicks into a field and replaces its contents, leaving the field still being edited.
	/// </summary>
	private void TypeInto(Field field, string text)
	{
		harness.App.Mouse.Click(field.Centre.X, field.Centre.Y);
		harness.App.Keyboard.Press(ImGuiKey.A, ctrl: true);
		harness.App.Keyboard.Type(text);
	}

	private void CommitWithEnter() => harness.App.Keyboard.Press(ImGuiKey.Enter);

	[TestMethod]
	public void AnUntouchedFieldReportsNothing()
	{
		Field field = new() { Model = "Name" };
		harness.Draw = field.Draw;

		harness.App.Step(10);

		Assert.AreEqual(0, field.Commits, "A field nobody touched wrote to the model.");
	}

	[TestMethod]
	public void FinishingAnEditReportsTheNewValue()
	{
		Field field = new() { Model = "Name" };
		harness.Draw = field.Draw;
		harness.App.Step(2);

		TypeInto(field, "User");
		CommitWithEnter();

		Assert.AreEqual(1, field.Commits);
		Assert.AreEqual("User", field.LastCommit);
	}

	/// <summary>
	/// The point of the widget: an editing session is one write, and so one undo entry, however
	/// many frames it spans.
	/// </summary>
	[TestMethod]
	public void AnEditIsReportedOnceRatherThanPerFrame()
	{
		Field field = new() { Model = "Name" };
		harness.Draw = field.Draw;
		harness.App.Step(2);

		TypeInto(field, "User");
		CommitWithEnter();
		harness.App.Step(30);

		Assert.AreEqual(1, field.Commits, "The field kept writing to the model after the edit finished.");
	}

	[TestMethod]
	public void LeavingAFieldWithoutChangingItReportsNothing()
	{
		Field field = new() { Model = "Name" };
		harness.Draw = field.Draw;
		harness.App.Step(2);

		harness.App.Mouse.Click(field.Centre.X, field.Centre.Y);
		harness.App.Keyboard.Press(ImGuiKey.Enter);
		harness.App.Step(3);

		Assert.AreEqual(0, field.Commits);
		Assert.AreEqual("Name", field.Model);
	}

	/// <summary>
	/// Escape abandons the edit, so nothing should reach the model - and no undo entry should be
	/// pushed for an edit the user took back.
	/// </summary>
	[TestMethod]
	public void AbandoningAnEditReportsNothing()
	{
		Field field = new() { Model = "Name" };
		harness.Draw = field.Draw;
		harness.App.Step(2);

		TypeInto(field, "User");
		harness.App.Keyboard.Press(ImGuiKey.Escape);
		harness.App.Step(3);

		Assert.AreEqual(0, field.Commits);
		Assert.AreEqual("Name", field.Model);
	}

	/// <summary>
	/// An edit spanning several frames must accumulate. The scratch buffer is what carries it, so
	/// this fails the moment the buffer is dropped or overwritten between frames.
	/// </summary>
	[TestMethod]
	public void AnEditSpanningManyFramesKeepsEveryCharacter()
	{
		Field field = new() { Model = "Name" };
		harness.Draw = field.Draw;
		harness.App.Step(2);

		TypeInto(field, "LongerReplacement");
		CommitWithEnter();

		Assert.AreEqual("LongerReplacement", field.LastCommit);
	}

	/// <summary>
	/// Every member row in the editor draws its name field as "##Name" and relies on the
	/// surrounding PushID to tell the rows apart. Keying the scratch buffer on the label rather
	/// than the resolved id lets one row's half-typed text into the next row's field.
	/// </summary>
	/// <remarks>
	/// The leak has to be measured on screen. ImGui keeps its own copy of the text while a widget
	/// is being edited, so the row being typed into still commits the right value with the buffers
	/// shared - the row that is wrong is the other one, which is handed its sibling's text to draw.
	/// So this compares the pixels of the second row against the same pixels before the edit
	/// started: nothing about that row changed, so nothing about it should be drawn differently.
	/// </remarks>
	[TestMethod]
	public void TwoRowsSharingALabelDoNotShareABuffer()
	{
		Field first = new() { Model = "Alpha", Id = "##Name", Scope = "row0" };
		Field second = new() { Model = "Beta", Id = "##Name", Scope = "row1" };
		harness.Draw = () =>
		{
			first.Draw();
			second.Draw();
		};
		harness.App.Step(3);

		CapturedFrame before = harness.App.Capture();
		Rectangle secondRow = second.Bounds;

		// Long and unlike "Beta", so a leak into the second row is unmistakable on screen.
		TypeInto(first, "WWWWWWWWWWWW");
		CapturedFrame during = harness.App.Capture();

		AssertUnchanged(before, during, secondRow, "The second row was redrawn while its sibling was being edited, so the sibling's text leaked into it.");

		CommitWithEnter();

		Assert.AreEqual("WWWWWWWWWWWW", first.LastCommit);
		Assert.AreEqual(1, first.Commits);
		Assert.AreEqual(0, second.Commits, "Editing one row wrote to another row's model.");
		Assert.AreEqual("Beta", second.Model);
	}

	private static void AssertUnchanged(CapturedFrame before, CapturedFrame after, Rectangle region, string message)
	{
		for (int y = region.MinY; y < region.MaxY; y++)
		{
			for (int x = region.MinX; x < region.MaxX; x++)
			{
				if (before.GetPixel(x, y) != after.GetPixel(x, y))
				{
					Assert.Fail($"{message} First difference at ({x}, {y}).");
				}
			}
		}
	}

	/// <summary>
	/// While a field is not being edited the model is the source of truth, so an undo that changes
	/// the value shows up the moment the field is redrawn - and does not get written back over.
	/// </summary>
	[TestMethod]
	public void AFieldThatIsNotBeingEditedFollowsTheModel()
	{
		Field field = new() { Model = "Name" };
		harness.Draw = field.Draw;
		harness.App.Step(2);

		TypeInto(field, "User");
		CommitWithEnter();
		Assert.AreEqual("User", field.Model);

		// What an undo of that rename does to the model.
		field.Model = "Name";
		harness.App.Step(5);

		Assert.AreEqual(1, field.Commits, "The field wrote its own last text back over the undone value.");
		Assert.AreEqual("Name", field.Model);
	}
}
