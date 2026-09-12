// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Generation;

using System.Globalization;

using ktsu.CodeBlocker;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;

/// <summary>
/// Emits C# POCOs for a schema's classes and enums.
/// </summary>
/// <remarks>
/// The type mapping is the exact inverse of the one <see cref="Models.Schema.AddClass(Type)"/>
/// uses to import CLR types, so generating from a schema and reimporting the result reproduces
/// the schema it started from. That round trip is what keeps the two mappings honest, and it is
/// covered by a test that compiles the generated source and reimports the compiled types.
/// </remarks>
public sealed class CSharpCodeGenerator : ISchemaCodeGenerator
{
	/// <summary>
	/// The language id this generator answers to.
	/// </summary>
	public const string LanguageId = LanguageName.CSharpName;

	/// <summary>
	/// What a type this generator cannot yet spell is written as.
	/// </summary>
	/// <remarks>
	/// Named rather than repeated, because two places have to agree on it: <see cref="MapType"/>
	/// writes it, and a semantic type over a representation that reached it cannot emit its
	/// conversions, since C# refuses a user-defined conversion to or from <c>object</c>.
	/// </remarks>
	private const string UnspeakableType = "object?";

	/// <inheritdoc />
	public string Language => LanguageId;

	/// <inheritdoc />
	public IReadOnlyDictionary<string, string> Generate(Models.Schema schema, SchemaCodeGenerator configuration)
	{
		Ensure.NotNull(schema);
		Ensure.NotNull(configuration);

		Dictionary<string, string> files = [];

		foreach (SchemaEnum schemaEnum in schema.Enums)
		{
			files[$"{schemaEnum.Name}.g.cs"] = GenerateEnum(schemaEnum, configuration.Namespace);
		}

		foreach (SchemaSemanticType semanticType in schema.SemanticTypes)
		{
			files[$"{semanticType.Name}.g.cs"] = GenerateSemanticType(semanticType, configuration.Namespace);
		}

		foreach (SchemaClass schemaClass in schema.Classes)
		{
			files[$"{schemaClass.Name}.g.cs"] = GenerateClass(schemaClass, configuration.Namespace);
		}

		foreach (SchemaInterface schemaInterface in schema.Interfaces)
		{
			files[$"{schemaInterface.Name}.g.cs"] = GenerateInterface(schemaInterface, configuration.Namespace);
		}

		return files;
	}

	private static string GenerateEnum(SchemaEnum schemaEnum, CodeNamespace codeNamespace)
	{
		using CodeBlocker code = CodeBlocker.Create();
		WriteHeader(code, codeNamespace);

		WriteDocComment(code, schemaEnum.Description);
		code.WriteLine($"public enum {CSharpKeywords.Identifier(schemaEnum.Name)}");
		using (new Scope(code))
		{
			foreach (EnumValueName value in schemaEnum.Values)
			{
				code.WriteLine($"{CSharpKeywords.Identifier(value)},");
			}
		}

		return code.ToString();
	}

	/// <summary>
	/// Writes a class out as the C# type that represents it.
	/// </summary>
	/// <remarks>
	/// A class that promises to travel as raw bytes is emitted as a sequentially laid out
	/// <c>struct</c>, and every other class stays a <c>class</c>. A reference type could not keep
	/// that promise however carefully it was written - an instance is an address, its fields are
	/// somewhere else, and the CLR is free to order them - so a consumer needing the C# and C++
	/// versions of a class to be the same bytes had nothing to compare. Sequential layout is what
	/// makes the schema's member order load-bearing on this side too, and it is why the promise is
	/// narrow: it changes the shape of the classes that make it and no others.
	/// <para>
	/// <c>SchemaTravelsAsBytes</c> is still written. The struct is how the promise is kept and the
	/// attribute is how it is recorded - the CLR spells layout in ways that do not survive being
	/// read back as intent, so <see cref="Models.ClrTypeImporter"/> reads the attribute rather than
	/// inferring the promise from the shape.
	/// </para>
	/// </remarks>
	private static string GenerateClass(SchemaClass schemaClass, CodeNamespace codeNamespace)
	{
		using CodeBlocker code = CodeBlocker.Create();
		WriteHeader(code, codeNamespace);

		WriteDocComment(code, schemaClass.Description);

		if (schemaClass.TravelsAsBytes)
		{
			code.WriteLine("[ktsu.Schema.Runtime.SchemaTravelsAsBytes]");
			code.WriteLine("[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]");
		}

		code.WriteLine($"public {(schemaClass.TravelsAsBytes ? "struct" : "class")} {CSharpKeywords.Identifier(schemaClass.Name)}");
		using (new Scope(code))
		{
			bool first = true;

			if (schemaClass.TravelsAsBytes && schemaClass.Members.Any(m => InitialiserFor(m).Length > 0))
			{
				WriteValueTypeConstructor(code, schemaClass.Name);
				first = false;
			}

			foreach (SchemaMember member in schemaClass.Members)
			{
				if (!first)
				{
					code.NewLine();
				}

				first = false;

				WriteDocComment(code, member.Description);
				WriteSchemaKeyAttribute(code, member.Type);
				WriteMetadataAttributes(code, member);
				code.WriteLine($"public {MapType(member.Type)} {CSharpKeywords.Identifier(member.Name)} {{ get; set; }}{InitialiserFor(member)}");
			}
		}

		return code.ToString();
	}

