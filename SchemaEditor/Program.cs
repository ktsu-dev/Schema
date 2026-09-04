// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

using ktsu.ImGui.App;

/// <summary>
/// The application entry point.
/// </summary>
/// <remarks>
/// Separate from <see cref="SchemaEditor"/> because starting the application is not part of editing
/// a schema: the configuration names a windowing framework and a delegate type for each callback,
/// and holding all of that in the editor class counts against its coupling budget without earning
/// anything. It also keeps the whole of the host's contract with ImGuiApp readable in one place.
/// </remarks>
internal static class Program
{
	/// <summary>
	/// Builds the host configuration for an editor instance.
	/// </summary>
	/// <remarks>
	/// Split out from <see cref="Main"/>, and taking the editor rather than reaching for
	/// <see cref="SchemaEditor.Instance"/>, so the headless test harness can drive frames through
	/// the same configuration the real application runs. A test that assembled its own callbacks
	/// would be exercising a parallel host rather than this one, and would keep passing after a
	/// callback here was renamed or dropped.
	/// </remarks>
	/// <param name="editor">The editor the callbacks are bound to.</param>
	/// <returns>The configuration to hand to <see cref="ImGuiApp"/>.</returns>
	internal static ImGuiAppConfig CreateConfig(SchemaEditor editor) =>
		new()
		{
			// The startup title only; SchemaEditor keeps it current from there, showing the open
			// document and whether it has unsaved changes.
			Title = nameof(SchemaEditor),
			OnStart = SchemaEditor.OnStart,
			OnUpdate = editor.OnTick,
			OnRender = editor.OnRender,
			OnAppMenu = editor.OnMenu,

			// Refuses a close that would discard unsaved work, so the editor can ask first.
			OnClosing = editor.ShouldClose,
		};

	private static void Main(string[] _) => ImGuiApp.Start(CreateConfig(SchemaEditor.Instance));
}
