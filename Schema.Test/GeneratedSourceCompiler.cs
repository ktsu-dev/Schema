// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Tests;

using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Compiles generated C# so tests can assert that it builds and then reimport the compiled types.
/// </summary>
/// <remarks>
/// Asserting on the generated text alone would only prove the generator emits the strings the test
/// expects. Compiling it is the only way to know the output is valid C#, and loading it is what
/// makes the generate-then-reimport round trip possible.
/// </remarks>
internal static class GeneratedSourceCompiler
{
	/// <summary>
	/// Compiles source files into a loadable assembly, failing the test with the compiler's own
	/// diagnostics if it does not build.
	/// </summary>
	/// <param name="files">Source keyed by file name.</param>
	/// <returns>The compiled, loaded assembly.</returns>
	internal static Assembly Compile(IReadOnlyDictionary<string, string> files)
	{
		using MemoryStream stream = new();
		EmitResult emitted = CreateCompilation(files).Emit(stream);

		if (!emitted.Success)
		{
			Assert.Fail(DescribeFailure(emitted, files));
		}

		return Assembly.Load(stream.ToArray());
	}

	private static CSharpCompilation CreateCompilation(IReadOnlyDictionary<string, string> files) =>
		CSharpCompilation.Create(
			$"Generated_{Guid.NewGuid():N}",
			files.Select(f => CSharpSyntaxTree.ParseText(f.Value, path: f.Key)),
			LoadedAssemblyReferences(),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

	/// <summary>
	/// References everything already loaded into this test process, which is the simplest way to
	/// give the generated source the same view of the world the tests have - including the
	/// library's own runtime colour types.
	/// </summary>
	private static IEnumerable<MetadataReference> LoadedAssemblyReferences() =>
		AppDomain.CurrentDomain.GetAssemblies()
			.Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
			.Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location));

	private static string DescribeFailure(EmitResult emitted, IReadOnlyDictionary<string, string> files)
	{
		string errors = string.Join(
			Environment.NewLine,
			emitted.Diagnostics
				.Where(d => d.Severity == DiagnosticSeverity.Error)
				.Select(d => d.ToString()));

		string source = string.Join(
			Environment.NewLine,
			files.Select(f => $"// {f.Key}{Environment.NewLine}{f.Value}"));

		return $"Generated source did not compile:{Environment.NewLine}{errors}{Environment.NewLine}Source:{Environment.NewLine}{source}";
	}
}
