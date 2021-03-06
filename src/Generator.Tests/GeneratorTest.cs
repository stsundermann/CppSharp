﻿using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using CppSharp.AST;
using CppSharp.Generators;
using NUnit.Framework;

namespace CppSharp.Utils
{
    /// <summary>
    /// The main base class for a generator-based tests project.
    /// </summary>
    public abstract class GeneratorTest : ILibrary
    {
        readonly string name;
        readonly GeneratorKind kind;

        protected GeneratorTest(string name, GeneratorKind kind)
        {
            this.name = name;
            this.kind = kind;
        }

        public virtual void Setup(Driver driver)
        {
            var options = driver.Options;
            options.LibraryName = name;
            options.GeneratorKind = kind;
            options.OutputDir = Path.Combine(GetOutputDirectory(), "gen", name);
            options.SharedLibraryName = name + ".Native";
            options.GenerateLibraryNamespace = true;
            options.Quiet = true;
            options.IgnoreParseWarnings = true;

            driver.Diagnostics.EmitMessage("");
            driver.Diagnostics.EmitMessage("Generating bindings for {0} ({1})",
                options.LibraryName, options.GeneratorKind.ToString());

            // Workaround for CLR which does not check for .dll if the
            // name already has a dot.
            if (System.Type.GetType("Mono.Runtime") == null)
                options.SharedLibraryName += ".dll";

            var path = Path.GetFullPath(GetTestsDirectory(name));
            options.addIncludeDirs(path);

            var headersPaths = new System.Collections.Generic.List<string> {
                Path.GetFullPath(Path.Combine(path, "../../deps/llvm/tools/clang/lib/Headers"))
            };

            if (IsMacOS) {
                options.addArguments ("-stdlib=libc++");
            }

            foreach (var header in headersPaths)
                options.addSystemIncludeDirs(header);

            driver.Diagnostics.EmitMessage("Looking for tests in: {0}", path);
            var files = Directory.EnumerateFiles(path, "*.h");
            foreach (var file in files)
                options.Headers.Add(Path.GetFileName(file));
        }

        public virtual void Preprocess(Driver driver, ASTContext ctx)
        {
        }

        public virtual void Postprocess(Driver driver, ASTContext ctx)
        {
        }

        public virtual void SetupPasses(Driver driver)
        {
        }

        #region Helpers
        public static string GetTestsDirectory(string name)
        {
            var directory = Directory.GetParent(Directory.GetCurrentDirectory());

            while (directory != null)
            {
                var path = Path.Combine(directory.FullName, "tests", name);

                if (Directory.Exists(path))
                    return path;

                directory = directory.Parent;
            }

            throw new Exception(string.Format(
                "Tests directory for project '{0}' was not found", name));
        }

        static string GetOutputDirectory()
        {
            var directory = Directory.GetParent(Directory.GetCurrentDirectory());

            while (directory != null)
            {
                var path = Path.Combine(directory.FullName, "obj");

                if (Directory.Exists(path))
                    return directory.FullName;

                directory = directory.Parent;
            }

            throw new Exception("Could not find tests output directory");
        }

        [DllImport ("libc")]
        static extern int uname (IntPtr buf);

        public static bool IsMacOS {
            get {
                if (Environment.OSVersion.Platform != PlatformID.Unix)
                    return false;

                IntPtr buf = Marshal.AllocHGlobal (8192);
                if (uname (buf) == 0) {
                    string os = Marshal.PtrToStringAnsi (buf);
                    switch (os) {
                    case "Darwin":
                        return true;
                    }
                }
                Marshal.FreeHGlobal (buf);

                return false;
            }
        }
        #endregion
    }

    /// <summary>
    /// The main NUnit fixture base class for a generator-based tests project.
    /// Provides support for a text-based test system that looks for lines
    /// in the native test declarations that match a certain pattern, which
    /// are used for certain kinds of tests that cannot be done with just
    /// C# code and using the generated wrappers.
    /// </summary>
    [TestFixture]
    public abstract class GeneratorTestFixture
    {
        readonly string assemblyName;

        protected GeneratorTestFixture()
        {
            var location = Assembly.GetCallingAssembly().Location;
            assemblyName = Path.GetFileNameWithoutExtension(location);
        }

        static bool GetGeneratorKindFromLang(string lang, out GeneratorKind kind)
        {
            kind = GeneratorKind.CSharp;

            switch(lang)
            {
            case "CSharp":
            case "C#":
                kind = GeneratorKind.CSharp;
                return true;
            case "CLI":
                kind = GeneratorKind.CLI;
                return true;
            }

            return false;
        }

        [Test]
        public void CheckDirectives()
        {
            var name = assemblyName.Substring(0, assemblyName.IndexOf('.'));
            var kind = assemblyName.Substring(assemblyName.LastIndexOf('.') + 1);
            GeneratorKind testKind;
            if (!GetGeneratorKindFromLang(kind, out testKind))
                throw new NotSupportedException("Unknown language generator");

            var path = Path.GetFullPath(GeneratorTest.GetTestsDirectory(name));

            foreach (var header in Directory.EnumerateFiles(path, "*.h"))
            {
                var headerText = File.ReadAllText(header);

                // Parse the header looking for suitable lines to test.
                foreach (var line in File.ReadAllLines(header))
                {
                    var match = Regex.Match(line, @"^\s*///*\s*(\S+)\s*:\s*(.*)");
                    if (!match.Success)
                        continue;

                    var matchLang = match.Groups[1].Value;
                    GeneratorKind matchKind;
                    if (!GetGeneratorKindFromLang(matchLang.ToUpper(), out matchKind))
                        continue;

                    if (matchKind != testKind)
                        continue;

                    var matchText = match.Groups[2].Value;
                    if (string.IsNullOrWhiteSpace(matchText))
                        continue;

                    var matchIndex = headerText.IndexOf(matchText, StringComparison.Ordinal);
                    Assert.IsTrue(matchIndex != -1,
                        string.Format("Could not match '{0}' in file '{1}'",
                        matchText, header));
                }
            }

        }
    }
}
