// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;
using ktsu.UndoRedo;

/// <summary>
/// The property panel for a code generator, and the action that runs it.
/// </summary>
/// <remarks>
/// Until this existed a code generator could be created and deleted but never configured, so its
/// output path could not be set and every code generator in a saved schema tripped the "does not
/// specify an output path" validation warning.
/// </remarks>
internal sealed class CodeGeneratorPanel(SchemaEditor schemaEditor)
{
	private Popups Popups => schemaEditor.Popups;

	internal void Show(Schema schema, SchemaCodeGenerator codeGenerator)
	{
		if (!ImGui.CollapsingHeader($"{codeGenerator.Name} Properties", ImGuiTreeNodeFlags.DefaultOpen))
		{
			return;
		}

		schemaEditor.ShowRenameField(
			$"##CodeGeneratorName{codeGenerator.Name}",
			codeGenerator.Name,
			newName => schema.TryRenameCodeGenerator(codeGenerator, newName.As<CodeGeneratorName>()),
			"code generator");

		ImGui.TextUnformatted("Language:");
		ImGui.SameLine();
		ShowLanguageSelector(codeGenerator);

		ImGui.TextUnformatted("Namespace:");
		ImGui.SameLine();
		if (EditField.Text($"##CodeGeneratorNamespace{codeGenerator.Name}", SchemaEditor.FieldWidth * 2, codeGenerator.Namespace, out string codeNamespace))
		{
			CodeNamespace previous = codeGenerator.Namespace;
			CodeNamespace next = codeNamespace.As<CodeNamespace>();
			schemaEditor.Execute(new DelegateCommand(
				$"Set Namespace '{next}'",
				() => codeGenerator.Namespace = next,
				() => codeGenerator.Namespace = previous,
				ChangeType.Modify));
		}

		ImGui.TextUnformatted("Output Path:");
		ImGui.SameLine();
		if (EditField.Text($"##CodeGeneratorOutput{codeGenerator.Name}", SchemaEditor.FieldWidth * 2, codeGenerator.OutputPath, out string outputPath))
		{
			SetOutputPath(codeGenerator, outputPath.As<RelativeDirectoryPath>());
		}

		ImGui.SameLine();
		if (ImGui.Button($"Browse...##CodeGeneratorBrowse{codeGenerator.Name}"))
		{
			SchemaCodeGenerator captured = codeGenerator;
			Popups.OpenBrowserDirectory("Choose Output Directory", (directory) =>
				SetOutputPath(captured, ((string)directory).As<RelativeDirectoryPath>()));
		}

		schemaEditor.ShowDescriptionEditor(
			$"##CodeGeneratorDescription{codeGenerator.Name}",
			"Description:",
			codeGenerator.Description,
			value => codeGenerator.Description = value);

		ImGui.Separator();
		ShowGenerateButton(schema, codeGenerator);
	}

	private void ShowLanguageSelector(SchemaCodeGenerator codeGenerator)
	{
		string label = string.IsNullOrEmpty(codeGenerator.Language) ? "<Select Language>" : codeGenerator.Language;
		ImGui.Button($"{label}##CodeGeneratorLanguage{codeGenerator.Name}", new Vector2(SchemaEditor.FieldWidth, 0));

		if (!ImGui.BeginPopupContextItem($"##CodeGeneratorLanguageSelect{codeGenerator.Name}", ImGuiPopupFlags.MouseButtonLeft))
		{
			return;
		}

		foreach (string language in SchemaGenerator.SupportedLanguages)
		{
			if (ImGui.Selectable(language))
			{
				LanguageName previous = codeGenerator.Language;
				LanguageName next = language.As<LanguageName>();
				schemaEditor.Execute(new DelegateCommand(
					$"Set Language '{next}'",
					() => codeGenerator.Language = next,
					() => codeGenerator.Language = previous,
					ChangeType.Modify));
			}
		}

		ImGui.EndPopup();
	}

	private void SetOutputPath(SchemaCodeGenerator codeGenerator, RelativeDirectoryPath path)
	{
		RelativeDirectoryPath previous = codeGenerator.OutputPath;
		schemaEditor.Execute(new DelegateCommand(
			$"Set Output Path '{path}'",
			() => codeGenerator.OutputPath = path,
			() => codeGenerator.OutputPath = previous,
			ChangeType.Modify));
	}

	/// <summary>
	/// Runs one code generator and reports what happened.
	/// </summary>
	/// <remarks>
	/// Output paths are relative to the schema file, so generating needs a saved schema. The panel
	/// says why rather than offering a button that would fail.
	/// </remarks>
	private void ShowGenerateButton(Schema schema, SchemaCodeGenerator codeGenerator)
	{
		if (!schema.CanResolvePaths)
		{
			using (EditorTheme.Warning())
			{
				ImGui.TextUnformatted("Save the schema before generating: output paths are relative to it.");
			}

			return;
		}

		if (!ImGui.Button($"Generate##CodeGeneratorGenerate{codeGenerator.Name}"))
		{
			return;
		}

		SchemaGenerationResult result = SchemaGenerator.GenerateToDisk(schema, codeGenerator);

		if (result.IsSuccess)
		{
			Popups.OpenMessageOK("Generated", result.Message);
			return;
		}

		// A refusal because the schema has errors is worth pointing at the diagnostics tab, since
		// that is where the user can act on them.
		string detail = result.Status == SchemaGenerationStatus.SchemaInvalid
			? $"{result.Message}\n\nSee the Diagnostics tab for all {result.Issues.Count} of them."
			: result.Message;

		Popups.OpenMessageOK("Cannot Generate", detail);
	}
}