	/// <summary>
	/// Writes the parameterless constructor a value type needs before its members may have
	/// initialisers.
	/// </summary>
	/// <remarks>
	/// A struct whose members have initialisers and which declares no constructor of its own does
	/// not compile, because <c>default</c> reaches an instance without running them. Declaring one
	/// is what lets a member's default survive into <c>new T()</c>. <c>default(T)</c> is still all
	/// zeroes, which is what it means for the bytes to be the whole of the value rather than a
	/// defect in the defaults.
	/// </remarks>
	private static void WriteValueTypeConstructor(CodeBlocker code, ClassName name)
	{
		string identifier = CSharpKeywords.Identifier(name);

		code.WriteLine("/// <summary>");
		code.WriteLine($"/// Initialises a new <see cref=\"{identifier}\"/> at the defaults the schema declared.");
		code.WriteLine("/// </summary>");
		code.WriteLine($"public {identifier}()");
		code.WriteLine("{");
		code.WriteLine("}");
	}

	/// <summary>
	/// Emits a semantic type: the shim that makes two values sharing a representation stop being
	/// interchangeable.
	/// </summary>
	/// <remarks>
	/// A struct holding one value, so it is the same bytes as the thing it shims and a class that
	/// travels as bytes may hold one. What it adds is what it refuses: C# spells "explicit" and
	/// "implicit" on a conversion directly, so the schema's two conventions - crossing into or out
	/// of the representation is always explicit, and a type refining another widens implicitly and
	/// narrows explicitly - are the conversions themselves rather than a comment beside them.
	/// <para>
	/// A <c>record struct</c> for equality: two of these are the same when their values are, which
	/// is what a distinct name over an existing type means, and writing that by hand would be
	/// <c>Equals</c>, <c>GetHashCode</c> and two operators emitted identically every time.
	/// </para>
	/// </remarks>
	private static string GenerateSemanticType(SchemaSemanticType semanticType, CodeNamespace codeNamespace)
	{
		using CodeBlocker code = CodeBlocker.Create();
		WriteHeader(code, codeNamespace);

		WriteDocComment(code, semanticType.Description);

		string name = CSharpKeywords.Identifier(semanticType.Name);
		SchemaSemanticType? refined = semanticType.Refines().FirstOrDefault();
		string refines = refined is null
			? string.Empty
			: $"(Refines = typeof({CSharpKeywords.Identifier(refined.Name)}))";

		code.WriteLine($"[ktsu.Schema.Runtime.SchemaSemanticType{refines}]");
		WriteMetadataAttributes(code, semanticType);
		code.WriteLine("[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]");
		code.WriteLine($"public readonly record struct {name}");

		using (new Scope(code))
		{
			string underlying = MapType(semanticType.Representation());

			code.WriteLine($"private {name}({underlying} value) => Value = value;");
			code.NewLine();
			code.WriteLine("/// <summary>");
			code.WriteLine("/// Gets the value this is represented as.");
			code.WriteLine("/// </summary>");
			code.WriteLine($"public {underlying} Value {{ get; }}");

			// A representation this generator cannot spell arrives as object, which C# refuses to
			// convert to or from. The schema is one validation has already refused generation for,
			// so the type is still emitted under its own name - it just cannot be crossed into.
			if (!string.Equals(underlying, UnspeakableType, StringComparison.Ordinal))
			{
				WriteConversions(code, name, underlying, refined);
			}
		}

		return code.ToString();
	}

