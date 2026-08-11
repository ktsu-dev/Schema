// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Types;
/// <summary>
/// Contains various schema types used in the application.
/// </summary>
public static class SchemaTypes
{
	/// <summary>
	/// Gets the type qualifier string.
	/// </summary>
	public static string TypeQualifier => $"{typeof(SchemaTypes).FullName}+";
}
