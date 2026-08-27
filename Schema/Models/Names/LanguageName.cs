// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models.Names;

using ktsu.Semantics.Strings;

/// <summary>
/// Represents the target language of a code generator as a strong string type.
/// </summary>
public sealed record class LanguageName : SemanticString<LanguageName>
{
	/// <summary>
	/// The language name of the first-party C# generator.
	/// </summary>
	public const string CSharpName = "csharp";

	/// <summary>
	/// Gets the language name of the first-party C# generator.
	/// </summary>
	public static LanguageName CSharp { get; } = CSharpName.As<LanguageName>();
}