	/// <summary>
	/// Writes a semantic type's conversions: into and out of its representation, and to and from
	/// the type it refines.
	/// </summary>
	private static void WriteConversions(CodeBlocker code, string name, string underlying, SchemaSemanticType? refined)
	{
		code.NewLine();
		code.WriteLine("/// <summary>");
		code.WriteLine($"/// Explicit: a bare value never becomes a {name} by accident.");
		code.WriteLine("/// </summary>");
		code.WriteLine($"public static explicit operator {name}({underlying} value) => new(value);");

		code.NewLine();
		code.WriteLine("/// <summary>");
		code.WriteLine("/// Explicit in this direction too: leaving the type is a decision as well.");
		code.WriteLine("/// </summary>");
		code.WriteLine($"public static explicit operator {underlying}({name} value) => value.Value;");

		if (refined is null)
		{
			return;
		}

		string broader = CSharpKeywords.Identifier(refined.Name);

		code.NewLine();
		code.WriteLine("/// <summary>");
		code.WriteLine($"/// Widening is implicit: this is {Article(broader)} {broader}.");
		code.WriteLine("/// </summary>");
		code.WriteLine($"public static implicit operator {broader}({name} value) => ({broader})value.Value;");

		code.NewLine();
		code.WriteLine("/// <summary>");
		code.WriteLine($"/// Narrowing is explicit: not every {broader} is {Article(name)} {name}.");
		code.WriteLine("/// </summary>");
		code.WriteLine($"public static explicit operator {name}({broader} value) => new(value.Value);");
	}

	/// <summary>
	/// Picks the indefinite article for a type name, so a generated comment reads as a sentence.
	/// </summary>
	private static string Article(string name) =>
		name.Length > 0 && "AEIOU".Contains(char.ToUpperInvariant(name[0]), StringComparison.Ordinal) ? "an" : "a";

	/// <summary>
	/// Emits an interface: the declaration an implementation is written against.
	/// </summary>
	/// <remarks>
	/// Named exactly as the schema names it, with no <c>I</c> prefix. The prefix is the C#
	/// convention and it is not available here: the compiled type's name is what
	/// <see cref="Models.ClrTypeImporter"/> reads back, so adding one would have to be stripped
	/// again, and stripping cannot tell a prefix from a first letter - a schema interface called
	/// <c>Item</c> would come back as <c>tem</c>. This is the same rule the classes and the enums
	/// follow, for the same reason.
	/// </remarks>
	private static string GenerateInterface(SchemaInterface schemaInterface, CodeNamespace codeNamespace)
	{
		using CodeBlocker code = CodeBlocker.Create();
		WriteHeader(code, codeNamespace);

		WriteDocComment(code, schemaInterface.Description);
		code.WriteLine($"public interface {CSharpKeywords.Identifier(schemaInterface.Name)}");

		using (new Scope(code))
		{
			bool first = true;
			foreach (SchemaFunction function in schemaInterface.Functions)
			{
				if (!first)
				{
					code.NewLine();
				}

				first = false;
				WriteFunction(code, function);
			}
		}

		return code.ToString();
	}

	/// <summary>
	/// Writes one signature.
	/// </summary>
	/// <remarks>
	/// A query is recorded with an attribute rather than spelled, because C# has no way to say that
	/// calling a method leaves the receiver alone - the one of the five conventions the language
	/// cannot express.
	/// </remarks>
	private static void WriteFunction(CodeBlocker code, SchemaFunction function)
	{
		WriteDocComment(code, function.Description);

		if (function.IsQuery)
		{
			code.WriteLine("[ktsu.Schema.Runtime.SchemaQuery]");
		}

		IEnumerable<string> parameters = function.Parameters.Select(MapParameter);
		string returned = MapReturnType(function.ReturnType);

		code.WriteLine($"{returned} {CSharpKeywords.Identifier(function.Name)}({string.Join(", ", parameters)});");
	}

