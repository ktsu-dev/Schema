// Copyright (c) 2023-2026 ktsu-dev contributors

// Both test assemblies are named, rather than only the one that reads this project's internals.
// ktsu.Sdk's KTSU0002 requires a non-test project to expose its internals to the repository's test
// projects, and there are two of them now; which of the two a given project actually needs is not
// what the rule is checking.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ktsu.SchemaEditor.Test")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ktsu.Schema.Test")]

namespace ktsu.SchemaEditor;

using System.Collections.ObjectModel;
using ktsu.AppDataStorage;
using ktsu.ImGui.App;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Instantiated via reflection by AppDataStorage")]
internal sealed class AppData : AppData<AppData>
{
	public AbsoluteFilePath CurrentSchemaPath { get; set; } = new();
	public ClassName CurrentClassName { get; set; } = new();
	public ImGuiAppWindowState WindowState { get; set; } = new();
	public HashSet<string> HiddenItems { get; set; } = [];
	public Dictionary<string, Collection<float>> DividerStates { get; set; } = [];
	public Popups Popups { get; set; } = new();

	/// <summary>
	/// Gets or sets the name of the ktsu.ThemeProvider theme to apply, or empty for the default.
	/// </summary>
	/// <remarks>
	/// Stored by name rather than as colours so a theme that is revised upstream is picked up
	/// rather than frozen at whatever it looked like when the setting was written.
	/// </remarks>
	public string ThemeName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the most recently opened schema files, newest first.
	/// </summary>
	/// <remarks>
	/// Bounded to <see cref="MaxRecentFiles"/> so the menu stays usable and the settings file
	/// stays small. Entries that no longer exist on disk are skipped when the menu is built rather
	/// than pruned here, so a file on a disconnected drive comes back when the drive returns.
	/// </remarks>
	public Collection<AbsoluteFilePath> RecentFiles { get; set; } = [];

	/// <summary>
	/// The most recent files retained.
	/// </summary>
	public const int MaxRecentFiles = 10;

	/// <summary>
	/// Records a file as the most recently opened one.
	/// </summary>
	/// <param name="path">The file that was opened or saved.</param>
	public void RecordRecentFile(AbsoluteFilePath path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return;
		}

		for (int index = RecentFiles.Count - 1; index >= 0; index--)
		{
			if (string.Equals(RecentFiles[index], path, StringComparison.Ordinal))
			{
				RecentFiles.RemoveAt(index);
			}
		}

		RecentFiles.Insert(0, path);

		while (RecentFiles.Count > MaxRecentFiles)
		{
			RecentFiles.RemoveAt(RecentFiles.Count - 1);
		}
	}
}
