// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp;

/// <summary>
/// How a target spells a type this generator cannot invent, and where that spelling comes from.
/// </summary>
/// <remarks>
/// The two travel together because they are one fact. A generated file naming
/// <c>holo::Vector3</c> without including the header that declares it does not compile, and
/// nothing but whoever supplied the name knows which header that is.
/// </remarks>
/// <param name="Name">The type's name, qualified as the target writes it.</param>
/// <param name="Include">
/// The header that declares it. Written with angle brackets for a system header and quoted
/// otherwise, following the convention the AST's imports already use; empty for a type that needs
/// no include.
/// </param>
public sealed record CppTypeSpelling(string Name, string Include)
{
	/// <summary>
	/// Gets the spelling of a type the standard library already provides.
	/// </summary>
	/// <param name="name">The type's name, including its <c>std::</c> qualification.</param>
	/// <param name="header">The standard header, without its angle brackets.</param>
	/// <returns>The spelling.</returns>
	public static CppTypeSpelling Standard(string name, string header) => new(name, $"<{header}>");
}
