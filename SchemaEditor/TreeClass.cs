// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

using System.Diagnostics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Styler;
using ktsu.ImGui.Widgets;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;
using ktsu.UndoRedo;

internal sealed class TreeClass(SchemaEditor schemaEditor)
{
	private Popups Popups => schemaEditor.Popups;

	internal void Show()
	{
		Schema? schema = schemaEditor.CurrentSchema;
		if (schema is not null)
		{
			IReadOnlyCollection<SchemaClass> children = schema.Classes;

			string name = "Classes";
			ButtonTree<SchemaClass>.ShowTree(name, $"{name} ({children.Count})", children, new()
			{
				Collapsible = true,
				GetText = (x) => $"{x.Name} ({x.Members.Count})",
				GetTooltip = (x) => x.Description,
				GetIssue = schemaEditor.GetIssueFor,
				GetId = (x) => x.Name,
				OnTreeEnd = (t) =>
				{
					using (t.Child)
					{
						ShowNewClass(schema);
					}
				},
				OnItemClick = schemaEditor.EditClass,
				OnItemEnd = ShowMemberTree,
				OnItemContextMenu = (x) =>
				{
					SchemaClass captured = x;

					bool renameChosen = ImGui.Selectable($"Rename {captured.Name}");
					ImGuiProbes.MarkItem($"Rename{captured.Name}");
					if (renameChosen)
					{
						schemaEditor.PromptRename("class", captured.Name,
							newName => schema.TryRenameClass(captured, newName.As<ClassName>()));
					}

					bool deleteChosen = ImGui.Selectable($"Delete {captured.Name}");
					ImGuiProbes.MarkItem($"Delete{captured.Name}");
					if (deleteChosen)
					{
						// Restored where it was rather than appended; see DeleteMember in the panel.
						int index = schema.ClassSet.IndexOf(captured);
						schemaEditor.Execute(new DelegateCommand(
							$"Delete Class '{captured.Name}'",
							() => captured.TryRemove(),
							() =>
							{
								schema.RestoreClass(captured);
								schema.ClassSet.Move(captured, index);
							},
							ChangeType.Delete));
					}
				},
			}, parent: null);
		}
	}

	private void ShowMemberTree(ImGuiWidgets.Tree parent, SchemaClass schemaClass)
	{
		SchemaChildSet<SchemaMember, MemberName> children = schemaClass.Members;

		ImGui.PushID(schemaClass.Name);
		ImGuiProbes.PushScope(schemaClass.Name);
		ButtonTree<SchemaMember>.ShowTree(schemaClass.Name, $"{schemaClass.Name} ({children.Count})", children, new()
		{
			GetText = (x) => x.Name,
			GetTooltip = (x) => string.IsNullOrEmpty(x.Description)
				? x.Type.DisplayName
				: $"{x.Type.DisplayName}\n\n{x.Description}",
			GetIssue = schemaEditor.GetIssueFor,
			GetId = (x) => x.Name,
			OnItemClick = (x) => schemaEditor.EditClass(schemaClass),
			OnTreeEnd = (t) =>
			{
				using (t.Child)
				{
					ShowNewMember(schemaClass);
				}
			},
			OnItemContextMenu = (x) =>
			{
				SchemaMember captured = x;

				bool renameChosen = ImGui.Selectable($"Rename {captured.Name}");
				ImGuiProbes.MarkItem($"Rename{captured.Name}");
				if (renameChosen)
				{
					schemaEditor.PromptRename("member", captured.Name,
						newName => schemaClass.TryRenameMember(captured, newName.As<MemberName>()));
				}

				bool deleteChosen = ImGui.Selectable($"Delete {captured.Name}");
				ImGuiProbes.MarkItem($"Delete{captured.Name}");
				if (deleteChosen)
				{
					// Restored where it was rather than appended; see DeleteMember in the panel.
					int index = schemaClass.IndexOfMember(captured);
					schemaEditor.Execute(new DelegateCommand(
						$"Delete Member '{captured.Name}'",
						() => captured.TryRemove(),
						() =>
						{
							schemaClass.RestoreMember(captured);
							schemaClass.TryMoveMember(captured, index);
						},
						ChangeType.Delete));
				}
			},
		}, parent);
		ImGuiProbes.PopScope();
		ImGui.PopID();
	}

	private void ShowNewClass(Schema schema)
	{
		using (Button.Alignment.Left())
		{
			bool clicked = ImGui.Button("+ New Class");
			ImGuiProbes.MarkItem("NewClass");
			if (clicked)
			{
				Popups.OpenInputString("Input", "New Class Name", string.Empty, (newName) =>
				{
					ClassName className = newName.As<ClassName>();
					if (schema.GetClass(className) is not null)
					{
						Popups.OpenMessageOK("Error", $"A Class with that name ({newName}) already exists.");
						return;
					}

					SchemaClass? addedClass = null;
					schemaEditor.Execute(new DelegateCommand(
						$"Add Class '{className}'",
						() =>
						{
							if (addedClass is null)
							{
								addedClass = schema.AddClass(className);
							}
							else
							{
								schema.RestoreClass(addedClass);
							}

							schemaEditor.EditClass(className);
						},
						() => addedClass?.TryRemove(),
						ChangeType.Insert));
				});
			}
		}
	}

	private void ShowNewMember(SchemaClass schemaClass)
	{
		using (Button.Alignment.Left())
		{
			bool clicked = ImGui.Button("+ New Member");
			ImGuiProbes.MarkItem("NewMember");
			if (clicked)
			{
				Popups.OpenInputString("Input", "New Member Name", string.Empty, (newName) =>
				{
					MemberName memberName = newName.As<MemberName>();
					if (schemaClass.GetMember(memberName) is not null)
					{
						Popups.OpenMessageOK("Error", $"A Member with that name ({newName}) already exists.");
						return;
					}

					SchemaMember? addedMember = null;
					schemaEditor.Execute(new DelegateCommand(
						$"Add Member '{memberName}'",
						() =>
						{
							if (addedMember is null)
							{
								addedMember = schemaClass.AddMember(memberName);
							}
							else
							{
								schemaClass.RestoreMember(addedMember);
							}
						},
						() => addedMember?.TryRemove(),
						ChangeType.Insert));

					if (addedMember is not null)
					{
						Debug.Assert(addedMember.ParentSchema is not null);
						Popups.OpenTypeList("Select Type", "Type", addedMember.ParentSchema.GetAvailableTypes(), addedMember.Type, addedMember.SetType);
					}
				});
			}
		}
	}
}
