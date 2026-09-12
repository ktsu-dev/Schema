// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Runtime;

/// <summary>
/// The CLR representation of the schema's <c>Optional</c>: a value that may be absent.
/// </summary>
/// <remarks>
/// <c>T?</c> would be the idiomatic spelling and is not usable here, because it means two different
/// things. Over a value type it is <see cref="Nullable{T}"/>, a type; over a reference type it is an
/// annotation, which is not part of the type at all and cannot be read back off a
/// <see cref="Type"/>. A generator that wrote <c>T?</c> would therefore round-trip an
/// <c>Optional&lt;Int&gt;</c> and lose an <c>Optional&lt;Item&gt;</c>, which is the one asymmetry
/// the mapping cannot afford - so this is one type, meaning one thing, whatever it wraps.
/// <para>
/// The importer still reads a <see cref="Nullable{T}"/> as the value it wraps rather than as an
/// optional, because a hand-written <c>int?</c> predates any of this and changing what it imports as
/// would reinterpret schemas nobody edited.
/// </para>
/// </remarks>
/// <typeparam name="T">What is present when anything is.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Naming",
	"CA1716:Identifiers should not match keywords",
	Justification = "The schema type is named Optional, and a runtime type standing for one is named after it. Renaming here would leave the two vocabularies disagreeing.")]
public readonly record struct Optional<T>
{
	private Optional(bool present, T? value)
	{
		HasValue = present;
		StoredValue = value;
	}

	/// <summary>
	/// Gets a value indicating whether there is a value.
	/// </summary>
	public bool HasValue { get; }

	/// <summary>
	/// Gets the value.
	/// </summary>
	/// <exception cref="InvalidOperationException">There is no value.</exception>
	public T Value => HasValue
		? StoredValue!
		: throw new InvalidOperationException("The optional is empty, so it has no value.");

	/// <summary>
	/// Gets the absent one, which is also what a default-constructed instance is.
	/// </summary>
	public static Optional<T> None => default;

	private T? StoredValue { get; }

	/// <summary>
	/// Makes one carrying a value.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>An optional holding it.</returns>
	public static Optional<T> Some(T value) => new(true, value);

	/// <summary>
	/// Gets the value, without throwing when there is none.
	/// </summary>
	/// <param name="value">The value, when there is one.</param>
	/// <returns><see langword="true"/> when there is a value.</returns>
	public bool TryGetValue(out T? value)
	{
		value = StoredValue;
		return HasValue;
	}
}
