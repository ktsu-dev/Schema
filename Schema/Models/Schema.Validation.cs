// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Collections.ObjectModel;
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
		ValidateDataSources(issues);
		ValidateCodeGenerators(issues);

		return issues;
	}

	private void ValidateUniqueNames(Collection<SchemaValidationIssue> issues)
	{
		ReportDuplicates(issues, ClassesInternal.Select(c => c.Name.ToString()), "class");
		ReportDuplicates(issues, EnumsInternal.Select(e => e.Name.ToString()), "enum");
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
	private static void ValidateNameNotEmpty(Collection<SchemaValidationIssue> issues, string name, string kind, string path)
	{
		if (string.IsNullOrEmpty(name))
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = path,
				Message = $"{kind} has an empty name.",
			});
		}
	}

	private void ValidateClasses(Collection<SchemaValidationIssue> issues)
	{
		foreach (SchemaClass schemaClass in ClassesInternal)
		{
			string classPath = PathSegment(schemaClass.Name);
			ValidateNameNotEmpty(issues, schemaClass.Name, "Class", classPath);

			ReportDuplicates(issues, schemaClass.Members.Select(m => $"{schemaClass.Name}.{m.Name}"), "member");

			foreach (SchemaMember member in schemaClass.Members)
			{
				string memberPath = $"{classPath}.{PathSegment(member.Name)}";
				ValidateNameNotEmpty(issues, member.Name, "Member", memberPath);

				if (member.Type is None)
				{
					// A valid intermediate editing state, but not a generatable schema.
					issues.Add(new()
					{
						Severity = SchemaValidationSeverity.Warning,
						Path = memberPath,
						Message = "Member does not have a type set.",
					});
				}

				ValidateType(issues, member.Type, memberPath);
			}
		}
	}

	private void ValidateEnums(Collection<SchemaValidationIssue> issues)
	{
		foreach (SchemaEnum schemaEnum in EnumsInternal)
		{
			string enumPath = PathSegment(schemaEnum.Name);
			ValidateNameNotEmpty(issues, schemaEnum.Name, "Enum", enumPath);

			ReportDuplicates(issues, schemaEnum.Values.Select(v => $"{schemaEnum.Name}.{v}"), "enum value");

			foreach (Names.EnumValueName value in schemaEnum.Values)
			{
				ValidateNameNotEmpty(issues, value, "Enum value", $"{enumPath}.{PathSegment(value)}");
			}
		}
	}

	private void ValidateType(Collection<SchemaValidationIssue> issues, BaseType type, string path)
	{
		switch (type)
		{
			case Enum enumType:
				ValidateEnumReference(issues, enumType, path);
				break;

			case Object objectType:
				ValidateClassReference(issues, objectType.ClassName, path);
				break;

			case Array arrayType:
				ValidateArray(issues, arrayType, path);
				break;

			default:
				break;
		}
	}

	private void ValidateEnumReference(Collection<SchemaValidationIssue> issues, Enum enumType, string path)
	{
		if (string.IsNullOrEmpty(enumType.EnumName))
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = path,
				Message = "Enum type does not specify an enum name.",
			});
		}
		else if (!TryGetEnum(enumType.EnumName, out _))
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = path,
				Message = $"Enum type references unknown enum '{enumType.EnumName}'.",
			});
		}
	}

	private void ValidateClassReference(Collection<SchemaValidationIssue> issues, Names.ClassName className, string path)
	{
		if (string.IsNullOrEmpty(className))
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = path,
				Message = "Object type does not specify a class name.",
			});
		}
		else if (!TryGetClass(className, out _))
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = path,
				Message = $"Object type references unknown class '{className}'.",
			});
		}
	}

	private void ValidateArray(Collection<SchemaValidationIssue> issues, Array arrayType, string path)
	{
		ValidateType(issues, arrayType.ElementType, path);
		ValidateArrayContainer(issues, arrayType, path);

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
			});
		}
		else if (!keyMember.Type.IsPrimitive)
		{
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Error,
				Path = path,
				Message = $"Array key '{arrayType.Key}' on class '{elementClass.Name}' must be a primitive type but is '{keyMember.Type.DisplayName}'.",
			});
		}
	}

	private static void ValidateArrayContainer(Collection<SchemaValidationIssue> issues, Array arrayType, string path)
	{
		if (string.IsNullOrEmpty(arrayType.Container))
		{
			// Array.IsKeyed requires a container, and a generator has nothing to map without one.
			issues.Add(new()
			{
				Severity = SchemaValidationSeverity.Warning,
				Path = path,
				Message = "Array does not specify a container.",
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
				});
			}
			else if (!TryGetClass(dataSource.ClassName, out _))
			{
				issues.Add(new()
				{
					Severity = SchemaValidationSeverity.Error,
					Path = dataSource.Name,
					Message = $"Data source references unknown class '{dataSource.ClassName}'.",
				});
			}

			if (string.IsNullOrEmpty(dataSource.File))
			{
				issues.Add(new()
				{
					Severity = SchemaValidationSeverity.Warning,
					Path = dataSource.Name,
					Message = "Data source does not specify a file.",
				});
			}
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
				});
			}
		}
	}
}
