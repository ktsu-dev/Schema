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
