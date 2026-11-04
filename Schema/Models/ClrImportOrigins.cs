// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// The CLR type each declaration imported into a schema was read from, keyed by the kind of
/// declaration and its name.
/// </summary>
/// <remarks>
/// A schema names a declaration by the type's simple name, so <c>A.Item</c> and <c>B.Item</c>
/// both ask for <c>Item</c>. Without a record of which type took the name first, a member typed
/// <c>B.Item</c> was silently given the class built from <c>A.Item</c>, members and all, and the
/// schema described a model that does not exist. Held beside the schema rather than in it, because
/// the origin is a fact about an import and not something a schema file says, and weakly, so it
/// goes when the schema does. A declaration with no recorded origin was written by hand and is
/// still reused by name, as it always was.
/// </remarks>
internal static class ClrImportOrigins
{
	private static readonly ConditionalWeakTable<Schema, Dictionary<(string Kind, string Name), Type>> Origins = [];

	/// <summary>
	/// Refuses <paramref name="type"/> when a different type has already been imported under the
	/// same kind and name.
	/// </summary>
	/// <exception cref="InvalidOperationException">Another type already holds the name.</exception>
	internal static void EnsureNotClaimedByAnother(Schema schema, string kind, string name, Type type)
	{
		if (Origins.TryGetValue(schema, out Dictionary<(string Kind, string Name), Type>? origins) &&
			origins.TryGetValue((kind, name), out Type? origin) &&
			origin != type)
		{
			throw new InvalidOperationException(
				$"Both '{Describe(origin)}' and '{Describe(type)}' would be imported as the schema {kind} '{name}'. " +
				"A schema names a declaration by the type's simple name alone, so only one of them can have it.");
		}
	}

	/// <summary>Records that <paramref name="type"/> was imported as the named declaration.</summary>
	internal static void Claim(Schema schema, string kind, string name, Type type) =>
		Origins.GetOrCreateValue(schema)[(kind, name)] = type;

	/// <summary>The name a message uses for a type.</summary>
	internal static string Describe(Type type) => type.FullName ?? type.Name;
}
