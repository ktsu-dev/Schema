// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using ktsu.UndoRedo;

using BaseType = ktsu.Schema.Models.Types.BaseType;

/// <summary>
/// The property panel for a semantic type: its name, description and underlying type.
/// </summary>
/// <remarks>
/// Renaming goes through <see cref="Schema.TryRenameSemanticType"/>, so every member, parameter,
/// return type and refining semantic type naming this one follows it. The six metadata
/// properties a semantic type also carries are not edited here yet: the panel that edits them
/// on a member is written against <see cref="SchemaMember"/>.
/// </remarks>
internal sealed class SemanticTypePanel(SchemaEditor schemaEditor)
{
	private Popups Popups => schemaEditor.Popups;

	internal void Show(Schema schema, SchemaSemanticType semanticType)
	{
		if (!ImGui.CollapsingHeader($"{semanticType.Name} Properties", ImGuiTreeNodeFlags.DefaultOpen))
		{
			return;
		}

		schemaEditor.ShowRenameField(
			$"##SemanticTypeName{semanticType.Name}",
			semanticType.Name,
			newName => schema.TryRenameSemanticType(semanticType, newName.As<SemanticTypeName>()),
			"semantic type");

		schemaEditor.ShowDescriptionEditor($"##SemanticTypeDescription{semanticType.Name}", "Description:", semanticType.Description, value => semanticType.Description = value);

		ImGui.TextUnformatted("Underlying Type:");
		bool typeClicked = ImGui.Button($"{semanticType.UnderlyingType.DisplayName}##UnderlyingType", new Vector2(SchemaEditor.FieldWidth, 0));
		ImGuiProbes.MarkItem("UnderlyingType");
		if (typeClicked)
		{
			Popups.OpenTypeList("Select Type", "Underlying Type", schema.GetAvailableTypes(), semanticType.UnderlyingType, type => SetUnderlyingType(semanticType, type));
		}
	}

	private void SetUnderlyingType(SchemaSemanticType semanticType, BaseType type)
	{
		BaseType previous = semanticType.UnderlyingType;
		schemaEditor.Execute(new DelegateCommand(
			$"Set Underlying Type '{type.DisplayName}'",
			() => semanticType.SetUnderlyingType(type),
			() => semanticType.SetUnderlyingType(previous),
			ChangeType.Modify));
	}
}
