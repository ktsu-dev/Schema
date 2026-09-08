// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor.Test;

using System.Linq;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

using SchemaTypes = ktsu.Schema.Models.Types;

/// <summary>
/// The property panel for whichever element is selected: its name and description fields, and the
/// pickers that point one element at another.
/// </summary>
/// <remarks>
/// Everything here is reached the way a user reaches it - the field is clicked into, typed in and
/// left, the picker is opened and an option chosen - because the behaviour being checked lives in
/// what the widget reports across those frames rather than in the method it eventually calls.
/// </remarks>
[TestClass]
public sealed class ElementPanelTests
{
	private EditorHarness harness = null!;
	private Schema schema = null!;

	[TestInitialize]
	public void StartEditor()
	{
		harness = EditorHarness.Start();
		schema = new Schema();
		harness.Editor.CurrentSchema = schema;
	}

	[TestCleanup]
	public void StopEditor() => harness.Dispose();

	private SchemaClass AddClass(string name) => schema.AddClass(name.As<ClassName>())!;

	[TestMethod]
	public void RenamingAClassFromItsPanelRenamesIt()
	{
		SchemaClass user = AddClass("User");
		harness.Editor.EditClass(user);

		harness.Commit("field/ClassNameUser", "Account");

		Assert.AreEqual("Account", user.Name.ToString());
		Assert.IsNotNull(schema.GetClass("Account".As<ClassName>()));
	}

	[TestMethod]
	public void RenamingAClassFromItsPanelIsUndoable()
	{
		SchemaClass user = AddClass("User");
		harness.Editor.EditClass(user);
		harness.Commit("field/ClassNameUser", "Account");

		harness.Editor.UndoRedo.Undo();

		Assert.AreEqual("User", user.Name.ToString());
	}

	/// <summary>
	/// A name that collides with a sibling is refused with a message, and nothing is left on the
	/// undo stack for a rename that never happened.
	/// </summary>
	[TestMethod]
	public void RenamingAClassToANameAlreadyInUseIsRefused()
	{
		SchemaClass user = AddClass("User");
		AddClass("Account");
		harness.Editor.EditClass(user);

		harness.Commit("field/ClassNameUser", "Account");
		harness.App.Step(3);

		Assert.AreEqual("User", user.Name.ToString());
		Assert.IsTrue(harness.App.Probe.Matches("prompt/OK").Count > 0, "The collision was not reported.");
		Assert.IsFalse(harness.Editor.UndoRedo.CanUndo, "A rename that was refused left an undo entry behind.");
	}

	/// <summary>
	/// A description is multi-line, so the edit is finished by leaving the field rather than by
	/// pressing Enter, which the field takes as a newline.
	/// </summary>
	[TestMethod]
	public void EditingAClassDescriptionRecordsIt()
	{
		SchemaClass user = AddClass("User");
		harness.Editor.EditClass(user);

		harness.TypeInto("field/ClassDescriptionUser", "Someone with an account.");
		harness.Click("field/ClassNameUser");

		Assert.AreEqual("Someone with an account.", user.Description.ToString());
	}

	[TestMethod]
	public void RenamingAnEnumFromItsPanelRenamesIt()
	{
		SchemaEnum status = schema.AddEnum("Status".As<EnumName>())!;
		harness.Editor.EditEnum(status);

		harness.Commit("field/EnumNameStatus", "State");

		Assert.AreEqual("State", status.Name.ToString());
	}

	[TestMethod]
	public void EditingAnEnumDescriptionRecordsIt()
	{
		SchemaEnum status = schema.AddEnum("Status".As<EnumName>())!;
		harness.Editor.EditEnum(status);

		harness.TypeInto("field/EnumDescriptionStatus", "Where an order has got to.");
		harness.Click("field/EnumNameStatus");

		Assert.AreEqual("Where an order has got to.", status.Description.ToString());
	}

	[TestMethod]
	public void SettingADataSourcesFilePathRecordsIt()
	{
		DataSource users = schema.AddDataSource("Users".As<DataSourceName>())!;
		harness.Editor.EditDataSource(users);

		harness.Commit("field/DataSourceFileUsers", "data/users.json");

		Assert.AreEqual("data/users.json", users.File.ToString());
	}

	[TestMethod]
	public void ChoosingAClassForADataSourcePointsItAtThatClass()
	{
		AddClass("User");
		DataSource users = schema.AddDataSource("Users".As<DataSourceName>())!;
		harness.Editor.EditDataSource(users);

		harness.Click("class-selector/Users");
		harness.Click("class-option/User");

		Assert.AreEqual("User", users.ClassName.ToString());
	}

	[TestMethod]
	public void ChoosingNoClassForADataSourceClearsIt()
	{
		AddClass("User");
		DataSource users = schema.AddDataSource("Users".As<DataSourceName>())!;
		users.ClassName = "User".As<ClassName>();
		harness.Editor.EditDataSource(users);

		harness.Click("class-selector/Users");
		harness.Click("class-option/<none>");

		Assert.AreEqual(string.Empty, users.ClassName.ToString());
	}

	[TestMethod]
	public void ChoosingAClassForADataSourceIsUndoable()
	{
		AddClass("User");
		DataSource users = schema.AddDataSource("Users".As<DataSourceName>())!;
		harness.Editor.EditDataSource(users);
		harness.Click("class-selector/Users");
		harness.Click("class-option/User");

		harness.Editor.UndoRedo.Undo();

		Assert.AreEqual(string.Empty, users.ClassName.ToString());
	}

