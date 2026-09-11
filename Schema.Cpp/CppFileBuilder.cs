// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp;

using System.Globalization;

using ktsu.Coder.Ast;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;

using SchemaTypes = ktsu.Schema.Models.Types;

/// <summary>
/// Builds the AST for one generated header.
/// </summary>
/// <remarks>
/// One file per schema element, because a header is what a C++ program includes and a program that
/// wants one component should not compile every other. What goes in a file is decided here; how it
/// is written is <c>ktsu.Coder</c>'s.
/// </remarks>
/// <param name="schema">The schema being generated.</param>
/// <param name="configuration">The generator configuration, which names the namespace.</param>
/// <param name="options">What the target says about the types this generator cannot invent.</param>
internal sealed class CppFileBuilder(Models.Schema schema, SchemaCodeGenerator configuration, CppGeneratorOptions options)
{
	/// <summary>The header that declares the two traits every layout promise is asserted with.</summary>
	private const string TypeTraitsHeader = "<type_traits>";

	/// <summary>
	/// What a semantic type calls the type it is represented as.
	/// </summary>
	/// <remarks>
	/// An alias rather than the representation spelled out at each use, so the shim reads the same
	/// whatever it is over, and changing what it is over is one line.
	/// </remarks>
	private const string UnderlyingAlias = "underlying";

	/// <summary>The member a semantic type holds its value in, and the parameter that fills it.</summary>
	/// <remarks>
	/// Trailing underscore because it is private and the accessor beside it is called
	/// <c>value()</c>: the two would otherwise be the same name.
	/// </remarks>
	private const string ValueField = "value_";

	/// <summary>What both the accessor and the parameter that reaches it are called.</summary>
	private const string ValueName = "value";

	/// <summary>
	/// Builds the header for an enum.
	/// </summary>
	/// <remarks>
	/// At namespace scope and in a file of its own, rather than nested in the class that names it.
	/// A schema's enum is a top-level element any class may refer to, so nesting it in the one
	/// class that happens to use it today would move the type the moment a second class used it -
	/// a source-breaking change to every caller, caused by an unrelated edit somewhere else.
	/// </remarks>
	/// <param name="schemaEnum">The enum.</param>
	/// <returns>The file.</returns>
	public SourceFile Enum(SchemaEnum schemaEnum)
	{
		CppTypeMapper mapper = new(schema, options);

		// Fixed-width, so an enumerator's value is the same size wherever the file is compiled and
		// a class holding one has the same layout everywhere.
		mapper.Require("<cstdint>");

		EnumDeclaration declaration = new(schemaEnum.Name.ToString()) { UnderlyingType = "std::uint8_t" };
		Describe(declaration, schemaEnum.Description);

		foreach (EnumValueName value in schemaEnum.Values)
		{
			declaration.Members.Add(new EnumMember(value.ToString()));
		}

		return File(schemaEnum.Name.ToString(), mapper, declaration);
	}

	/// <summary>
	/// Builds the header for a class.
	/// </summary>
	/// <param name="schemaClass">The class.</param>
	/// <returns>The file.</returns>
	public SourceFile Class(SchemaClass schemaClass)
	{
		CppTypeMapper mapper = new(schema, options);

		ClassDeclaration declaration = new(schemaClass.Name.ToString()) { Kind = TypeDeclarationKind.Struct };
		Describe(declaration, schemaClass.Description);

		foreach (SchemaMember member in schemaClass.Members)
		{
			declaration.Members.Add(Field(member, mapper));
		}

		List<AstNode> members = [declaration];
		members.AddRange(LayoutAssertions(schemaClass, mapper));

		return File(schemaClass.Name.ToString(), mapper, [.. members]);
	}

	/// <summary>
	/// Builds the header for a semantic type: the shim that makes two values sharing a
	/// representation stop being interchangeable.
	/// </summary>
	/// <param name="semanticType">The semantic type.</param>
	/// <returns>The file.</returns>
	public SourceFile SemanticType(SchemaSemanticType semanticType)
	{
		CppTypeMapper mapper = new(schema, options);
		string name = semanticType.Name.ToString();

		ClassDeclaration declaration = new(name);
		Describe(declaration, semanticType.Description);
		foreach (string line in CppMemberDocumentation.ForCarrier(semanticType))
		{
			declaration.Documentation.Add(line);
		}

		declaration.Members.Add(new UsingAlias(UnderlyingAlias, mapper.Map(semanticType.Representation())));

		SchemaSemanticType? refined = semanticType.Refines().FirstOrDefault();
		if (refined is not null)
		{
			// Named three times below, so the header that declares it is wanted here rather than
			// left to whoever includes this one.
			mapper.Require($"\"{refined.Name}{options.HeaderExtension}\"");
			declaration.Members.Add(new UsingAlias("refines", refined.Name.ToString()));
		}

		declaration.Members.Add(new FunctionDeclaration(name)
		{
			Kind = FunctionKind.Constructor,
			IsCompileTimeEvaluable = true,
			IsNoThrow = true,
			Definition = FunctionDefinition.Defaulted,
		});

		declaration.Members.Add(FromUnderlying(name));
		declaration.Members.Add(Accessor());

		if (refined is not null)
		{
			declaration.Members.Add(Widening(refined.Name.ToString()));
			declaration.Members.Add(Narrowing(name, refined.Name.ToString()));
		}

		declaration.Members.Add(Comparison(name, "==", "bool"));
		declaration.Members.Add(Comparison(name, "<=>", "auto"));
		declaration.Members.Add(new FieldDeclaration(ValueField, UnderlyingAlias) { Visibility = Visibility.Private });

		List<AstNode> members = [declaration];
		members.AddRange(Assertions(name, "it appears in components", mapper));

		return File(name, mapper, [.. members]);
	}

