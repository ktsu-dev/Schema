// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cli;

/// <summary>
/// What the program hosting these commands is called, and what it adds to them.
/// </summary>
/// <remarks>
/// <see cref="SchemaCommandLine"/> is a library, so it cannot know either. The command name is
/// whatever the host was installed as, and the extra options are the host's own: a host exists in
/// the first place to register the generators a schema can name, and a generator with something to
/// be told needs somewhere to be told it.
/// <para>
/// This is only for the usage text. A host parses its own options and hands on what is left, which
/// is what keeps <see cref="SchemaCommandLine"/>'s own parsing unaware of them.
/// </para>
/// </remarks>
public sealed record SchemaCommandLineHost
{
	/// <summary>
	/// Gets the name the host is invoked by.
	/// </summary>
	public string CommandName { get; init; } = "schema";

	/// <summary>
	/// Gets the host's own options, each already laid out as a usage line.
	/// </summary>
	public IReadOnlyList<string> Options { get; init; } = [];
}
