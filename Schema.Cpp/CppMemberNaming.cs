// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp;

/// <summary>
/// How a target spells the name of a member, function or parameter.
/// </summary>
/// <remarks>
/// Only these. A type keeps the schema's spelling either way, because a type name is the same word
/// in the schema and in the header, and rewriting it would leave a reader translating between the
/// two.
/// </remarks>
public enum CppMemberNaming
{
	/// <summary>
	/// <c>body_kind</c> for a schema's <c>BodyKind</c>. The standard library's own convention, and
	/// the default.
	/// </summary>
	SnakeCase,

	/// <summary>
	/// The name exactly as the schema writes it, for a target whose convention already matches.
	/// </summary>
	AsWritten,
}
