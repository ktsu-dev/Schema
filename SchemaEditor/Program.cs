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
	private static void Main(string[] _) =>
		ImGuiApp.Start(new()
		{
			// The startup title only; SchemaEditor keeps it current from there, showing the open
			// document and whether it has unsaved changes.
			Title = nameof(SchemaEditor),
			OnStart = SchemaEditor.OnStart,
			OnUpdate = SchemaEditor.Instance.OnTick,
			OnRender = SchemaEditor.Instance.OnRender,
			OnAppMenu = SchemaEditor.Instance.OnMenu,

			// Refuses a close that would discard unsaved work, so the editor can ask first.
			OnClosing = SchemaEditor.Instance.ShouldClose,
		});
}
