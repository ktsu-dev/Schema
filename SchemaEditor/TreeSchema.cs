// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SchemaEditor;

internal sealed class TreeSchema(SchemaEditor schemaEditor)
{
	private TreeEnum TreeEnum { get; } = new(schemaEditor);
	private TreeClass TreeClass { get; } = new(schemaEditor);
	private TreeDataSource TreeDataSource { get; } = new(schemaEditor);
	private TreeCodeGenerator TreeCodeGenerator { get; } = new(schemaEditor);

	internal void Show()
	{
		TreeEnum.Show();
		TreeClass.Show();
		TreeDataSource.Show();
		TreeCodeGenerator.Show();
	}
}
