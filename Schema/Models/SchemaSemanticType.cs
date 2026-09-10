// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Text.Json.Serialization;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Quantities;

/// <summary>
/// A named type that is a distinct type, but is represented as an existing one.
/// </summary>
/// <remarks>
/// An entity id is a number, and so is a texture id, and adding one to the other is nonsense that
/// compiles. A semantic type is how the schema says the two are different things: both are stored
/// as a <see cref="Long"/> and neither is interchangeable with the other or with a bare number.
/// <para>
/// The point is what it refuses. Crossing into or out of the underlying type is always explicit —
/// a bare number never becomes an <c>EntityId</c> by accident, and leaving the type is a named
/// call rather than an implicit conversion. That rule is a property of every semantic type rather
/// than something each declaration restates, for the same reason the interface conventions are.
/// </para>
/// <para>
/// A semantic type may refine another, in which case the narrower widens to the broader implicitly
/// and narrows back explicitly: <c>Weight</c> is a <c>ForceMagnitude</c>, but not every force is a
/// weight.
/// </para>
/// </remarks>
public class SchemaSemanticType : SchemaChild<SemanticTypeName>, ISchemaMetadataCarrier
{
	/// <summary>
	/// Gets the type this one is represented as.
	/// </summary>
	/// <remarks>
	/// <see cref="JsonIncludeAttribute"/> is required because the setter is private, matching
	/// <see cref="SchemaMember.Type"/>: without it the type is written on save and silently
	/// dropped on load.
	/// </remarks>
	[JsonInclude]
	public BaseType UnderlyingType { get; private set; } = new None();

	/// <summary>
	/// Gets or sets the unit values of this type are measured in.
	/// </summary>
	/// <remarks>
	/// Intrinsic where it belongs to the type rather than the use: a <c>Metres</c> is metres
	/// everywhere it appears, so stating it once here is what stops every member restating it.
	/// </remarks>
	public UnitSymbol? Unit { get; set; }

	/// <summary>
	/// Gets or sets the range values of this type are bounded to, if any.
	/// </summary>
	public MemberRange? Range { get; set; }

	/// <summary>
	/// Gets or sets the value taken when none is supplied.
	/// </summary>
	public MemberDefault? DefaultValue { get; set; }

	/// <summary>
	/// Gets or sets how values of this type should be encoded when sent over a network.
	/// </summary>
	public MemberNetwork? Network { get; set; }

	/// <summary>
	/// Gets or sets how two values of this type may be blended.
	/// </summary>
	public Interpolation Interpolation { get; set; }

	/// <summary>
	/// Gets or sets a hint about how an editor should present values of this type.
	/// </summary>
	public EditorHint? Editor { get; set; }

	/// <summary>
	/// Sets the type this one is represented as.
	/// </summary>
	/// <param name="type">The underlying type.</param>
	public void SetUnderlyingType(BaseType type)
	{
		Ensure.NotNull(type);
		UnderlyingType = type;
		UnderlyingType.AssociateWith(ParentSchema);
	}

	/// <summary>
	/// Resolves <see cref="Unit"/> to a unit from <c>ktsu.Semantics.Quantities</c>.
	/// </summary>
	/// <param name="unit">The resolved unit, or null when there is no unit or it does not
	/// resolve.</param>
	/// <param name="error">Why it did not resolve. Empty when there is no unit at all.</param>
	/// <returns><see langword="true"/> when there is a unit and it resolved.</returns>
	public bool TryResolveUnit(out IUnit? unit, out string error)
	{
		unit = null;
		error = string.Empty;
		return Unit is not null && UnitRegistry.TryResolve(Unit, out unit, out error);
	}

	/// <summary>
	/// Walks the chain of semantic types this one refines, narrowest first.
	/// </summary>
	/// <remarks>
	/// Stops at the first type already seen, so a declaration cycle is reported by
	/// <see cref="Schema.Validate"/> rather than hanging whoever walks it.
	/// </remarks>
	/// <returns>Each semantic type this one refines, in order.</returns>
	public IEnumerable<SchemaSemanticType> Refines()
	{
		HashSet<string> seen = [Name.ToString()];
		BaseType current = UnderlyingType;

		while (current is Semantic semantic && semantic.Declaration is SchemaSemanticType declaration)
		{
			if (!seen.Add(declaration.Name.ToString()))
			{
				yield break;
			}

			yield return declaration;
			current = declaration.UnderlyingType;
		}
	}

	/// <summary>
	/// Gets the type values of this one are ultimately stored as, following any chain of
	/// refinement down to the first type that is not itself semantic.
	/// </summary>
	/// <returns>The representation, or the last resolvable type when the chain is broken.</returns>
	public BaseType Representation()
	{
		SchemaSemanticType deepest = Refines().LastOrDefault() ?? this;
		return deepest.UnderlyingType;
	}

	/// <inheritdoc />
	public override bool TryRemove() => ParentSchema?.TryRemoveSemanticType(this) ?? false;

	/// <inheritdoc />
	public override string ToString() => $"{Name} ({UnderlyingType})";
}
