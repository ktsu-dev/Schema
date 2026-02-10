// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.SchemaEditor;

using ImGuiNET;

using ktsu.ImGui.Styler;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;

using ktsu.Semantics.Strings;

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
				GetId = (x) => x.Name,
				OnTreeEnd = (t) =>
				{
					using (t.Child)
					{
						ShowNewCodeGenerator(schema);
					}
				},
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

	private void ShowNewCodeGenerator(Schema schema)
	{
		using (Button.Alignment.Left())
		{
			if (ImGui.Button("+ New Code Generator"))
			{
				Popups.OpenInputString("Input", "New Code Generator Name", string.Empty, (newName) =>
				{
					CodeGeneratorName codeGeneratorName = newName.As<CodeGeneratorName>();
					if (!schema.TryAddCodeGenerator(codeGeneratorName))
					{
						Popups.OpenMessageOK("Error", $"A Code Generator with that name ({newName}) already exists.");
					}
				});
			}
		}
	}
}
