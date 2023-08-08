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
        
        private const string HandlingAutoBindingsMethodName = "HandleAutoBindings";
        private const string HandlingAutoDisposeMethodName = "HandleAutoDispose";
        
        private static readonly LocalizableString AutoBindingsRuleTitle = "HandleAutoBindings method should be called during constructor.";
        private static readonly LocalizableString AutoBindingsRuleMessageFormat = "HandleAutoBindings method should be called during constructor.";
        private static readonly LocalizableString AutoBindingsRuleDescription = "HandleAutoBindings method should be called during constructor.";
        
        private static readonly LocalizableString AutoDisposeNotCalledTitle = "HandleAutoDispose method should be called during Dispose() method.";
        private static readonly LocalizableString AutoDisposeNotCalledMessageFormat = "HandleAutoDispose method should be called during Dispose() method.";
        private static readonly LocalizableString AutoDisposeNotCalledDescription = "HandleAutoDispose method should be called during Dispose() method.";
        
        private static readonly LocalizableString AutoDisposeMoreThanOnceTitle = "HandleAutoDispose method should be called only once during Dispose() method.";
        private static readonly LocalizableString AutoDisposeMoreThanOnceMessageFormat = "HandleAutoDispose method should be called only once during Dispose() method.";
        private static readonly LocalizableString AutoDisposeMoreThanOnceDescription = "HandleAutoDispose method should be called only once during Dispose() method.";

        private static readonly DiagnosticDescriptor AutoBindingsRule = new DiagnosticDescriptor(DiagnosticId, AutoBindingsRuleTitle, AutoBindingsRuleMessageFormat,
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: AutoBindingsRuleDescription, helpLinkUri: HelpLinkUri);
        
        private static readonly DiagnosticDescriptor AutoDisposeNotCalledRule = new DiagnosticDescriptor(DiagnosticId, AutoDisposeNotCalledTitle, AutoDisposeNotCalledMessageFormat,
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: AutoDisposeNotCalledDescription, helpLinkUri: HelpLinkUri);
        
        private static readonly DiagnosticDescriptor AutoDisposeCalledMoreThanOnceRule = new DiagnosticDescriptor(DiagnosticId, AutoDisposeMoreThanOnceTitle, AutoDisposeMoreThanOnceMessageFormat,
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: AutoDisposeMoreThanOnceDescription, helpLinkUri: HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(AutoBindingsRule, AutoDisposeNotCalledRule, AutoDisposeCalledMoreThanOnceRule);


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

                ReportNotCallingHandleAutoBindingsInConstructor(context, classNode, classDeclarationSyntax);
                ReportNotCallingHandleAutoDisposeInDispose(context, classNode);
            }
        }

        private static void ReportNotCallingHandleAutoBindingsInConstructor(SyntaxNodeAnalysisContext context, SyntaxNode classNode, ClassDeclarationSyntax classDeclarationSyntax)
        {
            var constructors = classNode.ChildNodes().OfType<ConstructorDeclarationSyntax>().ToImmutableArray();
            if (!constructors.Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(AutoBindingsRule, classDeclarationSyntax.GetLocation()));

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

                    if (methodSymbol.Name == HandlingAutoBindingsMethodName)
                    {
                        return;
                    }
                }
            }

            var diagnostic = Diagnostic.Create(AutoBindingsRule, constructors.First().GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
        
        private static void ReportNotCallingHandleAutoDisposeInDispose(SyntaxNodeAnalysisContext context, SyntaxNode classNode)
        {
            var disposeMethodSyntax = classNode
                .ChildNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(
                    method =>
                    {
                        bool isDisposeImplementation = method.Modifiers.Any(SyntaxKind.PublicKeyword);
                        isDisposeImplementation &= method.ReturnType is PredefinedTypeSyntax predefinedType && predefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword);
                        isDisposeImplementation &= method.ParameterList.Parameters.Count == 0;
                        isDisposeImplementation &= method.Identifier.Text == "Dispose";

                        return isDisposeImplementation;
                    })
                .FirstOrDefault();
            
            if (disposeMethodSyntax == null)
            {
                return;
            }

            var callsToHandleAutoDispose = disposeMethodSyntax
                .DescendantNodesAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .Where(
                    invocationExpressionSyntax =>
                    {
                        if (!IsInvocationOfThisObjectsMethod(invocationExpressionSyntax))
                        {
                            return false;
                        }

                        if (context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax).Symbol is not IMethodSymbol methodSymbol)
                        {
                            return false;
                        }

                        return methodSymbol.Name == HandlingAutoDisposeMethodName;
                    }
                )
                .ToArray();

            if (callsToHandleAutoDispose.Length == 1)
            {
                return;
            }

            if (callsToHandleAutoDispose.Length == 0)
            {
                var diagnostic = Diagnostic.Create(AutoDisposeNotCalledRule, disposeMethodSyntax.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
            else
            {
                foreach (var invocationSyntax in callsToHandleAutoDispose)
                {
                    var diagnostic = Diagnostic.Create(AutoDisposeCalledMoreThanOnceRule, invocationSyntax.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static SyntaxNode GetHandwrittenPartOfClass(ITypeSymbol classSymbol)
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
