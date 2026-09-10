// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Which way a parameter's value travels.
/// </summary>
/// <remarks>
/// This is what a schema says instead of const-ness. Const is a property of one language's spelling
/// of a parameter; direction is a property of the call, and every language can spell it. A C++
/// generator emits <see cref="In"/> as a const reference or a by-value copy and <see cref="Out"/>
/// as a mutable one; a C# generator emits <c>in</c>, <c>out</c> and <c>ref</c>. Neither decision
/// belongs in the schema.
/// </remarks>
/// <remarks>
/// Written by name rather than by ordinal, so inserting a direction cannot silently change what an
/// existing schema means.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<ParameterDirection>))]
public enum ParameterDirection
{
	/// <summary>
	/// The caller supplies the value and the callee does not modify it. The default.
	/// </summary>
	In,

	/// <summary>
	/// The callee produces the value; whatever the caller passed in is not read.
	/// </summary>
	Out,

	/// <summary>
	/// The caller supplies a value and the callee modifies it in place.
	/// </summary>
	InOut,
}
