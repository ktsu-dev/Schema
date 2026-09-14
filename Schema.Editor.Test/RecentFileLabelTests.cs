// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor.Test;

/// <summary>
/// The label a recent file is offered under in the File menu.
/// </summary>
/// <remarks>
/// <para>
/// Offering the whole path was not a display preference that happened to be verbose. A menu is as
/// wide as its widest label, and a submenu that will not fit beside the menu that opened it is
/// placed on top of that menu instead - which puts the item that opened it underneath the submenu
/// rather than under the pointer, so ImGui takes the pointer to have left and closes the submenu
/// on the frame after it opened. Open Recent was therefore unusable for anyone whose schemas lived
/// deep enough, and how deep that was depended on the window's width.
/// </para>
/// <para>
/// Frameless, so it runs on every platform. That matters here more than usual: this was found by
/// two tests failing on macOS alone, for no reason macOS was responsible for - its temporary
/// directory is simply 44 characters deeper than <c>/tmp</c>.
/// </para>
/// </remarks>
[TestClass]
public sealed class RecentFileLabelTests
{
	[TestMethod]
	public void APathThatFitsIsOfferedWhole()
	{
		string path = new('x', SchemaEditor.MaxRecentFileLabelLength);

		Assert.AreEqual(path, SchemaEditor.ElideRecentFileLabel(path));
	}

	[TestMethod]
	public void ALongerPathIsCutToTheSameWidth()
	{
		string label = SchemaEditor.ElideRecentFileLabel(new string('x', SchemaEditor.MaxRecentFileLabelLength * 4));

		Assert.AreEqual(SchemaEditor.MaxRecentFileLabelLength, label.Length, "A label past the budget is what makes the menu wider than the window.");
	}

	/// <summary>
	/// The end rather than the beginning: what tells two recent files apart is the file name and
	/// the directory holding it, and the root they share is the part worth losing.
	/// </summary>
	[TestMethod]
	public void WhatIsKeptIsTheEndOfThePath()
	{
		string path = $"/a/very/long/root/that/nobody/needs/to/read/again/and/again/and/again/{new string('d', 20)}/schema.json";

		string label = SchemaEditor.ElideRecentFileLabel(path);

		StringAssert.EndsWith(label, $"{new string('d', 20)}/schema.json", StringComparison.Ordinal);
		StringAssert.StartsWith(label, "…", StringComparison.Ordinal);
	}

	/// <summary>
	/// A file name longer than the whole budget still leaves a label of the right width, rather
	/// than a negative slice or a label that is once again as wide as the path.
	/// </summary>
	[TestMethod]
	public void AFileNameLongerThanTheBudgetIsCutToo()
	{
		string path = $"/schemas/{new string('n', SchemaEditor.MaxRecentFileLabelLength * 2)}.schema.json";

		Assert.AreEqual(SchemaEditor.MaxRecentFileLabelLength, SchemaEditor.ElideRecentFileLabel(path).Length);
	}
}
