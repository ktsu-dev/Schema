// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Generation;

using System.Collections.Frozen;

/// <summary>
/// The words C# has taken, and what to write instead when a schema names one.
/// </summary>
/// <remarks>
/// A schema names its elements in its own vocabulary and has no reason to know this list, so a
/// class called <c>Object</c> or a member called <c>Params</c> is an ordinary thing for someone to
/// write. Unlike C++, C# has somewhere to put it: <c>@</c> makes any keyword an identifier, and the
/// <c>@</c> is source syntax rather than part of the name, so the compiled type is still called
/// <c>Object</c> and <see cref="Models.ClrTypeImporter"/> reads back the name the schema wrote.
/// That is why this escapes where the C++ generator refuses — the difference is in the languages,
/// not in the two generators' opinions.
/// <para>
/// Reserved keywords only. A contextual keyword — <c>value</c>, <c>record</c>, <c>async</c> — is an
/// identifier everywhere it is not in the one position that makes it a keyword, and a property
/// called <c>Value</c> is never in that position.
/// </para>
/// </remarks>
internal static class CSharpKeywords
{
	/// <summary>
	/// The reserved keywords of C#.
	/// </summary>
	private static readonly FrozenSet<string> Reserved = new[]
	{
		"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
		"class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
		"enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
		"foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
		"long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
		"private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
		"sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
		"try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
		"void", "volatile", "while",
	}.ToFrozenSet(StringComparer.Ordinal);

	/// <summary>
	/// Writes a schema name as a C# identifier.
	/// </summary>
	/// <param name="name">The name as the schema writes it.</param>
	/// <returns>The name, escaped when C# has taken it.</returns>
	/// <remarks>
	/// Every place a schema name becomes an identifier goes through this, declaration and use
	/// alike: escaping a declaration and not its uses produces a file that does not compile, which
	/// is the same outcome as not escaping at all with more steps.
	/// </remarks>
	public static string Identifier(string name) => Reserved.Contains(name) ? $"@{name}" : name;
}
