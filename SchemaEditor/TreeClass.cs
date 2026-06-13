// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.SchemaEditor;

using System.Diagnostics;

using Hexa.NET.ImGui;

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
					if (ImGui.Selectable($"Delete {x.Name}"))
					{
						SchemaClass captured = x;
						schemaEditor.UndoRedo.Execute(new DelegateCommand(
							$"Delete Class '{captured.Name}'",
							() => captured.TryRemove(),
							() => schema.RestoreClass(captured),
							ChangeType.Delete));
					}
				},
			}, parent: null);
		}
	}

	private void ShowMemberTree(ImGuiWidgets.Tree parent, SchemaClass schemaClass)
	{
		IReadOnlyCollection<SchemaMember> children = schemaClass.Members;

		ImGui.PushID(schemaClass.Name);
		ButtonTree<SchemaMember>.ShowTree(schemaClass.Name, $"{schemaClass.Name} ({children.Count})", children, new()
		{
			GetText = (x) => x.Name,
			GetTooltip = (x) => x.Type.DisplayName,
			GetId = (x) => x.Name,
			OnTreeEnd = (t) =>
			{
				using (t.Child)
				{
					ShowNewMember(schemaClass);
				}
			},
			OnItemContextMenu = (x) =>
			{
				if (ImGui.Selectable($"Delete {x.Name}"))
				{
					SchemaMember captured = x;
					schemaEditor.UndoRedo.Execute(new DelegateCommand(
						$"Delete Member '{captured.Name}'",
						() => captured.TryRemove(),
						() => schemaClass.RestoreMember(captured),
						ChangeType.Delete));
				}
			},
		}, parent);
		ImGui.PopID();
	}

	private void ShowNewClass(Schema schema)
	{
		using (Button.Alignment.Left())
		{
			if (ImGui.Button("+ New Class"))
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
					schemaEditor.UndoRedo.Execute(new DelegateCommand(
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
			if (ImGui.Button("+ New Member"))
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
					schemaEditor.UndoRedo.Execute(new DelegateCommand(
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
						Popups.OpenTypeList("Select Type", "Type", addedMember.ParentSchema.GetTypes(), addedMember.Type, addedMember.SetType);
					}
				});
			}
		}
	}
}
