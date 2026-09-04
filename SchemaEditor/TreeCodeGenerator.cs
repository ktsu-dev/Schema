// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Styler;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using ktsu.UndoRedo;

internal sealed class TreeCodeGenerator(SchemaEditor schemaEditor)
{
	private Popups Popups => schemaEditor.Popups;

	internal void Show()
	{
		Schema? schema = schemaEditor.CurrentSchema;
		if (schema is not null)
		{
			IReadOnlyCollection<SchemaCodeGenerator> children = schema.CodeGenerators;

			string name = "Code Generators";
			ButtonTree<SchemaCodeGenerator>.ShowTree(name, $"{name} ({children.Count})", children, new()
			{
				Collapsible = true,
				GetText = (x) => x.Name,
				GetTooltip = (x) => x.Description,
				GetIssue = schemaEditor.GetIssueFor,
				GetId = (x) => x.Name,
				OnItemClick = schemaEditor.EditCodeGenerator,
				OnTreeEnd = (t) =>
				{
					using (t.Child)
					{
						ShowNewCodeGenerator(schema);
					}
				},
				OnItemContextMenu = (x) =>
				{
					SchemaCodeGenerator captured = x;

					bool renameChosen = ImGui.Selectable($"Rename {captured.Name}");
					ImGuiProbes.MarkItem($"Rename{captured.Name}");
					if (renameChosen)
					{
						schemaEditor.PromptRename("code generator", captured.Name,
							newName => schema.TryRenameCodeGenerator(captured, newName.As<CodeGeneratorName>()));
					}

					bool deleteChosen = ImGui.Selectable($"Delete {captured.Name}");
					ImGuiProbes.MarkItem($"Delete{captured.Name}");
					if (deleteChosen)
					{
						schemaEditor.Execute(new DelegateCommand(
							$"Delete Code Generator '{captured.Name}'",
							() => captured.TryRemove(),
							() => schema.RestoreCodeGenerator(captured),
							ChangeType.Delete));
					}
				},
			}, parent: null);
		}
	}

	private void ShowNewCodeGenerator(Schema schema)
	{
		using (Button.Alignment.Left())
		{
			bool clicked = ImGui.Button("+ New Code Generator");
			ImGuiProbes.MarkItem("NewCodeGenerator");
			if (clicked)
			{
				Popups.OpenInputString("Input", "New Code Generator Name", string.Empty, (newName) =>
				{
					CodeGeneratorName codeGeneratorName = newName.As<CodeGeneratorName>();
					if (schema.GetCodeGenerator(codeGeneratorName) is not null)
					{
						Popups.OpenMessageOK("Error", $"A Code Generator with that name ({newName}) already exists.");
						return;
					}

					SchemaCodeGenerator? addedCodeGenerator = null;
					schemaEditor.Execute(new DelegateCommand(
						$"Add Code Generator '{codeGeneratorName}'",
						() =>
						{
							if (addedCodeGenerator is null)
							{
								addedCodeGenerator = schema.AddCodeGenerator(codeGeneratorName);
							}
							else
							{
								schema.RestoreCodeGenerator(addedCodeGenerator);
							}
						},
						() => addedCodeGenerator?.TryRemove(),
						ChangeType.Insert));
				});
			}
		}
	}
}
