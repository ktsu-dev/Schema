// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp;

/// <summary>
/// The schema says something this generator's target cannot express.
/// </summary>
/// <remarks>
/// Thrown rather than returned because there is nothing partial to hand back: a header missing the
/// one type it could not spell does not compile, and emitting a placeholder would put the error in
/// generated code rather than at the schema line that caused it. The message names the schema
/// element and what the target would need in order to say it.
/// </remarks>
public sealed class CppGenerationException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CppGenerationException"/> class.
	/// </summary>
	public CppGenerationException()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CppGenerationException"/> class.
	/// </summary>
	/// <param name="message">What could not be expressed, and what would be needed to express it.</param>
	public CppGenerationException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CppGenerationException"/> class.
	/// </summary>
	/// <param name="message">What could not be expressed.</param>
	/// <param name="innerException">The exception that caused this one.</param>
	public CppGenerationException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
