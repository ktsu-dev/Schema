// Copyright (c) 2023-2026 ktsu-dev contributors

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.SchemaEditor;

using System.Collections.ObjectModel;
using System.Linq;

using Hexa.NET.ImGui;

using ktsu.ImGui.Styler;
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
	/// Draws the error and warning counts, so the schema's health is visible without opening the
	/// diagnostics tab.
	/// </summary>
	private void ShowValidationSummary()
	{
		if (Diagnostics.Count == 0)
		{
			return;
		}

		int errors = ErrorCount;
		int warnings = WarningCount;

		ImGui.Separator();
		using (Theme.FromColor(errors > 0 ? Palette.Semantic.Error : Palette.Semantic.Warning))
		{
			ImGui.TextUnformatted(FormatSummary(errors, warnings));
		}
	}

	private static string FormatSummary(int errors, int warnings)
	{
		string errorText = $"{errors} error{(errors == 1 ? string.Empty : "s")}";
		string warningText = $"{warnings} warning{(warnings == 1 ? string.Empty : "s")}";
		return $"{errorText}, {warningText}";
	}

	private void ShowDiagnosticsPanel()
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

		ImGui.TextUnformatted(FormatSummary(ErrorCount, WarningCount));
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

		using (Theme.FromColor(isError ? Palette.Semantic.Error : Palette.Semantic.Warning))
		{
			ImGui.TextUnformatted(isError ? "Error" : "Warning");
		}

		ImGui.SameLine();

		// Selectable rather than text so the whole row is a navigation target.
		if (ImGui.Selectable($"{issue.Path}: {issue.Message}##{issue.Path}{issue.Message}"))
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
