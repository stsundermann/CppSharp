using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Cxxi
{
    public enum CppAbi
    {
        Itanium,
        Microsoft,
        ARM
    }

    /// <summary>
    /// Represents a parsed C++ unit.
    /// </summary>
    [DebuggerDisplay("File = {FileName}, Ignored = {Ignore}")]
    public class TranslationUnit : Namespace
    {
        public TranslationUnit(string file)
        {
            ForwardReferences = new List<Declaration>();
            Macros = new List<MacroDefinition>();
            FilePath = file;
        }

        /// Forward reference declarations.
        public List<Declaration> ForwardReferences;

        /// Contains the macros present in the unit.
        public List<MacroDefinition> Macros;

        /// If the module should be ignored.
        public override bool Ignore
        {
            get { return ExplicityIgnored; }
        }

        public bool IsSystemHeader { get; set; }
        
        /// Contains the path to the file.
        public string FilePath;

        /// Contains the name of the file.
        public string FileName
        {
            get { return Path.GetFileName(FilePath); }
        }

        /// Contains the name of the module.
        public string FileNameWithoutExtension
        {
            get { return Path.GetFileNameWithoutExtension(FileName); }
        }

        /// Contains the include path.
        public string IncludePath;
    }

    /// <summary>
    /// A library contains all the modules.
    /// </summary>
    public class Library
    {
        public string Name;
        public string Native;
        public List<TranslationUnit> TranslationUnits;

        public Library(string name, string native)
        {
            Name = name;
            Native = native;
            TranslationUnits = new List<TranslationUnit>();
        }

        /// Finds an existing module or creates a new one given a file path.
        public TranslationUnit FindOrCreateModule(string file)
        {
            var module = TranslationUnits.Find(m => m.FilePath.Equals(file));

            if (module == null)
            {
                module = new TranslationUnit(file);
                TranslationUnits.Add(module);
            }

            return module;
        }

        /// Finds an existing enum in the library modules.
        public Enumeration FindEnum(string name)
        {
            foreach (var module in TranslationUnits)
            {
                var type = module.FindEnum(name);
                if (type != null) return type;
            }

            return null;
        }

        /// Finds an existing struct/class in the library modules.
        public Class FindClass(string name, bool create = false)
        {
            foreach (var module in TranslationUnits)
            {
                var type = module.FindClass(name, create);
                if (type != null) return type;
            }

            return null;
        }
    }
}