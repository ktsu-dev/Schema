// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Collections.ObjectModel;
using System.Globalization;
using ktsu.Semantics.Quantities;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Types;

/// <summary>
/// Validation support for <see cref="Schema"/>.
/// </summary>
public partial class Schema
{
	/// <summary>
	/// Validates the referential integrity of the schema and returns any issues found.
	/// </summary>
	/// <remarks>
	/// Checks that enum, class, and array key references resolve, that names are unique,
	/// and that data sources and code generators are fully configured. A schema with no
	/// <see cref="SchemaValidationSeverity.Error"/> issues is safe to consume for code
	/// generation or data validation.
	/// </remarks>
	/// <returns>The collection of issues found; empty if the schema is fully valid.</returns>
	public Collection<SchemaValidationIssue> Validate()
	{
		Collection<SchemaValidationIssue> issues = [];

		ValidateUniqueNames(issues);
		ValidateClasses(issues);
		ValidateEnums(issues);
		ValidateInterfaces(issues);
		ValidateSemanticTypes(issues);
		ValidateErrorType(issues);
		ValidateDataSources(issues);
		ValidateCodeGenerators(issues);

		return issues;
	}

	private void ValidateUniqueNames(Collection<SchemaValidationIssue> issues)
	{
		ReportDuplicates(issues, ClassesInternal.Select(c => c.Name.ToString()), "class");
		ReportDuplicates(issues, EnumsInternal.Select(e => e.Name.ToString()), "enum");
		ReportDuplicates(issues, InterfacesInternal.Select(i => i.Name.ToString()), "interface");
		ReportDuplicates(issues, SemanticTypesInternal.Select(t => t.Name.ToString()), "semantic type");
		ReportDuplicates(issues, DataSourcesInternal.Select(d => d.Name.ToString()), "data source");
		ReportDuplicates(issues, CodeGeneratorsInternal.Select(g => g.Name.ToString()), "code generator");
	}

