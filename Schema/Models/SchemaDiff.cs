// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Collections.ObjectModel;

using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;

/// <summary>
/// The differences between two versions of a schema that matter to data bound to the earlier one.
/// </summary>
/// <remarks>
/// <para>
/// Two versions are matched through the rename history the current one carries
/// (<see cref="Schema.RenameHistory"/>), not by guessing from names. The previous version's history
/// is a prefix of the current one's when the current version was edited from it, and the entries
/// past that prefix are exactly the renames made since. Replaying them in order maps every class,
/// enum, member and enum value of the previous version to the name it has now, so a renamed member
/// is told apart from a removed one and an added one, which a comparison of names alone cannot do.
/// </para>
/// <para>
/// When the previous history is not a prefix - the two versions were edited apart, or the current
/// one was edited by something that does not record renames - the entries past the longest common
/// prefix are replayed anyway and the disagreement is reported in <see cref="Warnings"/>. A rename
/// that was not recorded then reads as a removal and an addition, which loses data on migration
/// rather than misplacing it.
/// </para>
/// <para>
/// A member is retyped when the shape of its data changed, judged with the renames applied: a
/// member holding a renamed class still holds the same class.
/// </para>
/// </remarks>
public sealed class SchemaDiff
{
	private readonly Dictionary<string, string> classNames = new(StringComparer.Ordinal);
	private readonly Dictionary<string, string> enumNames = new(StringComparer.Ordinal);
	private readonly Dictionary<(string Owner, string Name), string> memberNames = [];
	private readonly Dictionary<(string Owner, string Name), string> enumValueNames = [];
	private readonly Collection<SchemaChange> changes = [];
	private readonly Collection<string> warnings = [];

	private SchemaDiff(Schema previous, Schema current)
	{
		Previous = previous;
		Current = current;
	}

	/// <summary>
	/// Gets the earlier version of the schema.
	/// </summary>
	public Schema Previous { get; }

	/// <summary>
	/// Gets the later version of the schema.
	/// </summary>
	public Schema Current { get; }

	/// <summary>
	/// Gets every difference found, classes and enums first, each followed by its members or values.
	/// </summary>
	public IReadOnlyList<SchemaChange> Changes => changes;

	/// <summary>
	/// Gets anything that makes the matching less certain than the rename history would.
	/// </summary>
	public IReadOnlyList<string> Warnings => warnings;

	/// <summary>
	/// Gets whether any change requires data bound to the previous version to be migrated.
	/// </summary>
	public bool HasDataChanges => changes.Any(change => change.AffectsData);

	/// <summary>
	/// Compares two versions of a schema.
	/// </summary>
	/// <param name="previous">The version data was bound to.</param>
	/// <param name="current">The version to migrate it to.</param>
	/// <returns>The differences between them.</returns>
	public static SchemaDiff Compare(Schema previous, Schema current)
	{
		Ensure.NotNull(previous);
		Ensure.NotNull(current);

		SchemaDiff diff = new(previous, current);
		diff.MapNames();
		diff.CompareEnums();
		diff.CompareClasses();
		return diff;
	}

	/// <summary>
	/// Finds the class a class of the previous version became.
	/// </summary>
	/// <param name="previousClass">A class of <see cref="Previous"/>.</param>
	/// <returns>Its counterpart in <see cref="Current"/>, or null if it was removed.</returns>
	public SchemaClass? CurrentClassFor(SchemaClass previousClass)
	{
		Ensure.NotNull(previousClass);
		return CurrentClassFor(previousClass.Name);
	}

