// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Text.Json;

using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

/// <summary>
/// Reads the <c>{Classes, Enums}</c> schema format that Carbon Monoxide and Dungeoneer were
/// written in, and produces the <see cref="Schema"/> it describes.
/// </summary>
/// <remarks>
/// <para>
/// This lives in the test project rather than the library on purpose. The format is two
/// repositories' history, read once to migrate them; publishing a reader for it would commit
/// this library to a dialect nobody else has and nobody will write again. What is worth keeping
/// is the migration being *reproducible* - <c>samples/</c> is checked against what this produces,
/// so the committed schemas cannot drift from the legacy files beside them.
/// </para>
/// <para>
/// It reads a set of files into <b>one</b> schema rather than one schema per file. That is forced
/// rather than chosen: a member's <c>className</c> must name a class in the same schema, and 14 of
/// the references in the legacy set cross a file boundary - 7 of those reaching out of Dungeoneer
/// into Carbon Monoxide's shared types. The legacy format's own attempt at this, a per-file
/// <c>References</c> list, is not usable: <c>rect.schema.json</c> spells it <c>References</c>,
/// <c>spline.schema.json</c> spells it <c>ReferencedSchemas</c>, and <c>line.schema.json</c>
/// names <c>Vector2</c> while declaring neither. So the reader resolves by name across everything
/// it is given, which is what the generated C++ did anyway.
/// </para>
/// </remarks>
internal static class LegacySchemaReader
{
	/// <summary>
	/// The legacy type names that map to a built-in, and what each maps to. Anything else names a
	/// class or, with <c>EnumProperties</c>, an enum.
	/// </summary>
	private static readonly Dictionary<string, Func<BaseType>> Primitives = new(StringComparer.Ordinal)
	{
		["int"] = () => new Int(),
		["float"] = () => new Float(),
		["string"] = () => new Models.Types.String(),
		["bool"] = () => new Bool(),
	};

	/// <summary>
	/// Reads legacy documents into one schema.
	/// </summary>
	/// <param name="documents">The legacy JSON documents, in any order.</param>
	/// <returns>The schema they describe.</returns>
	/// <exception cref="InvalidDataException">A document names a type nothing declares.</exception>
	public static Schema Read(IEnumerable<string> documents)
	{
		List<JsonElement> roots = [.. documents.Select(json => JsonDocument.Parse(json).RootElement)];
		Schema schema = new();

		// Two passes, because a member may name a class declared in a document read later - and
		// with the reference lists unusable there is no order that would avoid it.
		DeclareEverything(schema, roots);
		FillMembers(schema, roots);

		return schema;
	}

	/// <summary>
	/// Declares every class and enum, so the second pass can resolve a reference to any of them.
	/// </summary>
	private static void DeclareEverything(Schema schema, List<JsonElement> roots)
	{
		foreach (JsonElement root in roots)
		{
			foreach (JsonElement legacyClass in Classes(root))
			{
				schema.AddClass(Name(legacyClass).As<ClassName>());
			}

			foreach (JsonProperty legacyEnum in Enums(root))
			{
				SchemaEnum added = schema.AddEnum(legacyEnum.Name.As<EnumName>())!;

				foreach (JsonElement value in legacyEnum.Value.EnumerateArray())
				{
					added.TryAddValue(value.GetString()!.As<EnumValueName>());
				}
			}
		}
	}

	/// <summary>
	/// Gives every declared class its members, in the order the legacy file declared them.
	/// </summary>
	private static void FillMembers(Schema schema, List<JsonElement> roots)
	{
		foreach (JsonElement root in roots)
		{
			foreach (JsonElement legacyClass in Classes(root))
			{
				SchemaClass target = schema.GetClass(Name(legacyClass).As<ClassName>())!;

				foreach (JsonElement member in Members(legacyClass))
				{
					SchemaMember added = target.AddMember(Name(member).As<MemberName>())!;
					added.SetType(TypeOf(schema, member));
					added.Description = Description(member);
				}
			}
		}
	}

	/// <summary>
	/// Reads a member's legacy type into the type it means.
	/// </summary>
	private static BaseType TypeOf(Schema schema, JsonElement member)
	{
		string typeName = member.GetProperty("Typename").GetString()!;

		if (Primitives.TryGetValue(typeName, out Func<BaseType>? primitive))
		{
			return primitive();
		}

		return typeName switch
		{
			// An array carries its element in a sibling object rather than in the type, and the
			// legacy format has only the one container, so every array is a sequence.
			"array" => new Models.Types.Array
			{
				ElementType = Named(schema, member.GetProperty("ArrayProperties").GetProperty("ElementTypename").GetString()!),
				Container = ContainerName.Vector,
			},
			"enum" => Named(schema, member.GetProperty("EnumProperties").GetProperty("Name").GetString()!),
			_ => Named(schema, typeName),
		};
	}

	/// <summary>
	/// Resolves a name to the class or enum it refers to.
	/// </summary>
	/// <remarks>
	/// A name is resolved against what was declared rather than guessed at from spelling, which is
	/// what catches a reference the legacy set never resolved either. Refusing here rather than
	/// producing a <see cref="None"/> is deliberate: a schema that quietly holds an unchosen type
	/// validates as "still being edited" rather than as the error it is.
	/// </remarks>
	private static BaseType Named(Schema schema, string name)
	{
		if (Primitives.TryGetValue(name, out Func<BaseType>? primitive))
		{
			return primitive();
		}

		if (schema.TryGetClass(name.As<ClassName>(), out _))
		{
			return new Models.Types.Object { ClassName = name.As<ClassName>() };
		}

		return schema.TryGetEnum(name.As<EnumName>(), out _)
			? new Models.Types.Enum { EnumName = name.As<EnumName>() }
			: throw new InvalidDataException($"The legacy schemas name '{name}', which none of them declares.");
	}

	private static List<JsonElement> Classes(JsonElement root) =>
		root.TryGetProperty("Classes", out JsonElement classes)
			? [.. classes.EnumerateArray()]
			: [];

	private static List<JsonProperty> Enums(JsonElement root) =>
		root.TryGetProperty("Enums", out JsonElement enums)
			? [.. enums.EnumerateObject()]
			: [];

	private static List<JsonElement> Members(JsonElement legacyClass) =>
		legacyClass.TryGetProperty("Members", out JsonElement members)
			? [.. members.EnumerateArray()]
			: [];

	private static string Name(JsonElement element) => element.GetProperty("Name").GetString()!;

	private static SchemaChildDescription Description(JsonElement element) =>
		element.TryGetProperty("Description", out JsonElement description)
			? (description.GetString() ?? string.Empty).As<SchemaChildDescription>()
			: new SchemaChildDescription();
}
