// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.SchemaEditor;

using System;
using System.Diagnostics;
using System.Numerics;

using ImGuiNET;

using ktsu.Extensions;
using ktsu.ImGuiApp;
using ktsu.ImGuiStyler;
using ktsu.ImGuiWidgets;
using ktsu.Schema;
using ktsu.StrongPaths;

public class SchemaEditor
{
	public static SchemaEditor Instance { get; } = new();
	internal Schema? CurrentSchema { get; set; }
	internal SchemaClass? CurrentClass { get; set; }
	internal DataSource? CurrentDataSource { get; set; }
	internal AppData Options { get; } = new();
	internal static float FieldWidth => ImGui.GetIO().DisplaySize.X * 0.15f;
	private DateTime LastSaveOptionsTime { get; set; } = DateTime.MinValue;
	private DateTime SaveOptionsQueuedTime { get; set; } = DateTime.MinValue;
	private TimeSpan SaveOptionsDebounceTime { get; } = TimeSpan.FromSeconds(3);
	private ImGuiWidgets.DividerContainer DividerContainerCols { get; init; }

	private ImGuiAppWindowState InitialWindowState { get; init; }
	internal Popups Popups { get; } = new();
	private TreeSchema TreeSchema { get; init; }

	private static void Main(string[] _)
	{
		var title = nameof(SchemaEditor);
		if (Instance.CurrentSchema is not null)
		{
			title += $" - {Instance.CurrentSchema.FilePath.FileName}";
		}

		ImGuiApp.Start(title, Instance.InitialWindowState, Instance.OnStart, Instance.OnTick, Instance.OnMenu, Instance.OnWindowResized);
	}

	public SchemaEditor()
	{
		TreeSchema = new(this);
		DividerContainerCols =
			new(
				"RootDivider",
				DividerResized,
				ImGuiWidgets.DividerLayout.Columns,
				[
					new("Left", 0.25f, ShowLeftPanel),
					new("Right", 0.75f, ShowRightPanel),
				]
			);

		Options = AppData.LoadOrCreate();
		Popups = Options.Popups;
		InitialWindowState = Options.WindowState;

		// restore open schema
		if (Schema.TryLoad(Options.CurrentSchemaPath, out var previouslyOpenSchema) && previouslyOpenSchema is not null)
		{
			CurrentSchema = previouslyOpenSchema;
			CurrentClass = null;
			CurrentClass = CurrentSchema.GetClass(Options.CurrentClassName);
		}

		// restore divider states
		if (Options.DividerStates.TryGetValue(DividerContainerCols.Id, out var sizes))
		{
			DividerContainerCols.SetSizesFromList(sizes);
		}
	}

	private void OnStart()
	{
	}

	private void OnWindowResized() => QueueSaveOptions();

	private void DividerResized(ImGuiWidgets.DividerContainer container)
	{
		Options.DividerStates[container.Id] = container.GetSizes();
		QueueSaveOptions();
	}

	//Dont call this directly, call QueueSaveOptions instead so that we can debounce the saves and avoid saving multiple times per frame or multiple frames in a row
	private void SaveOptionsInternal()
	{
		Options.CurrentSchemaPath = CurrentSchema?.FilePath ?? new();
		Options.CurrentClassName = CurrentClass?.Name ?? new();
		Options.DividerStates[DividerContainerCols.Id] = DividerContainerCols.GetSizes();
		Options.WindowState = ImGuiApp.WindowState;
		Options.Popups = Popups;
		Options.Save();
	}

	private void QueueSaveOptions() => SaveOptionsQueuedTime = DateTime.Now;

	private void SaveOptionsIfRequired()
	{
		//debounce the save requests and avoid saving multiple times per frame or multiple frames in a row
		if ((SaveOptionsQueuedTime > LastSaveOptionsTime) && ((DateTime.Now - SaveOptionsQueuedTime) > SaveOptionsDebounceTime))
		{
			SaveOptionsInternal();
			LastSaveOptionsTime = DateTime.Now;
		}
	}

	private void OnTick(float dt)
	{
		using (Theme.Color(Theme.Palette.Normal))
		{
			DividerContainerCols.Tick(dt);

			SaveOptionsIfRequired();

			Popups.Update();
		}
	}

	private void ShowLeftPanel(float dt) => TreeSchema.Show();

	private void ShowRightPanel(float dt)
	{
		ShowSchemaConfig();
		ShowMembers();
	}

	private void Reset()
	{
		CurrentSchema = null;
		CurrentClass = null;
	}

