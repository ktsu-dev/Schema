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

	/// <summary>
	/// Gets the element names this generator cannot accept because it writes a file of that name for itself.
	/// </summary>
	/// <remarks>
	/// Output is keyed by file name and most of those names are the schema's own, so an element
	/// named after one of the generator's support files would overwrite it. Validation reports such
	/// a name as an error, which keeps generation from running. Compared case-insensitively.
	/// </remarks>
	/// <param name="configuration">The code generator element holding the settings to generate under.</param>
	/// <returns>The reserved names, without any extension; empty when the generator writes nothing of its own.</returns>
	public IReadOnlyCollection<string> ReservedNames(SchemaCodeGenerator configuration) => [];

	/// <summary>
	/// Gets what stops a namespace being written in this generator's language, if anything.
	/// </summary>
	/// <remarks>
	/// The configured namespace is written into every generated file as it is, and each language
	/// spells one differently: <c>Game.Core</c> in C#, <c>game::core</c> in C++. Validation reports
	/// the answer as an error, so a namespace the output could not compile with is caught before
	/// generation rather than when the generated code is built.
	/// </remarks>
	/// <param name="codeNamespace">The configured namespace, which is not empty.</param>
	/// <returns>Why the namespace cannot be written, or <see langword="null"/> when it can.</returns>
	public string? NamespaceProblem(string codeNamespace) => null;
}
