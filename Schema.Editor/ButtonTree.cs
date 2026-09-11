// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor;

using System;

using Hexa.NET.ImGui;

using ktsu.Extensions;
using ktsu.ImGui.Probes;
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

	/// <summary>
	/// The width to draw a tree row at.
	/// </summary>
	/// <remarks>
	/// The rows share a width so the tree reads as a column rather than a ragged edge, but that
	/// width has to be a minimum rather than a fixed size: ImGui clips a button's label to its
	/// frame, so "Code Generators (0)" - the longest label in the tree - lost its count entirely
	/// at the sizes the editor actually runs at.
	/// </remarks>
	/// <param name="text">The label that has to fit.</param>
	/// <returns>The column width, or the width the text needs when that is more.</returns>
	private static float RowWidth(string text) =>
		MathF.Max(SchemaEditor.FieldWidth, ImGui.CalcTextSize(text).X + (ImGui.GetStyle().FramePadding.X * 2));

	/// <summary>
	/// Draws a tree of rows, one button each.
	/// </summary>
	/// <remarks>
	/// Takes the editor rather than reaching for <see cref="SchemaEditor.Instance"/>, because which
	/// rows are folded open is part of that editor's settings.
	/// </remarks>
	/// <param name="editor">The editor the tree belongs to.</param>
	/// <param name="id">A stable id for the tree, which is also what its folded state is keyed by.</param>
	/// <param name="text">The tree's heading.</param>
	/// <param name="items">The rows to draw.</param>
	/// <param name="config">What each row does.</param>
	/// <param name="parent">The tree this one hangs under, or null when it is a root.</param>
	internal static void ShowTree(SchemaEditor editor, string id, string text, IEnumerable<TItem> items, Config config, ImGuiWidgets.Tree? parent)
	{
		bool isRoot = parent is null;
		bool treeIsOpen = !isRoot || editor.IsVisible(id);

		if (isRoot)
		{
			using (Button.Alignment.Left())
			{
				ImGui.Button(text, new(RowWidth(text), 0));
				ImGuiProbes.MarkItem($"Root{id}");
			}

			ImGui.SameLine();
			if (ImGui.ArrowButton($"##Arrow{id}", treeIsOpen ? ImGuiDir.Down : ImGuiDir.Up))
			{
				editor.ToggleVisibility(id);
			}
		}

		if (treeIsOpen)
		{
			using (ImGuiWidgets.Tree tree = new())
			{
				config.OnTreeStart?.Invoke(tree);

				foreach (TItem? item in items.ToCollection())
				{
					ShowTreeItem(editor, id, config, tree, item);
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

	private static void ShowTreeItem(SchemaEditor editor, string id, Config config, ImGuiWidgets.Tree tree, TItem? item)
	{
		if (item is not null)
		{
			string buttonText = config.GetText?.Invoke(item) ?? item.ToString() ?? string.Empty;
			string itemId = config.GetId?.Invoke(item) ?? $"{id}.{buttonText}";
			bool itemIsOpen = !config.Collapsible || editor.IsVisible(itemId);
			using (tree.Child)
			{
				config.OnItemStart?.Invoke(tree, item);

				SchemaValidationIssue? issue = config.GetIssue?.Invoke(item);

				using (Button.Alignment.Left())
				using (issue is null ? null : EditorTheme.Severity(issue.Severity))
				{
					ImGui.Button($"{buttonText}##Btn{itemId}", new(RowWidth(buttonText), 0));

					// Every tree row in the editor is drawn here, so marking it here is what lets a
					// test address any of them - a class, a member, an enum value, a data source -
					// by name rather than by pixel position. Marking costs nothing when no probe is
					// installed, which is every run that is not a test.
					ImGuiProbes.MarkItem($"Btn{itemId}");

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
						editor.ToggleVisibility(itemId);
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