	private void OnMenu()
	{
		if (ImGui.BeginMenu("File"))
		{
			if (ImGui.MenuItem("New"))
			{
				New();
			}

			if (ImGui.MenuItem("Open"))
			{
				Open();
			}

			if (ImGui.MenuItem("Save"))
			{
				Save();
			}

			ImGui.Separator();

			var schemaFilePath = CurrentSchema?.FilePath ?? "";
			if (ImGui.MenuItem("Open Externally", !string.IsNullOrEmpty(schemaFilePath)))
			{
				using var p = new Process();
				p.StartInfo.FileName = $"explorer.exe";
				p.StartInfo.Arguments = schemaFilePath;
				p.Start();
			}

			ImGui.EndMenu();
		}
	}

	private void New()
	{
		Reset();
		CurrentSchema = new Schema();
		QueueSaveOptions();
	}

	private void Open()
	{
		Popups.OpenBrowserFileOpen("Open Schema", (filePath) =>
		{
			Reset();
			if (Schema.TryLoad(filePath, out var schema) && schema is not null)
			{
				CurrentSchema = schema;
				CurrentClass = CurrentSchema?.FirstClass;
				QueueSaveOptions();
			}
			else
			{
				Popups.OpenMessageOK("Error", "Failed to load schema.");
			}
		}, "*.schema.json");
	}

	private void Save()
	{
		if (string.IsNullOrEmpty(CurrentSchema?.FilePath ?? new()))
		{
			SaveAs();
			return;
		}

		CurrentSchema?.Save();
	}

	private void SaveAs()
	{
		Popups.OpenBrowserFileSave("Save Schema", (filePath) =>
		{
			CurrentSchema?.ChangeFilePath(filePath);
			Save();
			QueueSaveOptions();
		}, "*.schema.json");
	}

	internal static bool ToggleVisibility(string key)
	{
		Instance.QueueSaveOptions();
		if (Instance.Options.HiddenItems.Remove(key))
		{
			return false;
		}

		Instance.Options.HiddenItems.Add(key);
		return true;
	}

	internal static bool IsVisible(string key) => !Instance.Options.HiddenItems.Contains(key);

	public void ShowMemberConfig(Schema schema, SchemaMember schemaMember)
	{
		ArgumentNullException.ThrowIfNull(schema);
		ArgumentNullException.ThrowIfNull(schemaMember);

		if (ImGui.Button($"{schemaMember.Type.DisplayName}##Type{schemaMember.Name}", new Vector2(FieldWidth, 0)))
		{
			Popups.OpenTypeList("Select Type", "Type", schema.GetTypes(), schemaMember.Type, schemaMember.SetType);
		}

		if (schemaMember.Type is SchemaTypes.Array array)
		{
			ImGui.SameLine();
			ImGui.SetNextItemWidth(FieldWidth);
			string container = array.Container;
			ImGui.InputText($"##Container{schemaMember.Name}", ref container, 64);
			array.Container = (ContainerName)container;

			if (array.ElementType is SchemaTypes.Object obj && obj.Class is not null)
			{
				ImGui.SameLine();
				ImGui.Button(array.Key, new Vector2(FieldWidth, 0));
				if (ImGui.BeginPopupContextItem($"##{schemaMember.Name}Key", ImGuiPopupFlags.MouseButtonLeft))
				{
					if (ImGui.Selectable("<none>"))
					{
						array.Key = new();
					}

					foreach (var primitiveMember in obj.Class.Members.Where(m => m.Type.IsPrimitive).OrderBy(m => m.Name))
					{
						if (ImGui.Selectable(primitiveMember.Name))
						{
							array.Key = primitiveMember.Name;
						}
					}

					ImGui.EndPopup();
				}
			}
		}
	}

