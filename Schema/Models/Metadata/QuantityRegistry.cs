// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Metadata;

using System.Collections.ObjectModel;
using System.Reflection;

using ktsu.Semantics.Quantities;

/// <summary>
/// The physical quantities a schema may name, read out of <c>ktsu.Semantics.Quantities</c>.
/// </summary>
/// <remarks>
/// <para>
/// The same arrangement as <see cref="UnitRegistry"/>, and for the same reason: the vocabulary is
/// that library's and keeping a list here would fall behind it. A quantity added there is nameable
/// here with no edit in this repository.
/// </para>
/// <para>
/// <b>A quantity is not a unit, and that distinction is the point.</b> A member holds a
/// <c>Mass</c>; the kilograms are how its stored number is read. Naming the type for the unit says
/// the presentation twice and makes the wrong copy load-bearing - which is what
/// <c>Semantic(Kilograms)</c> did before this existed, and why a schema that wanted a mass had to
/// declare one.
/// </para>
/// <para>
/// Every quantity is generic over its storage, so what is registered is the open type
/// <c>Mass&lt;&gt;</c>. <see cref="Closed"/> closes it over a storage type when a generator needs
/// a name to write.
/// </para>
/// </remarks>
public static class QuantityRegistry
{
	/// <summary>
	/// What the registry knows about one quantity.
	/// </summary>
	/// <param name="Name">The quantity's name, without its arity.</param>
	/// <param name="Definition">The open generic type, <c>Mass&lt;&gt;</c>.</param>
	/// <param name="Components">How many components a value has: 0 for a magnitude, 1 for a signed
	/// scalar, 2 to 4 for a vector.</param>
	/// <param name="Dimension">The eight exponents, from the quantity itself.</param>
	public sealed record QuantityInfo(string Name, Type Definition, int Components, DimensionInfo Dimension);

	/// <summary>
	/// How far a chain of widenings is followed before it is treated as one that does not end.
	/// </summary>
	private const int WideningLimit = 8;

	private static readonly Lazy<ReadOnlyDictionary<string, QuantityInfo>> Registry = new(Build);

	/// <summary>
	/// Every quantity that can be named, in name order.
	/// </summary>
	public static ReadOnlyCollection<QuantityInfo> All =>
		new([.. Registry.Value.Values.OrderBy(quantity => quantity.Name, StringComparer.Ordinal)]);

	/// <summary>
	/// Resolves a name to the quantity it refers to.
	/// </summary>
	/// <param name="name">The quantity's name, such as <c>Mass</c> or <c>Velocity3D</c>.</param>
	/// <param name="quantity">The quantity, when the name resolves.</param>
	/// <returns><see langword="true"/> when the name is one this vocabulary has.</returns>
	public static bool TryResolve(string? name, out QuantityInfo? quantity)
	{
		quantity = null;

		return !string.IsNullOrWhiteSpace(name) && Registry.Value.TryGetValue(name, out quantity);
	}

	/// <summary>
	/// Closes a quantity over the storage its values are kept in.
	/// </summary>
	/// <param name="quantity">The quantity.</param>
	/// <param name="storage">The storage type, such as <see cref="float"/>.</param>
	/// <returns>The closed type, <c>Mass&lt;float&gt;</c>.</returns>
	public static Type Closed(QuantityInfo quantity, Type storage)
	{
		Ensure.NotNull(quantity);

		return quantity.Definition.MakeGenericType(storage);
	}

	/// <summary>
	/// Reads the vocabulary out of the assembly that defines it.
	/// </summary>
	/// <remarks>
	/// A quantity is a generic <c>readonly record struct</c> implementing one of the five vector
	/// interfaces. The arity of that interface is what says how many components a value has, which
	/// is the one thing a name alone does not.
	/// </remarks>
	private static ReadOnlyDictionary<string, QuantityInfo> Build()
	{
		Dictionary<string, QuantityInfo> registry = new(StringComparer.Ordinal);

		foreach (Type type in typeof(Mass<>).Assembly.GetTypes())
		{
			if (!type.IsValueType || !type.IsGenericTypeDefinition || type.GetGenericArguments().Length != 1)
			{
				continue;
			}

			if (ComponentsOf(type) is not int components)
			{
				continue;
			}

			string name = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];

			if (DimensionOf(type) is DimensionInfo dimension)
			{
				registry[name] = new QuantityInfo(name, type, components, dimension);
			}
		}

