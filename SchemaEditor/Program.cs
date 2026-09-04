// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

using ktsu.ImGui.App;

/// <summary>
/// The application entry point.
/// </summary>
/// <remarks>
/// This file holds the one statement that cannot be executed by a test: it opens a window and does
/// not return until the application closes. That is why it is a file of its own, and the only part
/// of the editor excluded from coverage measurement - what the host is configured with lives in
/// <see cref="EditorHost"/>, which the tests drive.
/// </remarks>
internal static class Program
{
	private static void Main(string[] _) => ImGuiApp.Start(EditorHost.CreateConfig(SchemaEditor.Instance));
}