	/// <summary>
	/// Builds the header for an interface: the abstract class an implementation is written
	/// against.
	/// </summary>
	/// <param name="schemaInterface">The interface.</param>
	/// <returns>The file.</returns>
	public SourceFile Interface(SchemaInterface schemaInterface)
	{
		CppTypeMapper mapper = new(schema, options);
		string name = schemaInterface.Name.ToString();

		ClassDeclaration declaration = new(name) { Kind = TypeDeclarationKind.Interface };
		Describe(declaration, schemaInterface.Description);

		declaration.Members.Add(new FunctionDeclaration(name)
		{
			Kind = FunctionKind.Constructor,
			Definition = FunctionDefinition.Defaulted,
		});

		// Copying something addressed by reference silently makes a second one. Deleted rather
		// than left implicit, so the mistake is a compile error rather than a duplicate world.
		FunctionDeclaration copyConstructor = new(name)
		{
			Kind = FunctionKind.Constructor,
			Definition = FunctionDefinition.Deleted,
		};
		copyConstructor.Parameters.Add(Borrowed(name));
		declaration.Members.Add(copyConstructor);

		FunctionDeclaration copyAssignment = new("=")
		{
			Kind = FunctionKind.Operator,
			ReturnType = new TypeReference(name) { Indirection = TypeIndirection.Reference },
			Definition = FunctionDefinition.Deleted,
		};
		copyAssignment.Parameters.Add(Borrowed(name));
		declaration.Members.Add(copyAssignment);

		declaration.Members.Add(new FunctionDeclaration(name)
		{
			Kind = FunctionKind.Destructor,
			IsVirtual = true,
			Definition = FunctionDefinition.Defaulted,
		});

		foreach (SchemaFunction function in schemaInterface.Functions)
		{
			declaration.Members.Add(Method(function, mapper));
		}

		return File(name, mapper, declaration);
	}

	private FunctionDeclaration Method(SchemaFunction function, CppTypeMapper mapper)
	{
		FunctionDeclaration method = new(CppNaming.Member(function.Name.ToString(), options.MemberNaming))
		{
			ReturnType = mapper.Map(function.ReturnType),
			IsAbstract = true,

			// Ignoring a value that may be a failure, or a value that was the only reason to call,
			// is a mistake worth a diagnostic rather than a convention nobody reads.
			MustUseResult = function.ReturnType is not SchemaTypes.Void,

			// A query leaves the receiver unchanged, which C++ spells as a trailing const. It is
			// what lets a caller holding a const reference call it at all.
			IsReadOnly = function.IsQuery,
		};

		Describe(method, function.Description);

		foreach (SchemaParameter parameter in function.Parameters)
		{
			method.Parameters.Add(new Parameter(CppNaming.Member(parameter.Name.ToString(), options.MemberNaming))
			{
				Type = mapper.MapParameter(parameter),
			});
		}

		return method;
	}

	private FieldDeclaration Field(SchemaMember member, CppTypeMapper mapper)
	{
		FieldDeclaration field = new(
			CppNaming.Member(member.Name.ToString(), options.MemberNaming),
			mapper.Map(member.Type));

		foreach (string line in CppMemberDocumentation.For(member))
		{
			field.Documentation.Add(line);
		}

		if (member.DefaultValue is not null)
		{
			field.InitialValue = Initialiser(member, mapper);
		}

		return field;
	}