	/// <summary>
	/// Finds the member a member of the previous version became.
	/// </summary>
	/// <param name="previousClass">The class of <see cref="Previous"/> the member belongs to.</param>
	/// <param name="previousMember">The member's name in the previous version.</param>
	/// <returns>Its counterpart in <see cref="Current"/>, or null if it or its class was removed.</returns>
	public SchemaMember? CurrentMemberFor(SchemaClass previousClass, string previousMember)
	{
		Ensure.NotNull(previousClass);

		SchemaClass? currentClass = CurrentClassFor(previousClass);
		if (currentClass is null || !memberNames.TryGetValue((previousClass.Name, previousMember), out string? name))
		{
			return null;
		}

		return currentClass.Members.FirstOrDefault(member => string.Equals(member.Name, name, StringComparison.Ordinal));
	}

	/// <summary>
	/// Finds the value an enum value of the previous version became.
	/// </summary>
	/// <param name="previousEnum">The enum's name in the previous version.</param>
	/// <param name="previousValue">The value's name in the previous version.</param>
	/// <returns>Its name in <see cref="Current"/>, or null if it or its enum was removed.</returns>
	public string? CurrentEnumValueFor(string previousEnum, string previousValue)
	{
		SchemaEnum? currentEnum = CurrentEnumFor(previousEnum);
		if (currentEnum is null || !enumValueNames.TryGetValue((previousEnum, previousValue), out string? name))
		{
			return null;
		}

		return currentEnum.Values.Any(value => string.Equals(value, name, StringComparison.Ordinal)) ? name : null;
	}

	/// <summary>
	/// Gets whether a value of <paramref name="previousType"/> is already a value of
	/// <paramref name="currentType"/>, once renames are applied.
	/// </summary>
	/// <param name="previousType">A type from <see cref="Previous"/>.</param>
	/// <param name="currentType">A type from <see cref="Current"/>.</param>
	/// <returns>True if the data a member of either type holds has the same shape.</returns>
	public bool HasSameShape(BaseType previousType, BaseType currentType)
	{
		Ensure.NotNull(previousType);
		Ensure.NotNull(currentType);

		return (Represented(previousType), Represented(currentType)) switch
		{
			(Types.Object p, Types.Object c) => MapsTo(classNames, p.ClassName, c.ClassName),
			(Types.Enum p, Types.Enum c) => MapsTo(enumNames, p.EnumName, c.EnumName),
			(Types.Array p, Types.Array c) =>
				string.Equals(p.Container, c.Container, StringComparison.Ordinal) &&
				KeyMapsTo(p, c) &&
				HasSameShape(p.ElementType, c.ElementType),
			(WrapperType p, WrapperType c) => p.GetType() == c.GetType() && HasSameShape(p.ElementType, c.ElementType),
			(Vector p, Vector c) => p.GetType() == c.GetType() && HasSameShape(p.ElementType, c.ElementType),
			(BaseType p, BaseType c) => p.Equals(c),
		};
	}

	internal SchemaClass? CurrentClassFor(string previousClass) =>
		classNames.TryGetValue(previousClass, out string? name)
			? Current.Classes.FirstOrDefault(schemaClass => string.Equals(schemaClass.Name, name, StringComparison.Ordinal))
			: null;

	internal SchemaEnum? CurrentEnumFor(string previousEnum) =>
		enumNames.TryGetValue(previousEnum, out string? name)
			? Current.Enums.FirstOrDefault(schemaEnum => string.Equals(schemaEnum.Name, name, StringComparison.Ordinal))
			: null;

	/// <summary>
	/// The type data of <paramref name="type"/> is stored as: a semantic type is its representation.
	/// </summary>
	internal static BaseType Represented(BaseType type) =>
		type is Semantic { Declaration: SchemaSemanticType declaration } && declaration.Representation() is BaseType representation and not Semantic
			? representation
			: type;

	private static bool MapsTo(Dictionary<string, string> names, string previous, string current) =>
		names.TryGetValue(previous, out string? mapped) && string.Equals(mapped, current, StringComparison.Ordinal);

	private bool KeyMapsTo(Types.Array previous, Types.Array current)
	{
		if (string.IsNullOrEmpty(previous.Key) || string.IsNullOrEmpty(current.Key))
		{
			return string.IsNullOrEmpty(previous.Key) == string.IsNullOrEmpty(current.Key);
		}

		return previous.ElementType is Types.Object element &&
			memberNames.TryGetValue((element.ClassName, previous.Key), out string? key) &&
			string.Equals(key, current.Key, StringComparison.Ordinal);
	}

