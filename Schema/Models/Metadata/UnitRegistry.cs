// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Metadata;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ktsu.Semantics.Quantities;
using ktsu.Semantics.Quantities.Units;

/// <summary>
/// Resolves the unit text written in a schema to a unit from
/// <c>ktsu.Semantics.Quantities</c>.
/// </summary>
/// <remarks>
/// The registry is built by reflecting over every <see cref="IUnit"/> in the quantities
/// assembly rather than being a list maintained here. That is deliberate: a hand-maintained
/// copy would be a second source of truth for conversion factors, and would silently fall
/// behind every unit added upstream. The cost is one reflection pass, done once.
/// </remarks>
public static class UnitRegistry
{
	private static readonly Lazy<Registry> Lookup = new(Build, isThreadSafe: true);

	private sealed class Registry
	{
		// Ordinal, not OrdinalIgnoreCase. Unit names are PascalCase and unique, so
		// case-insensitivity buys nothing -- and it actively breaks disambiguation:
		// "rad" would match the *name* Rad (radiation absorbed dose) before the
		// symbol lookup ran, silently resolving to a different dimension instead of
		// reporting that the symbol is shared with Radian. Found by running it.
		public Dictionary<string, IUnit> ByName { get; } = new(StringComparer.Ordinal);

		// A list, not a single unit: symbols are not unique. Ambiguity is reported to the
		// caller rather than resolved by picking one, because picking one silently would
		// mean a schema saying "rad" got radians or radiation dose depending on assembly
		// load order.
		public Dictionary<string, List<IUnit>> BySymbol { get; } = new(StringComparer.Ordinal);
	}

	private static Registry Build()
	{
		Registry registry = new();

		foreach (Type type in typeof(Meter).Assembly.GetTypes())
		{
			if (type.IsAbstract || !type.IsClass || !typeof(IUnit).IsAssignableFrom(type))
			{
				continue;
			}

			// Every generated unit is a record with a public parameterless constructor.
			// One that is not is not a unit this registry can offer.
			if (type.GetConstructor(Type.EmptyTypes) is null)
			{
				continue;
			}

			if (Activator.CreateInstance(type) is not IUnit unit)
			{
				continue;
			}

			registry.ByName[unit.Name] = unit;

			if (!registry.BySymbol.TryGetValue(unit.Symbol, out List<IUnit>? sharing))
			{
				sharing = [];
				registry.BySymbol[unit.Symbol] = sharing;
			}

			sharing.Add(unit);
		}

		return registry;
	}

	/// <summary>
	/// Gets every unit the registry knows, ordered by name.
	/// </summary>
	/// <remarks>
	/// Exposed so an editor can offer a unit picker without reflecting over the quantities
	/// assembly itself.
	/// </remarks>
	public static ReadOnlyCollection<IUnit> All =>
		Lookup.Value.ByName.Values.OrderBy(unit => unit.Name, StringComparer.Ordinal).ToList().AsReadOnly();

	/// <summary>
	/// Resolves unit text to a unit.
	/// </summary>
	/// <param name="text">A unit symbol (<c>m/s</c>) or unit name (<c>MeterPerSecond</c>).</param>
	/// <param name="unit">The resolved unit, or null when the text does not resolve.</param>
	/// <param name="error">
	/// Why it did not resolve, phrased for the person who wrote the schema. Empty on success.
	/// </param>
	/// <returns><see langword="true"/> when the text resolved to exactly one unit.</returns>
	/// <remarks>
	/// Names are tried before symbols. Every name is unique, so a name always resolves to one
	/// unit; a symbol may not, and the ambiguous case is what names exist to escape.
	/// </remarks>
	public static bool TryResolve(string? text, out IUnit? unit, out string error)
	{
		unit = null;
		error = string.Empty;

		if (string.IsNullOrWhiteSpace(text))
		{
			error = "no unit given";
			return false;
		}

		Registry registry = Lookup.Value;

		if (registry.ByName.TryGetValue(text, out IUnit? named))
		{
			unit = named;
			return true;
		}

		if (!registry.BySymbol.TryGetValue(text, out List<IUnit>? sharing))
		{
			error = $"'{text}' is not a known unit symbol or unit name";
			return false;
		}

		if (sharing.Count > 1)
		{
			string candidates = string.Join(", ", sharing.Select(candidate => candidate.Name).OrderBy(name => name, StringComparer.Ordinal));
			error = $"'{text}' is ambiguous: it is the symbol of {candidates}. Write the unit's name instead of its symbol.";
			return false;
		}

		unit = sharing[0];
		return true;
	}

	/// <summary>
	/// Resolves unit text to a unit, or throws.
	/// </summary>
	/// <param name="text">A unit symbol or unit name.</param>
	/// <returns>The resolved unit.</returns>
	/// <exception cref="ArgumentException">The text is not a known, unambiguous unit.</exception>
	public static IUnit Resolve(string? text) =>
		TryResolve(text, out IUnit? unit, out string error) && unit is not null
			? unit
			: throw new ArgumentException(error, nameof(text));
}
