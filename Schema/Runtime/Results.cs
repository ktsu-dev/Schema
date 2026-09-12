// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Runtime;

/// <summary>
/// The CLR representation of the schema's <c>Result</c>: what a call that can fail returns.
/// </summary>
/// <remarks>
/// Here for the same reason <see cref="ColorRgb"/> is - the base class library has nothing that
/// means this, and a generator inventing its own would break the round trip that reimports
/// generated types back into a schema.
/// <para>
/// The error is a type parameter rather than a fixed type, but it is not a per-signature choice:
/// the schema names one error enum and every fallible signature in it returns that one, so a
/// generator writes the same <typeparamref name="TError"/> throughout a schema. It is a parameter
/// only because the library cannot know which enum a given schema declares.
/// </para>
/// </remarks>
/// <typeparam name="TValue">What a successful call produced.</typeparam>
/// <typeparam name="TError">The schema's error enum.</typeparam>
public readonly record struct Result<TValue, TError>
	where TError : struct, Enum
{
	private Result(bool succeeded, TValue? value, TError error)
	{
		IsSuccess = succeeded;
		StoredValue = value;
		Error = error;
	}

	/// <summary>
	/// Gets a value indicating whether the call succeeded.
	/// </summary>
	public bool IsSuccess { get; }

	/// <summary>
	/// Gets why the call failed. Meaningful only when <see cref="IsSuccess"/> is false.
	/// </summary>
	public TError Error { get; }

	/// <summary>
	/// Gets what the call produced.
	/// </summary>
	/// <exception cref="InvalidOperationException">The call failed, so there is no value.</exception>
	public TValue Value => IsSuccess
		? StoredValue!
		: throw new InvalidOperationException($"The call failed with '{Error}', so it produced no value.");

	private TValue? StoredValue { get; }

	/// <summary>
	/// Makes a result carrying what the call produced.
	/// </summary>
	/// <param name="value">What the call produced.</param>
	/// <returns>A successful result.</returns>
	public static Result<TValue, TError> Ok(TValue value) => new(true, value, default);

	/// <summary>
	/// Makes a result carrying why the call failed.
	/// </summary>
	/// <param name="error">Why it failed.</param>
	/// <returns>A failed result.</returns>
	public static Result<TValue, TError> Fail(TError error) => new(false, default, error);

	/// <summary>
	/// Gets what the call produced, without throwing when it failed.
	/// </summary>
	/// <param name="value">What the call produced, when it succeeded.</param>
	/// <returns><see langword="true"/> when the call succeeded.</returns>
	public bool TryGetValue(out TValue? value)
	{
		value = StoredValue;
		return IsSuccess;
	}
}

/// <summary>
/// The CLR representation of the schema's <c>Result&lt;Void&gt;</c>: a call that can fail and
/// produces nothing.
/// </summary>
/// <remarks>
/// A separate type because C# has no <c>void</c> type argument, so the value-carrying form cannot
/// be closed over nothing the way C++'s can. The arity is what tells the two apart on reimport.
/// </remarks>
/// <typeparam name="TError">The schema's error enum.</typeparam>
public readonly record struct Result<TError>
	where TError : struct, Enum
{
	private Result(bool succeeded, TError error)
	{
		IsSuccess = succeeded;
		Error = error;
	}

	/// <summary>
	/// Gets a value indicating whether the call succeeded.
	/// </summary>
	public bool IsSuccess { get; }

	/// <summary>
	/// Gets why the call failed. Meaningful only when <see cref="IsSuccess"/> is false.
	/// </summary>
	public TError Error { get; }

	/// <summary>
	/// Makes a result saying the call succeeded.
	/// </summary>
	/// <returns>A successful result.</returns>
	public static Result<TError> Ok() => new(true, default);

	/// <summary>
	/// Makes a result carrying why the call failed.
	/// </summary>
	/// <param name="error">Why it failed.</param>
	/// <returns>A failed result.</returns>
	public static Result<TError> Fail(TError error) => new(false, error);
}
