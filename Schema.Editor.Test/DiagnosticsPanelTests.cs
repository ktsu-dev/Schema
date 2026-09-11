// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor.Test;

using System.Linq;

using ktsu.ImGui.App.Testing;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

using SchemaTypes = ktsu.Schema.Models.Types;

/// <summary>
/// The diagnostics panel itself: what it lists, in what order, and what clicking a row does.
/// </summary>
/// <remarks>
/// Driven through <see cref="WidgetHarness"/> rather than through the editor, for the same reason
/// the class graph is: the panel lives behind a tab, and the tab bar comes from a widget library
/// that neither records its tabs for a probe nor takes a selection from outside - so there is no
/// tab for a test to click. Drawing the panel directly is what the tab delegate does anyway.
/// </remarks>
[TestClass]
public sealed class DiagnosticsPanelTests
{
	private WidgetHarness harness = null!;

	[TestInitialize]
	public void StartHarness()
	{
		harness = WidgetHarness.Start();
		harness.Draw = harness.Editor.ShowDiagnosticsPanel;
	}

	[TestCleanup]
	public void StopHarness() => harness.Dispose();

	/// <summary>
	/// A schema with one warning and one error, each naming a different element, so the panel has
	/// both severities to draw and something to order them by.
	/// </summary>
	private static Schema BuildSchemaWithAWarningAndAnError()
	{
		Schema schema = new();

		// A member with no type: a warning, reported against the member.
		SchemaClass user = schema.AddClass("User".As<ClassName>())!;
		user.AddMember("Untyped".As<MemberName>());

		// A data source pointing at a class that is not there: an error, against the data source.
		DataSource dataSource = schema.AddDataSource("Users".As<DataSourceName>())!;
		dataSource.ClassName = "Missing".As<ClassName>();

		return schema;
	}

	/// <summary>
	/// Validates immediately, rather than waiting out the debounce, and draws the frames that put
	/// the result on screen.
	/// </summary>
	private void Validate()
	{
		harness.Editor.RequestValidation();
		harness.Editor.UpdateValidation(SchemaEditor.ValidationDebounceSeconds);
		harness.App.Step(3);
	}

	/// <summary>
	/// The rows the panel drew, named as the panel records them: severity and path.
	/// </summary>
	private string[] ListedIssues =>
		[.. harness.App.Probe.KnownNames
			.Where(name => name.Contains("/diagnostic/", StringComparison.Ordinal))
			.Select(name => name[(name.LastIndexOf('/') + 1)..])];

	[TestMethod]
	public void NothingIsListedWithNoSchemaOpen()
	{
		harness.App.Step(2);

		Assert.IsNull(harness.Editor.CurrentSchema, "This test is about the panel with no document behind it.");
		Assert.IsEmpty(ListedIssues);
	}

	[TestMethod]
	public void NothingIsListedForASchemaWithNoIssues()
	{
		Schema schema = new();
		schema.AddClass("User".As<ClassName>())!.AddMember("Id".As<MemberName>())!.SetType(new SchemaTypes.Int());
		harness.Editor.CurrentSchema = schema;
		Validate();

		Assert.IsEmpty(harness.Editor.Diagnostics, "This schema was supposed to start clean.");
		Assert.IsEmpty(ListedIssues);
	}

	[TestMethod]
	public void EveryIssueGetsARow()
	{
		harness.Editor.CurrentSchema = BuildSchemaWithAWarningAndAnError();
		Validate();

		// In any order: the panel sorts its rows by severity, which is what the next test is about.
		Assert.AreSequenceEqual(
			harness.Editor.Diagnostics.Select(i => $"{i.Severity}:{i.Path}").Distinct().ToArray(),
			ListedIssues.Distinct().ToArray(),
			SequenceOrder.InAnyOrder);
	}

	/// <summary>
	/// Errors are what stops a schema being usable, so they are listed above the warnings however
	/// validation happened to report them.
	/// </summary>
	[TestMethod]
	public void ErrorsAreListedBeforeWarnings()
	{
		harness.Editor.CurrentSchema = BuildSchemaWithAWarningAndAnError();
		Validate();

		Rectangle error = harness.App.Probe.Rect("diagnostic/Error:Users")
			?? throw new AssertFailedException("The data source's error was not listed.");
		Rectangle warning = harness.App.Probe.Rect("diagnostic/Warning:User.Untyped")
			?? throw new AssertFailedException("The member's warning was not listed.");

		Assert.IsLessThan(warning.MinY, error.MinY, "The warning was listed above the error.");
	}

	/// <summary>
	/// The row is a navigation target rather than a line of text: clicking it selects the element
	/// the issue is about, which is the whole reason it is a selectable.
	/// </summary>
	[TestMethod]
	public void ClickingARowSelectsTheElementTheIssueIsAbout()
	{
		harness.Editor.CurrentSchema = BuildSchemaWithAWarningAndAnError();
		Validate();

		harness.App.Click("diagnostic/Error:Users");
		harness.App.Step(2);

		Assert.AreEqual("Users", harness.Editor.CurrentDataSource?.Name.ToString());
	}

	/// <summary>
	/// A member has no panel of its own, so its row selects the class its row is drawn in.
	/// </summary>
	[TestMethod]
	public void ClickingAMemberRowSelectsItsOwningClass()
	{
		harness.Editor.CurrentSchema = BuildSchemaWithAWarningAndAnError();
		Validate();

		harness.App.Click("diagnostic/Warning:User.Untyped");
		harness.App.Step(2);

		Assert.AreEqual("User", harness.Editor.CurrentClass?.Name.ToString());
	}
}
