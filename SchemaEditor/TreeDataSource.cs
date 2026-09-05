// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Styler;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using ktsu.UndoRedo;

internal sealed class TreeDataSource(SchemaEditor schemaEditor)
{
	private Popups Popups => schemaEditor.Popups;

	internal void Show()
	{
		Schema? schema = schemaEditor.CurrentSchema;
		if (schema is not null)
		{
			IReadOnlyCollection<DataSource> children = schema.DataSources;

			string name = "DataSources";
			ButtonTree<DataSource>.ShowTree(name, $"{name} ({children.Count})", children, new()
			{
				Collapsible = true,
				GetText = (x) => x.Name,
				GetTooltip = (x) => x.Description,
				GetIssue = schemaEditor.GetIssueFor,
				GetId = (x) => x.Name,
				OnTreeEnd = (t) =>
				{
					using (t.Child)
					{
						ShowNewDataSource(schema);
					}
				},
				OnItemClick = schemaEditor.EditDataSource,
				OnItemContextMenu = (x) =>
				{
					DataSource captured = x;

					bool renameChosen = ImGui.Selectable($"Rename {captured.Name}");
					ImGuiProbes.MarkItem($"Rename{captured.Name}");
					if (renameChosen)
					{
						schemaEditor.PromptRename("data source", captured.Name,
							newName => schema.TryRenameDataSource(captured, newName.As<DataSourceName>()));
					}

					bool deleteChosen = ImGui.Selectable($"Delete {captured.Name}");
					ImGuiProbes.MarkItem($"Delete{captured.Name}");
					if (deleteChosen)
					{
						// Restored where it was rather than appended; see DeleteMember in the panel.
						int index = schema.DataSourceSet.IndexOf(captured);
						schemaEditor.Execute(new DelegateCommand(
							$"Delete Data Source '{captured.Name}'",
							() => captured.TryRemove(),
							() =>
							{
								schema.RestoreDataSource(captured);
								schema.DataSourceSet.Move(captured, index);
							},
							ChangeType.Delete));
					}
				},
			}, parent: null);
		}
	}

	private void ShowNewDataSource(Schema schema)
	{
		using (Button.Alignment.Left())
		{
			bool clicked = ImGui.Button("+ New Data Source");
			ImGuiProbes.MarkItem("NewDataSource");
			if (clicked)
			{
				Popups.OpenInputString("Input", "New Data Source Name", string.Empty, (newName) =>
				{
					DataSourceName dataSourceName = newName.As<DataSourceName>();
					if (schema.GetDataSource(dataSourceName) is not null)
					{
						Popups.OpenMessageOK("Error", $"A Data Source with that name ({newName}) already exists.");
						return;
					}

					DataSource? addedDataSource = null;
					schemaEditor.Execute(new DelegateCommand(
						$"Add Data Source '{dataSourceName}'",
						() =>
						{
							if (addedDataSource is null)
							{
								addedDataSource = schema.AddDataSource(dataSourceName);
							}
							else
							{
								schema.RestoreDataSource(addedDataSource);
							}

							schemaEditor.EditDataSource(dataSourceName);
						},
						() => addedDataSource?.TryRemove(),
						ChangeType.Insert));
				});
			}
		}
	}
}
