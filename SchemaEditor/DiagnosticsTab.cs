// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

using System.Collections.Generic;

using ktsu.ImGui.Widgets;

/// <summary>
/// The diagnostics tab's label, which carries the schema's issue counts.
/// </summary>
/// <remarks>
/// The counts used to be drawn at the end of the application menu bar, where ImGuiApp's own menus
/// follow the editor's and left them reading as a menu nobody could open. The tab is where the
/// issues themselves are, so the count is one click from the list it counts.
///
/// Separate from <see cref="SchemaEditor"/> because reaching a tab by id to rewrite its label
/// brings the widget library's tab types with it, and that class is already at the coupling the
/// analyzer allows.
/// </remarks>
internal static class DiagnosticsTab
{
	/// <summary>
	/// The tab's name, which is the whole label while the schema is clean.
	/// </summary>
	internal const string Name = "Diagnostics";

	/// <summary>
	/// Puts the counts on the tab, or takes them off once there is nothing to report.
	/// </summary>
	/// <param name="tabs">The tab bar the diagnostics tab belongs to.</param>
	/// <param name="tabId">The tab, as the panel recorded it when adding it.</param>
	/// <param name="errors">How many errors the schema has.</param>
	/// <param name="warnings">How many warnings it has.</param>
	internal static void ShowCounts(ImGuiWidgets.TabPanel tabs, string tabId, int errors, int warnings)
	{
		if (tabs.GetTabById(tabId) is ImGuiWidgets.Tab tab)
		{
			tab.Label = errors + warnings == 0 ? Name : $"{Name} ({FormatSummary(errors, warnings)})";
		}
	}

	/// <summary>
	/// Gets the label the tab is carrying.
	/// </summary>
	/// <param name="tabs">The tab bar the diagnostics tab belongs to.</param>
	/// <param name="tabId">The tab, as the panel recorded it when adding it.</param>
	/// <returns>The label, or empty when the tab is not there.</returns>
	internal static string LabelOf(ImGuiWidgets.TabPanel tabs, string tabId) =>
		tabs.GetTabById(tabId)?.Label ?? string.Empty;

	/// <summary>
	/// Counts the issues, leaving out whichever kind there are none of.
	/// </summary>
	/// <remarks>
	/// A schema with two errors and no warnings reads as "2 errors" rather than "2 errors, 0
	/// warnings": the zero is noise in a tab label, and it was never news in the panel either.
	/// </remarks>
	/// <param name="errors">How many errors the schema has.</param>
	/// <param name="warnings">How many warnings it has.</param>
	/// <returns>The counts, as text.</returns>
	internal static string FormatSummary(int errors, int warnings)
	{
		List<string> parts = [];

		if (errors > 0)
		{
			parts.Add($"{errors} error{(errors == 1 ? string.Empty : "s")}");
		}

		if (warnings > 0)
		{
			parts.Add($"{warnings} warning{(warnings == 1 ? string.Empty : "s")}");
		}

		return string.Join(", ", parts);
	}
}
