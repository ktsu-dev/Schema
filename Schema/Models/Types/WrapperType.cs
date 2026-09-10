// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Types;

/// <summary>
/// Base for a type that wraps exactly one other type.
/// </summary>
/// <remarks>
/// <see cref="Span"/>, <see cref="Handle"/>, <see cref="Result"/> and <see cref="Optional"/> differ
/// only in what wrapping means, not in how they are built: each holds one element type, has to pass
/// its schema association down to it, and compares by it. That is what lives here, so adding
/// another wrapper is a declaration rather than a fourth copy of the same three overrides.
/// </remarks>
public abstract class WrapperType : BaseType
{
	/// <summary>
	/// Gets the type being wrapped.
	/// </summary>
	public BaseType ElementType { get; init; } = new None();

	/// <inheritdoc />
	/// <remarks>
	/// Passes the association down, so a class named inside the wrapper still resolves.
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
	/// The wrapper kind is already compared by <see cref="BaseType.Equals(BaseType)"/>, which
	/// tests the runtime type first, so only the element needs comparing here.
	/// </remarks>
	protected override bool EqualsCore(BaseType other) =>
		other is WrapperType otherWrapper && ElementType.Equals(otherWrapper.ElementType);

	/// <inheritdoc />
	protected override int GetHashCodeCore() => ElementType.GetHashCode();

	/// <inheritdoc />
	public override string ToString() => $"{GetType().Name}<{ElementType}>";
}
