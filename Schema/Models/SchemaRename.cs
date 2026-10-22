// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Text.Json.Serialization;

/// <summary>
/// One rename, as recorded in a schema's <see cref="Schema.RenameHistory"/>.
/// </summary>
/// <remarks>
/// A rename cannot be told from a removal and an addition by comparing two versions of a schema:
/// both leave one name gone and another present. Data bound to the old version holds the old name,
/// and migrating it needs to know which of the two happened - a renamed member's values move, a
/// removed member's are dropped. So the schema says so itself, at the moment it happens.
/// <para>
/// Names are recorded as text, as they were at the time: <see cref="Owner"/> is the class or enum's
/// name when the member or value was renamed, not its name now. Replaying the history in order is
/// what turns either into the other.
/// </para>
/// </remarks>
public sealed class SchemaRename : IEquatable<SchemaRename>
{
	/// <summary>
	/// Gets what was renamed.
	/// </summary>
	[JsonPropertyName("kind")]
	public SchemaRenameKind Kind { get; init; }

	/// <summary>
	/// Gets the class a renamed member belongs to, or the enum a renamed value belongs to, by its
	/// name at the time; null for a class or enum rename.
	/// </summary>
	[JsonPropertyName("owner")]
	public string? Owner { get; init; }

	/// <summary>
	/// Gets the name before the rename.
	/// </summary>
	[JsonPropertyName("from")]
	public string From { get; init; } = string.Empty;

	/// <summary>
	/// Gets the name after the rename.
	/// </summary>
	[JsonPropertyName("to")]
	public string To { get; init; } = string.Empty;

	/// <summary>
	/// Gets whether this rename exactly undoes <paramref name="other"/>.
	/// </summary>
	/// <param name="other">The rename to compare with.</param>
	/// <returns>True if both rename the same kind of thing, in the same owner, back the other way.</returns>
	public bool Reverses(SchemaRename other)
	{
		Ensure.NotNull(other);

		return Kind == other.Kind &&
			string.Equals(Owner, other.Owner, StringComparison.Ordinal) &&
			string.Equals(From, other.To, StringComparison.Ordinal) &&
			string.Equals(To, other.From, StringComparison.Ordinal);
	}

	/// <inheritdoc/>
	public bool Equals(SchemaRename? other) =>
		other is not null &&
		Kind == other.Kind &&
		string.Equals(Owner, other.Owner, StringComparison.Ordinal) &&
		string.Equals(From, other.From, StringComparison.Ordinal) &&
		string.Equals(To, other.To, StringComparison.Ordinal);

	/// <inheritdoc/>
	public override bool Equals(object? obj) => Equals(obj as SchemaRename);

	/// <inheritdoc/>
	public override int GetHashCode() => HashCode.Combine(Kind, Owner, From, To);

	/// <inheritdoc/>
	public override string ToString() => Owner is null
		? $"{Kind} {From} -> {To}"
		: $"{Kind} {Owner}.{From} -> {Owner}.{To}";
}