	/// <summary>
	/// Maps a return type, which is the one position where a function returning nothing can be
	/// spelled.
	/// </summary>
	/// <remarks>
	/// <c>void</c> is not a type C# has anywhere else - there is no field, property or parameter of
	/// one - so <see cref="MapType"/> cannot answer this and a <c>Void</c> reaching it is a schema
	/// that says a member carries no value, which is not a declaration C# has either.
	/// </remarks>
	private static string MapReturnType(BaseType type) =>
		type is Models.Types.Void ? "void" : MapType(type);

	/// <summary>
	/// Writes a parameter, which is the one place direction changes how the type is spelled.
	/// </summary>
	/// <remarks>
	/// On a <see cref="Span"/> direction describes the elements rather than the view, so an
	/// <c>In</c> one is a <c>ReadOnlySpan</c> and the others are a <c>Span</c> - the same reading
	/// the C++ generator gives it, where the difference is a <c>const</c> on the element.
	/// <para>
	/// On everything else it is the parameter modifier: <c>out</c>, <c>ref</c>, and nothing at all
	/// for <c>In</c>, because an ordinary by-value parameter is already one the caller supplies and
	/// the callee does not modify. Each of the three reads back off the compiled signature, which
	/// is what keeps direction in the round trip.
	/// </para>
	/// </remarks>
	private static string MapParameter(SchemaParameter parameter)
	{
		string name = CSharpKeywords.Identifier(parameter.Name);

		// A view is spelled by the parameter rather than by MapType, because the direction that
		// decides which of the two span types it is belongs to the parameter and a type has no
		// route back to one.
		if (parameter.Type is Span span)
		{
			string element = MapType(span.ElementType);
			string view = parameter.Direction == ParameterDirection.In
				? $"System.ReadOnlySpan<{element}>"
				: $"System.Span<{element}>";

			return $"{view} {name}";
		}

		string type = MapType(parameter.Type);

		string modifier = parameter.Direction switch
		{
			ParameterDirection.Out => "out ",
			ParameterDirection.InOut => "ref ",
			_ => string.Empty,
		};

		return $"{modifier}{type} {name}";
	}

	private static void WriteHeader(CodeBlocker code, CodeNamespace codeNamespace)
	{
		code.WriteLine("// <auto-generated>");
		code.WriteLine("//     Generated from a .schema.json file by ktsu.Schema. Edits will be lost.");
		code.WriteLine("// </auto-generated>");
		code.NewLine();
		code.WriteLine("#nullable enable");
		code.NewLine();

		if (!string.IsNullOrEmpty(codeNamespace))
		{
			// File-scoped, so the body below needs no extra indentation.
			code.WriteLine($"namespace {codeNamespace};");
			code.NewLine();
		}
	}

	/// <summary>
	/// Writes a description out as an XML doc comment, which is the whole reason descriptions are
	/// carried on every element.
	/// </summary>
	private static void WriteDocComment(CodeBlocker code, string description)
	{
		if (string.IsNullOrEmpty(description))
		{
			return;
		}

		code.WriteLine("/// <summary>");
		foreach (string line in description.Split('\n'))
		{
			code.WriteLine($"/// {Escape(line.TrimEnd('\r'))}");
		}

		code.WriteLine("/// </summary>");
	}

	/// <summary>
	/// Records a keyed map's key member on the generated property.
	/// </summary>
	/// <remarks>
	/// The emitted dictionary type says what the key is but not where it comes from, so without
	/// this the schema could not be reconstructed from the generated code.
	/// </remarks>
	private static void WriteSchemaKeyAttribute(CodeBlocker code, BaseType type)
	{
		if (type is Models.Types.Array arrayType &&
			string.Equals(arrayType.Container, ContainerName.MapName, StringComparison.Ordinal) &&
			!string.IsNullOrEmpty(arrayType.Key))
		{
			code.WriteLine($"[ktsu.Schema.Runtime.SchemaKey(\"{arrayType.Key}\")]");
		}
	}