	/// <summary>
	/// Builds what a member starts at.
	/// </summary>
	/// <remarks>
	/// A member with no default is left to the language: a field with no initialiser is
	/// value-initialised, so a default-constructed instance is the one the schema described. A
	/// member with one starts at it, constructed through whatever type it has - which for a
	/// semantic type is the explicit constructor that stops a bare number becoming one by
	/// accident.
	/// </remarks>
	private static Expression Initialiser(SchemaMember member, CppTypeMapper mapper)
	{
		// An enum's default is a value's name, so it is qualified here rather than in the schema:
		// the schema already knows which enum, and making an author write the qualification would
		// be making them write C++.
		if (member.Type is SchemaTypes.Enum enumType)
		{
			return new VariableReference($"{enumType.EnumName}::{member.DefaultValue}");
		}

		BaseType represented = Represented(member.Type);
		string literal = Literal(member.DefaultValue!, represented);

		// Written verbatim rather than as a typed literal node, because a typed node would reformat
		// it - losing the float suffix that stops a braced initialiser refusing the value for
		// narrowing.
		return represented == member.Type && member.Type is Bool or Int or Long or Float or Double
			? new VariableReference(literal)
			: new ConstructionExpression(mapper.Map(member.Type)) { Arguments = { new VariableReference(literal) } };
	}

	/// <summary>
	/// Writes a default the way C++ has to read it.
	/// </summary>
	/// <remarks>
	/// A value stored as a <c>float</c> needs both a decimal point and an <c>f</c>: <c>1</c> is an
	/// int and <c>1.0</c> is a double, and a braced initialiser refuses either for narrowing, which
	/// is the whole reason to brace it. Everything else is the text the schema holds.
	/// </remarks>
	private static string Literal(MemberDefault value, BaseType represented) =>
		represented is Float && value is NumberDefault number
			? $"{number.Value.ToString("0.0###############", CultureInfo.InvariantCulture)}f"
			: value.ToString();

	/// <summary>
	/// Follows a semantic type down to what it is stored as, which is what decides how a literal
	/// has to be written.
	/// </summary>
	private static BaseType Represented(BaseType type) =>
		type is Semantic { Declaration: SchemaSemanticType declaration } && declaration.Representation() is not Semantic
			? declaration.Representation()
			: type;

	/// <summary>
	/// Builds what a generated type promises that the type itself cannot say.
	/// </summary>
	private static IEnumerable<AstNode> LayoutAssertions(SchemaClass schemaClass, CppTypeMapper mapper) =>
		schemaClass.TravelsAsBytes
			? Assertions(schemaClass.Name.ToString(), "it crosses a language boundary and the wire as bytes", mapper)
			: [];

	private static IEnumerable<AstNode> Assertions(string name, string why, CppTypeMapper mapper)
	{
		mapper.Require(TypeTraitsHeader);

		yield return new CompileTimeAssertion(
			$"std::is_trivially_copyable_v<{name}>",
			$"{name} must be trivially copyable: {why}");

		yield return new CompileTimeAssertion(
			$"std::is_standard_layout_v<{name}>",
			$"{name} must be standard layout for its field offsets to be stable");
	}

	private static FunctionDeclaration FromUnderlying(string name)
	{
		FunctionDeclaration constructor = new(name)
		{
			Kind = FunctionKind.Constructor,
			IsExplicit = true,
			IsCompileTimeEvaluable = true,
			IsNoThrow = true,
		};

		constructor.Documentation.Add($"Explicit: a bare value never becomes {Article(name)} {name} by accident.");
		constructor.Parameters.Add(new Parameter(ValueName, UnderlyingAlias));
		constructor.Initialisers.Add(new MemberInitialiser(ValueField, new VariableReference(ValueName)));
		return constructor;
	}

	private static FunctionDeclaration Accessor()
	{
		FunctionDeclaration accessor = new(ValueName)
		{
			ReturnType = UnderlyingAlias,
			IsPure = true,
			IsCompileTimeEvaluable = true,
			IsReadOnly = true,
			IsNoThrow = true,
		};

		accessor.Documentation.Add("Named, because getting the value back out is a decision too.");
		accessor.Body.Add(new ReturnStatement(new VariableReference(ValueField)));
		return accessor;
	}

	private static FunctionDeclaration Widening(string broader)
	{
		FunctionDeclaration widening = new(broader)
		{
			Kind = FunctionKind.ConversionOperator,
			ReturnType = broader,
			IsPure = true,
			IsCompileTimeEvaluable = true,
			IsReadOnly = true,
			IsNoThrow = true,
		};

		widening.Documentation.Add($"Widening is implicit: this is {Article(broader)} {broader}.");
		widening.Body.Add(new ReturnStatement(
			new ConstructionExpression(broader) { Arguments = { new VariableReference(ValueField) } }));

		return widening;
	}

	private static FunctionDeclaration Narrowing(string name, string broader)
	{
		FunctionDeclaration narrowing = new("from")
		{
			ReturnType = name,
			IsPure = true,
			IsStatic = true,
			IsCompileTimeEvaluable = true,
			IsNoThrow = true,
		};

		narrowing.Documentation.Add($"Narrowing is explicit and named: not every {broader} is {Article(name)} {name}.");
		narrowing.Parameters.Add(new Parameter(ValueName, broader));
		narrowing.Body.Add(new ReturnStatement(new ConstructionExpression(name)
		{
			Arguments = { new VariableReference($"{ValueName}.{ValueName}()") },
		}));

		return narrowing;
	}

