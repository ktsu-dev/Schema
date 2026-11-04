// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Styler;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using ktsu.UndoRedo;

/// <summary>
/// The tree of the schema's semantic types: adding, renaming and deleting them.
/// </summary>
internal sealed class TreeSemanticType(SchemaEditor schemaEditor)
{
	private Popups Popups => schemaEditor.Popups;

	internal void Show()
	{
		Schema? schema = schemaEditor.CurrentSchema;
		if (schema is not null)
		{
			IReadOnlyCollection<SchemaSemanticType> children = schema.SemanticTypes;

			string name = "Semantic Types";
			ButtonTree<SchemaSemanticType>.ShowTree(schemaEditor, name, $"{name} ({children.Count})", children, new()
			{
				Collapsible = true,
				GetText = (x) => $"{x.Name} ({x.UnderlyingType.DisplayName})",
				GetTooltip = (x) => x.Description,
				GetIssue = schemaEditor.GetIssueFor,
				GetId = (x) => x.Name,
				OnItemClick = schemaEditor.EditSemanticType,
				OnTreeStart = (t) =>
				{
					using (t.Child)
					{
						ShowNewSemanticType(schema);
					}
				},
				OnItemContextMenu = (x) =>
				{
					SchemaSemanticType captured = x;

					bool renameChosen = ImGui.Selectable($"Rename {captured.Name}");
					ImGuiProbes.MarkItem($"Rename{captured.Name}");
					if (renameChosen)
					{
						schemaEditor.PromptRename("semantic type", captured.Name,
							newName => schema.TryRenameSemanticType(captured, newName.As<SemanticTypeName>()));
					}

					bool deleteChosen = ImGui.Selectable($"Delete {captured.Name}");
					ImGuiProbes.MarkItem($"Delete{captured.Name}");
					if (deleteChosen)
					{
						// Restored where it was rather than appended; see DeleteMember in the panel.
						int index = schema.SemanticTypeSet.IndexOf(captured);
						schemaEditor.Execute(new DelegateCommand(
							$"Delete Semantic Type '{captured.Name}'",
							() => captured.TryRemove(),
							() =>
							{
								schema.RestoreSemanticType(captured);
								schema.SemanticTypeSet.Move(captured, index);
							},
							ChangeType.Delete));
					}
				},
			}, parent: null);
		}
	}

	private void ShowNewSemanticType(Schema schema)
	{
		using (Button.Alignment.Left())
		{
			bool clicked = ImGui.Button("+ New Semantic Type");
			ImGuiProbes.MarkItem("NewSemanticType");
			if (clicked)
			{
				Popups.OpenInputString("Input", "New Semantic Type Name", string.Empty, (newName) =>
				{
					SemanticTypeName semanticTypeName = newName.As<SemanticTypeName>();
					if (schema.GetSemanticType(semanticTypeName) is not null)
					{
						Popups.OpenMessageOK("Error", $"A Semantic Type with that name ({newName}) already exists.");
						return;
					}

					SchemaSemanticType? added = null;
					schemaEditor.Execute(new DelegateCommand(
						$"Add Semantic Type '{semanticTypeName}'",
						() =>
						{
							if (added is null)
							{
								added = schema.AddSemanticType(semanticTypeName);
							}
							else
							{
								schema.RestoreSemanticType(added);
							}
						},
						() => added?.TryRemove(),
						ChangeType.Insert));
				});
			}
		}
	}
}