	private void MapNames()
	{
		foreach (SchemaClass schemaClass in Previous.Classes)
		{
			classNames[schemaClass.Name] = schemaClass.Name;
			foreach (SchemaMember member in schemaClass.Members)
			{
				memberNames[(schemaClass.Name, member.Name)] = member.Name;
			}
		}

		foreach (SchemaEnum schemaEnum in Previous.Enums)
		{
			enumNames[schemaEnum.Name] = schemaEnum.Name;
			foreach (EnumValueName value in schemaEnum.Values)
			{
				enumValueNames[(schemaEnum.Name.ToString(), value.ToString())] = value;
			}
		}

		IReadOnlyList<SchemaRename> before = Previous.RenameHistory;
		IReadOnlyList<SchemaRename> after = Current.RenameHistory;

		int common = 0;
		while (common < before.Count && common < after.Count && before[common].Equals(after[common]))
		{
			common++;
		}

		if (common < before.Count)
		{
			warnings.Add($"The previous schema's rename history is not a prefix of the current one's: they agree on the first {common} of {before.Count} entries. Renames made only in the previous version are not known to the current one.");
		}

		foreach (SchemaRename rename in after.Skip(common))
		{
			Replay(rename);
		}
	}

	/// <summary>
	/// Applies one recorded rename to the names the previous version's elements have reached.
	/// </summary>
	/// <remarks>
	/// A member or value rename names its owner as it was called at the time, which is the name the
	/// owner has reached after every earlier entry - so matching against the class or enum name
	/// mapped so far is what finds it. A rename of something the previous version never had matches
	/// nothing, which is right: data bound to the previous version holds none of it.
	/// </remarks>
	private void Replay(SchemaRename rename)
	{
		switch (rename.Kind)
		{
			case SchemaRenameKind.Class:
				Rename(classNames, rename);
				break;

			case SchemaRenameKind.Enum:
				Rename(enumNames, rename);
				break;

			case SchemaRenameKind.Member:
				RenameOwned(memberNames, classNames, rename);
				break;

			case SchemaRenameKind.EnumValue:
				RenameOwned(enumValueNames, enumNames, rename);
				break;

			default:
				break;
		}
	}

	private static void Rename(Dictionary<string, string> names, SchemaRename rename)
	{
		foreach (string key in names.Where(entry => string.Equals(entry.Value, rename.From, StringComparison.Ordinal)).Select(entry => entry.Key).ToList())
		{
			names[key] = rename.To;
		}
	}

	private static void RenameOwned(Dictionary<(string Owner, string Name), string> names, Dictionary<string, string> owners, SchemaRename rename)
	{
		foreach ((string Owner, string Name) key in names
			.Where(entry =>
				string.Equals(entry.Value, rename.From, StringComparison.Ordinal) &&
				owners.TryGetValue(entry.Key.Owner, out string? owner) &&
				string.Equals(owner, rename.Owner, StringComparison.Ordinal))
			.Select(entry => entry.Key)
			.ToList())
		{
			names[key] = rename.To;
		}
	}

