namespace ktsu.SchemaEditor;

using System;

using ImGuiNET;

using ktsu.Extensions;
using ktsu.ImGuiStyler;
using ktsu.ImGuiWidgets;

internal class ButtonTree { }
internal class ButtonTree<TItem> : ButtonTree
{
	internal class Config
	{
		public bool Collapsible { get; set; }
		public Action<ImGuiWidgets.Tree>? OnTreeStart { get; set; }
		public Action<ImGuiWidgets.Tree, TItem>? OnItemStart { get; set; }
		public Func<TItem, string>? GetText { get; set; }
		public Func<TItem, string>? GetTooltip { get; set; }
		public Func<TItem, string>? GetId { get; set; }
		public Action<TItem>? OnItemClick { get; set; }
		public Action<TItem>? OnItemDoubleClick { get; set; }
		public Action<TItem>? OnItemContextMenu { get; set; }
		public Action<ImGuiWidgets.Tree, TItem>? OnItemEnd { get; set; }
		public Action<ImGuiWidgets.Tree>? OnTreeEnd { get; set; }
	}

	internal static void ShowTree(string id, string text, IEnumerable<TItem> items) => ShowTree(id, text, items, new(), null);
	internal static void ShowTree(string id, string text, IEnumerable<TItem> items, Config config, ImGuiWidgets.Tree? parent)
	{
		bool isRoot = parent is null;
		bool treeIsOpen = !isRoot || SchemaEditor.IsVisible(id);

		if (isRoot)
		{
			using (Button.Alignment.Left())
			{
				ImGui.Button(text, new(SchemaEditor.FieldWidth, 0));
			}

			ImGui.SameLine();
			if (ImGui.ArrowButton($"##Arrow{id}", treeIsOpen ? ImGuiDir.Down : ImGuiDir.Up))
			{
				SchemaEditor.ToggleVisibility(id);
			}
		}

		if (treeIsOpen)
		{
			using (var tree = new ImGuiWidgets.Tree())
			{
				config.OnTreeStart?.Invoke(tree);

				foreach (var item in items.ToCollection())
				{
					ShowTreeItem(id, config, tree, item);
				}

				config?.OnTreeEnd?.Invoke(tree);

				ImGui.NewLine();
			}
		}
	}

	private static void ShowTreeItem(string id, Config config, ImGuiWidgets.Tree tree, TItem? item)
	{
		if (item is not null)
		{
			string buttonText = config.GetText?.Invoke(item) ?? item.ToString() ?? string.Empty;
			string itemId = config.GetId?.Invoke(item) ?? $"{id}.{buttonText}";
			bool itemIsOpen = !config.Collapsible || SchemaEditor.IsVisible(itemId);
			using (tree.Child)
			{
				config.OnItemStart?.Invoke(tree, item);

				using (Button.Alignment.Left())
				{
					ImGui.Button($"{buttonText}##Btn{itemId}", new(SchemaEditor.FieldWidth, 0));
					if (ImGui.IsItemClicked())
					{
						if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
						{
							if (config.OnItemDoubleClick is not null)
							{
								config.OnItemDoubleClick(item);
							}
						}
						else
						{
							config.OnItemClick?.Invoke(item);
						}
					}

					if (config.OnItemContextMenu is not null)
					{
						if (ImGui.BeginPopupContextItem())
						{
							config.OnItemContextMenu(item);
							ImGui.EndPopup();
						}
					}

					if (config.GetTooltip is not null)
					{
						if (ImGui.IsItemHovered())
						{
							ImGui.SetTooltip(config.GetTooltip(item));
						}
					}
				}

				if (config.Collapsible)
				{
					ImGui.SameLine();
					if (ImGui.ArrowButton($"##Arrow{itemId}", itemIsOpen ? ImGuiDir.Down : ImGuiDir.Up))
					{
						SchemaEditor.ToggleVisibility(itemId);
					}
				}
			}

			if (itemIsOpen)
			{
				config.OnItemEnd?.Invoke(tree, item);
			}
		}
	}
}
