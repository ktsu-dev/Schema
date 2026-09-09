// Copyright (c) 2023-2026 ktsu-dev contributors

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ktsu.SchemaEditor;

using System;
using System.Diagnostics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets;
using ktsu.IntervalAction;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;
using ktsu.UndoRedo;
using ktsu.UndoRedo.Contracts;
using ktsu.UndoRedo.Core.Services;

public partial class SchemaEditor
{
	public static SchemaEditor Instance { get; } = new();
	internal Schema? CurrentSchema { get; set; }
	internal AbsoluteFilePath CurrentSchemaPath { get; set; } = new();
	internal SchemaClass? CurrentClass { get; set; }
	internal DataSource? CurrentDataSource { get; set; }
	internal SchemaEnum? CurrentEnum { get; set; }
	internal SchemaCodeGenerator? CurrentCodeGenerator { get; set; }
	internal AppData Options { get; }
	internal static float FieldWidth => ImGui.GetIO().DisplaySize.X * 0.15f;
	private bool OptionsDirty { get; set; }
#pragma warning disable IDE0052 // Remove unread private member - reference needed to prevent GC
	private readonly IntervalAction? autoSaveOptionsAction;
#pragma warning restore IDE0052
	private ImGuiWidgets.DividerContainer DividerContainerCols { get; init; }

	internal IUndoRedoService UndoRedo { get; }
	internal Popups Popups { get; }
	private TreeSchema TreeSchema { get; init; }
	private CodeGeneratorPanel CodeGeneratorPanel { get; init; }

	private MemberGridPanel MemberGrid { get; init; }
	private ClassGraphView ClassGraph { get; } = new();
	private ImGuiWidgets.TabPanel MainTabs { get; }

	// The tab whose label carries the validation counts.
	private readonly string diagnosticsTabId;

	// Tab content delegates are parameterless, so the current frame's delta is stashed here for them.
	private float currentDeltaTime;

	public SchemaEditor()
	{
		UndoRedo = new UndoRedoService(new StackManager(), new SaveBoundaryManager(), new CommandMerger());
		TreeSchema = new(this);
		CodeGeneratorPanel = new(this);
		MemberGrid = new(this);
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

		// The tab bar lives inside the right divider zone, which is already a child window, so the tab
		// content flows from the cursor without overlapping the bar. The left zone keeps the schema tree.
		MainTabs = new ImGuiWidgets.TabPanel("MainViews", closable: false, reorderable: false);
		MainTabs.AddTab("Editor", ShowEditorPanel);
		MainTabs.AddTab("Class Graph", () => ClassGraph.Show(CurrentSchema, currentDeltaTime));
		diagnosticsTabId = MainTabs.AddTab(DiagnosticsTab.Name, ShowDiagnosticsPanel);

		Options = AppData.LoadOrCreate();
		Popups = Options.Popups;

		autoSaveOptionsAction = IntervalAction.Start(new()
		{
			Action = () =>
			{
				if (OptionsDirty)
				{
					OptionsDirty = false;
					SaveOptionsInternal();
				}
			},
			ActionInterval = TimeSpan.FromSeconds(3),
			IntervalType = IntervalType.FromLastCompletion,
		});

		// restore open schema
		if (SchemaFile.TryLoad(Options.CurrentSchemaPath, out Schema? previouslyOpenSchema) && previouslyOpenSchema is not null)
		{
			CurrentSchema = previouslyOpenSchema;
			CurrentSchemaPath = Options.CurrentSchemaPath;
			CurrentClass = null;
			CurrentClass = CurrentSchema.GetClass(Options.CurrentClassName);
			RequestValidation();
		}

		// restore divider states
		if (Options.DividerStates.TryGetValue(DividerContainerCols.Id, out System.Collections.ObjectModel.Collection<float>? sizes))
		{
			DividerContainerCols.SetSizesFromList(sizes);
		}
	}

	/// <summary>
	/// Applies the saved theme once ImGui exists to receive it.
	/// </summary>
	/// <remarks>
	/// A theme has to be applied here rather than in the constructor: it writes ImGui's style
	/// colours, and there is no ImGui context until the application has started.
	/// </remarks>
	internal void OnStart() => EditorTheme.Apply(Options.ThemeName);

