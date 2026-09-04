// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

using System;
using System.IO.Abstractions.TestingHelpers;

using ktsu.ImGui.App.Testing;

/// <summary>
/// An editor running headlessly, with frames advanced by the test rather than by a display.
/// </summary>
/// <remarks>
/// <para>
/// The editor's code is immediate-mode draw calls: a value is read, a widget is drawn, and what the
/// widget reports decides what happens next, all inside one function. None of that executes without
/// a live ImGui context, which is why the editor had no tests at all. <see cref="ImGuiAppHarness"/>
/// supplies that context with no window, no display and no GPU - it rasterizes in software and
/// injects input straight into ImGui's event queue - so the real draw code runs on a headless
/// continuous integration runner.
/// </para>
/// <para>
/// The configuration comes from <see cref="EditorHost.CreateConfig"/>, the same one the application
/// starts with, so a callback renamed or dropped there breaks these tests instead of leaving them
/// passing against a host that no longer exists.
/// </para>
/// <para>
/// Settings are redirected to an in-memory file system before the editor is constructed, because
/// <see cref="AppData"/> otherwise reads and writes the real settings of whoever runs the suite -
/// and the editor saves them on exit, so a test could overwrite a developer's open document and
/// recent files.
/// </para>
/// </remarks>
internal sealed class EditorHarness : IDisposable
{
	/// <summary>
	/// Gets the editor under test.
	/// </summary>
	internal SchemaEditor Editor { get; }

	/// <summary>
	/// Gets the harness advancing its frames.
	/// </summary>
	internal ImGuiAppHarness App { get; }

	private bool disposed;

	private EditorHarness(SchemaEditor editor, ImGuiAppHarness app)
	{
		Editor = editor;
		App = app;
	}

	/// <summary>
	/// Starts an editor with empty settings and advances the frames it needs to be drawing.
	/// </summary>
	/// <returns>The running harness. Dispose it to release the ImGui context.</returns>
	internal static EditorHarness Start()
	{
		// Must precede the constructor: it is the constructor that loads the settings.
		ktsu.AppDataStorage.AppData.ConfigureForTesting(() => new MockFileSystem());

		SchemaEditor editor = new();
		ImGuiAppHarness app = ImGuiAppHarness.Start(EditorHost.CreateConfig(editor), new HarnessOptions());

		// The first frame builds the font atlas and lays the panels out; nothing is measurable
		// before it has run.
		app.Step();

		return new EditorHarness(editor, app);
	}

	/// <summary>
	/// Advances frames until a condition holds, failing the test if it never does.
	/// </summary>
	/// <param name="condition">Checked before the first frame and after every frame.</param>
	/// <param name="description">What was being waited for, for the failure message.</param>
	/// <param name="maxFrames">The frame budget. Frames rather than time, so a loaded runner is slower without being flakier.</param>
	internal void StepUntil(Func<bool> condition, string description, int maxFrames = 120)
	{
		if (!App.StepUntil(condition, maxFrames))
		{
			Assert.Fail($"{description} did not happen within {maxFrames} frames.");
		}
	}

	/// <summary>
	/// Waits for a marked item to be drawn, then clicks it.
	/// </summary>
	/// <remarks>
	/// The frames between the item first appearing and the click are not padding. A modal sizes
	/// itself from its contents on the frame it appears and is centred on the next, so the
	/// rectangle recorded for a control on its first frame is not where that control ends up;
	/// clicking there hits the background instead.
	/// </remarks>
	/// <param name="item">A marked name, or the trailing part of one.</param>
	internal void Click(string item)
	{
		StepUntil(() => App.Probe.Matches(item).Count > 0, $"'{item}' appearing");
		App.Step(3);
		App.Click(item);
		App.Step(2);
	}

	/// <summary>
	/// Right-clicks a marked item, which is how the tree opens an item's context menu.
	/// </summary>
	/// <param name="item">A marked name, or the trailing part of one.</param>
	internal void RightClick(string item)
	{
		StepUntil(() => App.Probe.Matches(item).Count > 0, $"'{item}' appearing");
		App.Step(3);

		Rectangle rect = App.Probe.Rect(item) ?? throw new AssertFailedException($"'{item}' was not recorded.");
		App.Mouse.Click((rect.MinX + rect.MaxX) * 0.5f, (rect.MinY + rect.MaxY) * 0.5f, 1);
		App.Step(3);
	}

	/// <summary>
	/// Types a value into a marked text field, replacing whatever it holds.
	/// </summary>
	/// <param name="field">A marked name, or the trailing part of one.</param>
	/// <param name="text">The text to leave in the field.</param>
	internal void TypeInto(string field, string text)
	{
		Click(field);
		App.Keyboard.Press(Hexa.NET.ImGui.ImGuiKey.A, ctrl: true);
		App.Keyboard.Type(text);
	}

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		disposed = true;
		App.Dispose();
		ktsu.AppDataStorage.AppData.ResetFileSystem();
	}
}
