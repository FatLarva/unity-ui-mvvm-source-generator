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
        private const string LifetimeDisposableName = "_lifetimeDisposable";
        private const string DisposeMethodName = "Dispose";
        
        private static readonly LocalizableString AutoBindingsRuleTitle = $"{HandlingAutoBindingsMethodName} method should be called during constructor. And it should be called only once.";
        private static readonly LocalizableString AutoBindingsRuleMessageFormat = $"{HandlingAutoBindingsMethodName} method should be called during constructor. And it should be called only once.";
        private static readonly LocalizableString AutoBindingsRuleDescription = $"{HandlingAutoBindingsMethodName} method should be called during constructor. And it should be called only once.";
        
        private static readonly LocalizableString AutoDisposeNotCalledTitle = $"{HandlingAutoDisposeMethodName} method should be called during {DisposeMethodName}() method.";
        private static readonly LocalizableString AutoDisposeNotCalledMessageFormat = $"{HandlingAutoDisposeMethodName} method should be called during {DisposeMethodName}() method.";
        private static readonly LocalizableString AutoDisposeNotCalledDescription = $"{HandlingAutoDisposeMethodName} method should be called during {DisposeMethodName}() method.";
        
        private static readonly LocalizableString AutoDisposeMoreThanOnceTitle = $"{HandlingAutoDisposeMethodName} method should be called only once during {DisposeMethodName}() method.";
        private static readonly LocalizableString AutoDisposeMoreThanOnceMessageFormat = $"{HandlingAutoDisposeMethodName} method should be called only once during {DisposeMethodName}() method.";
        private static readonly LocalizableString AutoDisposeMoreThanOnceDescription = $"{HandlingAutoDisposeMethodName} method should be called only once during {DisposeMethodName}() method.";
        
        private static readonly LocalizableString LifetimeDisposableDirectDisposeTitle = $"{LifetimeDisposableName}.{DisposeMethodName}() should not be called directly. Call {HandlingAutoDisposeMethodName}() instead.";
        private static readonly LocalizableString LifetimeDisposableDirectDisposeMessageFormat = $"{LifetimeDisposableName}.{DisposeMethodName}() should not be called directly. Call {HandlingAutoDisposeMethodName}() instead.";
        private static readonly LocalizableString LifetimeDisposableDirectDisposeDescription = $"{LifetimeDisposableName}.{DisposeMethodName}() should not be called directly. Call {HandlingAutoDisposeMethodName}() instead.";

        private static readonly DiagnosticDescriptor AutoBindingsRule = new DiagnosticDescriptor(DiagnosticId, AutoBindingsRuleTitle, AutoBindingsRuleMessageFormat,
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: AutoBindingsRuleDescription, helpLinkUri: HelpLinkUri);
        
        private static readonly DiagnosticDescriptor AutoDisposeNotCalledRule = new DiagnosticDescriptor(DiagnosticId, AutoDisposeNotCalledTitle, AutoDisposeNotCalledMessageFormat,
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: AutoDisposeNotCalledDescription, helpLinkUri: HelpLinkUri);
        
        private static readonly DiagnosticDescriptor AutoDisposeCalledMoreThanOnceRule = new DiagnosticDescriptor(DiagnosticId, AutoDisposeMoreThanOnceTitle, AutoDisposeMoreThanOnceMessageFormat,
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: AutoDisposeMoreThanOnceDescription, helpLinkUri: HelpLinkUri);
        
        private static readonly DiagnosticDescriptor LifetimeDisposableDirectDisposeRule = new DiagnosticDescriptor(DiagnosticId, LifetimeDisposableDirectDisposeTitle, LifetimeDisposableDirectDisposeMessageFormat,
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: LifetimeDisposableDirectDisposeDescription, helpLinkUri: HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(AutoBindingsRule, AutoDisposeNotCalledRule, AutoDisposeCalledMoreThanOnceRule, LifetimeDisposableDirectDisposeRule);

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
                ReportDirectlyCallingDisposeOnLifetimeDisposable(context, classNode);
            }
        }

        private void ReportDirectlyCallingDisposeOnLifetimeDisposable(SyntaxNodeAnalysisContext context, SyntaxNode classNode)
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
                        isDisposeImplementation &= method.Identifier.Text == DisposeMethodName;

                        return isDisposeImplementation;
                    })
                .FirstOrDefault();
            
            if (disposeMethodSyntax == null)
            {
                return;
            }

            if (IsDisposeCalledOnLifetimeDisposable(disposeMethodSyntax, out Location location))
            {
                var diagnostic = Diagnostic.Create(LifetimeDisposableDirectDisposeRule, location);
                context.ReportDiagnostic(diagnostic);
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
                var handleAutoBindingsCalls = constructorDeclarationSyntax.DescendantNodesAndSelf()
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

                            return methodSymbol.Name == HandlingAutoBindingsMethodName;
                        }
                    )
                    .ToArray();
                
                if (handleAutoBindingsCalls.Length == 1)
                {
                    return;
                }

                if (handleAutoBindingsCalls.Length == 0)
                {
                    var diagnostic = Diagnostic.Create(AutoBindingsRule, constructorDeclarationSyntax.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
                else
                {
                    foreach (var invocationSyntax in handleAutoBindingsCalls)
                    {
                        var diagnostic = Diagnostic.Create(AutoBindingsRule, invocationSyntax.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
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
                        isDisposeImplementation &= method.Identifier.Text == DisposeMethodName;

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
        
        private static bool IsDisposeCalledOnLifetimeDisposable(MethodDeclarationSyntax methodSyntax, out Location location)
        {
            foreach (var invocation in methodSyntax.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is MemberAccessExpressionSyntax { Name: { Identifier: { Text: DisposeMethodName } } } memberAccess &&
                    memberAccess.Expression is IdentifierNameSyntax { Identifier: { Text: LifetimeDisposableName } })
                {
                    location = memberAccess.GetLocation();
                    return true;
                }
            }

            location = null;
            return false;
        }
    }
}
