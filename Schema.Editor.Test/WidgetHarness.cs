// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor.Test;

using System;
using System.IO.Abstractions.TestingHelpers;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;

/// <summary>
/// A headless ImGui frame with nothing in it but the widget under test.
/// </summary>
/// <remarks>
/// <para>
/// Separate from <see cref="EditorHarness"/>, which drives the whole editor through the real
/// application configuration. A widget like <see cref="EditField"/> is a unit below that: what is
/// being tested is how it behaves across frames as ImGui reports the widget activating,
/// being edited and deactivating, and putting the whole editor on screen to reach it would only
/// add ways for the test to fail for reasons that are not the widget's.
/// </para>
/// <para>
/// It also carries an editor, for a panel that belongs to one but sits behind a tab the editor
/// cannot be asked to open - the widget library hosting the tabs neither records them for a probe
/// nor takes a selection from outside. Drawing such a panel here reaches the same code the tab
/// delegate runs. Its settings are redirected to an in-memory file system for the same reason
/// <see cref="EditorHarness"/> redirects them: the editor otherwise reads and writes the settings
/// of whoever runs the suite.
/// </para>
/// </remarks>
internal sealed class WidgetHarness : IDisposable
{
	/// <summary>
	/// Gets the harness advancing the frames.
	/// </summary>
	internal ImGuiAppHarness App { get; }

	/// <summary>
	/// Gets the editor a panel under test belongs to. Untouched by a test drawing a widget that
	/// has nothing to do with the editor.
	/// </summary>
	internal SchemaEditor Editor { get; }

	/// <summary>
	/// Gets or sets what to draw each frame. Called from inside a live frame, so it may call ImGui
	/// freely.
	/// </summary>
	internal Action Draw { get; set; } = () => { };

	private bool disposed;

	private WidgetHarness(ImGuiAppHarness app, SchemaEditor editor)
	{
		App = app;
		Editor = editor;
	}

	/// <summary>
	/// Starts a harness and advances the first frame, which builds the font atlas.
	/// </summary>
	/// <returns>The running harness. Dispose it to release the ImGui context.</returns>
	internal static WidgetHarness Start()
	{
		WidgetHarness? harness = null;

		ImGuiAppConfig config = new()
		{
			Title = nameof(WidgetHarness),
			OnRender = _ => harness?.Draw(),
		};

		// Must precede the constructor: it is the constructor that loads the settings.
		ktsu.AppDataStorage.AppData.ConfigureForTesting(() => new MockFileSystem());

		SchemaEditor editor = new();
		ImGuiAppHarness app = ImGuiAppHarness.Start(config, new HarnessOptions());
		harness = new WidgetHarness(app, editor);
		app.Step();

		return harness;
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
