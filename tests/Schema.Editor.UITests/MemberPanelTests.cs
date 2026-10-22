// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor.UITests;

using System.Linq;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// The member rows in the class panel: the controls that reorder and remove members.
/// </summary>
[TestClass]
public sealed class MemberPanelTests
{
	private EditorHarness harness = null!;
	private SchemaClass user = null!;

	[TestInitialize]
	public void StartEditor()
	{
		harness = EditorHarness.Start();

		Schema schema = new();
		user = schema.AddClass("User".As<ClassName>())!;
		user.AddMember("Id".As<MemberName>());
		user.AddMember("Age".As<MemberName>());
		user.AddMember("Email".As<MemberName>());

		harness.Editor.CurrentSchema = schema;
		harness.Editor.EditClass(user);
	}

	[TestCleanup]
	public void StopEditor() => harness.Dispose();

	private string[] MemberNames => [.. user.Members.Select(m => m.Name.ToString())];

	/// <summary>
	/// Asserts the class holds exactly these members, in this order.
	/// </summary>
	private void AssertMembers(params string[] expected) =>
		CollectionAssert.AreEqual(expected, MemberNames, $"Members were [{string.Join(", ", MemberNames)}].");

	[TestMethod]
	public void TheMembersAreShownInOrder() =>
		AssertMembers("Id", "Age", "Email");

	[TestMethod]
	public void MovingAMemberUpSwapsItWithThePreviousOne()
	{
		harness.Click("memberAge/MoveUp");

		AssertMembers("Age", "Id", "Email");
	}

	[TestMethod]
	public void MovingAMemberDownSwapsItWithTheNextOne()
	{
		harness.Click("memberId/MoveDown");

		AssertMembers("Age", "Id", "Email");
	}

	[TestMethod]
	public void ReorderingIsUndoable()
	{
		harness.Click("memberEmail/MoveUp");
		AssertMembers("Id", "Email", "Age");

		harness.Editor.UndoRedo.Undo();

		AssertMembers("Id", "Age", "Email");
	}

	/// <summary>
	/// The first row cannot move up and the last cannot move down, so those controls are disabled -
	/// and a disabled ImGui control does not respond to a click.
	/// </summary>
	[TestMethod]
	public void TheEndsOfTheListCannotMoveFurther()
	{
		harness.Click("memberId/MoveUp");
		AssertMembers("Id", "Age", "Email");

		harness.Click("memberEmail/MoveDown");
		AssertMembers("Id", "Age", "Email");
	}

	[TestMethod]
	public void DeletingAMemberRemovesItFromItsClass()
	{
		harness.Click("memberAge/Delete");

		AssertMembers("Id", "Email");
	}

	/// <summary>
	/// Deleting restores in place rather than at the end, or an undo would silently reorder the
	/// class it was meant to put back.
	/// </summary>
	[TestMethod]
	public void DeletingAMemberIsUndoable()
	{
		harness.Click("memberAge/Delete");
		AssertMembers("Id", "Email");

		harness.Editor.UndoRedo.Undo();

		AssertMembers("Id", "Age", "Email");
	}

	/// <summary>
	/// A description is the user's own text, so a <c>%</c> in it is shown rather than read as a
	/// printf directive: <c>%s</c> would read an argument nobody passed, and <c>% o</c> would print
	/// a number.
	/// </summary>
	/// <remarks>
	/// The editor draws in a scaled font that is only current inside a frame, so the tooltip is
	/// measured against a second one whose description has no <c>%</c> at all, and the two widths
	/// are compared as a ratio, which the scale cancels out of.
	/// </remarks>
	[TestMethod]
	public void ADescriptionWithPercentSignsIsShownLiterallyInItsTooltip()
	{
		const string withPercent = "Reads 100%s of the time, % o or so";
		const string plain = "Reads every time";
		SetDescription("Age", withPercent);
		SetDescription("Id", plain);

		float percentWidth = TooltipWidth("memberAge");
		float plainWidth = TooltipWidth("memberId");

		float expected = ImGui.CalcTextSize(withPercent).X / ImGui.CalcTextSize(plain).X;
		Assert.AreEqual(expected, percentWidth / plainWidth, 0.02f, "The tooltip was not as wide as the description drawn literally.");
	}

	private void SetDescription(string member, string description) =>
		user.Members.First(m => m.Name.ToString() == member).Description = description.As<SchemaChildDescription>();

	/// <summary>
	/// Rests the pointer on a member's description toggle and measures the text of the tooltip it
	/// opens. The tooltip's text is recorded inside the member's probe scope, so it is named by it.
	/// </summary>
	private float TooltipWidth(string memberScope)
	{
		string item = $"{memberScope}/ToggleDescription";
		string tooltip = $"{memberScope}/{EditorTooltip.TextProbeName}";
		harness.StepUntil(() => harness.IsOnScreen(item), $"'{item}' appearing");
		Rectangle target = harness.App.Probe.Rect(item)!.Value;
		harness.App.Mouse.MoveTo((target.MinX + target.MaxX) * 0.5f, (target.MinY + target.MaxY) * 0.5f);
		harness.App.Step(2);
		harness.StepUntil(() => harness.IsOnScreen(tooltip), $"the tooltip of '{item}' appearing");

		Rectangle text = harness.App.Probe.Rect(tooltip)!.Value;
		return text.MaxX - text.MinX;
	}
}
