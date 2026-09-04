// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

using System;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;

/// <summary>
/// A headless ImGui frame with nothing in it but the widget under test.
/// </summary>
/// <remarks>
/// Separate from <see cref="EditorHarness"/>, which drives the whole editor through the real
/// application configuration. A widget like <see cref="EditField"/> is a unit below that: what is
/// being tested is how it behaves across frames as ImGui reports the widget activating,
/// being edited and deactivating, and putting the whole editor on screen to reach it would only
/// add ways for the test to fail for reasons that are not the widget's.
/// </remarks>
internal sealed class WidgetHarness : IDisposable
{
	/// <summary>
	/// Gets the harness advancing the frames.
	/// </summary>
	internal ImGuiAppHarness App { get; }

	/// <summary>
	/// Gets or sets what to draw each frame. Called from inside a live frame, so it may call ImGui
	/// freely.
	/// </summary>
	internal Action Draw { get; set; } = () => { };

	private bool disposed;

	private WidgetHarness(ImGuiAppHarness app) => App = app;

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

		ImGuiAppHarness app = ImGuiAppHarness.Start(config, new HarnessOptions());
		harness = new WidgetHarness(app);
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
	}
}
