// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.SchemaEditor;

using ImGuiNET;

using ktsu.ImGui.Styler;
using ktsu.ImGui.Widgets;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;

internal class TreeEnum(SchemaEditor schemaEditor)
{
	private Popups Popups => schemaEditor.Popups;

	internal void Show()
	{
		Schema? schema = schemaEditor.CurrentSchema;
		if (schema is not null)
		{
			IReadOnlyCollection<SchemaEnum> children = schema.Enums;

			string name = "Enums";
			ButtonTree<SchemaEnum>.ShowTree(name, $"{name} ({children.Count})", children, new()
			{
				Collapsible = true,
				GetText = (x) => $"{x.Name} ({x.Values.Count})",
				GetId = (x) => x.Name,
				OnTreeEnd = (t) =>
				{
					using (t.Child)
					{
						ShowNewEnum(schema);
					}
				},
				OnItemEnd = ShowEnumValueTree,
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

	private void ShowEnumValueTree(ImGuiWidgets.Tree parent, SchemaEnum schemaEnum)
	{
		IReadOnlyCollection<EnumValueName> children = schemaEnum.Values;
		ButtonTree<EnumValueName>.ShowTree(schemaEnum.Name, $"{schemaEnum.Name} ({children.Count})", children, new()
		{
			GetText = (x) => x,
			GetId = (x) => x,
			OnItemContextMenu = (x) =>
			{
				if (ImGui.Selectable($"Delete {x}"))
				{
					schemaEnum.TryRemoveValue(x);
				}
			},
			OnTreeEnd = (t) =>
			{
				using (t.Child)
				{
					ShowNewEnumValue(schemaEnum);
				}
			},
		}, parent);
	}

	private void ShowNewEnum(Schema schema)
	{
		using (Button.Alignment.Left())
		{
			if (ImGui.Button("+ New Enum"))
			{
				Popups.OpenInputString("Input", "New Enum Name", string.Empty, (newName) =>
				{
					if (schema.TryAddEnum((EnumName)newName))
					{

					}
					else
					{
						Popups.OpenMessageOK("Error", $"An Enum with that name ({newName}) already exists.");
					}
				});
			}
		}
	}

	private void ShowNewEnumValue(SchemaEnum schemaEnum)
	{
		using (Button.Alignment.Left())
		{
			if (ImGui.Button($"+ New Value##addEnumValue{schemaEnum.Name}"))
			{
				Popups.OpenInputString("Input", "New Enum Value", string.Empty, (newValue) =>
				{
					if (!schemaEnum.TryAddValue((EnumValueName)newValue))
					{
						Popups.OpenMessageOK("Error", $"A Enum Value with that name ({newValue}) already exists.");
					}
				});
			}
		}
	}
}