	[TestMethod]
	public void RenamingAMemberFromItsRowRenamesIt()
	{
		SchemaClass user = AddClass("User");
		SchemaMember id = user.AddMember("Id".As<MemberName>())!;
		harness.Editor.EditClass(user);

		harness.Commit("memberId/field/Name", "Identifier");

		Assert.AreEqual("Identifier", id.Name.ToString());
	}

	[TestMethod]
	public void ChoosingAMemberTypeSetsIt()
	{
		SchemaClass user = AddClass("User");
		SchemaMember id = user.AddMember("Id".As<MemberName>())!;
		harness.Editor.EditClass(user);

		harness.Click("memberId/Type");
		harness.Click("searchable-list/String");

		Assert.AreEqual("String", id.Type.DisplayName);
	}

	[TestMethod]
	public void ChoosingAMemberTypeIsUndoable()
	{
		SchemaClass user = AddClass("User");
		SchemaMember id = user.AddMember("Id".As<MemberName>())!;
		id.SetType(new SchemaTypes.Int());
		harness.Editor.EditClass(user);

		harness.Click("memberId/Type");
		harness.Click("searchable-list/String");

		harness.Editor.UndoRedo.Undo();

		Assert.AreEqual("Int", id.Type.DisplayName);
	}

	/// <summary>
	/// The container is only asked for on an array member, and is the one field the editor used to
	/// write to the model on every frame it was drawn.
	/// </summary>
	[TestMethod]
	public void SettingAnArraysContainerRecordsIt()
	{
		SchemaClass user = AddClass("User");
		SchemaMember tags = user.AddMember("Tags".As<MemberName>())!;
		tags.SetType(new SchemaTypes.Array() { ElementType = new SchemaTypes.String() });
		harness.Editor.EditClass(user);

		harness.Commit("memberTags/field/Container", "HashSet");

		Assert.AreEqual("HashSet", ((SchemaTypes.Array)tags.Type).Container.ToString());
	}

	/// <summary>
	/// An array of objects can be keyed by one of the element class's primitive members, which is
	/// the only case the key picker is offered in.
	/// </summary>
	[TestMethod]
	public void ChoosingAnArrayKeySetsIt()
	{
		SchemaClass user = AddClass("User");
		user.AddMember("Id".As<MemberName>())!.SetType(new SchemaTypes.Int());

		SchemaClass account = AddClass("Account");
		SchemaMember owners = account.AddMember("Owners".As<MemberName>())!;
		owners.SetType(new SchemaTypes.Array() { ElementType = new SchemaTypes.Object() { ClassName = user.Name } });
		harness.Editor.EditClass(account);

		harness.Click("memberOwners/KeySelector");
		harness.Click("key-option/Id");

		Assert.AreEqual("Id", ((SchemaTypes.Array)owners.Type).Key.ToString());
	}

	[TestMethod]
	public void ChoosingNoArrayKeyClearsIt()
	{
		SchemaClass user = AddClass("User");
		user.AddMember("Id".As<MemberName>())!.SetType(new SchemaTypes.Int());

		SchemaClass account = AddClass("Account");
		SchemaMember owners = account.AddMember("Owners".As<MemberName>())!;
		SchemaTypes.Array array = new() { ElementType = new SchemaTypes.Object() { ClassName = user.Name }, Key = "Id".As<MemberName>() };
		owners.SetType(array);
		harness.Editor.EditClass(account);

		harness.Click("memberOwners/KeySelector");
		harness.Click("key-option/<none>");

		Assert.AreEqual(string.Empty, array.Key.ToString());
	}

	/// <summary>
	/// A member's description is folded away until its arrow is opened, so that a class with a
	/// dozen members is still a grid rather than a wall of text boxes.
	/// </summary>
	[TestMethod]
	public void AMemberDescriptionIsHiddenUntilItsRowIsOpened()
	{
		SchemaClass user = AddClass("User");
		user.AddMember("Id".As<MemberName>());
		harness.Editor.EditClass(user);
		harness.App.Step(2);

		Assert.IsFalse(
			harness.App.Probe.KnownNames.Any(name => name.Contains("field/MemberDescription", StringComparison.Ordinal)),
			"The description editor was drawn before the row was opened.");
	}

	[TestMethod]
	public void OpeningAMemberRowShowsItsDescriptionEditor()
	{
		SchemaClass user = AddClass("User");
		user.AddMember("Id".As<MemberName>());
		harness.Editor.EditClass(user);

		harness.Click("memberId/ToggleDescription");

		Assert.IsTrue(harness.App.Probe.Matches("field/MemberDescriptionUser.Id").Count > 0, "The description editor was not drawn.");
	}

	/// <summary>
	/// Which rows are open is remembered rather than reset per frame, so the arrow closes what it
	/// opened.
	/// </summary>
	[TestMethod]
	public void ClosingAMemberRowHidesItsDescriptionEditorAgain()
	{
		SchemaClass user = AddClass("User");
		user.AddMember("Id".As<MemberName>());
		harness.Editor.EditClass(user);
		harness.Click("memberId/ToggleDescription");

		harness.Click("memberId/ToggleDescription");

		Assert.IsFalse(
			harness.IsOnScreen("field/MemberDescriptionUser.Id"),
			"The description editor was still being drawn after the row was closed.");
	}

	[TestMethod]
	public void EditingAMemberDescriptionRecordsIt()
	{
		SchemaClass user = AddClass("User");
		SchemaMember id = user.AddMember("Id".As<MemberName>())!;
		harness.Editor.EditClass(user);
		harness.Click("memberId/ToggleDescription");

		harness.TypeInto("field/MemberDescriptionUser.Id", "What identifies the user.");
		harness.Click("memberId/field/Name");

		Assert.AreEqual("What identifies the user.", id.Description.ToString());
	}
}
