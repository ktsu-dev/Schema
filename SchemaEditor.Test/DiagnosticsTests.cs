// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

using System.Linq;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// The diagnostics panel's logic: when validation runs, and what clicking an issue selects.
/// </summary>
[TestClass]
public sealed class DiagnosticsTests
{
	private EditorHarness harness = null!;

	[TestInitialize]
	public void StartEditor() => harness = EditorHarness.Start();

	[TestCleanup]
	public void StopEditor() => harness.Dispose();

	/// <summary>
	/// Builds a schema whose every element kind has something wrong with it, so one validation run
	/// produces an issue pointing at each.
	/// </summary>
	private static Schema BuildSchemaWithAnIssuePerElementKind()
	{
		Schema schema = new();

		// A member with no type: a warning naming the member.
		SchemaClass user = schema.AddClass("User".As<ClassName>())!;
		user.AddMember("Untyped".As<MemberName>());

		// An empty class name: an error naming the class.
		schema.AddClass(new ClassName());

		// An empty enum name: an error naming the enum.
		schema.AddEnum(new EnumName());

		// A data source with no class and no file: warnings naming the data source.
		schema.AddDataSource("Users".As<DataSourceName>());

		// A code generator with no output path and no language: warnings naming the generator.
		schema.AddCodeGenerator("CSharp".As<CodeGeneratorName>());

		return schema;
	}

	[TestMethod]
	public void ValidationDoesNotRunUntilTheSchemaSettles()
	{
		harness.Editor.CurrentSchema = BuildSchemaWithAnIssuePerElementKind();
		harness.Editor.RequestValidation();

		// Just short of the debounce: the edit is still in flight, so nothing has been validated.
		harness.Editor.UpdateValidation(SchemaEditor.ValidationDebounceSeconds - 0.01f);
		Assert.AreEqual(0, harness.Editor.Diagnostics.Count);

		harness.Editor.UpdateValidation(0.02f);
		Assert.IsTrue(harness.Editor.Diagnostics.Count > 0, "The schema should have been validated once the debounce elapsed.");
	}

	/// <summary>
	/// A burst of edits - the case the debounce exists for - must validate once at the end rather
	/// than once per edit.
	/// </summary>
	[TestMethod]
	public void EachEditRestartsTheDebounce()
	{
		harness.Editor.CurrentSchema = BuildSchemaWithAnIssuePerElementKind();

		for (int edit = 0; edit < 5; edit++)
		{
			harness.Editor.RequestValidation();
			harness.Editor.UpdateValidation(SchemaEditor.ValidationDebounceSeconds - 0.01f);
			Assert.AreEqual(0, harness.Editor.Diagnostics.Count, $"Validation ran while edit {edit} was still in flight.");
		}

		harness.Editor.UpdateValidation(0.02f);
		Assert.IsTrue(harness.Editor.Diagnostics.Count > 0);
	}

	[TestMethod]
	public void ValidationDoesNotRunAgainUntilSomethingChanges()
	{
		harness.Editor.CurrentSchema = BuildSchemaWithAnIssuePerElementKind();
		harness.Editor.RequestValidation();
		harness.Editor.UpdateValidation(SchemaEditor.ValidationDebounceSeconds);

		System.Collections.ObjectModel.Collection<SchemaValidationIssue> first = harness.Editor.Diagnostics;

		harness.Editor.UpdateValidation(10f);

		Assert.AreSame(first, harness.Editor.Diagnostics, "Validation re-ran without the schema having changed.");
	}

	[TestMethod]
	public void ValidatingWithNoSchemaClearsTheIssues()
	{
		harness.Editor.CurrentSchema = BuildSchemaWithAnIssuePerElementKind();
		harness.Editor.RequestValidation();
		harness.Editor.UpdateValidation(SchemaEditor.ValidationDebounceSeconds);
		Assert.IsTrue(harness.Editor.Diagnostics.Count > 0);

		harness.Editor.CurrentSchema = null;
		harness.Editor.RequestValidation();
		harness.Editor.UpdateValidation(SchemaEditor.ValidationDebounceSeconds);

		Assert.AreEqual(0, harness.Editor.Diagnostics.Count);
	}

	private SchemaValidationIssue IssueFor<TElement>() =>
		harness.Editor.Diagnostics.FirstOrDefault(i => i.Element is TElement)
			?? throw new AssertFailedException($"No validation issue was reported against a {typeof(TElement).Name}. Issues: {string.Join("; ", harness.Editor.Diagnostics.Select(i => $"{i.Path}: {i.Message}"))}");

	private void ValidateOnce()
	{
		harness.Editor.RequestValidation();
		harness.Editor.UpdateValidation(SchemaEditor.ValidationDebounceSeconds);
	}

	[TestMethod]
	public void NavigatingToAClassIssueSelectsTheClass()
	{
		harness.Editor.CurrentSchema = BuildSchemaWithAnIssuePerElementKind();
		ValidateOnce();

		SchemaValidationIssue issue = IssueFor<SchemaClass>();
		harness.Editor.NavigateTo(issue);

		Assert.AreSame(issue.Element, harness.Editor.CurrentClass);
	}