	/// <summary>
	/// Notes a theme the user chose, so it is there again next time.
	/// </summary>
	private void RecordThemeChoice()
	{
		Options.ThemeName = EditorTheme.CurrentName;
		QueueSaveOptions();
	}

	private void DividerResized(ImGuiWidgets.DividerContainer container)
	{
		Options.DividerStates[container.Id] = container.GetSizes();
		QueueSaveOptions();
	}

	//Dont call this directly, call QueueSaveOptions instead so that we can debounce the saves and avoid saving multiple times per frame or multiple frames in a row
	private void SaveOptionsInternal()
	{
		Options.CurrentSchemaPath = CurrentSchemaPath;
		Options.CurrentClassName = CurrentClass?.Name ?? new();
		Options.DividerStates[DividerContainerCols.Id] = DividerContainerCols.GetSizes();
		// Note: WindowState property access needs to be updated for the current ImGuiApp version
		// Options.WindowState = ImGuiApp.WindowState;
		Options.Popups = Popups;
		Options.Save();
	}

	private void QueueSaveOptions() => OptionsDirty = true;

	/// <summary>
	/// Runs an undoable command and notes that the schema changed.
	/// </summary>
	/// <remarks>
	/// Every mutation the editor makes goes through here rather than calling
	/// <see cref="IUndoRedoService.Execute"/> directly, so that undo coverage and revalidation
	/// cannot be forgotten for a new edit: the two things that must happen on every change happen
	/// in one place.
	/// </remarks>
	/// <param name="command">The command to execute.</param>
	internal void Execute(ICommand command)
	{
		UndoRedo.Execute(command);
		RequestValidation();
		QueueSaveOptions();
	}

	private void Undo()
	{
		UndoRedo.Undo();
		RequestValidation();
	}

	private void Redo()
	{
		UndoRedo.Redo();
		RequestValidation();
	}

	internal void OnTick(float dt)
	{
		ProcessKeyboardShortcuts();
		UpdateValidation(dt);
		UpdateWindowTitle();
		ProcessCloseRequest();
	}

	private void ProcessKeyboardShortcuts()
	{
		ImGuiIOPtr io = ImGui.GetIO();
		if (io.WantTextInput)
		{
			return;
		}

		bool ctrl = io.KeyCtrl;
		bool shift = io.KeyShift;

		if (ctrl && ImGui.IsKeyPressed(ImGuiKey.Z, false))
		{
			if (shift)
			{
				Redo();
			}
			else
			{
				Undo();
			}
		}
		else if (ctrl && ImGui.IsKeyPressed(ImGuiKey.Y, false))
		{
			Redo();
		}
		else if (ctrl && shift && ImGui.IsKeyPressed(ImGuiKey.S, false))
		{
			SaveAs();
		}
		else if (ctrl && ImGui.IsKeyPressed(ImGuiKey.S, false))
		{
			Save();
		}
		else if (ctrl && ImGui.IsKeyPressed(ImGuiKey.N, false))
		{
			New();
		}
		else if (ctrl && ImGui.IsKeyPressed(ImGuiKey.O, false))
		{
			Open();
		}
	}

	internal void OnRender(float dt)
	{
		// Stashed for the parameterless tab content delegates (the Class Graph needs the frame delta).
		currentDeltaTime = dt;

		// No theme colour is pushed around the whole application. Theme.FromColor is a scoped
		// colour for one widget - it is what marks an element that has an error or a warning - and
		// wrapping every frame in the primary colour tinted the entire interface with it, leaving
		// an ordinary button indistinguishable from an emphasised one. The application-wide look
		// belongs to the ktsu.ThemeProvider theme applied in OnStart.
		DividerContainerCols.Tick(dt);
		Popups.Update();

		if (EditorTheme.ShowBrowser())
		{
			RecordThemeChoice();
		}
	}

	private void ShowLeftPanel(float dt) => TreeSchema.Show();

	// The right zone hosts the Editor / Class Graph / Diagnostics tab bar.
	private void ShowRightPanel(float dt)
	{
		DiagnosticsTab.ShowCounts(MainTabs, diagnosticsTabId, ErrorCount, WarningCount);
		MainTabs.Draw();
	}

