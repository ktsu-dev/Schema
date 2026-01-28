// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ktsu.Schema.Test")]

namespace ktsu.SchemaEditor;

using System.Collections.ObjectModel;
using ktsu.AppDataStorage;
using ktsu.ImGui.App;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Instantiated via reflection by AppDataStorage")]
internal sealed class AppData : AppData<AppData>
{
	public AbsoluteFilePath CurrentSchemaPath { get; set; } = new();
	public ClassName CurrentClassName { get; set; } = new();
	public ImGuiAppWindowState WindowState { get; set; } = new();
	public HashSet<string> HiddenItems { get; set; } = [];
	public Dictionary<string, Collection<float>> DividerStates { get; set; } = [];
	public Popups Popups { get; set; } = new();
}
