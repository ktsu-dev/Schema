// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp;

using System.Collections.ObjectModel;

/// <summary>
/// What a target has to tell the C++ generator that its schema cannot.
/// </summary>
/// <remarks>
/// Almost everything a generated header says comes from the schema. A unit is a semantic type and
/// the generator emits it; a class is a struct and the generator emits that; the standard library
/// spells a string, a sequence, a view and an absent value, so the generator writes those itself.
/// <para>
/// What is left is the short list below: the types a program supplies for itself because the
/// standard library has no answer. There is no fixed-shape numeric vector in standard C++, no
/// identifier-with-a-generation, and no fallible return. A target that has them says how it spells
/// them; a target that has not is refused those schema types by name rather than handed code that
/// will not compile.
/// </para>
/// <para>
/// <see cref="ExistingTypes"/> is the other direction: a semantic type the target already declares
/// by hand is named rather than generated, which is what stops a generator emitting a second
/// <c>Kilograms</c> beside the one the engine's headers already have.
/// </para>
/// </remarks>
public sealed record CppGeneratorOptions
{
	/// <summary>
	/// Gets how the target spells a two-component vector, parameterised by its component type.
	/// </summary>
	/// <remarks>
	/// Null when the target has none, which refuses a <see cref="Models.Types.Vector2"/> rather
	/// than guessing. <c>std::array</c> is not a substitute: the arity would have to be a type
	/// argument, and a schema's vector is one value with named components rather than a sequence
	/// that happens to be short.
	/// </remarks>
	public CppTypeSpelling? Vector2 { get; init; }

	/// <summary>
	/// Gets how the target spells a three-component vector, parameterised by its component type.
	/// </summary>
	public CppTypeSpelling? Vector3 { get; init; }

	/// <summary>
	/// Gets how the target spells a four-component vector, parameterised by its component type.
	/// </summary>
	public CppTypeSpelling? Vector4 { get; init; }

	/// <summary>
	/// Gets how the target spells a three-channel colour.
	/// </summary>
	/// <remarks>
	/// Unparameterised: a colour's channels are floats, which the schema enforces.
	/// </remarks>
	public CppTypeSpelling? ColorRgb { get; init; }

	/// <summary>
	/// Gets how the target spells a four-channel colour.
	/// </summary>
	public CppTypeSpelling? ColorRgba { get; init; }

	/// <summary>
	/// Gets how the target spells an identifier the holder does not own, parameterised by what it
	/// identifies.
	/// </summary>
	public CppTypeSpelling? Handle { get; init; }

	/// <summary>
	/// Gets how the target spells the outcome of a call that can fail, parameterised by the value
	/// and then by the error.
	/// </summary>
	/// <remarks>
	/// Two type arguments, in that order: the schema's <see cref="Models.Types.Result"/> supplies
	/// the value and <see cref="Models.Schema.ErrorType"/> supplies the error, which is the same
	/// enum for every fallible signature in the schema.
	/// </remarks>
	public CppTypeSpelling? Result { get; init; }

	/// <summary>
	/// Gets how the target spells a date and time.
	/// </summary>
	/// <remarks>
	/// Separate from the rest because the standard library does have an answer and it is rarely
	/// the one a program wants: <c>std::chrono::system_clock::time_point</c> is a duration since
	/// an epoch rather than a calendar date, and its size is the implementation's to choose.
	/// </remarks>
	public CppTypeSpelling? DateTime { get; init; }

	/// <summary>
	/// Gets the semantic types the target already declares, keyed by the name the schema gives
	/// them.
	/// </summary>
	/// <remarks>
	/// A semantic type is otherwise generated: the schema says what it is represented as and what
	/// it refuses, and that is a whole C++ class. This names the ones that already exist, so a
	/// schema describing an engine that hand-wrote its units is generated against those rather
	/// than beside them. Matched by name, case-sensitively, as the schema writes it.
	/// </remarks>
	public IReadOnlyDictionary<string, CppTypeSpelling> ExistingTypes { get; init; } =
		new ReadOnlyDictionary<string, CppTypeSpelling>(new Dictionary<string, CppTypeSpelling>(StringComparer.Ordinal));

	/// <summary>
	/// Gets the line a generated file's banner opens with.
	/// </summary>
	/// <remarks>
	/// The tool's own name by default. A target that vendors the generator behind its own build
	/// step says so instead, because the reader of a generated file wants the name of the thing
	/// they would run again rather than the name of a library three levels down.
	/// </remarks>
	public string GeneratedBy { get; init; } = "ktsu.Schema";

	/// <summary>
	/// Gets the extension a generated header is given, including its leading dot.
	/// </summary>
	public string HeaderExtension { get; init; } = ".gen.hpp";

	/// <summary>
	/// Gets how the target spells the name of a member, function or parameter.
	/// </summary>
	public CppMemberNaming MemberNaming { get; init; } = CppMemberNaming.SnakeCase;
}
