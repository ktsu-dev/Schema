// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

using ktsu.ImGui.App.Testing;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

using SchemaTypes = ktsu.Schema.Models.Types;

/// <summary>
/// That an element carrying a validation error is actually drawn in the error colour.
/// </summary>
/// <remarks>
/// This is what the theme change was for. Colour is the editor's way of saying "this one is
/// wrong", and it only says anything if the rest of the interface is not already wearing it - the
/// editor used to tint every widget with the primary colour, leaving the marking nothing to stand
/// out against.
///
/// Measured on screen rather than through the model, because the model side is covered elsewhere
/// and it is the drawing that this is about. The default theme is VSCode Dark, whose error colour
/// is red; a test that pinned no theme could not ask this question.
/// </remarks>
[TestClass]
public sealed class ValidationMarkingTests
{
	private EditorHarness harness = null!;

	[TestInitialize]
	public void StartEditor()
	{
		harness = EditorHarness.Start();
		harness.Editor.Options.ThemeName = string.Empty;
		harness.Editor.OnStart();
	}

	[TestCleanup]
	public void StopEditor() => harness.Dispose();

	/// <summary>
	/// Counts pixels that are clearly red rather than any particular shade, so the test does not
	/// depend on the exact value the theme picks for an error.
	/// </summary>
	private int RedPixels()
	{
		CapturedFrame frame = harness.App.Capture();
		int count = 0;

		for (int y = 0; y < frame.Height; y++)
		{
			for (int x = 0; x < frame.Width; x++)
			{
				Rgba32 pixel = frame.GetPixel(x, y);
				if (pixel.R > 120 && pixel.R > pixel.G + 40 && pixel.R > pixel.B + 40)
				{
					count++;
				}
			}
		}

		return count;
	}

	private void Revalidate()
	{
		harness.Editor.RequestValidation();
		harness.Editor.UpdateValidation(SchemaEditor.ValidationDebounceSeconds);
		harness.App.Step(4);
	}

	[TestMethod]
	public void AMemberWhoseTypeIsBrokenIsDrawnInTheErrorColour()
	{
		Schema schema = new();
		SchemaClass user = schema.AddClass("User".As<ClassName>())!;
		SchemaMember id = user.AddMember("Id".As<MemberName>())!;
		id.SetType(new SchemaTypes.Int());

		harness.Editor.CurrentSchema = schema;
		harness.Editor.EditClass(user);
		Revalidate();

		Assert.AreEqual(0, harness.Editor.Diagnostics.Count, "This schema was supposed to start clean.");
		int before = RedPixels();

		// Point the member at a class that is not there: an error, reported against the member.
		id.SetType(new SchemaTypes.Object() { ClassName = "NoSuchClass".As<ClassName>() });
		Revalidate();

		Assert.IsNotNull(harness.Editor.GetIssueFor(id), "The broken reference should have been reported against the member.");
		Assert.IsTrue(RedPixels() > before, "The member carrying an error was drawn no differently from one without.");
	}

	/// <summary>
	/// The menu bar carries the counts as well, so a schema's health is visible without opening the
	/// diagnostics tab.
	/// </summary>
	[TestMethod]
	public void TheMenuBarIsDrawnInTheErrorColourWhileThereAreErrors()
	{
		Schema schema = new();
		schema.AddClass("User".As<ClassName>());
		harness.Editor.CurrentSchema = schema;
		Revalidate();
		int before = RedPixels();

		// An empty class name is an error, and nothing is selected, so the only thing that can
		// draw in the error colour is the summary in the menu bar.
		schema.AddClass(new ClassName());
		Revalidate();

		Assert.IsTrue(harness.Editor.Diagnostics.Count > 0);
		Assert.IsTrue(RedPixels() > before, "The menu bar did not report the errors in the error colour.");
	}
}
