// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Types;

using System.Text.Json.Serialization;
using ktsu.Schema.Models.Names;

/// <summary>
/// Represents an array type.
/// </summary>
public class Array : BaseType
{
	/// <summary>
	/// The container name for an ordered sequence, mapped to a list by a code generator.
	/// </summary>
	public const string VectorContainer = ContainerName.VectorName;

	/// <summary>
	/// The container name for a lookup keyed by <see cref="Key"/>, mapped to a dictionary by a
	/// code generator.
	/// </summary>
	public const string MapContainer = ContainerName.MapName;

	/// <summary>
	/// Gets the container names the library itself produces and understands.
	/// </summary>
	/// <remarks>
	/// <see cref="Container"/> is deliberately open-ended - a consumer may use its own container
	/// vocabulary - so a name outside this set is reported by
	/// <see cref="Schema.Validate"/> as a warning rather than an error.
	/// </remarks>
	public static IReadOnlyCollection<string> KnownContainers { get; } = [VectorContainer, MapContainer];

	/// <summary>
	/// Gets the element type of the array.
	/// </summary>
	public BaseType ElementType { get; init; } = new None();

	/// <summary>
	/// Gets or sets the container name.
	/// </summary>
	public ContainerName Container { get; set; } = new();

	/// <summary>
	/// Gets or sets the key member name.
	/// </summary>
	public MemberName Key { get; set; } = new();

	/// <summary>
	/// Gets or sets the type of a map's keys, for a map whose keys are not a member of its elements.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A map says what it is keyed by in one of two ways. <see cref="Key"/> names a member of the
	/// element class, which is the right answer when every element already carries its own key. That
	/// leaves no way to describe a <c>Dictionary&lt;string, string&gt;</c>, whose elements are not a
	/// class at all, or a <c>Dictionary&lt;string, Item&gt;</c> whose key is not one of
	/// <c>Item</c>'s members, so this is the other: the key's type, stated outright.
	/// </para>
	/// <para>
	/// Null when the map is keyed by a member, and then omitted from the file, so a schema written
	/// before this existed reads and writes exactly as it did. A map states one or the other, never
	/// both, which validation reports.
	/// </para>
	/// </remarks>
	public BaseType? KeyType { get; set; }

	/// <summary>
	/// Gets a value indicating whether the array is keyed.
	/// </summary>
	[JsonIgnore]
	public bool IsKeyed => ElementType.IsObject && !string.IsNullOrEmpty(Key) && !string.IsNullOrEmpty(Container);

	/// <summary>
	/// Tries to get the key member of the array.
	/// </summary>
	/// <param name="keyMember">The key member if found.</param>
	/// <returns>True if the key member is found; otherwise, false.</returns>
	public bool TryGetKeyMember(out SchemaMember? keyMember)
	{
		keyMember = null;
		if (ElementType is Object objectElement)
		{
			objectElement.Class?.TryGetMember(Key, out keyMember);
		}

		return keyMember is not null;
	}

	/// <inheritdoc />
	/// <remarks>
	/// Passes the association down to the element type, so an object or nested array inside this
	/// one can still find its way back to the schema.
	/// </remarks>
	public override void AssociateWith(SchemaMember schemaMember)
	{
		base.AssociateWith(schemaMember);
		ElementType.AssociateWith(schemaMember);
		KeyType?.AssociateWith(schemaMember);
	}

	/// <inheritdoc />
	/// <remarks>
	/// Passes the association down for the same reason as the member overload.
	/// </remarks>
	public override void AssociateWith(Schema? schema)
	{
		base.AssociateWith(schema);
		ElementType.AssociateWith(schema);
		KeyType?.AssociateWith(schema);
	}

	/// <inheritdoc />
	protected override bool EqualsCore(BaseType other) =>
		other is Array otherArray
			&& ElementType.Equals(otherArray.ElementType)
			&& string.Equals(Container, otherArray.Container, StringComparison.Ordinal)
			&& string.Equals(Key, otherArray.Key, StringComparison.Ordinal)
			&& Equals(KeyType, otherArray.KeyType);

	/// <inheritdoc />
	protected override int GetHashCodeCore() => HashCode.Combine(
		ElementType,
		StringComparer.Ordinal.GetHashCode(Container.ToString()),
		StringComparer.Ordinal.GetHashCode(Key.ToString()),
		KeyType);
}
