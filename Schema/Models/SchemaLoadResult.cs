// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Models;

/// <summary>
/// How an attempt to load a schema file ended.
/// </summary>
public enum SchemaLoadStatus
{
	/// <summary>
	/// The file was read, and migrated to the current format version if it was older.
	/// </summary>
	Success,

	/// <summary>
	/// The text is not valid JSON, or does not describe a schema.
	/// </summary>
	InvalidJson,

	/// <summary>
	/// The file declares a format version this build does not understand, because it was written
	/// by a newer version of the library.
	/// </summary>
	/// <remarks>
	/// Distinguished from <see cref="InvalidJson"/> so a caller can tell the user to upgrade
	/// rather than reporting a corrupt file, which is the wrong diagnosis and the wrong remedy.
	/// </remarks>
	UnsupportedFutureVersion,
}

/// <summary>
/// The outcome of loading a schema, carrying enough detail to explain a failure.
/// </summary>
public class SchemaLoadResult
{
	/// <summary>
	/// Gets how the load ended.
	/// </summary>
	public SchemaLoadStatus Status { get; init; }

	/// <summary>
	/// Gets the loaded schema, or null unless <see cref="Status"/> is
	/// <see cref="SchemaLoadStatus.Success"/>.
	/// </summary>
	public Schema? Schema { get; init; }

	/// <summary>
	/// Gets the format version the file declared, or
	/// <see cref="Models.Schema.PreVersioningFormatVersion"/> if it declared none.
	/// </summary>
	public int FileFormatVersion { get; init; }

	/// <summary>
	/// Gets a message describing the outcome, suitable for showing to a user.
	/// </summary>
	public string Message { get; init; } = string.Empty;

	/// <summary>
	/// Gets a value indicating whether the load succeeded.
	/// </summary>
	public bool IsSuccess => Status == SchemaLoadStatus.Success;

	/// <summary>
	/// Returns a string representation of the outcome.
	/// </summary>
	/// <returns>The status and message.</returns>
	public override string ToString() => $"{Status}: {Message}";
}
