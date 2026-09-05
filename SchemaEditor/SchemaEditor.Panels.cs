// Copyright (c) 2023-2026 ktsu-dev contributors

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.SchemaEditor;

using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;
using ktsu.UndoRedo;

using SchemaTypes = Schema.Models.Types;

/// <summary>
/// The property panels for the selected schema element, and the member grid.
/// </summary>
public partial class SchemaEditor
{
	private static Vector2 DescriptionSize => new(FieldWidth * 3, ImGui.GetTextLineHeight() * 3);

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

		ShowMembers();
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

		if (!ImGui.BeginPopupContextItem($"##DataSourceClassSelect{dataSource.Name}", ImGuiPopupFlags.MouseButtonLeft))
		{
			return;
		}

		if (ImGui.Selectable("<none>"))
		{
			SetDataSourceClass(dataSource, new ClassName());
		}

		foreach (SchemaClass schemaClass in schema.Classes)
		{
			if (ImGui.Selectable(schemaClass.Name))
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

	public static void ShowMemberHeadings()
	{
		ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 1.0f));
		ImGui.Button("Name", new Vector2(FieldWidth, 0));
		ImGui.SameLine();
		ImGui.Button("Type", new Vector2(FieldWidth, 0));
		ImGui.SameLine();
		ImGui.Button("Container", new Vector2(FieldWidth, 0));
		ImGui.SameLine();
		ImGui.Button("Key", new Vector2(FieldWidth, 0));
		ImGui.PopStyleColor();
	}

	private void ShowMembers()
	{
		if (CurrentClass is null || CurrentSchema is null)
		{
			return;
		}

		SchemaClass schemaClass = CurrentClass;
		if (!ImGui.CollapsingHeader($"{schemaClass.Name} Members", ImGuiTreeNodeFlags.DefaultOpen))
		{
			return;
		}

		float frameHeight = ImGui.GetFrameHeight();
		float spacing = ImGui.GetStyle().ItemSpacing.X;

		// Leave room for the reorder and delete controls that precede each row.
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ((frameHeight + spacing) * 4));
		ShowMemberHeadings();

		SchemaMember[] members = [.. schemaClass.Members];
		for (int index = 0; index < members.Length; index++)
		{
			ShowMemberRow(schemaClass, members[index], index, members.Length, frameHeight);
		}

		ImGui.NewLine();
	}

	private void ShowMemberRow(SchemaClass schemaClass, SchemaMember member, int index, int memberCount, float frameHeight)
	{
		ImGui.PushID($"member{member.Name}");

		// A probe scope alongside the ImGui id stack: PushID keeps two rows' widgets apart for
		// ImGui, and this keeps their recorded names apart for a test, which would otherwise see
		// one ambiguous "Delete" however many members the class has.
		ImGuiProbes.PushScope($"member{member.Name}");

		ShowMemberReorderButtons(schemaClass, member, index, memberCount);

		ImGui.SameLine();
		bool deleteClicked = ImGui.Button("X", new Vector2(frameHeight, 0));
		ImGuiProbes.MarkItem("Delete");
		if (deleteClicked)
		{
			DeleteMember(schemaClass, member);
		}

		ImGui.SameLine();
		string descriptionKey = $"memberdescription:{schemaClass.Name}.{member.Name}";
		bool descriptionOpen = !IsVisible(descriptionKey);
		if (ImGui.ArrowButton("##ToggleDescription", descriptionOpen ? ImGuiDir.Down : ImGuiDir.Right))
		{
			ToggleVisibility(descriptionKey);
		}

		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(string.IsNullOrEmpty(member.Description) ? "Add a description" : member.Description);
		}

		ImGui.SameLine();
		ShowMemberNameField(schemaClass, member);

		ImGui.SameLine();
		ShowMemberConfig(CurrentSchema!, member);

		ShowMemberIssueMarker(member);

		if (descriptionOpen)
		{
			ImGui.Indent();
			ShowDescriptionEditor(
				$"##MemberDescription{schemaClass.Name}.{member.Name}",
				"Description:",
				member.Description,
				value => member.Description = value);
			ImGui.Unindent();
		}

		ImGuiProbes.PopScope();
		ImGui.PopID();
	}

	/// <summary>
	/// Draws the up/down controls that move a member within its class.
	/// </summary>
	/// <remarks>
	/// Up/down rather than drag-and-drop: the member row is already five controls wide, and a drag
	/// source competing with the text field and the type button in that space is easy to trigger
	/// by accident.
	/// </remarks>
	private void ShowMemberReorderButtons(SchemaClass schemaClass, SchemaMember member, int index, int memberCount)
	{
		ImGui.BeginDisabled(index == 0);
		bool moveUp = ImGui.ArrowButton("##MoveUp", ImGuiDir.Up);
		ImGuiProbes.MarkItem("MoveUp");
		if (moveUp)
		{
			MoveMember(schemaClass, member, index - 1);
		}

		ImGui.EndDisabled();

		ImGui.SameLine();
		ImGui.BeginDisabled(index == memberCount - 1);
		bool moveDown = ImGui.ArrowButton("##MoveDown", ImGuiDir.Down);
		ImGuiProbes.MarkItem("MoveDown");
		if (moveDown)
		{
			MoveMember(schemaClass, member, index + 1);
		}

		ImGui.EndDisabled();
	}

	private void MoveMember(SchemaClass schemaClass, SchemaMember member, int newIndex)
	{
		int previousIndex = schemaClass.IndexOfMember(member);
		if (previousIndex < 0)
		{
			return;
		}

		Execute(new DelegateCommand(
			$"Move Member '{member.Name}'",
			() => schemaClass.TryMoveMember(member, newIndex),
			() => schemaClass.TryMoveMember(member, previousIndex),
			ChangeType.Move));
	}

	private void ShowMemberNameField(SchemaClass schemaClass, SchemaMember member)
	{
		if (EditField.Text("##Name", FieldWidth, member.Name, out string committed, 64))
		{
			ApplyRename("member", member.Name, committed,
				newName => schemaClass.TryRenameMember(member, newName.As<MemberName>()));
		}
	}

	/// <summary>
	/// Marks a member row that owns a validation issue, with the message as its tooltip.
	/// </summary>
	private void ShowMemberIssueMarker(SchemaMember member)
	{
		SchemaValidationIssue? issue = GetIssueFor(member);
		if (issue is null)
		{
			return;
		}

		ImGui.SameLine();
		using (EditorTheme.Severity(issue.Severity))
		{
			ImGui.TextUnformatted(issue.Severity == SchemaValidationSeverity.Error ? "!" : "?");
		}

		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(issue.Message);
		}
	}

	/// <summary>
	/// Removes a member, remembering where it was so an undo puts it back there.
	/// </summary>
	/// <remarks>
	/// <c>RestoreMember</c> appends, and member order is part of the schema's meaning rather than
	/// a display concern - it is the declaration order generated code uses, and it round-trips
	/// through the file. So restoring alone turns an undo into an edit of its own: delete a member
	/// from the middle of a class, undo, and the class comes back reordered.
	/// </remarks>
	private void DeleteMember(SchemaClass schemaClass, SchemaMember member)
	{
		int index = schemaClass.IndexOfMember(member);

		Execute(new DelegateCommand(
			$"Delete Member '{member.Name}'",
			() => member.TryRemove(),
			() =>
			{
				schemaClass.RestoreMember(member);
				schemaClass.TryMoveMember(member, index);
			},
			ChangeType.Delete));
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3267:Loops should be simplified with \"LINQ\" expressions", Justification = "We want to separate out ImGui calls from enumerations")]
	public void ShowMemberConfig(Schema schema, SchemaMember schemaMember)
	{
		Ensure.NotNull(schema);
		Ensure.NotNull(schemaMember);

		if (ImGui.Button($"{schemaMember.Type.DisplayName}##Type", new Vector2(FieldWidth, 0)))
		{
			SchemaMember captured = schemaMember;
			Popups.OpenTypeList("Select Type", "Type", schema.GetAvailableTypes(), captured.Type, (type) => SetMemberType(captured, type));
		}

		if (schemaMember.Type is not SchemaTypes.Array array)
		{
			return;
		}

		ImGui.SameLine();
		if (EditField.Text("##Container", FieldWidth, array.Container, out string container, 64))
		{
			ContainerName previous = array.Container;
			ContainerName next = container.As<ContainerName>();
			Execute(new DelegateCommand(
				$"Set Container '{next}'",
				() => array.Container = next,
				() => array.Container = previous,
				ChangeType.Modify));
		}

		if (array.ElementType is SchemaTypes.Object obj && obj.Class is not null)
		{
			ImGui.SameLine();
			ShowArrayKeySelector(array, obj.Class);
		}
	}

	private void SetMemberType(SchemaMember member, SchemaTypes.BaseType type)
	{
		SchemaTypes.BaseType previous = member.Type;
		Execute(new DelegateCommand(
			$"Set Type '{type.DisplayName}'",
			() => member.SetType(type),
			() => member.SetType(previous),
			ChangeType.Modify));
	}

	private void ShowArrayKeySelector(SchemaTypes.Array array, SchemaClass elementClass)
	{
		ImGui.Button(string.IsNullOrEmpty(array.Key) ? "<none>" : array.Key, new Vector2(FieldWidth, 0));

		if (!ImGui.BeginPopupContextItem("##Key", ImGuiPopupFlags.MouseButtonLeft))
		{
			return;
		}

		if (ImGui.Selectable("<none>"))
		{
			SetArrayKey(array, new MemberName());
		}

		foreach (SchemaMember primitiveMember in elementClass.Members.Where(m => m.Type.IsPrimitive).OrderBy(m => m.Name.ToString(), StringComparer.Ordinal))
		{
			if (ImGui.Selectable(primitiveMember.Name))
			{
				SetArrayKey(array, primitiveMember.Name);
			}
		}

		ImGui.EndPopup();
	}

	private void SetArrayKey(SchemaTypes.Array array, MemberName key)
	{
		MemberName previous = array.Key;
		Execute(new DelegateCommand(
			$"Set Array Key '{key}'",
			() => array.Key = key,
			() => array.Key = previous,
			ChangeType.Modify));
	}
}