	/// <summary>
	/// Records semantic metadata on whatever generated declaration carries it.
	/// </summary>
	/// <remarks>
	/// A C# type says what a value is and nothing about what it means, so without these the unit,
	/// range, default, interpolation and network encoding would be dropped by the reimport the
	/// round trip is built on - the same problem <see cref="WriteSchemaKeyAttribute"/> solves for
	/// a keyed map.
	/// <para>
	/// Written against <see cref="ISchemaMetadataCarrier"/> rather than a member, because a
	/// semantic type carries the same six properties and they are emitted the same way. That is
	/// the same reason validation reads the interface rather than each carrier growing a copy.
	/// </para>
	/// </remarks>
	private static void WriteMetadataAttributes(CodeBlocker code, ISchemaMetadataCarrier carrier)
	{
		if (carrier.Unit is not null)
		{
			code.WriteLine($"[ktsu.Schema.Runtime.SchemaUnit({Quote(carrier.Unit)})]");
		}

		if (carrier.Range is MemberRange range)
		{
			string wrap = range.Wrap ? ", Wrap = true" : string.Empty;
			code.WriteLine($"[ktsu.Schema.Runtime.SchemaRange({Literal(range.Minimum)}, {Literal(range.Maximum)}{wrap})]");
		}

		if (DefaultArgument(carrier.DefaultValue) is string argument)
		{
			code.WriteLine($"[ktsu.Schema.Runtime.SchemaDefault({argument})]");
		}

		if (carrier.Interpolation is not Interpolation.None)
		{
			code.WriteLine($"[ktsu.Schema.Runtime.SchemaInterpolation(ktsu.Schema.Models.Metadata.Interpolation.{carrier.Interpolation})]");
		}

		if (carrier.Network is MemberNetwork network)
		{
			code.WriteLine($"[ktsu.Schema.Runtime.SchemaNetwork({Literal(network.Quantise)}, {(network.Delta ? "true" : "false")})]");
		}

		if (carrier.Editor is not null)
		{
			code.WriteLine($"[ktsu.Schema.Runtime.SchemaEditorHint({Quote(carrier.Editor)})]");
		}
	}

	/// <summary>
	/// Writes a default as the argument that binds the right <c>SchemaDefault</c> constructor.
	/// </summary>
	/// <returns>The argument text, or null when the member has no default.</returns>
	private static string? DefaultArgument(MemberDefault? value) => value switch
	{
		NumberDefault number => Literal(number.Value),
		BooleanDefault boolean => boolean.Value ? "true" : "false",
		TextDefault text => Quote(text.Value),
		_ => null,
	};

	/// <summary>
	/// Writes a double as a C# literal of type <see cref="double"/>.
	/// </summary>
	/// <remarks>
	/// The <c>D</c> suffix is what picks the double overload of <c>SchemaDefault</c> rather than
	/// the one taking a bool or a string, and round-trip formatting is what keeps the value the
	/// schema holds - a bound written back one bit short is a range that no longer contains what
	/// it used to. Invariant, because a machine writing a decimal comma would emit source that
	/// does not compile.
	/// </remarks>
	private static string Literal(double value) => value switch
	{
		double.PositiveInfinity => "double.PositiveInfinity",
		double.NegativeInfinity => "double.NegativeInfinity",
		_ when double.IsNaN(value) => "double.NaN",
		_ => $"{value.ToString("R", CultureInfo.InvariantCulture)}D",
	};

	/// <summary>
	/// Writes text as a C# string literal.
	/// </summary>
	private static string Quote(string text) =>
		$"\"{text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

	private static string Escape(string text) =>
		text.Replace("&", "&amp;", StringComparison.Ordinal)
			.Replace("<", "&lt;", StringComparison.Ordinal)
			.Replace(">", "&gt;", StringComparison.Ordinal);

