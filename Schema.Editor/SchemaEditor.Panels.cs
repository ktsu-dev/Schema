// Copyright (c) 2023-2026 ktsu-dev contributors

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.Schema.Editor;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;
using ktsu.UndoRedo;

/// <summary>
/// The property panels for the selected schema element, and the member grid.
/// </summary>
public partial class SchemaEditor
{
	private static Vector2 DescriptionSize => new(FieldWidth * 3, ImGui.GetTextLineHeight() * 3);

	/// <summary>
	/// What a picker offers for "point this at nothing", and the name that option is recorded under.
	/// </summary>
	internal const string NoneOption = "<none>";

	/// <summary>
	/// Draws a description editor bound to a schema element, committing one undo entry per edit.
	/// </summary>
	/// <remarks>
	/// Takes accessors rather than the element itself because <see cref="SchemaChild{TName}"/> is
	/// generic in its name type, so there is no single parameter type covering every element kind.
	/// </remarks>
	/// <param name="id">A stable widget id for this element's description.</param>
	/// <param name="label">The label shown above the editor.</param>
	/// <param name="current">The description the element currently holds.</param>
	/// <param name="apply">Writes a description back to the element.</param>
	internal void ShowDescriptionEditor(string id, string label, SchemaChildDescription current, Action<SchemaChildDescription> apply)
	{
		ImGui.TextUnformatted(label);
		if (EditField.MultilineText(id, DescriptionSize, current, out string committed))
		{
			SchemaChildDescription previous = current;
			SchemaChildDescription next = committed.As<SchemaChildDescription>();
			Execute(new DelegateCommand(
				"Edit Description",
				() => apply(next),
				() => apply(previous),
				ChangeType.Modify));
		}
	}

	/// <summary>
	/// Draws the name field for the selected element, renaming through the schema so references
	/// are repointed and collisions are rejected.
	/// </summary>
	internal void ShowRenameField(string id, string currentName, Func<string, bool> tryRename, string kind)
	{
		ImGui.TextUnformatted("Name:");
		ImGui.SameLine();

		if (EditField.Text(id, FieldWidth * 2, currentName, out string committed))
		{
			ApplyRename(kind, currentName, committed, tryRename);
		}
	}

	/// <summary>
	/// Asks for a new name and applies it.
	/// </summary>
	/// <param name="kind">The kind of element, for the prompt and the undo entry.</param>
	/// <param name="currentName">The element's current name, offered as the default.</param>
	/// <param name="tryRename">Renames the element, reporting whether the new name was accepted.</param>
	internal void PromptRename(string kind, string currentName, Func<string, bool> tryRename) =>
		Popups.OpenInputString("Rename", $"New {kind} Name", currentName, (newName) =>
			ApplyRename(kind, currentName, newName, tryRename));

	/// <summary>
	/// Applies a rename and records it on the undo stack.
	/// </summary>
	/// <remarks>
	/// The rename is attempted first so a name that collides with a sibling is reported without
	/// leaving an entry on the undo stack. Re-running it from the command is a no-op, because
	/// renaming an element to the name it already has is allowed.
	/// </remarks>
	internal void ApplyRename(string kind, string previousName, string newName, Func<string, bool> tryRename)
	{
		if (string.Equals(previousName, newName, StringComparison.Ordinal))
		{
			return;
		}

		if (!tryRename(newName))
		{
			Popups.OpenMessageOK("Error", $"Cannot rename {kind} '{previousName}' to '{newName}': the name is empty or already in use.");
			return;
		}

		Execute(new DelegateCommand(
			$"Rename {kind} '{previousName}' to '{newName}'",
			() => tryRename(newName),
			() => tryRename(previousName),
			ChangeType.Modify));
	}

