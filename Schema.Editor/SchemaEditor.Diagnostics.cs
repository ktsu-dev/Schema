// Copyright (c) 2023-2026 ktsu-dev contributors

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.Schema.Editor;

using System.Collections.ObjectModel;
using System.Linq;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.Schema.Models;

/// <summary>
/// Running <see cref="Schema.Validate"/> as the schema changes, and showing what it reports.
/// </summary>
public partial class SchemaEditor
{
	/// <summary>
	/// How long the schema must sit unchanged before it is revalidated.
	/// </summary>
	/// <remarks>
	/// Validation walks every class, member and type, so running it per frame would burn the
	/// frame budget on a schema that has not changed. Debouncing also means a burst of edits -
	/// deleting a class, then its data source - validates once rather than once per edit.
	/// </remarks>
	internal const float ValidationDebounceSeconds = 0.35f;

	private bool validationPending;
	private float timeSinceValidationRequested;

	/// <summary>
	/// Gets the issues from the most recent validation run.
	/// </summary>
	internal Collection<SchemaValidationIssue> Diagnostics { get; private set; } = [];

	private int ErrorCount => Diagnostics.Count(i => i.Severity == SchemaValidationSeverity.Error);

	private int WarningCount => Diagnostics.Count(i => i.Severity == SchemaValidationSeverity.Warning);

	/// <summary>
	/// Notes that the schema changed and should be revalidated once it settles.
	/// </summary>
	internal void RequestValidation()
	{
		validationPending = true;
		timeSinceValidationRequested = 0f;
	}

	internal void UpdateValidation(float dt)
	{
		if (!validationPending)
		{
			return;
		}

		timeSinceValidationRequested += dt;
		if (timeSinceValidationRequested < ValidationDebounceSeconds)
		{
			return;
		}

		validationPending = false;
		timeSinceValidationRequested = 0f;
		Diagnostics = CurrentSchema?.Validate() ?? [];
	}

	/// <summary>
	/// Gets the diagnostics tab's label, which carries the issue counts.
	/// </summary>
	internal string DiagnosticsTabLabel => DiagnosticsTab.LabelOf(MainTabs, diagnosticsTabId);

	/// <summary>
	/// Draws the issue list behind the diagnostics tab.
	/// </summary>
	/// <remarks>
	/// Internal so a test can draw it directly. The tab bar hosting it comes from a widget library
	/// that neither records its tabs for a probe nor takes a selection from outside, so there is no
	/// tab for a test to click; drawing the panel is what the tab delegate does either way.
	/// </remarks>
	internal void ShowDiagnosticsPanel()
	{
		if (CurrentSchema is null)
		{
			ImGui.TextDisabled("No schema is open.");
			return;
		}

		if (Diagnostics.Count == 0)
		{
			ImGui.TextUnformatted("No issues found.");
			return;
		}

		using (EditorTheme.SeverityText(ErrorCount > 0 ? SchemaValidationSeverity.Error : SchemaValidationSeverity.Warning))
		{
			ImGui.TextUnformatted(DiagnosticsTab.FormatSummary(ErrorCount, WarningCount));
		}

		ImGui.Separator();

		// Errors first: they are what stops the schema being usable.
		foreach (SchemaValidationIssue issue in Diagnostics.OrderByDescending(i => i.Severity).ThenBy(i => i.Path, StringComparer.Ordinal))
		{
			ShowDiagnostic(issue);
		}
	}

	private void ShowDiagnostic(SchemaValidationIssue issue)
	{
		bool isError = issue.Severity == SchemaValidationSeverity.Error;

		using (EditorTheme.Severity(issue.Severity))
		{
			ImGui.TextUnformatted(isError ? "Error" : "Warning");
		}

		ImGui.SameLine();

		// Selectable rather than text so the whole row is a navigation target.
		bool clicked = ImGui.Selectable($"{issue.Path}: {issue.Message}##{issue.Path}{issue.Message}");

		// Severity and path rather than the whole row, so a test names the issue it means without
		// repeating the wording of the message - and so that an element with both an error and a
		// warning against it does not record two rows under one name.
		ImGuiProbes.MarkItem("diagnostic", $"{issue.Severity}:{issue.Path}");

		if (clicked)
		{
			NavigateTo(issue);
		}

		if (ImGui.IsItemHovered() && issue.Element is not null)
		{
			ImGui.SetTooltip("Click to select the element this refers to.");
		}
	}

	/// <summary>
	/// Selects the element an issue was reported against.
	/// </summary>
	/// <remarks>
	/// Uses the reference the issue carries rather than parsing its dotted path, which cannot be
	/// unambiguously split when a name contains a dot. A member selects its owning class, because
	/// that is the panel its row is drawn in.
	/// </remarks>
	internal void NavigateTo(SchemaValidationIssue issue)
	{
		switch (issue.Element)
		{
			case SchemaClass schemaClass:
				EditClass(schemaClass);
				break;

			case SchemaMember member when member.ParentClass is not null:
				EditClass(member.ParentClass);
				break;

			case SchemaEnum schemaEnum:
				EditEnum(schemaEnum);
				break;

			case DataSource dataSource:
				EditDataSource(dataSource);
				break;

			case SchemaCodeGenerator codeGenerator:
				EditCodeGenerator(codeGenerator);
				break;

			default:
				// A duplicate-name issue names no single element; there is nothing to select.
				break;
		}
	}

	/// <summary>
	/// Gets the most severe issue affecting an element, if any, for inline marking.
	/// </summary>
	/// <param name="element">The element to look up.</param>
	/// <returns>The issue to mark the element with, or null if it has none.</returns>
	internal SchemaValidationIssue? GetIssueFor(ISchemaElement? element) =>
		element is null
			? null
			: Diagnostics
				.Where(i => ReferenceEquals(i.Element, element))
				.OrderByDescending(i => i.Severity)
				.FirstOrDefault();
}