	public static void ShowMemberHeadings()
	{
		ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 1.0f));
		ImGui.Button("Name", new Vector2(FieldWidth, 0));
		ImGui.SameLine();
		ImGui.Button("Type", new Vector2(FieldWidth, 0));
		ImGui.SameLine();
		ImGui.Button("Container", new Vector2(FieldWidth, 0));
		ImGui.SameLine();
		ImGui.Button("Key", new Vector2(FieldWidth, 0));
		ImGui.PopStyleColor();
	}

	private void ShowMembers()
	{
		if (CurrentClass is not null)
		{
			if (ImGui.CollapsingHeader($"{CurrentClass.Name} Members", ImGuiTreeNodeFlags.DefaultOpen))
			{
				var frameHeight = ImGui.GetFrameHeight();
				var spacing = ImGui.GetStyle().ItemSpacing.X;
				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + frameHeight + spacing);

				ShowMemberHeadings();

				foreach (var schemaMember in CurrentClass.Members.ToCollection())
				{
					string name = schemaMember.Name;
					if (ImGui.Button($"X##deleteMember{name}", new Vector2(frameHeight, 0)))
					{
						schemaMember.TryRemove();
					}

					ImGui.SameLine();
					ImGui.SetNextItemWidth(FieldWidth);
					ImGui.InputText($"##{name}", ref name, 64, ImGuiInputTextFlags.ReadOnly);
					ImGui.SameLine();
					if (CurrentSchema is not null)
					{
						ShowMemberConfig(CurrentSchema, schemaMember);
					}
				}

				ImGui.NewLine();
			}
		}
	}

	private void ShowSchemaConfig()
	{
		if (CurrentSchema is not null)
		{
			if (string.IsNullOrEmpty(CurrentSchema.FilePath))
			{
				using (Theme.Color(Theme.Palette.Error))
				{
					ImGui.TextUnformatted("Schema has not been saved. Save it before configuring relative paths.");

					if (ImGui.Button("Save Now"))
					{
						SaveAs();
					}
				}

				return;
			}

			ImGui.TextUnformatted($"Schema Path: {CurrentSchema.FilePath}");

			var projectRootIsSet = ValidateProjectRootIsSet();
			ShowSetProjectRoot();
			var schemaLocationIsValid = ValidateSchemaLocation();

			if (projectRootIsSet && schemaLocationIsValid)
			{
				ImGui.TextUnformatted($"Data Path: {CurrentSchema.RelativePaths.RelativeDataSourcePath}");
				ImGui.SameLine();
				if (ImGui.Button("Set Data Path"))
				{
					Popups.OpenBrowserDirectory("Select Data Path", (path) => CurrentSchema.RelativePaths.RelativeDataSourcePath = path.RelativeTo(CurrentSchema.ProjectRootPath));
				}
			}
		}
	}

	private bool ValidateProjectRootIsSet()
	{
		if (string.IsNullOrEmpty(CurrentSchema?.RelativePaths.RelativeProjectRootPath))
		{
			using (Theme.Color(Theme.Palette.Warning))
			{
				ImGui.TextUnformatted("Set the path of the project's root directory.");
			}

			return false;
		}

		return true;
	}

	private void ShowSetProjectRoot()
	{
		if (CurrentSchema is not null)
		{
			ImGui.TextUnformatted($"Project Root Path: {CurrentSchema.RelativePaths.RelativeProjectRootPath}");
			ImGui.SameLine();
			if (ImGui.Button("Set Project Root"))
			{
				Popups.OpenBrowserDirectory("Select Project Root", (path) => CurrentSchema.RelativePaths.RelativeProjectRootPath = path.RelativeTo(CurrentSchema.FilePath));
			}
		}
	}

	private bool ValidateSchemaLocation()
	{
		if (CurrentSchema is not null)
		{
			var absoluteSchemaPath = (AbsoluteFilePath)Path.GetFullPath(CurrentSchema.FilePath);
			var expectedProjectRoot = (AbsoluteDirectoryPath)Path.GetFullPath(CurrentSchema.FilePath.DirectoryPath / CurrentSchema.RelativePaths.RelativeProjectRootPath);
			var expectedRelativeSchemaPath = (RelativeFilePath)absoluteSchemaPath.WeakString.RemovePrefix(expectedProjectRoot);
			var expectedSchemaPath = expectedProjectRoot / expectedRelativeSchemaPath;
			if (Path.GetFullPath(expectedSchemaPath) != Path.GetFullPath(absoluteSchemaPath))
			{
				using (Theme.Color(Theme.Palette.Error))
				{
					ImGui.TextUnformatted("Schema appears to have been moved.");
					ImGui.TextUnformatted("Reset the path of the project's root directory.");
				}

				ImGui.TextUnformatted($"Expected: {expectedSchemaPath}");
				ImGui.TextUnformatted($"Actual: {absoluteSchemaPath}");

				return false;
			}
		}

		return true;
	}

	internal void EditClass(ClassName name) => EditClass(CurrentSchema?.GetClass(name));

	internal void EditClass(SchemaClass? schemaClass)
	{
		CurrentClass = schemaClass;
		CurrentDataSource = null;
		QueueSaveOptions();
	}

	internal void EditDataSource(DataSourceName name) => EditDataSource(CurrentSchema?.GetDataSource(name));

	internal void EditDataSource(DataSource? dataSource)
	{
		CurrentClass = null;
		CurrentDataSource = dataSource;
		QueueSaveOptions();
	}
}