	private void ShowClassProperties()
	{
		if (CurrentClass is null || CurrentSchema is null)
		{
			return;
		}

		SchemaClass schemaClass = CurrentClass;
		Schema schema = CurrentSchema;

		if (ImGui.CollapsingHeader($"{schemaClass.Name} Properties", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ShowRenameField(
				$"##ClassName{schemaClass.Name}",
				schemaClass.Name,
				newName => schema.TryRenameClass(schemaClass, newName.As<ClassName>()),
				"class");

			ShowDescriptionEditor($"##ClassDescription{schemaClass.Name}", "Description:", schemaClass.Description, value => schemaClass.Description = value);
		}

		MemberGrid.Show(schema, schemaClass);
	}

	private void ShowEnumProperties()
	{
		if (CurrentEnum is null || CurrentSchema is null)
		{
			return;
		}

		SchemaEnum schemaEnum = CurrentEnum;
		Schema schema = CurrentSchema;

		if (!ImGui.CollapsingHeader($"{schemaEnum.Name} Properties", ImGuiTreeNodeFlags.DefaultOpen))
		{
			return;
		}

		ShowRenameField(
			$"##EnumName{schemaEnum.Name}",
			schemaEnum.Name,
			newName => schema.TryRenameEnum(schemaEnum, newName.As<EnumName>()),
			"enum");

		ShowDescriptionEditor($"##EnumDescription{schemaEnum.Name}", "Description:", schemaEnum.Description, value => schemaEnum.Description = value);
	}

	private void ShowDataSourceProperties()
	{
		if (CurrentDataSource is null || CurrentSchema is null)
		{
			return;
		}

		DataSource dataSource = CurrentDataSource;
		Schema schema = CurrentSchema;

		if (!ImGui.CollapsingHeader($"{dataSource.Name} Properties", ImGuiTreeNodeFlags.DefaultOpen))
		{
			return;
		}

		ShowRenameField(
			$"##DataSourceName{dataSource.Name}",
			dataSource.Name,
			newName => schema.TryRenameDataSource(dataSource, newName.As<DataSourceName>()),
			"data source");

		ImGui.TextUnformatted("File Path:");
		ImGui.SameLine();
		if (EditField.Text($"##DataSourceFile{dataSource.Name}", FieldWidth * 2, dataSource.File, out string filePath))
		{
			RelativeFilePath previous = dataSource.File;
			RelativeFilePath next = filePath.As<RelativeFilePath>();
			Execute(new DelegateCommand(
				$"Set Data Source File '{next}'",
				() => dataSource.File = next,
				() => dataSource.File = previous,
				ChangeType.Modify));
		}

		ImGui.TextUnformatted("Class:");
		ImGui.SameLine();
		ShowClassSelector(schema, dataSource);

		ShowDescriptionEditor($"##DataSourceDescription{dataSource.Name}", "Description:", dataSource.Description, value => dataSource.Description = value);
	}

	private void ShowClassSelector(Schema schema, DataSource dataSource)
	{
		string label = string.IsNullOrEmpty(dataSource.ClassName) ? "<Select Class>" : dataSource.ClassName;
		ImGui.Button($"{label}##DataSourceClass{dataSource.Name}", new Vector2(FieldWidth, 0));
		ImGuiProbes.MarkItem("class-selector", dataSource.Name);

		if (!ImGui.BeginPopupContextItem($"##DataSourceClassSelect{dataSource.Name}", ImGuiPopupFlags.MouseButtonLeft))
		{
			return;
		}

		bool none = ImGui.Selectable(NoneOption);
		ImGuiProbes.MarkItem("class-option", NoneOption);
		if (none)
		{
			SetDataSourceClass(dataSource, new ClassName());
		}

		foreach (SchemaClass schemaClass in schema.Classes)
		{
			bool chosen = ImGui.Selectable(schemaClass.Name);
			ImGuiProbes.MarkItem("class-option", schemaClass.Name);
			if (chosen)
			{
				SetDataSourceClass(dataSource, schemaClass.Name);
			}
		}

		ImGui.EndPopup();
	}

	private void SetDataSourceClass(DataSource dataSource, ClassName className)
	{
		ClassName previous = dataSource.ClassName;
		Execute(new DelegateCommand(
			$"Set Data Source Class '{className}'",
			() => dataSource.ClassName = className,
			() => dataSource.ClassName = previous,
			ChangeType.Modify));
	}
}
