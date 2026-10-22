// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

/// <summary>
/// What a <see cref="SchemaChange"/> did to the schema.
/// </summary>
public enum SchemaChangeKind
{
	/// <summary>A class was renamed.</summary>
	ClassRenamed,

	/// <summary>A class was removed.</summary>
	ClassRemoved,

	/// <summary>A class was added.</summary>
	ClassAdded,

	/// <summary>An enum was renamed.</summary>
	EnumRenamed,

	/// <summary>An enum was removed.</summary>
	EnumRemoved,

	/// <summary>An enum was added.</summary>
	EnumAdded,

	/// <summary>A member of a class was renamed.</summary>
	MemberRenamed,

	/// <summary>A member of a class was removed.</summary>
	MemberRemoved,

	/// <summary>A member was added to a class.</summary>
	MemberAdded,

	/// <summary>A member's type changed the shape of the data it holds.</summary>
	MemberRetyped,

	/// <summary>A value of an enum was renamed.</summary>
	EnumValueRenamed,

	/// <summary>A value of an enum was removed.</summary>
	EnumValueRemoved,

	/// <summary>A value was added to an enum.</summary>
	EnumValueAdded,
}
