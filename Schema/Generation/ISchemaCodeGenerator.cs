// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Generation;

using ktsu.Schema.Models;

/// <summary>
/// Turns a schema into source files for one target language.
/// </summary>
/// <remarks>
/// A generator returns file contents keyed by relative path rather than writing to disk, so it can
/// be exercised without a filesystem and so the decision about where output goes stays with the
/// caller. <see cref="SchemaGenerator"/> is what writes the result out.
/// </remarks>
public interface ISchemaCodeGenerator
{
	/// <summary>
	/// Gets the language this generator emits, matched case-insensitively against
	/// <see cref="SchemaCodeGenerator.Language"/>.
	/// </summary>
	public string Language { get; }

	/// <summary>
	/// Generates source for a schema.
	/// </summary>
	/// <remarks>
	/// The schema is guaranteed by <see cref="SchemaGenerator"/> to have no error-severity
	/// validation issues before this is called, so an implementation may assume its references
	/// resolve.
	/// </remarks>
	/// <param name="schema">The schema to generate from.</param>
	/// <param name="configuration">The code generator element holding the settings to generate under.</param>
	/// <returns>File contents keyed by path relative to the generator's output directory.</returns>
	public IReadOnlyDictionary<string, string> Generate(Models.Schema schema, SchemaCodeGenerator configuration);
}
