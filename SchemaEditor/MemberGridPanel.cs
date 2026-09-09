// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using ktsu.UndoRedo;

using SchemaTypes = Schema.Models.Types;

/// <summary>
/// The grid of member rows in the class panel: the controls that add, reorder, retype and remove a
/// class's members, and the folds that open each member's description and semantic metadata.
/// </summary>
/// <remarks>
/// Its own class rather than another part of <see cref="SchemaEditor"/>. The grid is the largest
/// panel the editor draws and the only one that reaches into a member's type, so keeping it here
/// couples the array container, the array key and the member's metadata to the grid that edits
/// them rather than to the editor as a whole.
/// </remarks>
/// <param name="schemaEditor">The editor this panel draws inside, which owns the undo stack.</param>
internal sealed class MemberGridPanel(SchemaEditor schemaEditor)
{
	private MemberSemanticsPanel Semantics { get; } = new(schemaEditor);

	internal static void ShowMemberHeadings()
	{
		ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 1.0f));
		ImGui.Button("Name", new Vector2(SchemaEditor.FieldWidth, 0));
		ImGui.SameLine();
		ImGui.Button("Type", new Vector2(SchemaEditor.FieldWidth, 0));
		ImGui.SameLine();
		ImGui.Button("Container", new Vector2(SchemaEditor.FieldWidth, 0));
		ImGui.SameLine();
		ImGui.Button("Key", new Vector2(SchemaEditor.FieldWidth, 0));
		ImGui.PopStyleColor();
	}

	/// <summary>
	/// Draws a class's members.
	/// </summary>
	/// <param name="schema">The schema the class belongs to.</param>
	/// <param name="schemaClass">The class whose members are being edited.</param>
	internal void Show(Schema schema, SchemaClass schemaClass)
	{
		if (!ImGui.CollapsingHeader($"{schemaClass.Name} Members", ImGuiTreeNodeFlags.DefaultOpen))
		{
			return;
		}

		float frameHeight = ImGui.GetFrameHeight();
		float spacing = ImGui.GetStyle().ItemSpacing.X;

		// Leave room for the reorder, delete and fold controls that precede each row.
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ((frameHeight + spacing) * 5));
		ShowMemberHeadings();

		SchemaMember[] members = [.. schemaClass.Members];
		for (int index = 0; index < members.Length; index++)
		{
			ShowMemberRow(schema, schemaClass, members[index], index, members.Length, frameHeight);
		}

		ImGui.NewLine();
	}

	private void ShowMemberRow(Schema schema, SchemaClass schemaClass, SchemaMember member, int index, int memberCount, float frameHeight)
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
		bool descriptionOpen = !schemaEditor.IsVisible(descriptionKey);
		bool toggleDescription = ImGui.ArrowButton("##ToggleDescription", descriptionOpen ? ImGuiDir.Down : ImGuiDir.Right);
		ImGuiProbes.MarkItem("ToggleDescription");
		if (toggleDescription)
		{
			schemaEditor.ToggleVisibility(descriptionKey);
		}

		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(string.IsNullOrEmpty(member.Description) ? "Add a description" : member.Description);
		}

		ImGui.SameLine();
		bool semanticsOpen = ShowSemanticsToggle(schemaClass, member, frameHeight);

		ImGui.SameLine();
		ShowMemberNameField(schemaClass, member);

		ImGui.SameLine();
		ShowMemberConfig(schema, member);

		ShowMemberIssueMarker(member);

		if (descriptionOpen)
		{
			ImGui.Indent();
			schemaEditor.ShowDescriptionEditor(
				$"##MemberDescription{schemaClass.Name}.{member.Name}",
				"Description:",
				member.Description,
				value => member.Description = value);
			ImGui.Unindent();
		}

		if (semanticsOpen)
		{
			Semantics.Show(schema, member);
		}

		ImGuiProbes.PopScope();
		ImGui.PopID();
	}

	/// <summary>
	/// Draws the control that folds a member's semantic metadata open, and reports whether it is
	/// open.
	/// </summary>
	/// <remarks>
	/// Labelled by what it holds rather than by a fixed glyph: a member that carries metadata says
	/// so with a dot while folded, which is the only way a folded row could show it at all. The
	/// button keeps the delete button's width either way, so a class of members with and without
	/// metadata still draws as a grid.
	/// </remarks>
	private bool ShowSemanticsToggle(SchemaClass schemaClass, SchemaMember member, float frameHeight)
	{
		string key = $"membersemantics:{schemaClass.Name}.{member.Name}";
		bool open = !schemaEditor.IsVisible(key);

		bool clicked = ImGui.Button(MemberSemanticsPanel.HasSemantics(member) ? "*" : "-", new Vector2(frameHeight, 0));
		ImGuiProbes.MarkItem("ToggleSemantics");
		if (clicked)
		{
			schemaEditor.ToggleVisibility(key);
			open = !open;
		}

		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(MemberSemanticsPanel.DescribeSemantics(member));
		}

		return open;
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

		schemaEditor.Execute(new DelegateCommand(
			$"Move Member '{member.Name}'",
			() => schemaClass.TryMoveMember(member, newIndex),
			() => schemaClass.TryMoveMember(member, previousIndex),
			ChangeType.Move));
	}

	private void ShowMemberNameField(SchemaClass schemaClass, SchemaMember member)
	{
		if (EditField.Text("##Name", SchemaEditor.FieldWidth, member.Name, out string committed, 64))
		{
			schemaEditor.ApplyRename("member", member.Name, committed,
				newName => schemaClass.TryRenameMember(member, newName.As<MemberName>()));
		}
	}

	/// <summary>
	/// Marks a member row that owns a validation issue, with the message as its tooltip.
	/// </summary>
	private void ShowMemberIssueMarker(SchemaMember member)
	{
		SchemaValidationIssue? issue = schemaEditor.GetIssueFor(member);
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

		schemaEditor.Execute(new DelegateCommand(
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
	internal void ShowMemberConfig(Schema schema, SchemaMember schemaMember)
	{
		Ensure.NotNull(schema);
		Ensure.NotNull(schemaMember);

		bool typeClicked = ImGui.Button($"{schemaMember.Type.DisplayName}##Type", new Vector2(SchemaEditor.FieldWidth, 0));
		ImGuiProbes.MarkItem("Type");
		if (typeClicked)
		{
			SchemaMember captured = schemaMember;
			schemaEditor.Popups.OpenTypeList("Select Type", "Type", schema.GetAvailableTypes(), captured.Type, (type) => SetMemberType(captured, type));
		}

		if (schemaMember.Type is not SchemaTypes.Array array)
		{
			return;
		}

		ImGui.SameLine();
		if (EditField.Text("##Container", SchemaEditor.FieldWidth, array.Container, out string container, 64))
		{
			ContainerName previous = array.Container;
			ContainerName next = container.As<ContainerName>();
			schemaEditor.Execute(new DelegateCommand(
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
		schemaEditor.Execute(new DelegateCommand(
			$"Set Type '{type.DisplayName}'",
			() => member.SetType(type),
			() => member.SetType(previous),
			ChangeType.Modify));
	}

	private void ShowArrayKeySelector(SchemaTypes.Array array, SchemaClass elementClass)
	{
		ImGui.Button(string.IsNullOrEmpty(array.Key) ? SchemaEditor.NoneOption : array.Key, new Vector2(SchemaEditor.FieldWidth, 0));
		ImGuiProbes.MarkItem("KeySelector");

		if (!ImGui.BeginPopupContextItem("##Key", ImGuiPopupFlags.MouseButtonLeft))
		{
			return;
		}

		bool none = ImGui.Selectable(SchemaEditor.NoneOption);
		ImGuiProbes.MarkItem("key-option", SchemaEditor.NoneOption);
		if (none)
		{
			SetArrayKey(array, new MemberName());
		}

		foreach (SchemaMember primitiveMember in elementClass.Members.Where(m => m.Type.IsPrimitive).OrderBy(m => m.Name.ToString(), StringComparer.Ordinal))
		{
			bool chosen = ImGui.Selectable(primitiveMember.Name);
			ImGuiProbes.MarkItem("key-option", primitiveMember.Name);
			if (chosen)
			{
				SetArrayKey(array, primitiveMember.Name);
			}
		}

		ImGui.EndPopup();
	}

	private void SetArrayKey(SchemaTypes.Array array, MemberName key)
	{
		MemberName previous = array.Key;
		schemaEditor.Execute(new DelegateCommand(
			$"Set Array Key '{key}'",
			() => array.Key = key,
			() => array.Key = previous,
			ChangeType.Modify));
	}
}
