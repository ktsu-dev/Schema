// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp;

using ktsu.Coder.Ast;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Types;

using SchemaTypes = ktsu.Schema.Models.Types;

/// <summary>
/// Turns a schema type into the C++ type that represents it, collecting the headers that type
/// needs along the way.
/// </summary>
/// <remarks>
/// One instance per file being generated, because the set of includes is a property of the file
/// rather than of the mapping. Anything the target has not said how to spell is refused by name;
/// see <see cref="CppGenerationException"/>.
/// </remarks>
/// <param name="schema">The schema being generated, which resolves the names a type holds.</param>
/// <param name="options">What the target says about the types this generator cannot invent.</param>
internal sealed class CppTypeMapper(Models.Schema schema, CppGeneratorOptions options)
{
	/// <summary>The headers the types mapped so far need, in the order they were first wanted.</summary>
	private readonly List<string> includes = [];

	/// <summary>
	/// Gets the headers every type mapped so far needs.
	/// </summary>
	public IReadOnlyList<string> Includes => includes;

	/// <summary>
	/// Maps a schema type to its C++ spelling.
	/// </summary>
	/// <param name="type">The schema type.</param>
	/// <returns>The C++ type.</returns>
	/// <exception cref="CppGenerationException">The target cannot spell it.</exception>
	public TypeReference Map(BaseType type) => type switch
	{
		Bool => new TypeReference("bool"),
		Int => Fixed("std::int32_t", "cstdint"),
		Long => Fixed("std::int64_t", "cstdint"),
		Float => new TypeReference("float"),
		Double => new TypeReference("double"),
		SchemaTypes.String => Fixed("std::string", "string"),
		SchemaTypes.TimeSpan => Fixed("std::chrono::duration<double>", "chrono"),
		SchemaTypes.DateTime => Supplied(options.DateTime, "DateTime", nameof(CppGeneratorOptions.DateTime)),
		SchemaTypes.Void => new TypeReference("void"),

		// The colours before their vector bases, since ColorRGB derives from Vector3.
		ColorRGB => Supplied(options.ColorRgb, "ColorRGB", nameof(CppGeneratorOptions.ColorRgb)),
		ColorRGBA => Supplied(options.ColorRgba, "ColorRGBA", nameof(CppGeneratorOptions.ColorRgba)),
		SchemaTypes.Vector2 vector => Parameterised(options.Vector2, "Vector2", nameof(CppGeneratorOptions.Vector2), vector.ElementType),
		SchemaTypes.Vector3 vector => Parameterised(options.Vector3, "Vector3", nameof(CppGeneratorOptions.Vector3), vector.ElementType),
		SchemaTypes.Vector4 vector => Parameterised(options.Vector4, "Vector4", nameof(CppGeneratorOptions.Vector4), vector.ElementType),

		SchemaTypes.Enum enumType => Generated(enumType.EnumName.ToString()),
		SchemaTypes.Object objectType => Generated(objectType.ClassName.ToString()),
		SchemaTypes.Interface interfaceType => Generated(interfaceType.InterfaceName.ToString()),
		Semantic semantic => MapSemantic(semantic),
		SchemaTypes.Quantity quantity => MapQuantity(quantity),

		SchemaTypes.Array arrayType => Generic(Fixed("std::vector", "vector"), arrayType.ElementType),
		Span span => Generic(Fixed("std::span", "span"), span.ElementType),
		Optional optional => Generic(Fixed("std::optional", "optional"), optional.ElementType),
		SchemaTypes.Handle handle => Parameterised(options.Handle, "Handle", nameof(CppGeneratorOptions.Handle), handle.ElementType),
		SchemaTypes.Result result => MapResult(result),

		None => throw new CppGenerationException(
			"A member or signature has no type chosen. Choose one, or remove the declaration."),

		_ => throw new CppGenerationException($"There is no C++ spelling for a {type.TypeName}."),
	};

	/// <summary>
	/// Maps a parameter, which is the one place direction changes the type rather than only how it
	/// is passed.
	/// </summary>
	/// <remarks>
	/// On a <see cref="Span"/> direction describes the elements: an <c>In Span&lt;Velocity&gt;</c>
	/// is <c>std::span&lt;const Velocity&gt;</c>, which is what makes a system's signature ordinary
	/// rather than needing a concept of its own. On everything else it describes the value, and a
	/// value large enough to be worth not copying is borrowed by reference.
	/// </remarks>
	/// <param name="parameter">The parameter.</param>
	/// <returns>The C++ type as it appears in the signature.</returns>
	public TypeReference MapParameter(SchemaParameter parameter)
	{
		Ensure.NotNull(parameter);

		TypeReference mapped = Map(parameter.Type);
		bool readOnly = parameter.Direction == ParameterDirection.In;

		if (parameter.Type is Span)
		{
			// The view itself is passed by value; what it looks at is what const applies to.
			if (readOnly && mapped.TypeArguments.Count == 1)
			{
				mapped.TypeArguments[0].IsReadOnly = true;
			}

			return mapped;
		}

		if (IsPassedByValue(parameter.Type))
		{
			return mapped;
		}

		mapped.IsReadOnly = readOnly;
		mapped.Indirection = TypeIndirection.Reference;
		return mapped;
	}

	/// <summary>
	/// Records that a header is needed, once.
	/// </summary>
	/// <param name="include">The header, delimited as the AST's imports are.</param>
	public void Require(string include)
	{
		if (!string.IsNullOrEmpty(include) && !includes.Contains(include))
		{
			includes.Add(include);
		}
	}

