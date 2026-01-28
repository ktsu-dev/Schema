// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.SchemaEditor;

internal sealed class TreeSchema(SchemaEditor schemaEditor)
{
	private TreeEnum TreeEnum { get; } = new(schemaEditor);
	private TreeClass TreeClass { get; } = new(schemaEditor);
	private TreeDataSource TreeDataSource { get; } = new(schemaEditor);

	internal void Show()
	{
		TreeEnum.Show();
		TreeClass.Show();
		TreeDataSource.Show();
	}
}
