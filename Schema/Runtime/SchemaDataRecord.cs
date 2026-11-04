// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Runtime;

using System.Text.Json.Nodes;

/// <summary>
/// One record of a data file, as a generated data editor lists it.
/// </summary>
/// <remarks>
/// A record is a view over the file's own JSON rather than a copy of it, so what it does not model
/// - a member whose type has no typed accessor, a property the class does not declare - survives an
/// edit and a save untouched. Generated records add one typed property per member on top of
/// <see cref="Json"/>, through <see cref="SchemaDataValues"/>.
/// </remarks>
/// <param name="json">The record's object in the data file.</param>
/// <param name="onEdited">Tells the editor listing the record that its data changed.</param>
public abstract class SchemaDataRecord(JsonObject json, Action onEdited)
{
	private readonly Action onEdited = Ensure.NotNull(onEdited);

	/// <summary>Gets the record's object in the data file, which every accessor reads and writes.</summary>
	/// <remarks>An edit made here directly is not seen by the editor until <see cref="MarkEdited"/> is called.</remarks>
	public JsonObject Json { get; } = Ensure.NotNull(json);

	/// <summary>Tells the editor listing this record that its data changed, which every generated setter does.</summary>
	public void MarkEdited() => onEdited();
}
