// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Text.Json.Serialization;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;

/// <summary>
/// One parameter of a function: what it carries and which way it travels.
/// </summary>
/// <remarks>
/// Order is the signature, so a function holds its parameters in a list rather than a set, and the
/// serializer preserves that order the way a class preserves member order.
/// </remarks>
public class SchemaParameter : SchemaFunctionChild<ParameterName>
{
	/// <summary>
	/// Gets the parameter's type.
	/// </summary>
	/// <remarks>
	/// <see cref="JsonIncludeAttribute"/> is required because the setter is private, matching
	/// <see cref="SchemaMember.Type"/>: without it the type is written on save and silently
	/// dropped on load.
	/// </remarks>
	[JsonInclude]
	public BaseType Type { get; private set; } = new None();

	/// <summary>
	/// Gets or sets which way the value travels. <see cref="ParameterDirection.In"/> by default.
	/// </summary>
	public ParameterDirection Direction { get; set; } = ParameterDirection.In;

	/// <summary>
	/// Sets the parameter's type.
	/// </summary>
	/// <param name="type">The type to set.</param>
	public void SetType(BaseType type)
	{
		Ensure.NotNull(type);
		Type = type;
		Type.AssociateWith(ParentSchema);
	}

	/// <inheritdoc />
	/// <remarks>
	/// Passes the association to the type, so a class or interface named in a signature resolves
	/// the same way a member's type does.
	/// </remarks>
	public new void AssociateWith(Schema schemaProvider)
	{
		base.AssociateWith(schemaProvider);
		Type.AssociateWith(schemaProvider);
	}

	/// <inheritdoc />
	public override bool TryRemove() => ParentFunction?.TryRemoveParameter(this) ?? false;

	/// <inheritdoc />
	public override string ToString() => $"{Direction.ToString().ToUpperInvariant()} {Type} {Name}";
}
