using CppSharp.AST;

namespace CppSharp.Passes
{
    // This pass visits all the translation units and checks if they originate from library being processed or
    // from some other library that is being depended upon. It will rename the root namespaces of all the "foreign"
    // libraries so that there wouldn't be clashes and so that the code generation phase would be able to generate
    // names with fully qualified namespace prefixes.
    public class RenameRootNamespacesPass : TranslationUnitPass
    {

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            if (!base.VisitTranslationUnit(unit))
                return false;


            var fileName = unit.TranslationUnit.FileName;
            if (Driver.RootNamespaceRenames.ContainsKey(fileName))
            {
                var rootNamespace = Driver.RootNamespaceRenames[fileName].rootNamespaceName;
                if (this.Driver.Options.OutputNamespace != rootNamespace)
                    unit.Name = rootNamespace;
            }
            else if (unit.GenerationKind == GenerationKind.Generate)
            {
                Driver.RootNamespaceRenames.Add(fileName, new Driver.TranslationUnitRenameInfo
                {
                    translationUnit = fileName,
                    rootNamespaceName = Driver.Options.OutputNamespace,
                });
            }
            return true;
        }

    }
}