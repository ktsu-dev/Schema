// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

using System.Collections.ObjectModel;

/// <summary>
/// Data migrated by <see cref="SchemaDataMigrator"/>, and what the migration had to give up.
/// </summary>
public sealed class SchemaMigrationResult
{
	/// <summary>
	/// Gets the migrated data, or the input unchanged when <see cref="Succeeded"/> is false.
	/// </summary>
	public string Json { get; init; } = string.Empty;

	/// <summary>
	/// Gets what the migration reported: a warning for every value dropped or replaced by a default,
	/// and an error when the data could not be migrated at all.
	/// </summary>
	public Collection<SchemaValidationIssue> Issues { get; init; } = [];

	/// <summary>
	/// Gets whether the data was migrated; false when an error stopped it.
	/// </summary>
	public bool Succeeded => !Issues.Any(issue => issue.Severity == SchemaValidationSeverity.Error);
}
