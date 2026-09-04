// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Styler;
using ktsu.ImGui.Widgets;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using ktsu.UndoRedo;

internal sealed class TreeEnum(SchemaEditor schemaEditor)
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
				GetTooltip = (x) => x.Description,
				GetIssue = schemaEditor.GetIssueFor,
				GetId = (x) => x.Name,
				OnItemClick = schemaEditor.EditEnum,
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
					SchemaEnum captured = x;

					bool renameChosen = ImGui.Selectable($"Rename {captured.Name}");
					ImGuiProbes.MarkItem($"Rename{captured.Name}");
					if (renameChosen)
					{
						schemaEditor.PromptRename("enum", captured.Name,
							newName => schema.TryRenameEnum(captured, newName.As<EnumName>()));
					}

					bool deleteChosen = ImGui.Selectable($"Delete {captured.Name}");
					ImGuiProbes.MarkItem($"Delete{captured.Name}");
					if (deleteChosen)
					{
						// Restored where it was rather than appended; see DeleteMember in the panel.
						int index = schema.EnumSet.IndexOf(captured);
						schemaEditor.Execute(new DelegateCommand(
							$"Delete Enum '{captured.Name}'",
							() => captured.TryRemove(),
							() =>
							{
								schema.RestoreEnum(captured);
								schema.EnumSet.Move(captured, index);
							},
							ChangeType.Delete));
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
				EnumValueName captured = x;

				if (ImGui.Selectable($"Rename {captured}"))
				{
					schemaEditor.PromptRename("enum value", captured,
						newName => schemaEnum.TryRenameValue(captured, newName.As<EnumValueName>()));
				}

				if (ImGui.Selectable($"Delete {captured}"))
				{
					schemaEditor.Execute(new DelegateCommand(
						$"Delete Enum Value '{captured}'",
						() => schemaEnum.TryRemoveValue(captured),
						() => schemaEnum.TryAddValue(captured),
						ChangeType.Delete));
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
			bool clicked = ImGui.Button("+ New Enum");
			ImGuiProbes.MarkItem("NewEnum");
			if (clicked)
			{
				Popups.OpenInputString("Input", "New Enum Name", string.Empty, (newName) =>
				{
					EnumName enumName = newName.As<EnumName>();
					if (schema.GetEnum(enumName) is not null)
					{
						Popups.OpenMessageOK("Error", $"An Enum with that name ({newName}) already exists.");
						return;
					}

					SchemaEnum? addedEnum = null;
					schemaEditor.Execute(new DelegateCommand(
						$"Add Enum '{enumName}'",
						() =>
						{
							if (addedEnum is null)
							{
								addedEnum = schema.AddEnum(enumName);
							}
							else
							{
								schema.RestoreEnum(addedEnum);
							}
						},
						() => addedEnum?.TryRemove(),
						ChangeType.Insert));
				});
			}
		}
	}

	private void ShowNewEnumValue(SchemaEnum schemaEnum)
	{
		using (Button.Alignment.Left())
		{
			bool clicked = ImGui.Button($"+ New Value##addEnumValue{schemaEnum.Name}");
			ImGuiProbes.MarkItem("NewValue");
			if (clicked)
			{
				Popups.OpenInputString("Input", "New Enum Value", string.Empty, (newValue) =>
				{
					EnumValueName valueName = newValue.As<EnumValueName>();
					if (schemaEnum.Values.Any(v => v == valueName))
					{
						Popups.OpenMessageOK("Error", $"A Enum Value with that name ({newValue}) already exists.");
						return;
					}

					schemaEditor.Execute(new DelegateCommand(
						$"Add Enum Value '{valueName}'",
						() => schemaEnum.TryAddValue(valueName),
						() => schemaEnum.TryRemoveValue(valueName),
						ChangeType.Insert));
				});
			}
		}
	}
}
