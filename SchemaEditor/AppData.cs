// Copyright (c) 2023-2026 ktsu-dev contributors

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