	private void ShowEditorPanel()
	{
		ShowSchemaConfig();

		if (CurrentClass is not null)
		{
			ShowClassProperties();
		}
		else if (CurrentDataSource is not null)
		{
			ShowDataSourceProperties();
		}
		else if (CurrentEnum is not null)
		{
			ShowEnumProperties();
		}
		else if (CurrentCodeGenerator is not null && CurrentSchema is not null)
		{
			CodeGeneratorPanel.Show(CurrentSchema, CurrentCodeGenerator);
		}
	}

	internal void OnMenu()
	{
		ShowFileMenu();
		ShowEditMenu();

		if (EditorTheme.ShowMenu())
		{
			RecordThemeChoice();
		}
	}

	/// <summary>
	/// Opens a menu, recording where its header was drawn.
	/// </summary>
	/// <remarks>
	/// ImGui gives a menu item no identity a probe can find on its own, so until the header and
	/// the items under it were marked there was no way for a test to reach New, Open, Save or
	/// Exit the way a user does - through the menu rather than by calling what it invokes.
	/// </remarks>
	/// <param name="label">The menu's label, which is also the name it is recorded under.</param>
	/// <param name="enabled">Whether the menu can be opened.</param>
	/// <returns>True while the menu is open, in which case it must be ended.</returns>
	internal static bool BeginMenu(string label, bool enabled = true)
	{
		bool open = ImGui.BeginMenu(label, enabled);
		ImGuiProbes.MarkItem("menu", label);
		return open;
	}

	/// <summary>
	/// Draws a menu item, recording where it was drawn.
	/// </summary>
	/// <param name="label">The item's label, which is also the name it is recorded under.</param>
	/// <param name="shortcut">The keyboard shortcut shown beside it.</param>
	/// <param name="enabled">Whether the item can be chosen.</param>
	/// <returns>True on the frame it is clicked.</returns>
	internal static bool MenuItem(string label, string shortcut = "", bool enabled = true)
	{
		bool clicked = ImGui.MenuItem(label, shortcut, false, enabled);
		ImGuiProbes.MarkItem("menu", label);
		return clicked;
	}

	private void ShowFileMenu()
	{
		if (!BeginMenu("File"))
		{
			return;
		}

		if (MenuItem("New", "Ctrl+N"))
		{
			New();
		}

		if (MenuItem("Open", "Ctrl+O"))
		{
			Open();
		}

		ShowRecentFilesMenu();

		ImGui.Separator();

		if (MenuItem("Save", "Ctrl+S", CurrentSchema is not null))
		{
			Save();
		}

		// Always available while a schema is open: without it there is no way to save a copy
		// somewhere else once the schema has a path.
		if (MenuItem("Save As...", "Ctrl+Shift+S", CurrentSchema is not null))
		{
			SaveAs();
		}

		ImGui.Separator();

		// Enabled rather than selected: the two-argument ImGui overload this used to call takes
		// the flag as the item's checked state, so the item was drawn with a tick beside it once
		// the schema had a path, and stayed clickable - handing an empty path to the shell -
		// while it had none.
		string schemaFilePath = CurrentSchemaPath;
		if (MenuItem("Open Externally", string.Empty, !string.IsNullOrEmpty(schemaFilePath)))
		{
			OpenExternally(schemaFilePath);
		}

		ImGui.Separator();

		if (MenuItem("Exit"))
		{
			ExitWithUnsavedChangesGuard();
		}

		ImGui.EndMenu();
	}

	private void ShowEditMenu()
	{
		if (!BeginMenu("Edit"))
		{
			return;
		}

		if (MenuItem("Undo", "Ctrl+Z", UndoRedo.CanUndo))
		{
			Undo();
		}

		if (MenuItem("Redo", "Ctrl+Y", UndoRedo.CanRedo))
		{
			Redo();
		}

		ImGui.EndMenu();
	}

