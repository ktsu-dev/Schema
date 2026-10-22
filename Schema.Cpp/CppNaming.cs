// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp;

using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// How a schema's names are spelled in C++.
/// </summary>
/// <remarks>
/// A schema writes names the way its own elements are named - <c>BodyKind</c>, <c>LoadTexture</c> -
/// and a target's convention for a member or a function is usually not that. Types keep the
/// schema's spelling, because a type name is the same word in both places and rewriting it would
/// leave the reader translating between the schema and the header.
/// </remarks>
internal static class CppNaming
{
	/// <summary>
	/// Spells a member, function or parameter name.
	/// </summary>
	/// <param name="name">The name as the schema writes it.</param>
	/// <param name="convention">The target's convention.</param>
	/// <returns>The name as the target writes it.</returns>
	/// <param name="kind">What the schema calls the element, for a message naming it.</param>
	public static string Member(string name, CppMemberNaming convention, string kind) =>
		CppKeywords.Check(
			convention == CppMemberNaming.SnakeCase ? SnakeCase(name) : name,
			kind,
			name);

	/// <summary>
	/// Refuses names that are distinct in the schema but would be spelled alike in C++.
	/// </summary>
	/// <remarks>
	/// The schema keeps names unique as written, and a convention that rewrites them can still map
	/// two onto one spelling: <c>PlayerID</c> and <c>PlayerId</c> are both <c>player_id</c>, as are
	/// <c>fooBar</c> and <c>foo_bar</c>. Emitted, that is two declarations of one name, which the
	/// compiler reports at the duplicate rather than at either schema element.
	/// </remarks>
	/// <param name="names">The names as the schema writes them, all in one scope.</param>
	/// <param name="convention">The target's convention.</param>
	/// <param name="kind">What the schema calls the elements, for the message.</param>
	/// <param name="owner">What the elements belong to, for the message.</param>
	/// <exception cref="CppGenerationException">Two names share a spelling.</exception>
	public static void EnsureDistinct(IEnumerable<string> names, CppMemberNaming convention, string kind, string owner)
	{
		Dictionary<string, string> seen = new(StringComparer.Ordinal);

		foreach (string name in names)
		{
			string spelled = convention == CppMemberNaming.SnakeCase ? SnakeCase(name) : name;

			if (seen.TryGetValue(spelled, out string? first) && !string.Equals(first, name, StringComparison.Ordinal))
			{
				throw new CppGenerationException(
					$"{kind}s '{first}' and '{name}' of '{owner}' are both spelled '{spelled}' in C++, so a "
					+ "header declaring them would declare one name twice. Rename one of them.");
			}

			seen.TryAdd(spelled, name);
		}
	}

	/// <summary>
	/// Spells a type's name, which is the schema's own spelling.
	/// </summary>
	/// <param name="name">The name as the schema writes it.</param>
	/// <param name="kind">What the schema calls the element, for a message naming it.</param>
	/// <returns>The name as the target writes it.</returns>
	/// <remarks>
	/// A type keeps its name because it is the same word in both places, so the only thing to do
	/// here is refuse the handful of words C++ will not accept as one.
	/// </remarks>
	public static string Type(string name, string kind) => CppKeywords.Check(name, kind, name);

	/// <summary>
	/// Turns a name written in the schema's style into <c>snake_case</c>.
	/// </summary>
	/// <remarks>
	/// A boundary is a lower-to-upper transition, or an upper followed by a lower where an
	/// acronym ends: <c>BodyKind</c> is <c>body_kind</c> and <c>LoadHDRTexture</c> is
	/// <c>load_hdr_texture</c> rather than <c>load_h_d_r_texture</c>. A name that already has
	/// underscores keeps them and gains none.
	/// </remarks>
	/// <param name="name">The name.</param>
	/// <returns>The name in snake case.</returns>
	public static string SnakeCase(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return string.Empty;
		}

		StringBuilder spelled = new(name.Length + 4);

		for (int index = 0; index < name.Length; index++)
		{
			char current = name[index];

			if (char.IsUpper(current) && index > 0 && NeedsSeparator(name, index))
			{
				spelled.Append('_');
			}

			spelled.Append(char.ToLower(current, CultureInfo.InvariantCulture));
		}

		return spelled.ToString();
	}

	/// <summary>
	/// Says whether an upper-case character at this position starts a new word.
	/// </summary>
	/// <remarks>
	/// It does when what precedes it is not upper case, and also when it is the last letter of a
	/// run of capitals followed by a lower-case one - which is where an acronym ends and the next
	/// word begins.
	/// </remarks>
	private static bool NeedsSeparator(string name, int index)
	{
		if (name[index - 1] == '_')
		{
			return false;
		}

		bool afterLower = !char.IsUpper(name[index - 1]);
		bool endsAnAcronym = index + 1 < name.Length && !char.IsUpper(name[index + 1]) && name[index + 1] != '_';

		return afterLower || endsAnAcronym;
	}
}
