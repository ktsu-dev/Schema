// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.SchemaEditor;

using ImGuiNET;

using ktsu.ImGuiStyler;
using ktsu.Schema;

internal class TreeDataSource(SchemaEditor schemaEditor)
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
						x.TryRemove();
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
					DataSourceName dataSourceName = (DataSourceName)newName;
					if (schema.TryAddDataSource(dataSourceName))
					{
						schemaEditor.EditDataSource(dataSourceName);
					}
					else
					{
						Popups.OpenMessageOK("Error", $"A Data Source with that name ({newName}) already exists.");
					}
				});
			}
		}
	}
}