	private void CompareEnums()
	{
		HashSet<string> matched = new(StringComparer.Ordinal);

		foreach (SchemaEnum previousEnum in Previous.Enums)
		{
			SchemaEnum? currentEnum = CurrentEnumFor(previousEnum.Name);
			if (currentEnum is null)
			{
				changes.Add(new() { Kind = SchemaChangeKind.EnumRemoved, PreviousName = previousEnum.Name });
				continue;
			}

			matched.Add(currentEnum.Name);
			if (!string.Equals(previousEnum.Name, currentEnum.Name, StringComparison.Ordinal))
			{
				changes.Add(new() { Kind = SchemaChangeKind.EnumRenamed, PreviousName = previousEnum.Name, CurrentName = currentEnum.Name });
			}

			HashSet<string> matchedValues = new(StringComparer.Ordinal);
			foreach (EnumValueName value in previousEnum.Values)
			{
				string? currentValue = CurrentEnumValueFor(previousEnum.Name, value);
				if (currentValue is null)
				{
					changes.Add(new() { Kind = SchemaChangeKind.EnumValueRemoved, Owner = currentEnum.Name, PreviousName = value });
					continue;
				}

				matchedValues.Add(currentValue);
				if (!string.Equals(value, currentValue, StringComparison.Ordinal))
				{
					changes.Add(new() { Kind = SchemaChangeKind.EnumValueRenamed, Owner = currentEnum.Name, PreviousName = value, CurrentName = currentValue });
				}
			}

			foreach (EnumValueName value in currentEnum.Values.Where(value => !matchedValues.Contains(value)))
			{
				changes.Add(new() { Kind = SchemaChangeKind.EnumValueAdded, Owner = currentEnum.Name, CurrentName = value });
			}
		}

		foreach (SchemaEnum currentEnum in Current.Enums.Where(schemaEnum => !matched.Contains(schemaEnum.Name)))
		{
			changes.Add(new() { Kind = SchemaChangeKind.EnumAdded, CurrentName = currentEnum.Name });
		}
	}

	private void CompareClasses()
	{
		HashSet<string> matched = new(StringComparer.Ordinal);

		foreach (SchemaClass previousClass in Previous.Classes)
		{
			SchemaClass? currentClass = CurrentClassFor(previousClass);
			if (currentClass is null)
			{
				changes.Add(new() { Kind = SchemaChangeKind.ClassRemoved, PreviousName = previousClass.Name });
				continue;
			}

			matched.Add(currentClass.Name);
			if (!string.Equals(previousClass.Name, currentClass.Name, StringComparison.Ordinal))
			{
				changes.Add(new() { Kind = SchemaChangeKind.ClassRenamed, PreviousName = previousClass.Name, CurrentName = currentClass.Name });
			}

			CompareMembers(previousClass, currentClass);
		}

		foreach (SchemaClass currentClass in Current.Classes.Where(schemaClass => !matched.Contains(schemaClass.Name)))
		{
			changes.Add(new() { Kind = SchemaChangeKind.ClassAdded, CurrentName = currentClass.Name });
		}
	}

	private void CompareMembers(SchemaClass previousClass, SchemaClass currentClass)
	{
		HashSet<string> matched = new(StringComparer.Ordinal);

		foreach (SchemaMember previousMember in previousClass.Members)
		{
			SchemaMember? currentMember = CurrentMemberFor(previousClass, previousMember.Name);
			if (currentMember is null)
			{
				changes.Add(new() { Kind = SchemaChangeKind.MemberRemoved, Owner = currentClass.Name, PreviousName = previousMember.Name });
				continue;
			}

			matched.Add(currentMember.Name);
			if (!string.Equals(previousMember.Name, currentMember.Name, StringComparison.Ordinal))
			{
				changes.Add(new() { Kind = SchemaChangeKind.MemberRenamed, Owner = currentClass.Name, PreviousName = previousMember.Name, CurrentName = currentMember.Name });
			}

			if (!HasSameShape(previousMember.Type, currentMember.Type))
			{
				changes.Add(new()
				{
					Kind = SchemaChangeKind.MemberRetyped,
					Owner = currentClass.Name,
					PreviousName = previousMember.Name,
					CurrentName = currentMember.Name,
					PreviousType = previousMember.Type,
					CurrentType = currentMember.Type,
				});
			}
		}

		foreach (SchemaMember currentMember in currentClass.Members.Where(member => !matched.Contains(member.Name)))
		{
			changes.Add(new() { Kind = SchemaChangeKind.MemberAdded, Owner = currentClass.Name, CurrentName = currentMember.Name });
		}
	}
}
