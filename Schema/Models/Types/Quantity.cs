// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Types;

using System.Text.Json.Serialization;

using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;

/// <summary>
/// A physical quantity from <c>ktsu.Semantics.Quantities</c>.
/// </summary>
/// <remarks>
/// <para>
/// A member that holds a mass says <c>Quantity(Mass)</c>. It does not declare a semantic type to
/// say it, and it does not name a unit to say it either: the kilograms are how the stored number
/// is read, which is a fact about the field, while the mass is what the value <i>is</i>.
/// </para>
/// <para>
/// This is the difference from <see cref="Semantic"/>, and both are wanted. A semantic type is
/// how a schema says that its own two numbers are different things - an entity id is not a texture
/// id - and nothing outside the schema has heard of either. A quantity is the opposite: the
/// vocabulary is shared, 212 names that both generators already emit, so naming one here reaches
/// a type the target already has rather than a copy this schema asked for.
/// </para>
/// <para>
/// It carries no <c>elementType</c> and no component count, because the name already fixes them.
/// <c>Velocity3D</c> is three components of a velocity; there is no <c>Velocity3D</c> of anything
/// else, which is what makes it a name rather than a shape.
/// </para>
/// </remarks>
public class Quantity : BaseType
{
	/// <summary>
	/// Gets or sets the quantity's name.
	/// </summary>
	public QuantityName QuantityName { get; set; } = new();

	/// <summary>
	/// Gets or sets the number a value of this quantity is kept in.
	/// </summary>
	/// <remarks>
	/// Every quantity in the vocabulary is generic over its storage - <c>Mass&lt;float&gt;</c>,
	/// <c>Mass&lt;double&gt;</c> - so a name alone is not yet a type a generator can write. This
	/// is the rest of it, and it is a property of the member rather than of the program because
	/// one field being a double where the rest are floats is an ordinary thing for a schema to
	/// say.
	/// <para>
	/// It defaults to <see cref="Float"/> and is omitted from the file when it is, the same
	/// arrangement <see cref="Vector.ElementType"/> has and for the same reason: a non-nullable
	/// property has no ignore condition that means "the same as saying nothing".
	/// </para>
	/// </remarks>
	public BaseType Storage { get; set; } = new Float();

	/// <summary>
	/// Gets what the registry knows about this quantity, or <see langword="null"/> when the name
	/// resolves to nothing.
	/// </summary>
	/// <remarks>
	/// Null is how an unresolved name reaches a caller, the same way an unresolved semantic type
	/// does: reported once by validation, and treated everywhere else as already reported.
	/// </remarks>
	[JsonIgnore]
	public QuantityRegistry.QuantityInfo? Resolved =>
		QuantityRegistry.TryResolve(QuantityName, out QuantityRegistry.QuantityInfo? quantity) ? quantity : null;

	/// <inheritdoc/>
	public override void AssociateWith(SchemaMember schemaMember)
	{
		base.AssociateWith(schemaMember);
		Storage.AssociateWith(schemaMember);
	}

	/// <inheritdoc/>
	public override void AssociateWith(Schema? schema)
	{
		base.AssociateWith(schema);
		Storage.AssociateWith(schema);
	}

	/// <inheritdoc/>
	public override string ToString() => QuantityName;

	/// <inheritdoc />
	protected override bool EqualsCore(BaseType other) =>
		other is Quantity otherQuantity
			&& string.Equals(QuantityName, otherQuantity.QuantityName, StringComparison.Ordinal)
			&& Storage.Equals(otherQuantity.Storage);

	/// <inheritdoc />
	protected override int GetHashCodeCore() =>
		HashCode.Combine(StringComparer.Ordinal.GetHashCode(QuantityName.ToString()), Storage);
}