	/// <summary>
	/// Opens a path with the operating system's default handler for it.
	/// </summary>
	/// <remarks>
	/// Delegates to the platform shell rather than naming an executable: "explorer.exe" does
	/// not exist on Linux or macOS, so the menu item used to take the editor down with an
	/// unhandled Win32Exception there. Process.Start can still fail on any platform - no shell
	/// association, a file that has since been removed - so failures are surfaced as a popup.
	/// </remarks>
	/// <param name="path">The path to open.</param>
	private void OpenExternally(string path)
	{
		try
		{
			using Process process = new();
			process.StartInfo.FileName = path;
			process.StartInfo.UseShellExecute = true;
			process.Start();
		}
		catch (Exception ex) when (ex is System.ComponentModel.Win32Exception
			or ObjectDisposedException
			or InvalidOperationException
			or PlatformNotSupportedException)
		{
			Popups.OpenMessageOK("Error", $"Failed to open '{path}' externally.\n\n{ex.Message}");
		}
	}

	/// <summary>
	/// Folds a collapsible part of the interface open or shut, and remembers which it now is.
	/// </summary>
	/// <remarks>
	/// An instance member rather than a static one reaching for <see cref="Instance"/>. The state
	/// belongs to the settings of the editor drawing the row, and while the application only ever
	/// has one editor, a test has one per test - so reading it off the singleton meant a row left
	/// open by one test came back open in the next.
	/// </remarks>
	/// <param name="key">What is being folded, as the caller names it.</param>
	/// <returns>True if it is now hidden.</returns>
	internal bool ToggleVisibility(string key)
	{
		QueueSaveOptions();
		if (Options.HiddenItems.Remove(key))
		{
			return false;
		}

		Options.HiddenItems.Add(key);
		return true;
	}

	/// <summary>
	/// Whether a collapsible part of the interface is folded open.
	/// </summary>
	/// <param name="key">What is being asked about, as the caller names it.</param>
	/// <returns>True if it is not hidden.</returns>
	internal bool IsVisible(string key) => !Options.HiddenItems.Contains(key);

	private void ShowSchemaConfig()
	{
		if (CurrentSchema is null)
		{
			return;
		}

		if (string.IsNullOrEmpty(CurrentSchemaPath))
		{
			using (EditorTheme.Error())
			{
				ImGui.TextUnformatted("Schema has not been saved. Save it before configuring relative paths.");

				if (ImGui.Button("Save Now"))
				{
					SaveAs();
				}
			}

			return;
		}

		ImGui.TextUnformatted($"Schema Path: {CurrentSchemaPath}");
	}

	internal void EditClass(ClassName name) => EditClass(CurrentSchema?.GetClass(name));

	internal void EditClass(SchemaClass? schemaClass)
	{
		ClearSelection();
		CurrentClass = schemaClass;
		SelectTree(TreeSchema.ClassesTab);
		QueueSaveOptions();
	}

	internal void EditDataSource(DataSourceName name) => EditDataSource(CurrentSchema?.GetDataSource(name));

	internal void EditDataSource(DataSource? dataSource)
	{
		ClearSelection();
		CurrentDataSource = dataSource;
		SelectTree(TreeSchema.DataSourcesTab);
		QueueSaveOptions();
	}

	internal void EditEnum(SchemaEnum? schemaEnum)
	{
		ClearSelection();
		CurrentEnum = schemaEnum;
		SelectTree(TreeSchema.EnumsTab);
		QueueSaveOptions();
	}

	internal void EditCodeGenerator(CodeGeneratorName name) => EditCodeGenerator(CurrentSchema?.GetCodeGenerator(name));

	internal void EditCodeGenerator(SchemaCodeGenerator? codeGenerator)
	{
		ClearSelection();
		CurrentCodeGenerator = codeGenerator;
		SelectTree(TreeSchema.CodeGeneratorsTab);
		QueueSaveOptions();
	}

	/// <summary>
	/// Opens the tree tab holding one kind of element.
	/// </summary>
	/// <param name="name">The tab's name, as the constants on <see cref="TreeSchema"/> give it.</param>
	internal void SelectTree(string name) => TreeSchema.SelectTab(name);

	private void ClearSelection()
	{
		CurrentClass = null;
		CurrentDataSource = null;
		CurrentEnum = null;
		CurrentCodeGenerator = null;
	}
}
