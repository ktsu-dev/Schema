// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

/// <summary>
/// That the harness itself works: without these, a failure anywhere else is ambiguous between the
/// editor being wrong and the harness never having drawn a frame.
/// </summary>
[TestClass]
public sealed class HarnessSmokeTests
{
	[TestMethod]
	public void EditorDrawsFramesHeadlessly()
	{
		using EditorHarness harness = EditorHarness.Start();

		int before = harness.App.FrameCount;
		harness.App.Step(3);

		Assert.AreEqual(before + 3, harness.App.FrameCount);
	}

	[TestMethod]
	public void EditorStartsWithNoDocument()
	{
		using EditorHarness harness = EditorHarness.Start();

		Assert.IsNull(harness.Editor.CurrentSchema);
		Assert.IsFalse(harness.Editor.HasUnsavedChanges);
		Assert.AreEqual("Untitled schema", harness.Editor.DocumentName);
	}
}
