// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Editor.UITests.Gallery;

using System;

using ktsu.Schema.Generation;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Metadata;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

using SchemaTypes = ktsu.Schema.Models.Types;

/// <summary>The sample schema every gallery picture is taken of.</summary>
/// <remarks>
/// Built in code rather than read from a file, so the pictures depend on nothing outside the test
/// assembly. It is small enough to read in a picture and covers what the editor is for: classes
/// referring to one another and to an enum, an array, a vector, members carrying units, ranges and
/// defaults, and a code generator. The path it claims to be saved at is the same on every machine,
/// so the editor's header reads the same in every picture; nothing is ever written there.
/// </remarks>
internal static class GallerySchema
{
	/// <summary>The class the gallery edits most.</summary>
	internal const string Player = "Player";

	/// <summary>The member whose metadata the gallery shows.</summary>
	internal const string Speed = "Speed";

	/// <summary>Gets the path the schema claims to be saved at.</summary>
	internal static AbsoluteFilePath Path { get; } =
		(OperatingSystem.IsWindows() ? @"C:\Projects\Game\game.schema.json" : "/projects/game/game.schema.json").As<AbsoluteFilePath>();

	/// <summary>Builds the schema.</summary>
	internal static Schema Build()
	{
		Schema schema = new();
		schema.SetSourceFile(Path);

		SchemaEnum rarity = schema.AddEnum("Rarity".As<EnumName>())!;
		rarity.Description = "How hard an item is to come by.".As<SchemaChildDescription>();
		foreach (string value in (string[])["Common", "Uncommon", "Rare", "Epic", "Legendary"])
		{
			rarity.TryAddValue(value.As<EnumValueName>());
		}

		SchemaClass item = schema.AddClass("Item".As<ClassName>())!;
		item.Description = "Anything a player can pick up and carry.".As<SchemaChildDescription>();
		item.AddMember("Name".As<MemberName>())!.SetType(new SchemaTypes.String());
		item.AddMember("Rarity".As<MemberName>())!.SetType(new SchemaTypes.Enum() { EnumName = rarity.Name });
		SchemaMember weight = item.AddMember("Weight".As<MemberName>())!;
		weight.SetType(new SchemaTypes.Float());
		weight.Unit = "kg".As<UnitSymbol>();
		weight.Range = new MemberRange { Minimum = 0, Maximum = 50 };
		item.AddMember("Value".As<MemberName>())!.SetType(new SchemaTypes.Int());

		SchemaClass inventory = schema.AddClass("Inventory".As<ClassName>())!;
		inventory.Description = "What a player is carrying.".As<SchemaChildDescription>();
		inventory.AddMember("Items".As<MemberName>())!.SetType(new SchemaTypes.Array()
		{
			ElementType = new SchemaTypes.Object() { ClassName = item.Name },
			Container = SchemaTypes.Array.VectorContainer.As<ContainerName>(),
		});
		SchemaMember capacity = inventory.AddMember("Capacity".As<MemberName>())!;
		capacity.SetType(new SchemaTypes.Int());
		capacity.Range = new MemberRange { Minimum = 1, Maximum = 64 };
		capacity.DefaultValue = new NumberDefault { Value = 20 };

		SchemaClass player = schema.AddClass(Player.As<ClassName>())!;
		player.Description = "Someone playing the game, and everything the game remembers about them.".As<SchemaChildDescription>();
		player.AddMember("Name".As<MemberName>())!.SetType(new SchemaTypes.String());
		SchemaMember health = player.AddMember("Health".As<MemberName>())!;
		health.SetType(new SchemaTypes.Int());
		health.Range = new MemberRange { Minimum = 0, Maximum = 100 };
		health.DefaultValue = new NumberDefault { Value = 100 };
		player.AddMember("Position".As<MemberName>())!.SetType(new SchemaTypes.Vector3());
		SchemaMember speed = player.AddMember(Speed.As<MemberName>())!;
		speed.SetType(new SchemaTypes.Float());
		speed.Description = "How fast the player walks.".As<SchemaChildDescription>();
		speed.Unit = "m/s".As<UnitSymbol>();
		speed.Range = new MemberRange { Minimum = 0, Maximum = 12 };
		speed.DefaultValue = new NumberDefault { Value = 5 };
		speed.Interpolation = Interpolation.Linear;
		player.AddMember("Inventory".As<MemberName>())!.SetType(new SchemaTypes.Object() { ClassName = inventory.Name });

		SchemaCodeGenerator csharp = schema.AddCodeGenerator("CSharp".As<CodeGeneratorName>())!;
		csharp.Description = "The game's data types, as C#.".As<SchemaChildDescription>();
		csharp.Language = CSharpCodeGenerator.LanguageId.As<LanguageName>();
		csharp.Namespace = "Game.Data".As<CodeNamespace>();
		csharp.OutputPath = "Generated".As<RelativeDirectoryPath>();

		return schema;
	}
}
