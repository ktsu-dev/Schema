// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

using System;

using Hexa.NET.ImGui;

using ktsu.Extensions;
using ktsu.ImGui.Styler;
using ktsu.ImGui.Widgets;
using ktsu.Schema.Models;

internal class ButtonTree { }
internal sealed class ButtonTree<TItem> : ButtonTree
{
	internal sealed class Config
	{
		public bool Collapsible { get; set; }
		public Action<ImGuiWidgets.Tree>? OnTreeStart { get; set; }
		public Action<ImGuiWidgets.Tree, TItem>? OnItemStart { get; set; }
		public Func<TItem, string>? GetText { get; set; }
		public Func<TItem, string>? GetTooltip { get; set; }

		/// <summary>
		/// Gets or sets an accessor for the validation issue an item owns, if any.
		/// </summary>
		/// <remarks>
		/// An item with an issue is tinted by severity and reports the issue's message in its
		/// tooltip, so a broken reference is visible in the tree without opening the diagnostics
		/// tab.
		/// </remarks>
		public Func<TItem, SchemaValidationIssue?>? GetIssue { get; set; }
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
			using (ImGuiWidgets.Tree tree = new())
			{
				config.OnTreeStart?.Invoke(tree);

				foreach (TItem? item in items.ToCollection())
				{
					ShowTreeItem(id, config, tree, item);
				}

				config?.OnTreeEnd?.Invoke(tree);

				ImGui.NewLine();
			}
		}
	}

	/// <summary>
	/// Shows the item's tooltip, combining its own description with any validation issue it owns.
	/// </summary>
	private static void ShowItemTooltip(Config config, TItem item, SchemaValidationIssue? issue)
	{
		if (!ImGui.IsItemHovered())
		{
			return;
		}

		string description = config.GetTooltip?.Invoke(item) ?? string.Empty;
		string[] lines = [.. new[] { description, issue?.Message ?? string.Empty }.Where(l => !string.IsNullOrEmpty(l))];

		if (lines.Length > 0)
		{
			ImGui.SetTooltip(string.Join("\n\n", lines));
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

				SchemaValidationIssue? issue = config.GetIssue?.Invoke(item);

				using (Button.Alignment.Left())
				using (issue is null
					? null
					: Theme.FromColor(issue.Severity == SchemaValidationSeverity.Error
						? Palette.Semantic.Error
						: Palette.Semantic.Warning))
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

					ShowItemTooltip(config, item, issue);
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