	private static void ReportDuplicates(Collection<SchemaValidationIssue> issues, IEnumerable<string> names, string kind)
	{
		foreach (string name in names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key))
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = name,
				Message = $"Duplicate {kind} name '{name}'.",
			});
		}
	}

	/// <summary>
	/// Renders a name for use in an issue path, standing in for a name that is empty.
	/// </summary>
	private static string PathSegment(string name) => string.IsNullOrEmpty(name) ? "(unnamed)" : name;

	/// <summary>
	/// Reports an element whose name is empty. Such an element cannot be code-generated, and
	/// duplicate detection only catches multiple empty names by accident.
	/// </summary>
	private static void ValidateNameNotEmpty(Collection<SchemaValidationIssue> issues, string name, string kind, string path, ISchemaElement? element)
	{
		if (string.IsNullOrEmpty(name))
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = path,
				Message = $"{kind} has an empty name.",
				Element = element,
			});
		}
	}

	private void ValidateClasses(Collection<SchemaValidationIssue> issues)
	{
		foreach (SchemaClass schemaClass in ClassesInternal)
		{
			string classPath = PathSegment(schemaClass.Name);
			ValidateNameNotEmpty(issues, schemaClass.Name, "Class", classPath, schemaClass);

			ReportDuplicates(issues, schemaClass.Members.Select(m => $"{schemaClass.Name}.{m.Name}"), "member");

			foreach (SchemaMember member in schemaClass.Members)
			{
				string memberPath = $"{classPath}.{PathSegment(member.Name)}";
				ValidateNameNotEmpty(issues, member.Name, "Member", memberPath, member);

				if (member.Type is None)
				{
					// A valid intermediate editing state, but not a generatable schema.
					issues.Add(new()
					{
						Severity = SchemaValidationSeverity.Warning,
						Path = memberPath,
						Message = "Member does not have a type set.",
						Element = member,
					});
				}

				ValidateType(issues, member.Type, memberPath, member);
				ValidateMetadata(issues, member, member.Type, memberPath, member, "member");
				ValidateTravelsAsBytes(issues, schemaClass, member, memberPath);
			}
		}
	}

	/// <summary>
	/// A class that travels as raw bytes may only hold members that travel the same way.
	/// </summary>
	/// <remarks>
	/// The promise is that an instance is copied whole without anyone reading a field on the way,
	/// so a member holding a reference to something elsewhere - text, a collection, a view, a
	/// class that makes no such promise - would arrive as an address that means nothing where it
	/// landed. Reported against the member rather than the class, because the member is what the
	/// author changes.
	/// </remarks>
	private void ValidateTravelsAsBytes(Collection<SchemaValidationIssue> issues, SchemaClass schemaClass, SchemaMember member, string path)
	{
		// A member with no type is already reported as one; saying it cannot travel as bytes as
		// well would be two issues for the same unfinished edit.
		if (!schemaClass.TravelsAsBytes || member.Type is None)
		{
			return;
		}

		string? reason = WhyItCannotTravelAsBytes(member.Type);
		if (reason is not null)
		{
			Report(issues, path, member, $"Class '{schemaClass.Name}' travels as bytes, but member '{member.Name}' {reason}.");
		}
	}

	/// <summary>
	/// Says why a type cannot be copied whole, or nothing when it can.
	/// </summary>
	/// <remarks>
	/// One level deep by design where a class is named: a class either makes the promise or does
	/// not, and it makes it for its own members in turn. So this reads a flag rather than walking
	/// a graph, and two classes holding each other cannot hang it.
	/// </remarks>
	private string? WhyItCannotTravelAsBytes(BaseType type) => type switch
	{
		Types.String => "is text, which is held elsewhere and reached by reference",

		Array => "is a collection, whose elements are held elsewhere",

		Span => "is a view onto memory the class does not own",

		// Trivially copyable in some languages and not in others, and in none of them is the
		// position of what it wraps something the schema can promise.
		Optional or Result => $"is carried by a {type.TypeName}, whose layout is the target language's to choose",

		Interface => "is an interface, which is a reference to whatever implements it",

		Types.Void => "carries no value",

		// A handle is an identifier the holder does not own - an index and a generation - so it
		// travels as bytes whatever it identifies.
		Handle => null,

		Vector vectorType => WhyItCannotTravelAsBytes(vectorType.ElementType),

		Semantic { Declaration: SchemaSemanticType declaration } => WhyARepresentationCannotTravelAsBytes(declaration.Representation()),

		Types.Object objectType => WhyANamedClassCannotTravelAsBytes(objectType.ClassName),

		_ => null,
	};

	/// <summary>
	/// Says why what a semantic type is represented as cannot travel as bytes, or nothing when it
	/// can.
	/// </summary>
	/// <remarks>
	/// A representation that is still semantic means the chain of refinement never reached
	/// anything real - a cycle, or a name that does not resolve - both of which
	/// <see cref="ValidateUnderlyingType"/> already reports. Stopping here is what keeps a cycle a
	/// reported error rather than a recursion with no bottom.
	/// </remarks>
	private string? WhyARepresentationCannotTravelAsBytes(BaseType representation) =>
		representation is Semantic ? null : WhyItCannotTravelAsBytes(representation);

	/// <summary>
	/// Says why a named class cannot be held by one that travels as bytes, or nothing when it can.
	/// </summary>
	/// <remarks>
	/// A class that does not resolve is already reported by <see cref="ValidateClassReference"/>,
	/// and is not refused again here.
	/// </remarks>
	private string? WhyANamedClassCannotTravelAsBytes(Names.ClassName className) =>
		TryGetClass(className, out SchemaClass? referenced) && referenced?.TravelsAsBytes == false
			? $"is a '{className}', which does not travel as bytes"
			: null;

	/// <summary>
	/// Checks the semantic metadata on a member: its unit, range, default, interpolation and
	/// network encoding.
	/// </summary>
	/// <remarks>
	/// These are checked here rather than at the point they are set, because most of them are
	/// only wrong in combination — a default is out of range only relative to a range, a wrap
	/// flag means nothing without one — and a schema being edited passes through inconsistent
	/// states that it would be unhelpful to reject one keystroke at a time.
	/// </remarks>
	private static void ValidateMetadata(Collection<SchemaValidationIssue> issues, ISchemaMetadataCarrier carrier, BaseType type, string path, ISchemaElement element, string kind)
	{
		ValidateMemberUnit(issues, carrier, type, path, element, kind);
		ValidateMemberRange(issues, carrier, type, path, element, kind);
		ValidateMemberDefault(issues, carrier, type, path, element, kind);
		ValidateMemberInterpolation(issues, carrier, type, path, element);
		ValidateMemberNetwork(issues, carrier, type, path, element);
	}

	/// <summary>
	/// A unit has to resolve, and has to be on something that can carry one.
	/// </summary>
	private static void ValidateMemberUnit(Collection<SchemaValidationIssue> issues, ISchemaMetadataCarrier carrier, BaseType type, string path, ISchemaElement element, string kind)
	{
		if (carrier.Unit is null)
		{
			return;
		}

		if (!CanCarryUnit(type))
		{
			Report(issues, path, element, $"A unit is meaningless on a {type.TypeName} {kind}; only numeric and vector members measure something.");
			return;
		}

		if (!UnitRegistry.TryResolve(carrier.Unit, out IUnit? _, out string error))
		{
			Report(issues, path, element, error);
		}
	}

	/// <summary>
	/// A range has to be satisfiable, has to be on something orderable, and a wrap flag needs
	/// a range to wrap into.
	/// </summary>
	private static void ValidateMemberRange(Collection<SchemaValidationIssue> issues, ISchemaMetadataCarrier carrier, BaseType type, string path, ISchemaElement element, string kind)
	{
		if (carrier.Range is null)
		{
			return;
		}

		if (!type.IsNumeric && !IsVector(type))
		{
			Report(issues, path, element, $"A range is meaningless on a {type.TypeName} {kind}.");
			return;
		}

		if (!carrier.Range.IsWellFormed)
		{
			Report(issues, path, element, $"The range minimum ({Number(carrier.Range.Minimum)}) is above its maximum ({Number(carrier.Range.Maximum)}), so no value satisfies it.");

			// A backwards range has no width to ask about, and saying so as well would be two
			// messages for one mistake.
			return;
		}

		// The range is well formed by here, so its maximum is at or above its minimum and this
		// says the two are the same value -- without comparing two doubles for equality, which is
		// brittle enough that the analyzers refuse it.
		if (carrier.Range.Wrap && carrier.Range.Maximum <= carrier.Range.Minimum)
		{
			Report(issues, path, element, "A wrapping range of zero width has no values to wrap into.");
		}
	}

	/// <summary>
	/// A default has to be of the member's own kind, and inside its range.
	/// </summary>
	private static void ValidateMemberDefault(Collection<SchemaValidationIssue> issues, ISchemaMetadataCarrier carrier, BaseType type, string path, ISchemaElement element, string kind)
	{
		switch (carrier.DefaultValue)
		{
			case null:
				return;

			case NumberDefault number:
				if (!type.IsNumeric && !IsVector(type))
				{
					Report(issues, path, element, $"A numeric default does not fit a {type.TypeName} {kind}.");
					return;
				}

				// double.IsInteger rather than a comparison against Math.Truncate: it asks the
				// question directly, and it does not compare two doubles for equality. It also
				// answers false for an infinity, which is not a whole number and cannot narrow to
				// one - the comparison called that whole, since truncating an infinity returns it.
				if (type.IsIntegral && !double.IsInteger(number.Value))
				{
					Report(issues, path, element, $"The default {Number(number.Value)} is not a whole number, but the member is {type.TypeName}.");
				}

				// A wrapping range is a period rather than a bound, so a value outside it is
				// un-normalised rather than wrong -- the same reading a validator must take of
				// live data, applied here to the default.
				if (carrier.Range is { Wrap: false } range && (number.Value < range.Minimum || number.Value > range.Maximum))
				{
					Report(issues, path, element, $"The default {Number(number.Value)} is outside the member's own range {range}.");
				}

				return;

			case BooleanDefault:
				if (type is not Bool)
				{
					Report(issues, path, element, $"A boolean default does not fit a {type.TypeName} {kind}.");
				}

				return;

			case TextDefault text:
				ValidateTextDefault(issues, type, path, element, kind, text);
				return;

			default:
				Report(issues, path, element, $"Unrecognised default of kind {carrier.DefaultValue.GetType().Name}.");
				return;
		}
	}

	/// <summary>
	/// A textual default names an enum value or supplies a string; anything else is a mismatch.
	/// </summary>
	private static void ValidateTextDefault(Collection<SchemaValidationIssue> issues, BaseType type, string path, ISchemaElement element, string kind, TextDefault text)
	{
		if (type is Types.String)
		{
			return;
		}

		if (type is not Types.Enum enumType)
		{
			Report(issues, path, element, $"A textual default does not fit a {type.TypeName} {kind}.");
			return;
		}

		// An unresolved enum reference is already reported by ValidateType; saying so again
		// here would be two messages for one problem.
		SchemaEnum? target = enumType.ParentMember?.ParentSchema?.TryGetEnum(enumType.EnumName, out SchemaEnum? found) == true ? found : null;
		if (target is null)
		{
			return;
		}

		if (!target.Values.Any(value => string.Equals(value, text.Value, StringComparison.Ordinal)))
		{
			string known = string.Join(", ", target.Values.Select(value => value.ToString()));
			Report(issues, path, element, $"The default '{text.Value}' is not a value of enum '{enumType.EnumName}'. Its values are: {known}.");
		}
	}

	/// <summary>
	/// Interpolation is a claim that the member has a meaningful midpoint.
	/// </summary>
	private static void ValidateMemberInterpolation(Collection<SchemaValidationIssue> issues, ISchemaMetadataCarrier carrier, BaseType type, string path, ISchemaElement element)
	{
		if (carrier.Interpolation is Interpolation.None)
		{
			return;
		}

		if (!type.IsNumeric && !IsVector(type))
		{
			Report(issues, path, element, $"A {type.TypeName} member has no meaningful value between two states, so it cannot be interpolated.");
			return;
		}

		// Stepping is a scheduling decision, not a geometric one, so it is the one mode that
		// makes sense without an arc.
		if (carrier.Interpolation is Interpolation.Spherical && carrier.Range is { Wrap: false } && !IsVector(type))
		{
			Report(issues, path, element, "Spherical interpolation takes the shortest arc, which needs a cyclic member: give the range wrap = true, or use linear interpolation.", SchemaValidationSeverity.Warning);
		}
	}

	/// <summary>
	/// A quantisation step has to be a step.
	/// </summary>
	private static void ValidateMemberNetwork(Collection<SchemaValidationIssue> issues, ISchemaMetadataCarrier carrier, BaseType type, string path, ISchemaElement element)
	{
		if (carrier.Network is null)
		{
			return;
		}

		if (carrier.Network.Quantise < 0.0)
		{
			Report(issues, path, element, $"A quantisation step must not be negative, but this one is {Number(carrier.Network.Quantise)}.");
		}

		if (carrier.Network.IsQuantised && !type.IsNumeric && !IsVector(type))
		{
			Report(issues, path, element, $"A {type.TypeName} member has no numeric value to quantise.");
		}

		// Quantising more coarsely than the whole range leaves one representable value.
		if (carrier.Network.IsQuantised && carrier.Range is { } bounds && bounds.IsWellFormed &&
			carrier.Network.Quantise > bounds.Maximum - bounds.Minimum)
		{
			Report(issues, path, element, $"The quantisation step {Number(carrier.Network.Quantise)} is wider than the member's whole range {bounds}, so every value would encode the same.", SchemaValidationSeverity.Warning);
		}
	}

	/// <summary>
	/// Only something with a magnitude measures anything.
	/// </summary>
	private static bool CanCarryUnit(BaseType type) => type.IsNumeric || IsVector(type);

	private static bool IsVector(BaseType type) => type is Vector2 or Vector3 or Vector4;

	/// <summary>
	/// Formats a number for a message, culture-invariantly: a validation message that says
	/// "0,5" on one machine and "0.5" on another is a support problem.
	/// </summary>
	private static string Number(double value) => value.ToString(CultureInfo.InvariantCulture);

	private static void Report(Collection<SchemaValidationIssue> issues, string path, ISchemaElement element, string message, SchemaValidationSeverity severity = SchemaValidationSeverity.Error) =>
		issues.Add(new()
		{
			Severity = severity,
			Path = path,
			Message = message,
			Element = element,
		});

	private void ValidateEnums(Collection<SchemaValidationIssue> issues)
	{
		foreach (SchemaEnum schemaEnum in EnumsInternal)
		{
			string enumPath = PathSegment(schemaEnum.Name);
			ValidateNameNotEmpty(issues, schemaEnum.Name, "Enum", enumPath, schemaEnum);

			ReportDuplicates(issues, schemaEnum.Values.Select(v => $"{schemaEnum.Name}.{v}"), "enum value");

			foreach (Names.EnumValueName value in schemaEnum.Values)
			{
				ValidateNameNotEmpty(issues, value, "Enum value", $"{enumPath}.{PathSegment(value)}", schemaEnum);
			}
		}
	}

	/// <summary>
	/// Checks the semantic types: what they shim, that the chain of refinement terminates, and
	/// that their metadata makes sense for what they are represented as.
	/// </summary>
	private void ValidateSemanticTypes(Collection<SchemaValidationIssue> issues)
	{
		foreach (SchemaSemanticType semanticType in SemanticTypesInternal)
		{
			string path = PathSegment(semanticType.Name);
			ValidateNameNotEmpty(issues, semanticType.Name, "Semantic type", path, semanticType);
			ValidateUnderlyingType(issues, semanticType, path);
			ValidateMetadata(issues, semanticType, semanticType.Representation(), path, semanticType, "semantic type");
		}
	}

	/// <summary>
	/// A semantic type is a distinct name for something already representable. That is only
	/// meaningful over a type whose values would otherwise be interchangeable.
	/// </summary>
	private void ValidateUnderlyingType(Collection<SchemaValidationIssue> issues, SchemaSemanticType semanticType, string path)
	{
		switch (semanticType.UnderlyingType)
		{
			case None:
				Report(issues, path, semanticType, "Semantic type has no underlying type chosen.");
				return;

			// A class, an interface and an enum are already distinct types: naming one again
			// buys nothing and leaves it ambiguous which conversions apply.
			case Object or Interface or Types.Enum:
				Report(issues, path, semanticType, $"Semantic type is declared over a {semanticType.UnderlyingType.TypeName}, which is already a distinct type. A semantic type distinguishes values that share a representation.");
				return;

			// Wrapping a wrapper crosses two orthogonal axes: Span, Handle, Result and Optional
			// describe how a value travels, not what it means.
			case Array or WrapperType or Types.Void:
				Report(issues, path, semanticType, $"Semantic type is declared over a {semanticType.UnderlyingType.TypeName}, which describes how a value is carried rather than what it is.");
				return;

			case Semantic semantic:
				ValidateRefinement(issues, semanticType, semantic, path);
				return;

			default:
				return;
		}
	}

	/// <summary>
	/// A semantic type may refine another, but the chain has to terminate somewhere real.
	/// </summary>
	private void ValidateRefinement(Collection<SchemaValidationIssue> issues, SchemaSemanticType semanticType, Semantic reference, string path)
	{
		if (string.IsNullOrEmpty(reference.SemanticTypeName))
		{
			Report(issues, path, semanticType, "Semantic type refines a semantic type but does not name it.");
			return;
		}

		if (!TryGetSemanticType(reference.SemanticTypeName, out _))
		{
			Report(issues, path, semanticType, $"Semantic type refines '{reference.SemanticTypeName}', which this schema does not declare.");
			return;
		}

		// Refines() stops at the first type it has already seen, so a cycle shows up as a chain
		// that never reaches a non-semantic type rather than as a hang.
		if (!semanticType.Refines().Any(refined => refined.UnderlyingType is not Semantic))
		{
			Report(issues, path, semanticType, $"Semantic type '{semanticType.Name}' refines itself, directly or through a cycle, so it is represented as nothing.");
		}
	}

	/// <summary>
	/// Checks the interfaces: names, and that every signature is one a generator can emit.
	/// </summary>
	private void ValidateInterfaces(Collection<SchemaValidationIssue> issues)
	{
		foreach (SchemaInterface schemaInterface in InterfacesInternal)
		{
			string interfacePath = PathSegment(schemaInterface.Name);
			ValidateNameNotEmpty(issues, schemaInterface.Name, "Interface", interfacePath, schemaInterface);

			ReportDuplicates(
				issues,
				schemaInterface.Functions.Select(f => $"{schemaInterface.Name}.{f.Name}"),
				"function");

			foreach (SchemaFunction function in schemaInterface.Functions)
			{
				ValidateFunction(issues, function, $"{interfacePath}.{PathSegment(function.Name)}");
			}
		}
	}

	/// <summary>
	/// Checks one function's return type and parameters.
	/// </summary>
	private void ValidateFunction(Collection<SchemaValidationIssue> issues, SchemaFunction function, string path)
	{
		ValidateNameNotEmpty(issues, function.Name, "Function", path, function);
		ValidateReturnType(issues, function, path);
		ValidateQueryAnswers(issues, function, path);

		ReportDuplicates(
			issues,
			function.Parameters.Select(p => $"{path}.{p.Name}"),
			"parameter");

		foreach (SchemaParameter parameter in function.Parameters)
		{
			ValidateParameter(issues, parameter, $"{path}({PathSegment(parameter.Name)})", function);
		}
	}

	/// <summary>
	/// A query answers. One that returns nothing changes nothing and says nothing, so calling it
	/// cannot be observed at all.
	/// </summary>
	/// <remarks>
	/// A warning rather than an error, because it is a signature someone is part way through
	/// writing as often as it is one they meant - the return type is usually the half that is
	/// missing. <c>Result&lt;Void&gt;</c> is exempt: it answers whether the call succeeded, which
	/// is something.
	/// </remarks>
	private static void ValidateQueryAnswers(Collection<SchemaValidationIssue> issues, SchemaFunction function, string path)
	{
		if (function.IsQuery && function.ReturnType is Types.Void)
		{
			Report(
				issues,
				path,
				function,
				$"Function '{function.Name}' is a query but returns nothing, so calling it cannot be observed. Give it a return type, or let it be a command.",
				SchemaValidationSeverity.Warning);
		}
	}

	private void ValidateReturnType(Collection<SchemaValidationIssue> issues, SchemaFunction function, string path)
	{
		switch (function.ReturnType)
		{
			case None:
				Report(issues, path, function, "Function has no return type chosen. Use Void for a function that returns nothing.");
				return;

			// Result<Void> is the honest spelling of "can fail, produces nothing", so the
			// element of a Result is exempt from the Void check the parameters get.
			case Result { ElementType: None }:
				Report(issues, path, function, "Function returns Result with no value type chosen. Use Result<Void> for a call that can fail and produces nothing.");
				return;

			default:
				ValidateType(issues, function.ReturnType, path, function);
				return;
		}
	}

	private void ValidateParameter(Collection<SchemaValidationIssue> issues, SchemaParameter parameter, string path, ISchemaElement element)
	{
		switch (parameter.Type)
		{
			case None:
				Report(issues, path, element, "Parameter has no type chosen.");
				return;

			case Void:
				Report(issues, path, element, "Parameter is Void, which carries no value. Remove it.");
				return;

			// Fallibility describes the call, not an argument to it.
			case Result:
				Report(issues, path, element, "Parameter is a Result. Only a return type may be fallible.");
				return;

			// An Array is a collection a class owns; a sequence crossing a boundary is a Span,
			// which is a borrow valid for the call. Allowing an Array here would be the first
			// place ownership became ambiguous, which is the thing the conventions exist to
			// prevent.
			case Array:
				Report(issues, path, element, "Parameter is an Array, which is an owned collection. Pass a Span to borrow a sequence for the call.");
				return;

			default:
				ValidateType(issues, parameter.Type, path, element);
				return;
		}
	}

	private void ValidateType(Collection<SchemaValidationIssue> issues, BaseType type, string path, ISchemaElement? element)
	{
		switch (type)
		{
			case Enum enumType:
				ValidateEnumReference(issues, enumType, path, element);
				break;

			case Object objectType:
				ValidateClassReference(issues, objectType.ClassName, path, element);
				break;

			case Array arrayType:
				ValidateArray(issues, arrayType, path, element);
				break;

			case Interface interfaceType:
				ValidateInterfaceReference(issues, interfaceType, path, element);
				break;

			case Semantic semanticReference:
				ValidateSemanticReference(issues, semanticReference, path, element);
				break;

			// A vector's components are a type in their own right, checked as one.
			case Vector vectorType:
				ValidateVectorElement(issues, vectorType, path, element);
				break;

			// Before the wrapper arm below, which would otherwise swallow it: a Result says a
			// call can fail, and what a failure says is the schema's to declare.
			case Result result:
				ValidateResultHasAnErrorType(issues, path, element);
				ValidateType(issues, result.ElementType, path, element);
				break;

			// Every wrapper resolves to whatever it wraps, so a class named inside a Span,
			// Handle, Result or Optional is checked exactly as one named directly is.
			case WrapperType wrapper:
				ValidateType(issues, wrapper.ElementType, path, element);
				break;

			default:
				break;
		}
	}

	/// <summary>
	/// The enum a failure carries has to be one the schema declares.
	/// </summary>
	/// <remarks>
	/// Only checked when one is named. A schema that declares none is reported at the
	/// <see cref="Result"/> that needed it, which is where an author can see what to do about it,
	/// rather than at a root property they may never have set.
	/// </remarks>
	private void ValidateErrorType(Collection<SchemaValidationIssue> issues)
	{
		if (!string.IsNullOrEmpty(ErrorType) && !TryGetEnum(ErrorType, out _))
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = ErrorType.ToString(),
				Message = $"The schema's error type names '{ErrorType}', which it does not declare as an enum.",
			});
		}
	}

	/// <summary>
	/// A call that can fail has to be able to say why.
	/// </summary>
	/// <remarks>
	/// Reported at the signature rather than at the schema root, because that is the declaration
	/// whose meaning is incomplete: a generator reaching this has a <c>Result</c> to emit and
	/// nothing to put in its error position.
	/// </remarks>
	private void ValidateResultHasAnErrorType(Collection<SchemaValidationIssue> issues, string path, ISchemaElement? element)
	{
		if (string.IsNullOrEmpty(ErrorType))
		{
			Report(issues, path, element!, "This can fail, but the schema declares no error type, so a failure has nothing to say. Set the schema's error type to an enum it declares.");
		}
	}

	/// <summary>
	/// A named semantic type has to be one the schema declares, exactly as a named class or
	/// interface does.
	/// </summary>
	private void ValidateSemanticReference(Collection<SchemaValidationIssue> issues, Semantic reference, string path, ISchemaElement? element)
	{
		if (string.IsNullOrEmpty(reference.SemanticTypeName))
		{
			Report(issues, path, element!, "Semantic type reference does not name a semantic type.");
			return;
		}

		if (!TryGetSemanticType(reference.SemanticTypeName, out _))
		{
			Report(issues, path, element!, $"Semantic type reference names '{reference.SemanticTypeName}', which this schema does not declare.");
		}
	}

	/// <summary>
	/// A vector is so many of something, and that something has to be a number.
	/// </summary>
	/// <remarks>
	/// Directly, or a semantic type that is represented as one - a <c>Vec3&lt;MetresPerSecond&gt;</c>
	/// is three numbers whatever the name says. Anything else is a collection of things rather
	/// than one value with components, which is what <see cref="Array"/> is for.
	/// </remarks>
	private void ValidateVectorElement(Collection<SchemaValidationIssue> issues, Vector vectorType, string path, ISchemaElement? element)
	{
		ValidateType(issues, vectorType.ElementType, path, element);

		// A colour's components are the channels, and every consumer of one - a picker, a
		// shader, a serialiser - reads them as floats. A colour of anything else is a Vector.
		if (vectorType is ColorRGB or ColorRGBA)
		{
			if (vectorType.ElementType is not Float)
			{
				Report(issues, path, element!, $"{vectorType.TypeName} has {vectorType.ElementType.DisplayName} components. A colour's channels are floats; use a vector for anything else.");
			}

			return;
		}

		BaseType component = vectorType.ElementType is Semantic semantic && semantic.Declaration is SchemaSemanticType declaration
			? declaration.Representation()
			: vectorType.ElementType;

		// An unresolved reference is already reported above; saying the components are not
		// numbers as well would be two issues for one mistake.
		if (vectorType.ElementType is Semantic && component is Semantic)
		{
			return;
		}

		if (!component.IsNumeric)
		{
			Report(issues, path, element!, $"{vectorType.TypeName} has {vectorType.ElementType.DisplayName} components. A vector's components are numbers, or a semantic type over one.");
		}
	}

	private void ValidateInterfaceReference(Collection<SchemaValidationIssue> issues, Interface interfaceType, string path, ISchemaElement? element)
	{
		if (string.IsNullOrEmpty(interfaceType.InterfaceName))
		{
			Report(issues, path, element!, "Interface type does not specify an interface name.");
			return;
		}

		if (!TryGetInterface(interfaceType.InterfaceName, out _))
		{
			Report(issues, path, element!, $"Interface type references '{interfaceType.InterfaceName}', which this schema does not declare.");
		}
	}

	private void ValidateEnumReference(Collection<SchemaValidationIssue> issues, Enum enumType, string path, ISchemaElement? element)
	{
		if (string.IsNullOrEmpty(enumType.EnumName))
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = path,
				Message = "Enum type does not specify an enum name.",
				Element = element,
			});
		}
		else if (!TryGetEnum(enumType.EnumName, out _))
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = path,
				Message = $"Enum type references unknown enum '{enumType.EnumName}'.",
				Element = element,
			});
		}
	}

	private void ValidateClassReference(Collection<SchemaValidationIssue> issues, Names.ClassName className, string path, ISchemaElement? element)
	{
		if (string.IsNullOrEmpty(className))
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = path,
				Message = "Object type does not specify a class name.",
				Element = element,
			});
		}
		else if (!TryGetClass(className, out _))
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = path,
				Message = $"Object type references unknown class '{className}'.",
				Element = element,
			});
		}
	}

	private void ValidateArray(Collection<SchemaValidationIssue> issues, Array arrayType, string path, ISchemaElement? element)
	{
		ValidateType(issues, arrayType.ElementType, path, element);
		ValidateArrayContainer(issues, arrayType, path, element);

		if (string.IsNullOrEmpty(arrayType.Key))
		{
			return;
		}

		if (arrayType.ElementType is not Object elementObject)
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = path,
				Message = $"Array specifies key '{arrayType.Key}' but its element type is not an object.",
				Element = element,
			});
			return;
		}

		if (!TryGetClass(elementObject.ClassName, out SchemaClass? elementClass) || elementClass is null)
		{
			// The dangling class reference is already reported by ValidateType above.
			return;
		}

		if (!elementClass.TryGetMember(arrayType.Key, out SchemaMember? keyMember) || keyMember is null)
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = path,
				Message = $"Array key '{arrayType.Key}' is not a member of class '{elementClass.Name}'.",
				Element = element,
			});
		}
		else if (!keyMember.Type.IsPrimitive)
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = path,
				Message = $"Array key '{arrayType.Key}' on class '{elementClass.Name}' must be a primitive type but is '{keyMember.Type.DisplayName}'.",
				Element = element,
			});
		}
	}

	private static void ValidateArrayContainer(Collection<SchemaValidationIssue> issues, Array arrayType, string path, ISchemaElement? element)
	{
		if (string.IsNullOrEmpty(arrayType.Container))
		{
			// Array.IsKeyed requires a container, and a generator has nothing to map without one.
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Warning,
				Path = path,
				Message = "Array does not specify a container.",
				Element = element,
			});
			return;
		}

		if (!Array.KnownContainers.Contains(arrayType.Container.ToString()))
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Warning,
				Path = path,
				Message = $"Array specifies container '{arrayType.Container}', which is not one of the containers this library understands ({string.Join(", ", Array.KnownContainers)}).",
				Element = element,
			});
			return;
		}

		if (arrayType.Container.ToString() == Array.MapContainer && string.IsNullOrEmpty(arrayType.Key))
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = path,
				Message = $"Array uses the '{Array.MapContainer}' container but does not specify a key to map by.",
				Element = element,
			});
		}
	}

	private void ValidateDataSources(Collection<SchemaValidationIssue> issues)
	{
		foreach (DataSource dataSource in DataSourcesInternal)
		{
			if (string.IsNullOrEmpty(dataSource.ClassName))
			{
				issues.Add(new()
				{
					Severity = SchemaValidationSeverity.Warning,
					Path = dataSource.Name,
					Message = "Data source does not specify a class.",
					Element = dataSource,
				});
			}
			else if (!TryGetClass(dataSource.ClassName, out _))
			{
				issues.Add(new()
				{
					Severity = SchemaValidationSeverity.Error,
					Path = dataSource.Name,
					Message = $"Data source references unknown class '{dataSource.ClassName}'.",
					Element = dataSource,
				});
			}

			if (string.IsNullOrEmpty(dataSource.File))
			{
				issues.Add(new()
				{
					Severity = SchemaValidationSeverity.Warning,
					Path = dataSource.Name,
					Message = "Data source does not specify a file.",
					Element = dataSource,
				});
			}
			else
			{
				ValidateDataSourceFileExists(issues, dataSource);
			}
		}
	}

	/// <summary>
	/// Reports a data source whose file is not there.
	/// </summary>
	/// <remarks>
	/// Only possible once the schema knows its own location, since the path is relative to it.
	/// A schema built in memory has no anchor, and its data sources are left unchecked rather
	/// than resolved against whatever the working directory happens to be.
	/// </remarks>
	private void ValidateDataSourceFileExists(Collection<SchemaValidationIssue> issues, DataSource dataSource)
	{
		if (!CanResolvePaths || !dataSource.TryResolveFile(out Semantics.Paths.AbsoluteFilePath resolved))
		{
			return;
		}

		try
		{
			if (!File.Exists(resolved))
			{
				issues.Add(new()
				{
					Severity = SchemaValidationSeverity.Error,
					Path = dataSource.Name,
					Message = $"Data source file '{dataSource.File}' does not exist (resolved to '{resolved}').",
					Element = dataSource,
				});
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Warning,
				Path = dataSource.Name,
				Message = $"Data source file '{dataSource.File}' could not be checked: {ex.Message}",
				Element = dataSource,
			});
		}
	}

	private void ValidateCodeGenerators(Collection<SchemaValidationIssue> issues)
	{
		foreach (SchemaCodeGenerator codeGenerator in CodeGeneratorsInternal)
		{
			if (string.IsNullOrEmpty(codeGenerator.OutputPath))
			{
				issues.Add(new()
				{
					Severity = SchemaValidationSeverity.Warning,
					Path = codeGenerator.Name,
					Message = "Code generator does not specify an output path.",
					Element = codeGenerator,
				});
			}

			if (string.IsNullOrEmpty(codeGenerator.Language))
			{
				issues.Add(new()
				{
					Severity = SchemaValidationSeverity.Warning,
					Path = codeGenerator.Name,
					Message = "Code generator does not specify a target language.",
					Element = codeGenerator,
				});
			}
		}
	}
}
