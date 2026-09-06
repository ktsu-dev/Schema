// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

using System;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// The schema tree panel: one tab per kind of schema element.
/// </summary>
/// <remarks>
/// Tabbed rather than stacked because the four trees share one narrow column, and a schema with a
/// dozen classes pushed the data sources and code generators off the bottom of it. Each tree keeps
/// its own heading, which is where its count is shown.
/// </remarks>
internal sealed class TreeSchema(SchemaEditor schemaEditor)
{
	internal const string EnumsTab = "Enums";
	internal const string ClassesTab = "Classes";
	internal const string DataSourcesTab = "Data Sources";
	internal const string CodeGeneratorsTab = "Code Generators";

	private TreeEnum TreeEnum { get; } = new(schemaEditor);
	private TreeClass TreeClass { get; } = new(schemaEditor);
	private TreeDataSource TreeDataSource { get; } = new(schemaEditor);
	private TreeCodeGenerator TreeCodeGenerator { get; } = new(schemaEditor);

	/// <summary>
	/// The tab to open on the next frame, or null when the open tab is whatever was clicked last.
	/// </summary>
	private string? tabToOpen;

	/// <summary>
	/// Opens the tab holding one kind of element, on the next frame drawn.
	/// </summary>
	/// <remarks>
	/// Selecting an element does this, so that navigating to a diagnostic - or to anything else
	/// the editor selects on the user's behalf - brings up the tree the element is in rather than
	/// leaving its row behind a tab the user has to find.
	/// </remarks>
	/// <param name="name">The tab's name, as the constants on this class give it.</param>
	internal void SelectTab(string name) => tabToOpen = name;

	internal void Show()
	{
		if (!ImGui.BeginTabBar("SchemaTrees"))
		{
			return;
		}

		ShowTab(EnumsTab, TreeEnum.Show);
		ShowTab(ClassesTab, TreeClass.Show);
		ShowTab(DataSourcesTab, TreeDataSource.Show);
		ShowTab(CodeGeneratorsTab, TreeCodeGenerator.Show);

		ImGui.EndTabBar();
		tabToOpen = null;
	}

	/// <summary>
	/// Draws one tree behind its tab.
	/// </summary>
	/// <param name="name">The kind of element the tree holds.</param>
	/// <param name="show">Draws the tree.</param>
	private void ShowTab(string name, Action show)
	{
		ImGuiTabItemFlags flags = name == tabToOpen ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
		bool selected = ImGui.BeginTabItem(name, flags);
		ImGuiProbes.MarkItem("tree-tab", name);

		if (selected)
		{
			show();
			ImGui.EndTabItem();
		}
	}
}
