namespace ktsu.SchemaEditor;

using ktsu.AppDataStorage;
using ktsu.StrongPaths;
using ktsu.ImGuiApp;
using System.Collections.ObjectModel;
using ktsu.Schema;

internal class AppData : AppData<AppData>
{
	public AbsoluteFilePath CurrentSchemaPath { get; set; } = new();
	public ClassName CurrentClassName { get; set; } = new();
	public ImGuiAppWindowState WindowState { get; set; } = new();
	public HashSet<string> HiddenItems { get; set; } = [];
	public Dictionary<string, Collection<float>> DividerStates { get; set; } = [];
	public Popups Popups { get; set; } = new();
}
