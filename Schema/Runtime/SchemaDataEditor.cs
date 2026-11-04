// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Runtime;

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Semantics.Strings;

/// <summary>
/// Loads, lists, edits, validates and saves the records of one data source.
/// </summary>
/// <remarks>
/// A code generator bound to a data source emits a sealed subclass of this with the schema embedded,
/// so the editor needs no schema file at run time. <see cref="Issues"/> is
/// <see cref="SchemaDataValidator.ValidateData"/> run over <see cref="ToJson"/> - the same check the
/// schema tool makes - so the editor accepts exactly what the tool accepts, and never a second
/// opinion about the data.
/// <para>
/// A file that is not JSON still loads: its issues say why, it lists no records, and an edit is
/// refused rather than replacing text the editor could not read.
/// </para>
/// </remarks>
/// <typeparam name="TRecord">The record type generated for the data source's class.</typeparam>
public abstract class SchemaDataEditor<TRecord>
	where TRecord : SchemaDataRecord
{
	private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

	private readonly Lazy<SchemaClass> schemaClass;
	private readonly Func<JsonObject, Action, TRecord> createRecord;
	private JsonNode? root;
	private string text = "[]";
	private bool edited;

	/// <summary>
	/// Initializes a new instance of the <see cref="SchemaDataEditor{TRecord}"/> class with no records.
	/// </summary>
	/// <param name="schemaJson">The schema, as its <c>.schema.json</c> text.</param>
	/// <param name="className">The class every record conforms to.</param>
	/// <param name="dataSourceName">The data source, which names the records in an issue's path.</param>
	/// <param name="createRecord">Wraps a record's object in its generated type.</param>
	protected SchemaDataEditor(string schemaJson, string className, string dataSourceName, Func<JsonObject, Action, TRecord> createRecord)
	{
		Ensure.NotNull(schemaJson);
		Ensure.NotNull(className);
		DataSourceName = Ensure.NotNull(dataSourceName);
		this.createRecord = Ensure.NotNull(createRecord);
		schemaClass = new(() => LoadClass(schemaJson, className));
		root = new JsonArray();
	}

	/// <summary>Gets the name of the data source this editor edits.</summary>
	public string DataSourceName { get; }

	/// <summary>Gets the class every record conforms to.</summary>
	public SchemaClass Class => schemaClass.Value;

	/// <summary>Gets whether the data read as JSON, and so can be listed and edited.</summary>
	public bool IsReadable => root is not null;

	/// <summary>
	/// Gets the records, in file order: the root object, or each object in a root array.
	/// </summary>
	/// <remarks>An array element that is not an object is not a record and is not listed; it is reported in <see cref="Issues"/>.</remarks>
	public ReadOnlyCollection<TRecord> Records
	{
		get
		{
			List<TRecord> records = [];
			switch (root)
			{
				case JsonObject single:
					records.Add(createRecord(single, MarkEdited));
					break;
				case JsonArray array:
					foreach (JsonObject record in array.OfType<JsonObject>())
					{
						records.Add(createRecord(record, MarkEdited));
					}

					break;
				default:
					break;
			}

			return records.AsReadOnly();
		}
	}

	/// <summary>Gets everything <see cref="SchemaDataValidator"/> reports about the data as it stands.</summary>
	public IReadOnlyList<SchemaValidationIssue> Issues => SchemaDataValidator.ValidateData(Class, ToJson(), DataSourceName);

	/// <summary>Gets whether the data has no errors; warnings do not count.</summary>
	public bool IsValid => Issues.All(issue => issue.Severity != SchemaValidationSeverity.Error);

	/// <summary>Replaces the data with the given text.</summary>
	/// <param name="json">The data file's text.</param>
	public void Parse(string json)
	{
		text = Ensure.NotNull(json);
		edited = false;
		try
		{
			// Default options, as the validator reads it: text with a comment in it is not data the
			// tool accepts, so it is not data this editor will list either.
			root = JsonNode.Parse(json);
		}
		catch (JsonException)
		{
			root = null;
		}
		catch (ArgumentException)
		{
			// A duplicate key: valid to the reader, refused by the node model.
			root = null;
		}
	}

	/// <summary>Replaces the data with the contents of a file.</summary>
	/// <param name="path">The file to read.</param>
	public void Load(string path) => Parse(File.ReadAllText(Ensure.NotNull(path)));

	/// <summary>Writes the data to a file.</summary>
	/// <param name="path">The file to write.</param>
	public void Save(string path) => File.WriteAllText(Ensure.NotNull(path), ToJson());

	/// <summary>
	/// Gets the data as text: what was loaded, until the first edit, and indented JSON after it.
	/// </summary>
	/// <returns>The data file's text.</returns>
	public string ToJson() => edited && root is not null ? root.ToJsonString(WriteOptions) : text;

	/// <summary>
	/// Adds an empty record. A file holding one object becomes an array holding it and the new one.
	/// </summary>
	/// <returns>The new record, to fill in through its accessors.</returns>
	public TRecord Add()
	{
		JsonObject record = [];
		switch (root)
		{
			case JsonArray array:
				array.Add(record);
				break;
			case JsonObject single:
				// The root object has no parent, so it moves into the array as itself and the
				// records already listed over it stay live.
				root = new JsonArray(single, record);
				break;
			default:
				throw Unreadable();
		}

		edited = true;
		return createRecord(record, MarkEdited);
	}

	/// <summary>Removes a record.</summary>
	/// <param name="record">A record this editor listed.</param>
	/// <returns>Whether the record was found and removed.</returns>
	public bool Remove(TRecord record)
	{
		Ensure.NotNull(record);
		switch (root)
		{
			case JsonArray array:
				for (int i = 0; i < array.Count; i++)
				{
					if (ReferenceEquals(array[i], record.Json))
					{
						array.RemoveAt(i);
						edited = true;
						return true;
					}
				}

				return false;
			case JsonObject single when ReferenceEquals(single, record.Json):
				root = new JsonArray();
				edited = true;
				return true;
			case null:
				throw Unreadable();
			default:
				return false;
		}
	}

	/// <summary>Marks the data as changed. A record's setters call this; an edit made through <see cref="SchemaDataRecord.Json"/> directly needs it.</summary>
	/// <remarks>Only <see cref="ToJson"/> reads this: until then it returns the loaded text verbatim.</remarks>
	public void MarkEdited()
	{
		if (root is null)
		{
			throw Unreadable();
		}

		edited = true;
	}

	private static SchemaClass LoadClass(string schemaJson, string className)
	{
		SchemaLoadResult result = SchemaSerializer.Load(schemaJson);
		Models.Schema schema = result.Schema
			?? throw new InvalidOperationException($"The embedded schema did not load: {result.Message}");
		return schema.GetClass(className.As<ClassName>())
			?? throw new InvalidOperationException($"The embedded schema has no class '{className}'.");
	}

	private static InvalidOperationException Unreadable() =>
		new("The data is not JSON, so there are no records to edit. Its issues say why.");
}