	/// <summary>
	/// Builds one of the comparison operators, which are symmetric and so belong beside the type
	/// rather than to either operand.
	/// </summary>
	private static FunctionDeclaration Comparison(string name, string symbol, string returnType)
	{
		FunctionDeclaration comparison = new(symbol)
		{
			Kind = FunctionKind.Operator,
			ReturnType = returnType,
			IsPure = true,
			IsFriend = true,
			IsCompileTimeEvaluable = true,
			IsNoThrow = true,
			Definition = FunctionDefinition.Defaulted,
		};

		// Unnamed: the parameters are there to make the signature, and naming them would invite
		// someone to look for a use a defaulted body does not have.
		comparison.Parameters.Add(new Parameter(string.Empty, name));
		comparison.Parameters.Add(new Parameter(string.Empty, name));
		return comparison;
	}

	/// <summary>
	/// Picks the article a generated sentence needs.
	/// </summary>
	/// <remarks>
	/// A comment is prose and reads as prose, so "a EntityId" is a small piece of grating that
	/// appears in every generated file until someone fixes it. Judged by the letter, which is
	/// wrong for the handful of names that start with a consonant sound spelled with a vowel and
	/// right for everything else.
	/// </remarks>
	private static string Article(string name) =>
		name.Length > 0 && "aeiouAEIOU".Contains(name[0], StringComparison.Ordinal) ? "an" : "a";

	private static Parameter Borrowed(string type) => new(string.Empty)
	{
		Type = new TypeReference(type) { IsReadOnly = true, Indirection = TypeIndirection.Reference },
	};

	private static void Describe(IHasDocumentation declaration, SchemaChildDescription description)
	{
		if (!string.IsNullOrEmpty(description))
		{
			declaration.Documentation.Add(description.ToString());
		}
	}

	/// <summary>
	/// Wraps declarations in the file that carries them: the banner, the includes each declaration
	/// turned out to need, and the namespace.
	/// </summary>
	private SourceFile File(string name, CppTypeMapper mapper, params AstNode[] members)
	{
		SourceFile file = new(name) { IsHeader = true };

		file.HeaderComment.Add($"Generated by {options.GeneratedBy}. Do not edit.");
		file.HeaderComment.Add(string.Empty);
		file.HeaderComment.Add($"Source: {Source()}");
		file.HeaderComment.Add(string.Empty);
		file.HeaderComment.Add("Editing this file is editing the wrong thing: it is derived from the schema,");
		file.HeaderComment.Add("and the next build overwrites it. Change the schema instead.");

		string ownHeader = $"\"{name}{options.HeaderExtension}\"";
		foreach (string include in Ordered([.. mapper.Includes.Where(i => !string.Equals(i, ownHeader, StringComparison.Ordinal))]))
		{
			file.Imports.Add(include);
		}

		// A generator with no namespace configured emits into the global one, which is legal and
		// rarely wanted - Schema.Validate reports it as a warning rather than refusing it, so the
		// declarations go straight into the file.
		if (string.IsNullOrEmpty(configuration.Namespace))
		{
			foreach (AstNode member in members)
			{
				file.Members.Add(member);
			}

			return file;
		}

		NamespaceDeclaration containing = new(configuration.Namespace.ToString());
		foreach (AstNode member in members)
		{
			containing.Members.Add(member);
		}

		file.Members.Add(containing);
		return file;
	}

	/// <summary>
	/// Orders the includes the way a C++ file is conventionally written: the standard library
	/// first, then the target's own, each sorted, with a blank line between the two groups.
	/// </summary>
	/// <remarks>
	/// An empty import is the AST's group separator rather than an import of nothing.
	/// </remarks>
	private static IEnumerable<string> Ordered(IReadOnlyList<string> includes)
	{
		string[] system = [.. includes.Where(i => i.StartsWith('<')).OrderBy(i => i, StringComparer.Ordinal)];
		string[] local = [.. includes.Where(i => !i.StartsWith('<')).OrderBy(i => i, StringComparer.Ordinal)];

		foreach (string include in system)
		{
			yield return include;
		}

		if (system.Length > 0 && local.Length > 0)
		{
			yield return string.Empty;
		}

		foreach (string include in local)
		{
			yield return include;
		}
	}

	/// <summary>
	/// Names the file this was generated from, so a reader knows what to edit instead.
	/// </summary>
	/// <remarks>
	/// A schema built in memory has no file to name, which is every generation driven from code
	/// rather than from disk - the tests below among them.
	/// </remarks>
	private string Source() =>
		string.IsNullOrEmpty(schema.SourceFileName) ? "the schema" : schema.SourceFileName;
}
