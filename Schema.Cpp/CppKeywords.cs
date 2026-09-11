// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp;

using System.Collections.Frozen;

/// <summary>
/// The words a generated name cannot be, because C++ has already taken them.
/// </summary>
/// <remarks>
/// A schema names its elements in its own vocabulary and has no reason to know this list, so a
/// member called <c>Operator</c> or <c>Delete</c> is an ordinary thing for someone to write — and
/// under <see cref="CppMemberNaming.SnakeCase"/> it is spelled <c>operator</c> or <c>delete</c> and
/// the header does not compile. C++ has no escape for this the way C# has <c>@</c>, so the name has
/// to change; refusing it by name is how the generator says so at the schema element rather than at
/// a compiler error a reader has to work backwards from.
/// <para>
/// Keywords and alternative tokens, which are the words the standard reserves. Not macros: the ones
/// a program is likely to collide with — <c>NULL</c>, <c>TRUE</c>, <c>min</c>, <c>max</c> — come
/// from headers rather than from the language, so which are defined depends on what a translation
/// unit included, and a generator guessing at that would refuse names that are perfectly good in
/// the file it is actually writing.
/// </para>
/// </remarks>
internal static class CppKeywords
{
	/// <summary>
	/// The keywords and alternative tokens of C++20.
	/// </summary>
	private static readonly FrozenSet<string> Reserved = new[]
	{
		"alignas", "alignof", "and", "and_eq", "asm", "atomic_cancel", "atomic_commit",
		"atomic_noexcept", "auto", "bitand", "bitor", "bool", "break", "case", "catch", "char",
		"char8_t", "char16_t", "char32_t", "class", "co_await", "co_return", "co_yield", "compl",
		"concept", "const", "const_cast", "consteval", "constexpr", "constinit", "continue",
		"decltype", "default", "delete", "do", "double", "dynamic_cast", "else", "enum", "explicit",
		"export", "extern", "false", "float", "for", "friend", "goto", "if", "inline", "int", "long",
		"mutable", "namespace", "new", "noexcept", "not", "not_eq", "nullptr", "operator", "or",
		"or_eq", "private", "protected", "public", "reflexpr", "register", "reinterpret_cast",
		"requires", "return", "short", "signed", "sizeof", "static", "static_assert", "static_cast",
		"struct", "switch", "synchronized", "template", "this", "thread_local", "throw", "true",
		"try", "typedef", "typeid", "typename", "union", "unsigned", "using", "virtual", "void",
		"volatile", "wchar_t", "while", "xor", "xor_eq",
	}.ToFrozenSet(StringComparer.Ordinal);

	/// <summary>
	/// Reports whether a spelled name is one C++ has taken.
	/// </summary>
	/// <param name="spelled">The name as it would appear in the header.</param>
	/// <returns><see langword="true"/> when the header would not compile with that name in it.</returns>
	/// <remarks>
	/// Case-sensitive, because the comparison is against the name the header will actually carry
	/// rather than against the name the schema wrote. <c>Delete</c> is a fine name for a type, which
	/// keeps its spelling; it is only a problem once a convention has lower-cased it.
	/// </remarks>
	public static bool Contains(string spelled) => Reserved.Contains(spelled);

	/// <summary>
	/// Refuses a name C++ has taken, and returns it otherwise.
	/// </summary>
	/// <param name="spelled">The name as it would appear in the header.</param>
	/// <param name="kind">What the schema calls the element, for the message.</param>
	/// <param name="written">The name as the schema wrote it, when a convention changed it.</param>
	/// <returns>The name.</returns>
	/// <exception cref="CppGenerationException">The name is a C++ keyword.</exception>
	public static string Check(string spelled, string kind, string written)
	{
		if (!Contains(spelled))
		{
			return spelled;
		}

		string became = string.Equals(spelled, written, StringComparison.Ordinal)
			? string.Empty
			: $" (written '{written}')";

		throw new CppGenerationException(
			$"{kind} '{spelled}'{became} is a C++ keyword, so a header declaring it would not "
			+ "compile. C++ has no way to escape one; rename the schema element.");
	}
}
