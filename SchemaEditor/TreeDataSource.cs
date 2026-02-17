// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.SchemaEditor;

using ImGuiNET;

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
					if (ImGui.Selectable($"Delete {x.Name}"))
					{
						DataSource captured = x;
						schemaEditor.UndoRedo.Execute(new DelegateCommand(
							$"Delete Data Source '{captured.Name}'",
							() => captured.TryRemove(),
							() => schema.RestoreDataSource(captured),
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
			if (ImGui.Button("+ New Data Source"))
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
					schemaEditor.UndoRedo.Execute(new DelegateCommand(
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