	/// <summary>
	/// Maps a schema type to the C# type that represents it.
	/// </summary>
	/// <remarks>
	/// Every case here has a counterpart in the import mapping, and the round-trip test fails if
	/// one of them drifts.
	/// </remarks>
	private static string MapType(BaseType type) => type switch
	{
		Bool => "bool",
		Int => "int",
		Long => "long",
		Float => "float",
		Double => "double",
		Models.Types.String => "string",
		Models.Types.DateTime => "System.DateTime",
		Models.Types.TimeSpan => "System.TimeSpan",

		// The colours before their vector bases: ColorRGB derives from Vector3.
		ColorRGB => "ktsu.Schema.Runtime.ColorRgb",
		ColorRGBA => "ktsu.Schema.Runtime.ColorRgba",

		// System.Numerics vectors hold floats and nothing else, so they represent a vector of
		// floats and only that. A vector of anything else is one of this library's, which is
		// generic over the component precisely because the System.Numerics ones are not.
		Vector2 { ElementType: Float } => "System.Numerics.Vector2",
		Vector3 { ElementType: Float } => "System.Numerics.Vector3",
		Vector4 { ElementType: Float } => "System.Numerics.Vector4",
		Vector2 vector => $"ktsu.Schema.Runtime.Vector2<{MapType(vector.ElementType)}>",
		Vector3 vector => $"ktsu.Schema.Runtime.Vector3<{MapType(vector.ElementType)}>",
		Vector4 vector => $"ktsu.Schema.Runtime.Vector4<{MapType(vector.ElementType)}>",

		// What the handle names is a type argument rather than anything stored, so this says which
		// handles may be passed where without saying that a handle to a mesh is laid out
		// differently from a handle to a texture - it is not.
		Handle handle => $"ktsu.Schema.Runtime.Handle<{MapType(handle.ElementType)}>",

		// The struct emitted for the semantic type, named the same way a class or an enum is.
		Semantic semanticType => CSharpKeywords.Identifier(semanticType.SemanticTypeName),

		Models.Types.Enum enumType => CSharpKeywords.Identifier(enumType.EnumName),
		Models.Types.Object objectType => CSharpKeywords.Identifier(objectType.ClassName),
		Models.Types.Interface interfaceType => CSharpKeywords.Identifier(interfaceType.InterfaceName),
		Models.Types.Array arrayType => MapArray(arrayType),

		Optional optional => $"ktsu.Schema.Runtime.Optional<{MapType(optional.ElementType)}>",
		Models.Types.Result result => MapResult(result),

		// A member left as None is reported by validation as a warning, not an error, so
		// generation is not refused for it. object keeps the output compiling.
		None => UnspeakableType,

		// A Span outside a signature, and a Void outside a return type: both are declarations C#
		// has no field or member for at all. A class that travels as bytes may hold neither, so
		// neither defeats that promise.
		_ => UnspeakableType,
	};

	/// <summary>
	/// Maps a fallible return, whose error type is the schema's rather than this signature's.
	/// </summary>
	/// <remarks>
	/// Two arities rather than one, because C# has no <c>void</c> type argument:
	/// <c>Result&lt;Void&gt;</c> is the form carrying only an outcome, and the arity is what tells
	/// the two apart when the generated code is read back.
	/// <para>
	/// A schema that returns a <c>Result</c> and names no error enum is one validation refuses, so
	/// reaching here without one means the generator was called directly on a schema nobody
	/// validated. It falls back rather than emitting a name that does not resolve.
	/// </para>
	/// </remarks>
	private static string MapResult(Models.Types.Result result)
	{
		if (result.ParentSchema?.ErrorType is not EnumName error || string.IsNullOrEmpty(error))
		{
			return UnspeakableType;
		}

		string failure = CSharpKeywords.Identifier(error);

		return result.ElementType is Models.Types.Void
			? $"ktsu.Schema.Runtime.Result<{failure}>"
			: $"ktsu.Schema.Runtime.Result<{MapType(result.ElementType)}, {failure}>";
	}

	private static string MapArray(Models.Types.Array arrayType)
	{
		string elementType = MapType(arrayType.ElementType);

		if (!string.Equals(arrayType.Container, ContainerName.MapName, StringComparison.Ordinal))
		{
			// vector, and any container a consumer has invented, is a sequence.
			return $"System.Collections.Generic.List<{elementType}>";
		}

		return $"System.Collections.Generic.Dictionary<{MapKeyType(arrayType)}, {elementType}>";
	}

	/// <summary>
	/// Gets the C# type of a map's key, taken from the member the array keys on.
	/// </summary>
	/// <remarks>
	/// Validation guarantees a keyed map names a primitive member of its element class, so this
	/// only falls back when the schema is one validation would have refused generation for.
	/// </remarks>
	private static string MapKeyType(Models.Types.Array arrayType)
	{
		if (arrayType.TryGetKeyMember(out SchemaMember? keyMember) && keyMember is not null)
		{
			return MapType(keyMember.Type);
		}

		return "string";
	}

