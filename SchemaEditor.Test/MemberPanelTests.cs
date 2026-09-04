// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

using System.Linq;

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
}