		return new ReadOnlyDictionary<string, QuantityInfo>(registry);
	}

	/// <summary>
	/// How many components a value of this quantity has, or null when it is not a quantity.
	/// </summary>
	/// <remarks>
	/// Named for the question rather than the answer, like <see cref="DimensionOf"/> beside it,
	/// because <c>QuantityInfo.Components</c> is the property a caller reads and a method of the
	/// same name on the enclosing class would shadow it from inside the record.
	/// </remarks>
	private static int? ComponentsOf(Type definition)
	{
		foreach (Type contract in definition.GetInterfaces())
		{
			if (!contract.IsGenericType)
			{
				continue;
			}

			string name = contract.GetGenericTypeDefinition().Name;

			if (name.StartsWith("IVector", StringComparison.Ordinal) &&
				int.TryParse(name.AsSpan(7, 1), out int components))
			{
				return components;
			}
		}

		return null;
	}

	/// <summary>
	/// The eight exponents a quantity carries.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A magnitude carries them directly, through <c>IPhysicalQuantity</c>. A vector form does
	/// not - <c>IVectorN</c> for N above zero declares components and no dimension - so its
	/// dimension is read off what <c>Magnitude()</c> answers with, which is the magnitude form of
	/// the same dimension. That is the relationship the vocabulary is built on rather than an
	/// inference: the sum of the squares of the components has twice a component's dimension and
	/// the square root halves it again.
	/// </para>
	/// <para>
	/// A <b>named overload</b> of a vector form answers neither. <c>Position3D</c> is a
	/// <c>Displacement3D</c> under another name and the vocabulary gives it no <c>Magnitude()</c>
	/// of its own, so the thing to follow is the one relationship it does declare: an overload
	/// widens implicitly to what it is an overload of. Six of the vocabulary's vector forms are
	/// reached only this way, which is the difference between 206 quantities and 212.
	/// </para>
	/// </remarks>
	private static DimensionInfo? DimensionOf(Type definition) => DimensionOfClosed(definition.MakeGenericType(typeof(double)), 0);

	/// <summary>
	/// The dimension of a closed quantity, following the widening chain when it has one.
	/// </summary>
	/// <param name="closed">The closed quantity type.</param>
	/// <param name="depth">How many widenings have been followed, which bounds a chain that loops.</param>
	private static DimensionInfo? DimensionOfClosed(Type closed, int depth)
	{
		if (depth > WideningLimit)
		{
			return null;
		}

		if (Declared(closed) is DimensionInfo declared)
		{
			return declared;
		}

		if (closed.GetMethod("Magnitude", BindingFlags.Public | BindingFlags.Instance)?.ReturnType is Type magnitude &&
			Declared(magnitude) is DimensionInfo measured)
		{
			return measured;
		}

		return Widened(closed) is Type wider ? DimensionOfClosed(wider, depth + 1) : null;
	}

	/// <summary>
	/// What this quantity widens implicitly to, when it is an overload of another.
	/// </summary>
	/// <remarks>
	/// Only a conversion onto another quantity counts. A quantity converts to plenty of things that
	/// are not one, and following a conversion to its storage type would answer with the dimension
	/// of a bare number - which is the one wrong answer that looks like a right one.
	/// </remarks>
	private static Type? Widened(Type closed)
	{
		foreach (MethodInfo conversion in closed.GetMethods(BindingFlags.Public | BindingFlags.Static))
		{
			if (conversion.Name != "op_Implicit" ||
				conversion.GetParameters() is not [ParameterInfo source] ||
				source.ParameterType != closed)
			{
				continue;
			}

			Type target = conversion.ReturnType;

			if (target.IsGenericType &&
				target.Assembly == closed.Assembly &&
				ComponentsOf(target.GetGenericTypeDefinition()) is not null)
			{
				return target;
			}
		}

		return null;
	}

	/// <summary>
	/// The dimension a closed quantity declares, when it declares one.
	/// </summary>
	/// <remarks>
	/// Written as two guards rather than one conditional. <c>property?.PropertyType == typeof(…)</c>
	/// is false when the property is absent, so the compact form never dereferenced a null - but
	/// neither the compiler's null-state analysis nor CodeQL can follow that, and a warning that
	/// has to be reasoned about every time it is read is worth two lines to remove.
	/// </remarks>
	private static DimensionInfo? Declared(Type closed)
	{
		PropertyInfo? property = closed.GetProperty("Dimension", BindingFlags.Public | BindingFlags.Instance);

		if (property is null || property.PropertyType != typeof(DimensionInfo))
		{
			return null;
		}

		return property.GetValue(Activator.CreateInstance(closed)) as DimensionInfo;
	}
}
