// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Generation;

/// <summary>
/// The schema says something a generator's target cannot express.
/// </summary>
/// <remarks>
/// A generator throws rather than returns because there is nothing partial to hand back: a file
/// missing the one type it could not spell does not compile, and emitting a placeholder would put
/// the error in generated code rather than at the schema element that caused it.
/// <para>
/// <b>The base class is what makes that refusal presentable.</b> It is a designed outcome, not a
/// fault - a target that has no fixed-shape vector is told so by name - and a caller has to be
/// able to tell it apart from a generator that has a bug in it, which is the difference between a
/// message and a stack trace. Catching <see cref="Exception"/> would conflate the two.
/// <see cref="SchemaGenerator.Generate"/> catches this and only this, and reports it as
/// <see cref="SchemaGenerationStatus.TargetCannotExpress"/>.
/// </para>
/// </remarks>
public class SchemaGenerationException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SchemaGenerationException"/> class.
	/// </summary>
	public SchemaGenerationException()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SchemaGenerationException"/> class.
	/// </summary>
	/// <param name="message">What could not be expressed, and what would be needed to express it.</param>
	public SchemaGenerationException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SchemaGenerationException"/> class.
	/// </summary>
	/// <param name="message">What could not be expressed.</param>
	/// <param name="innerException">The exception that caused this one.</param>
	public SchemaGenerationException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
