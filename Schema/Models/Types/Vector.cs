// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Types;

/// <summary>
/// Represents a vector type.
/// </summary>
/// <remarks>
/// A vector carries what its components are, not just how many there are. A velocity is three
/// metres per second and a position is three metres, and a schema that says only "three floats"
/// leaves the one fact worth knowing about either of them to a comment — which is the same argument
/// <see cref="Semantic"/> makes for a single value, applied to three of them.
/// <para>
/// <see cref="ElementType"/> defaults to <see cref="Float"/>, so a vector that says nothing about
/// its components is what a vector has always been.
/// </para>
/// </remarks>
public abstract class Vector : SystemObject
{
	/// <summary>
	/// Gets what each of the vector's components is.
	/// </summary>
	/// <remarks>
	/// A number, or a semantic type over one. A vector of objects is a collection, which is what
	/// <see cref="Array"/> is for, and validation refuses one here.
	/// </remarks>
	public BaseType ElementType { get; init; } = new Float();

	/// <inheritdoc />
	/// <remarks>
	/// Passes the association down, so a semantic type named as the component still resolves.
	/// </remarks>
	public override void AssociateWith(SchemaMember schemaMember)
	{
		base.AssociateWith(schemaMember);
		ElementType.AssociateWith(schemaMember);
	}

	/// <inheritdoc />
	/// <remarks>
	/// Passes the association down, for the same reason as the member overload.
	/// </remarks>
	public override void AssociateWith(Schema? schema)
	{
		base.AssociateWith(schema);
		ElementType.AssociateWith(schema);
	}

	/// <inheritdoc />
	/// <remarks>
	/// The vector's own kind is already compared by <see cref="BaseType.Equals(BaseType)"/>, which
	/// tests the runtime type first, so only the component needs comparing here.
	/// </remarks>
	protected override bool EqualsCore(BaseType other) =>
		other is Vector otherVector && ElementType.Equals(otherVector.ElementType);

	/// <inheritdoc />
	protected override int GetHashCodeCore() => ElementType.GetHashCode();

	/// <inheritdoc />
	public override string ToString() =>
		ElementType is Float ? GetType().Name : $"{GetType().Name}<{ElementType}>";
}
