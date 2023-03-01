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
        public bool IsEligible => ViewsClassesToProcess.Count > 0;
        
        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is ClassDeclarationSyntax classDeclarationSyntax &&
                classDeclarationSyntax.AttributeLists.Count > 0)
            {
                INamedTypeSymbol typeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) as INamedTypeSymbol;
                
                if (IsDerivedFrom(typeSymbol, "MonoBehaviour"))
                {
                    ImmutableArray<AttributeData>? attributes = typeSymbol?.GetAttributes();
                    if (attributes!.Any((AttributeData ad) => ad.AttributeClass?.Name == ViewModelGenerateAttributeTemplate.AttributeName))
                    {
                        ViewsClassesToProcess.Add(typeSymbol);
                    }
                }
            }
        }

        private bool IsDerivedFrom(INamedTypeSymbol baseType, string targetType)
        {
            while (baseType != null)
            {
                if (baseType.Name == targetType)
                    return true;

                baseType = baseType.BaseType;
            }

            return false;
        }
    }
}
