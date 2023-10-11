using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ViewsSourceGenerator
{
    internal class ViewClassSyntaxReceiver : ISyntaxContextReceiver
    {
        public readonly List<INamedTypeSymbol> ViewsClassesToProcess = new();
        public readonly List<INamedTypeSymbol> ModelsClassesToProcess = new();
        
        public bool IsEligibleForViewClassesProcessing => ViewsClassesToProcess.Count > 0;
        
        public bool IsEligibleForModelClassesProcessing => ModelsClassesToProcess.Count > 0;
        
        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is ClassDeclarationSyntax { AttributeLists: { Count: > 0 } } classDeclarationSyntax)
            {
                if (context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol typeSymbol)
                {
                    return;
                }
                
                ImmutableArray<AttributeData> attributes = typeSymbol.GetAttributes();
                if (attributes.Length == 0)
                {
                    return;
                }
                
                if (IsDerivedFrom(typeSymbol, "MonoBehaviour"))
                {
                    if (attributes.Any(ad => ad.AttributeClass?.Name == ViewModelGenerateAttributeTemplate.AttributeName))
                    {
                        ViewsClassesToProcess.Add(typeSymbol);
                    }
                }
                else
                {
                    if (attributes.Any(ad => ad.AttributeClass?.Name == CommonModelAttributeTemplate.AttributeName))
                    {
                        ModelsClassesToProcess.Add(typeSymbol);
                    }
                }
            }
        }

        private bool IsDerivedFrom(INamedTypeSymbol? baseType, string targetType)
        {
            while (baseType != null)
            {
                if (baseType.Name == targetType)
                {
                    return true;
                }

                baseType = baseType.BaseType;
            }

            return false;
        }
    }
}
