namespace ktsu.SchemaEditor;

using System.Diagnostics;

using ImGuiNET;

using ktsu.ImGuiStyler;
using ktsu.ImGuiWidgets;
using ktsu.Schema;

internal class TreeClass(SchemaEditor schemaEditor)
{
	private Popups Popups => schemaEditor.Popups;

	internal void Show()
	{
		var schema = schemaEditor.CurrentSchema;
		if (schema is not null)
		{
			var children = schema.Classes;

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
						x.TryRemove();
					}
				},
			}, parent: null);
		}
	}

	private void ShowMemberTree(ImGuiWidgets.Tree parent, SchemaClass schemaClass)
	{
		var children = schemaClass.Members;

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
					x.TryRemove();
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
					var className = (ClassName)newName;
					if (schema.TryAddClass(className))
					{
						schemaEditor.EditClass(className);
					}
					else
					{
						Popups.OpenMessageOK("Error", $"A Class with that name ({newName}) already exists.");
					}
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
					var schemaMember = schemaClass.AddMember((MemberName)newName);
					if (schemaMember is not null)
					{
						Debug.Assert(schemaMember.ParentSchema is not null);
						Popups.OpenTypeList("Select Type", "Type", schemaMember.ParentSchema.GetTypes(), schemaMember.Type, schemaMember.SetType);
					}
					else
					{
						Popups.OpenMessageOK("Error", $"A Member with that name ({newName}) already exists.");
					}
				});
			}
		}
	}
}
