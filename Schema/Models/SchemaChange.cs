// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using ktsu.Schema.Models.Types;

/// <summary>
/// One difference between two versions of a schema, as found by <see cref="SchemaDiff.Compare"/>.
/// </summary>
public sealed class SchemaChange
{
	/// <summary>
	/// Gets what changed.
	/// </summary>
	public SchemaChangeKind Kind { get; init; }

	/// <summary>
	/// Gets the class a member belongs to, or the enum a value belongs to, by its current name - or
	/// by its previous name when the owner itself was removed. Null for a change to a class or enum.
	/// </summary>
	public string? Owner { get; init; }

	/// <summary>
	/// Gets the name in the previous version; null for something added.
	/// </summary>
	public string? PreviousName { get; init; }

	/// <summary>
	/// Gets the name in the current version; null for something removed.
	/// </summary>
	public string? CurrentName { get; init; }

	/// <summary>
	/// Gets a retyped member's type in the previous version.
	/// </summary>
	public BaseType? PreviousType { get; init; }

	/// <summary>
	/// Gets a retyped member's type in the current version.
	/// </summary>
	public BaseType? CurrentType { get; init; }

	/// <summary>
	/// Gets whether data bound to the previous version has to change to conform to the current one.
	/// </summary>
	/// <remarks>
	/// Adding a class, an enum or an enum value leaves every existing value valid; everything else
	/// moves, drops, fills or converts something.
	/// </remarks>
	public bool AffectsData => Kind is not (SchemaChangeKind.ClassAdded or SchemaChangeKind.EnumAdded or SchemaChangeKind.EnumValueAdded);

	/// <inheritdoc/>
	public override string ToString()
	{
		string prefix = Owner is null ? string.Empty : $"{Owner}.";
		return Kind switch
		{
			SchemaChangeKind.ClassRenamed or SchemaChangeKind.EnumRenamed or SchemaChangeKind.MemberRenamed or SchemaChangeKind.EnumValueRenamed
				=> $"{Kind}: {prefix}{PreviousName} -> {prefix}{CurrentName}",
			SchemaChangeKind.MemberRetyped => $"{Kind}: {prefix}{CurrentName} {PreviousType} -> {CurrentType}",
			_ => $"{Kind}: {prefix}{CurrentName ?? PreviousName}",
		};
	}
}