	/// <summary>
	/// Gets the initialiser a property is given: the member's default when it has one, and
	/// otherwise whatever nullable reference analysis needs.
	/// </summary>
	/// <remarks>
	/// A default that is only recorded in an attribute is a default in name only - an instance of
	/// the generated type would still start at zero. The attribute is what lets the default be
	/// read back; this is what makes it true of the object.
	/// </remarks>
	private static string InitialiserFor(SchemaMember member) =>
		DefaultInitialiserFor(member) ?? member.Type switch
		{
			Models.Types.String => " = string.Empty;",
			Models.Types.Array arrayType => $" = new {MapArray(arrayType)}();",
			Models.Types.Object objectType => ObjectInitialiserFor(objectType),
			_ => string.Empty,
		};

	/// <summary>
	/// Gets the initialiser a member holding another class is given.
	/// </summary>
	/// <remarks>
	/// A class starts at null and is given one, because a member that has to be constructed before
	/// it can be read is not the thing the schema described. A class that travels as bytes is a
	/// struct, and a struct is already that thing - so it is given none, which is also what keeps a
	/// promising class free of the constructor an initialiser would oblige it to declare.
	/// <para>
	/// The class a member names is only unresolvable on a schema validation would have refused
	/// generation for, and constructing one is what every class did before, so that is where this
	/// falls back to.
	/// </para>
	/// </remarks>
	private static string ObjectInitialiserFor(Models.Types.Object objectType) =>
		objectType.Class?.TravelsAsBytes == true
			? string.Empty
			: $" = new {CSharpKeywords.Identifier(objectType.ClassName)}();";

	/// <summary>
	/// Gets the initialiser for a member's default, or null when it has none that fits.
	/// </summary>
	/// <remarks>
	/// A default of a kind the member cannot hold is a validation error, and generation is refused
	/// for a schema that has one - so the mismatched cases here are only reachable by calling this
	/// generator directly on a schema that was never validated. They fall through to the type's
	/// own initialiser rather than emitting source that does not compile.
	/// </remarks>
	private static string? DefaultInitialiserFor(SchemaMember member) => (member.DefaultValue, member.Type) switch
	{
		(NumberDefault number, Int) => $" = {(long)number.Value};",
		(NumberDefault number, Long) => $" = {(long)number.Value}L;",
		(NumberDefault number, Float) when !double.IsFinite(number.Value) => $" = float.{NonFinite(number.Value)};",
		(NumberDefault number, Double) when !double.IsFinite(number.Value) => $" = double.{NonFinite(number.Value)};",
		(NumberDefault number, Float) => $" = {number.Value.ToString("R", CultureInfo.InvariantCulture)}f;",
		(NumberDefault number, Double) => $" = {Literal(number.Value)};",
		(BooleanDefault boolean, Bool) => $" = {(boolean.Value ? "true" : "false")};",
		(TextDefault text, Models.Types.String) => $" = {Quote(text.Value)};",
		(TextDefault text, Models.Types.Enum enumType) => $" = {CSharpKeywords.Identifier(enumType.EnumName)}.{CSharpKeywords.Identifier(text.Value)};",
		_ => null,
	};

	/// <summary>
	/// Names one of the two values a floating-point number can hold that has no literal.
	/// </summary>
	/// <remarks>
	/// The C++ generator refuses these, because C++ can only reach them through
	/// <c>&lt;limits&gt;</c> and a generated header quietly growing an include to write a default
	/// nobody meant is worse than being told. C# has them as named members of the type itself, so
	/// there is nothing to refuse: writing <c>NaNf</c>, which is what the round-trip form gave, was
	/// the only problem.
	/// </remarks>
	/// <param name="value">The value.</param>
	/// <returns>The member's name.</returns>
	/// <remarks>
	/// Guards rather than the constant patterns this would otherwise read as, because a pattern
	/// matches by equality and <see cref="double.NaN"/> is not equal to itself - so <c>double.NaN
	/// =&gt; "NaN"</c> is an arm that can never be taken, and the one value most likely to reach
	/// here would fall through to the last one.
	/// </remarks>
	private static string NonFinite(double value) => value switch
	{
		_ when double.IsNaN(value) => "NaN",
		_ when double.IsPositiveInfinity(value) => "PositiveInfinity",
		_ => "NegativeInfinity",
	};

}