	/// <summary>
	/// A member has no panel of its own; its row is drawn in its class's, so that is what must be
	/// selected.
	/// </summary>
	[TestMethod]
	public void NavigatingToAMemberIssueSelectsItsOwningClass()
	{
		harness.Editor.CurrentSchema = BuildSchemaWithAnIssuePerElementKind();
		ValidateOnce();

		SchemaValidationIssue issue = IssueFor<SchemaMember>();
		harness.Editor.NavigateTo(issue);

		Assert.AreSame(((SchemaMember)issue.Element!).ParentClass, harness.Editor.CurrentClass);
	}

	[TestMethod]
	public void NavigatingToAnEnumIssueSelectsTheEnum()
	{
		harness.Editor.CurrentSchema = BuildSchemaWithAnIssuePerElementKind();
		ValidateOnce();

		SchemaValidationIssue issue = IssueFor<SchemaEnum>();
		harness.Editor.NavigateTo(issue);

		Assert.AreSame(issue.Element, harness.Editor.CurrentEnum);
	}

	[TestMethod]
	public void NavigatingToADataSourceIssueSelectsTheDataSource()
	{
		harness.Editor.CurrentSchema = BuildSchemaWithAnIssuePerElementKind();
		ValidateOnce();

		SchemaValidationIssue issue = IssueFor<DataSource>();
		harness.Editor.NavigateTo(issue);

		Assert.AreSame(issue.Element, harness.Editor.CurrentDataSource);
	}

	[TestMethod]
	public void NavigatingToACodeGeneratorIssueSelectsTheCodeGenerator()
	{
		harness.Editor.CurrentSchema = BuildSchemaWithAnIssuePerElementKind();
		ValidateOnce();

		SchemaValidationIssue issue = IssueFor<SchemaCodeGenerator>();
		harness.Editor.NavigateTo(issue);

		Assert.AreSame(issue.Element, harness.Editor.CurrentCodeGenerator);
	}

	/// <summary>
	/// Selecting one element must clear the rest, or two panels would claim to be showing the
	/// current selection at once.
	/// </summary>
	[TestMethod]
	public void NavigatingClearsThePreviousSelection()
	{
		harness.Editor.CurrentSchema = BuildSchemaWithAnIssuePerElementKind();
		ValidateOnce();

		harness.Editor.NavigateTo(IssueFor<SchemaClass>());
		harness.Editor.NavigateTo(IssueFor<DataSource>());

		Assert.IsNull(harness.Editor.CurrentClass);
		Assert.IsNotNull(harness.Editor.CurrentDataSource);
	}

	/// <summary>
	/// A duplicate-name issue names no single element, so there is nothing to select and nothing
	/// should change.
	/// </summary>
	[TestMethod]
	public void NavigatingToAnIssueWithNoElementChangesNothing()
	{
		harness.Editor.CurrentSchema = BuildSchemaWithAnIssuePerElementKind();
		ValidateOnce();
		harness.Editor.NavigateTo(IssueFor<SchemaClass>());
		SchemaClass? selected = harness.Editor.CurrentClass;

		harness.Editor.NavigateTo(new SchemaValidationIssue
		{
			Severity = SchemaValidationSeverity.Error,
			Path = "User",
			Message = "Duplicate class name 'User'.",
		});

		Assert.AreSame(selected, harness.Editor.CurrentClass);
	}

	[TestMethod]
	public void AnElementWithNoIssueIsNotMarked()
	{
		Schema schema = new();
		SchemaClass clean = schema.AddClass("Clean".As<ClassName>())!;
		harness.Editor.CurrentSchema = schema;
		ValidateOnce();

		Assert.IsNull(harness.Editor.GetIssueFor(clean));
	}

	[TestMethod]
	public void AnElementWithNoElementReferenceIsNotMatched() =>
		Assert.IsNull(harness.Editor.GetIssueFor(null));

	/// <summary>
	/// The inline marker has room for one issue, so it must be the most severe one affecting the
	/// element rather than whichever validation happened to report first.
	/// </summary>
	[TestMethod]
	public void TheMarkerForAnElementIsItsMostSevereIssue()
	{
		Schema schema = new();

		// No class and no file: the missing class is a warning, and the missing file is another.
		// Pointing it at a class that does not exist makes the first an error instead.
		DataSource dataSource = schema.AddDataSource("Users".As<DataSourceName>())!;
		dataSource.ClassName = "Missing".As<ClassName>();

		harness.Editor.CurrentSchema = schema;
		ValidateOnce();

		SchemaValidationIssue? marked = harness.Editor.GetIssueFor(dataSource);

		Assert.IsNotNull(marked);
		Assert.AreEqual(SchemaValidationSeverity.Error, marked.Severity);
		Assert.IsTrue(
			harness.Editor.Diagnostics.Any(i => ReferenceEquals(i.Element, dataSource) && i.Severity == SchemaValidationSeverity.Warning),
			"This test only proves anything while the data source also has a warning to be outranked.");
	}
}
