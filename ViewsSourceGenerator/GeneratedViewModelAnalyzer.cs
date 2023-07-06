using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ViewsSourceGenerator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class GeneratedViewModelAnalyzer : DiagnosticAnalyzer
    {
        private const string DiagnosticId = "GeneratedViewModelAnalyzer";
        private const string Category = "ConstructionSafety";
        private const string HelpLinkUri = "";
        
        private const string HandlingMethodName = "HandleAutoBindings";
        
        private static readonly LocalizableString Title = "HandleAutoBindings method should be called during constructor.";
        private static readonly LocalizableString MessageFormat = "HandleAutoBindings method should be called during constructor.";
        private static readonly LocalizableString Description = "HandleAutoBindings method should be called during constructor.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description,
            helpLinkUri: HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);


        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
            var classSymbol = (ITypeSymbol)context.ContainingSymbol;

            if (classSymbol != null && HasGeneratedViewModelAttribute(classSymbol))
            {
                SyntaxNode classNode = GetHandwrittenPartOfClass(classSymbol);

                if (classNode == null)
                {
                    return;
                }

                var constructors = classNode.ChildNodes().OfType<ConstructorDeclarationSyntax>().ToImmutableArray();
                if (!constructors.Any())
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, classDeclarationSyntax.GetLocation()));
                    return;
                }

                foreach (var constructorDeclarationSyntax in constructors)
                {
                    foreach (var expressionSyntax in constructorDeclarationSyntax.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
                    {
                        if (!IsInvocationOfThisObjectsMethod(expressionSyntax))
                        {
                            continue;
                        }

                        if (context.SemanticModel.GetSymbolInfo(expressionSyntax).Symbol is not IMethodSymbol methodSymbol)
                        {
                            continue;
                        }

                        if (methodSymbol.Name == HandlingMethodName)
                        {
                            return;
                        }
                    }
                }
                
                var diagnostic = Diagnostic.Create(Rule, constructors.First().GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private SyntaxNode GetHandwrittenPartOfClass(ITypeSymbol classSymbol)
        {
            var className = classSymbol.Name;
            foreach (var declarationReference in classSymbol.DeclaringSyntaxReferences)
            {
                var classDeclaration = declarationReference
                    .GetSyntax()
                    .DescendantNodesAndSelf()
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault(classDeclaration => (string)classDeclaration.Identifier.Value == className);

                if (classDeclaration == null)
                {
                    continue;
                }

                if (classDeclaration.AttributeLists.Any(attributeList => attributeList.Attributes.Any(attributeSyntax => attributeSyntax.Name.ToFullString() == GeneratedViewModelAttributeTemplate.MetaDataName)))
                {
                    continue;
                }

                return declarationReference.GetSyntax();
            }

            return null;
        }

        private static bool HasGeneratedViewModelAttribute(ISymbol classSymbol)
        {
            return classSymbol.GetAttributes()
                .Any(ad => ad?.AttributeClass?.ToDisplayString() == GeneratedViewModelAttributeTemplate.MetaDataName);
        }
        
        private static bool IsInvocationOfThisObjectsMethod(InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var target = memberAccess.Expression;

                if (target is IdentifierNameSyntax)
                {
                    return false;
                }
                else if (target is ThisExpressionSyntax)
                {
                    return true;
                }
            }
            else if (invocation.Expression is IdentifierNameSyntax)
            {
                return true;
            }

            return false;
        }
    }
}