	/// <summary>
	/// Says whether a value of this type is small enough that passing it whole is the ordinary
	/// choice.
	/// </summary>
	/// <remarks>
	/// A number, an enumerator, a handle and a semantic type over any of those are register-sized
	/// or close to it, so a reference would cost more than it saved. Everything else is borrowed.
	/// </remarks>
	private static bool IsPassedByValue(BaseType type) => type switch
	{
		Bool or Int or Long or Float or Double => true,
		SchemaTypes.Enum or SchemaTypes.Handle => true,

		// A magnitude or a signed scalar is one number under a name; a vector form is two to four,
		// which is where borrowing starts to pay, so this reads the shape rather than assuming.
		SchemaTypes.Quantity quantity => quantity.Resolved?.Components <= 1,
		Semantic { Declaration: SchemaSemanticType declaration } =>
			declaration.Representation() is not Semantic && IsPassedByValue(declaration.Representation()),
		_ => false,
	};

	/// <summary>
	/// Names a quantity where the target's vocabulary put it.
	/// </summary>
	/// <remarks>
	/// Nothing is generated for it, which is the whole of what separates it from the semantic type
	/// below: a semantic type is a class this generator writes, and a quantity is one the target
	/// already has because it ran the same vocabulary generator this schema reads its names from.
	/// </remarks>
	private TypeReference MapQuantity(SchemaTypes.Quantity quantity)
	{
		if (options.Quantities is not CppQuantitySpelling spelling)
		{
			throw new CppGenerationException(
				$"The schema holds a {quantity.QuantityName}, and this target declares no physical quantities. Set '{CppGeneratorOptionsFile.NameOf(nameof(CppGeneratorOptions.Quantities))}' in the {CppGeneratorOptionsFile.Option} file, or {nameof(CppGeneratorOptions)}.{nameof(CppGeneratorOptions.Quantities)} in a host, to where ktsu.Semantics.Cpp generated them.");
		}

		// A C++ quantity is a class and not a template, so the storage was fixed when the
		// vocabulary was generated. C# closes one per member, so the two can disagree - and two
		// languages disagreeing about the width of a member is exactly what a class promising to
		// travel as bytes cannot survive.
		if (!string.Equals(quantity.Storage.TypeName, spelling.Storage, StringComparison.Ordinal))
		{
			throw new CppGenerationException(
				$"The schema stores a {quantity.QuantityName} in a {quantity.Storage.TypeName}, and this target's quantities are generated over {spelling.Storage}. A C++ quantity is a class rather than a template, so there is no {spelling.Storage} vocabulary and a {quantity.Storage.TypeName} one to choose between.");
		}

		Require(spelling.Include);
		return new TypeReference(spelling.Qualified(quantity.QuantityName.ToString()));
	}

	private TypeReference MapSemantic(Semantic semantic)
	{
		string name = semantic.SemanticTypeName.ToString();

		// A type the target already declares is named rather than generated, which is what stops a
		// second Kilograms appearing beside the one the engine's headers already have.
		if (options.ExistingTypes.TryGetValue(name, out CppTypeSpelling? existing))
		{
			Require(existing.Include);
			return new TypeReference(existing.Name);
		}

		return Generated(name);
	}

	/// <summary>
	/// Names a type this generator emits, and requires the header it emits it into.
	/// </summary>
	/// <remarks>
	/// A generated type lives in a header of its own, so naming one is also asking for it. The
	/// file being built drops its own name from the list, since a header including itself is the
	/// one case where this would be wrong.
	/// </remarks>
	private TypeReference Generated(string name)
	{
		Require($"\"{name}{options.HeaderExtension}\"");
		return new TypeReference(name);
	}

	private TypeReference MapResult(SchemaTypes.Result result)
	{
		TypeReference outcome = Parameterised(options.Result, "Result", nameof(CppGeneratorOptions.Result), result.ElementType);

		// The error is the schema's, once, rather than this signature's. Validation has already
		// refused a Result in a schema that names no error type, so reaching here without one
		// would mean generating from a schema nobody validated.
		if (string.IsNullOrEmpty(schema.ErrorType))
		{
			throw new CppGenerationException(
				"A signature returns a Result, but the schema names no error type. Set the schema's error type to an enum it declares.");
		}

		outcome.TypeArguments.Add(new TypeReference(schema.ErrorType.ToString()));
		return outcome;
	}

	private TypeReference Fixed(string name, string header)
	{
		Require($"<{header}>");
		return new TypeReference(name);
	}

	private TypeReference Generic(TypeReference outer, BaseType element)
	{
		outer.TypeArguments.Add(Map(element));
		return outer;
	}

	private TypeReference Parameterised(CppTypeSpelling? spelling, string schemaType, string optionName, BaseType element) =>
		Generic(Supplied(spelling, schemaType, optionName), element);

	private TypeReference Supplied(CppTypeSpelling? spelling, string schemaType, string optionName)
	{
		if (spelling is null)
		{
			// Both spellings, because there are two ways to supply these and the message cannot
			// tell which one the reader used: a host that constructs the record names the
			// property, and a build that runs the tool names the key in its options file.
			throw new CppGenerationException(
				$"The schema uses {schemaType}, which standard C++ has no type for. Set '{CppGeneratorOptionsFile.NameOf(optionName)}' in the {CppGeneratorOptionsFile.Option} file, or {nameof(CppGeneratorOptions)}.{optionName} in a host, to how this target spells it.");
		}

		Require(spelling.Include);
		return new TypeReference(spelling.Name);
	}
}
