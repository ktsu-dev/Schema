// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Text.Json.Serialization;

/// <summary>
/// What a recorded rename renamed.
/// </summary>
/// <remarks>
/// Written by name rather than by ordinal, so adding a kind cannot change what an existing file means.
/// Only the four names data is bound through are recorded: a class and an enum, because a member and
/// a value are identified by the type they belong to, and a member and an enum value, because those
/// are the names that appear in a data file. Renaming an interface, a semantic type, a data source or
/// a code generator changes nothing any data file holds, so none of them is recorded.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<SchemaRenameKind>))]
public enum SchemaRenameKind
{
	/// <summary>
	/// A class was renamed.
	/// </summary>
	Class,

	/// <summary>
	/// An enum was renamed.
	/// </summary>
	Enum,

	/// <summary>
	/// A member of a class was renamed. <see cref="SchemaRename.Owner"/> names the class.
	/// </summary>
	Member,

	/// <summary>
	/// A value of an enum was renamed. <see cref="SchemaRename.Owner"/> names the enum.
	/// </summary>
	EnumValue,
}
