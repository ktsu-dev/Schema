// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Schema.Cpp.Test;

using System.Diagnostics;

using ktsu.Schema.Cpp;
using ktsu.Schema.Models;
using ktsu.Schema.Models.Names;
using ktsu.Schema.Models.Types;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// What a description spanning several lines becomes in a generated header.
/// </summary>
/// <remarks>
/// A description is written behind <c>//</c>, which ends at a bare carriage return as well as at a
/// line feed, so a description has to be written as one comment line per line of text or whatever
/// follows the terminator is compiled.
/// </remarks>
[TestClass]
public class DocCommentLineTests
{
	/// <summary>
	/// The terminators a description might carry, each followed by text that is not valid C++.
	/// </summary>
	private const string Description =
		"first line\nsecond = line\r\nthird ( line\rfourth } line\u0085fifth ; line\u2028sixth , line\u2029seventh line";

	/// <summary>
	/// Each line of the description becomes its own comment line, on the struct and on its field,
	/// and none of the terminators survives into the header.
	/// </summary>
	[TestMethod]
	public void EachLineOfTheDescriptionIsItsOwnCommentLine()
	{
		string text = Generate()["Body.gen.hpp"];

		foreach (string line in (string[])["first line", "second = line", "third ( line", "fourth } line", "fifth ; line", "sixth , line", "seventh line"])
		{
			Assert.AreEqual(2, text.Split($"// {line}").Length - 1, $"'{line}' should be a comment line on the struct and on the field");
		}

		foreach (char terminator in (char[])['\r', '\u0085', '\u2028', '\u2029'])
		{
			Assert.DoesNotContain(terminator.ToString(), text);
		}
	}

	/// <summary>
	/// The header compiles, which it only does if no line of the description escaped its comment.
	/// </summary>
	[TestMethod]
	public void AMultiLineDescriptionCompiles()
	{
		IReadOnlyDictionary<string, string> files = Generate();
		string directory = Path.Join(Path.GetTempPath(), $"schema-doc-{Guid.NewGuid():N}");

		Directory.CreateDirectory(directory);

		try
		{
			foreach ((string name, string text) in files)
			{
				File.WriteAllText(Path.Join(directory, name), text);
			}

			string includes = string.Concat(files.Keys.Order(StringComparer.Ordinal).Select(name => $"#include \"{name}\"\n"));
			File.WriteAllText(Path.Join(directory, "all.cpp"), $"{includes}\nint main() {{ return 0; }}\n");

			(int exitCode, string output) = Compile(directory, "all.cpp");

			Assert.AreEqual(0, exitCode, $"the generated C++ should compile:\n{output}");
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	/// <summary>
	/// A description whose line ends in a backslash leaves the declaration after it in place.
	/// </summary>
	/// <remarks>
	/// A backslash at the end of a line splices the next line onto it before comments are
	/// removed, so a <c>//</c> comment ending in one swallows the declaration it documents. On a
	/// member the header still compiles, with only a <c>-Wcomment</c> warning and the member gone,
	/// so the check is that the struct still has it. On the struct the swallowed line is
	/// <c>struct Body</c>, and the header failing to compile is what gives it away. Trailing spaces
	/// after the backslash are covered too: GCC and Clang splice across them.
	/// </remarks>
	[TestMethod]
	[DataRow("Install dir, e.g. C:\\Games\\")]
	[DataRow("Install dir, e.g. C:\\Games\\   ")]
	[DataRow("first line ends here\\\nsecond line too\\")]
	public void ADescriptionEndingInABackslashKeepsTheDeclarationAfterIt(string description)
	{
		foreach (string? classDescription in (string?[])["A body.", null])
		{
			IReadOnlyDictionary<string, string> files = Generate(description, classDescription);
			string directory = Path.Join(Path.GetTempPath(), $"schema-doc-{Guid.NewGuid():N}");

			Directory.CreateDirectory(directory);

			try
			{
				foreach ((string name, string text) in files)
				{
					File.WriteAllText(Path.Join(directory, name), text);
				}

				string includes = string.Concat(files.Keys.Order(StringComparer.Ordinal).Select(name => $"#include \"{name}\"\n"));
				File.WriteAllText(
					Path.Join(directory, "all.cpp"),
					$"{includes}\nstatic_assert(requires(sample::Body body) {{ body.speed; }}, \"Body lost its speed member\");\nint main() {{ return 0; }}\n");

				(int exitCode, string output) = Compile(directory, "all.cpp");

				string where = classDescription is null ? "on the struct and its member" : "on the member";
				Assert.AreEqual(0, exitCode, $"with the description {where}, the header should compile and keep Body::speed:\n{output}");
				Assert.DoesNotContain("-Wcomment", output, $"with the description {where}, no comment line should end in a backslash:\n{output}");
			}
			finally
			{
				Directory.Delete(directory, recursive: true);
			}
		}
	}

	private static IReadOnlyDictionary<string, string> Generate(string description = Description, string? classDescription = null)
	{
		Schema schema = new();

		SchemaCodeGenerator configuration = schema.AddCodeGenerator("Cpp".As<CodeGeneratorName>())!;
		configuration.Language = CppCodeGenerator.LanguageId.As<LanguageName>();
		configuration.Namespace = "sample".As<CodeNamespace>();

		SchemaClass body = schema.AddClass("Body".As<ClassName>())!;
		body.Description = (classDescription ?? description).As<SchemaChildDescription>();

		SchemaMember speed = body.AddMember("Speed".As<MemberName>())!;
		speed.SetType(new Float());
		speed.Description = description.As<SchemaChildDescription>();

		return new CppCodeGenerator().Generate(schema, configuration);
	}

	private static (int ExitCode, string Output) Compile(string directory, string file)
	{
		string? compiler = Find("g++") ?? Find("clang++");

		if (compiler is null)
		{
			Assert.Inconclusive("no C++ compiler on PATH, so the generated header was not compiled.");
		}

		using Process process = new()
		{
			StartInfo = new ProcessStartInfo(compiler!)
			{
				WorkingDirectory = directory,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
			},
		};

		foreach (string argument in (string[])["-std=c++20", "-Wall", "-Wextra", "-fsyntax-only", "-I.", file])
		{
			process.StartInfo.ArgumentList.Add(argument);
		}

		process.Start();
		string output = process.StandardError.ReadToEnd() + process.StandardOutput.ReadToEnd();
		process.WaitForExit();

		return (process.ExitCode, output);
	}

	private static string? Find(string executable) =>
		(Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
			.Select(directory => Path.Join(directory, executable))
			.FirstOrDefault(File.Exists);
}
