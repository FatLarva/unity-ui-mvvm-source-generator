using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ViewsSourceGenerator.Linq;

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

        private static readonly LocalizableString AutoBindingsRuleTitle =
            $"{HandlingAutoBindingsMethodName} method should be called during constructor. And it should be called only once.";

        private static readonly LocalizableString AutoBindingsRuleMessageFormat =
            $"{HandlingAutoBindingsMethodName} method should be called during constructor. And it should be called only once.";

        private static readonly LocalizableString AutoBindingsRuleDescription =
            $"{HandlingAutoBindingsMethodName} method should be called during constructor. And it should be called only once.";

        private static readonly LocalizableString AutoBindingsWrongArgumentsRuleTitle =
            $"{HandlingAutoBindingsMethodName} method called with the wrong parameters.";

        private static readonly LocalizableString AutoBindingsWrongArgumentsRuleMessageFormat =
            $"{HandlingAutoBindingsMethodName} method called with the wrong parameters.";

        private static readonly LocalizableString AutoBindingsWrongArgumentsRuleDescription =
            $"{HandlingAutoBindingsMethodName} method called with the wrong parameters.";

        private static readonly LocalizableString AutoDisposeNotCalledTitle =
            $"{HandlingAutoDisposeMethodName} method should be called during {DisposeMethodName}() method.";

        private static readonly LocalizableString AutoDisposeNotCalledMessageFormat =
            $"{HandlingAutoDisposeMethodName} method should be called during {DisposeMethodName}() method.";

        private static readonly LocalizableString AutoDisposeNotCalledDescription =
            $"{HandlingAutoDisposeMethodName} method should be called during {DisposeMethodName}() method.";

        private static readonly LocalizableString AutoDisposeMoreThanOnceTitle =
            $"{HandlingAutoDisposeMethodName} method should be called only once during {DisposeMethodName}() method.";

        private static readonly LocalizableString AutoDisposeMoreThanOnceMessageFormat =
            $"{HandlingAutoDisposeMethodName} method should be called only once during {DisposeMethodName}() method.";

        private static readonly LocalizableString AutoDisposeMoreThanOnceDescription =
            $"{HandlingAutoDisposeMethodName} method should be called only once during {DisposeMethodName}() method.";

        private static readonly LocalizableString LifetimeDisposableDirectDisposeTitle =
            $"{LifetimeDisposableName}.{DisposeMethodName}() should not be called directly. Call {HandlingAutoDisposeMethodName}() instead.";

        private static readonly LocalizableString LifetimeDisposableDirectDisposeMessageFormat =
            $"{LifetimeDisposableName}.{DisposeMethodName}() should not be called directly. Call {HandlingAutoDisposeMethodName}() instead.";

        private static readonly LocalizableString LifetimeDisposableDirectDisposeDescription =
            $"{LifetimeDisposableName}.{DisposeMethodName}() should not be called directly. Call {HandlingAutoDisposeMethodName}() instead.";

        private static readonly DiagnosticDescriptor AutoBindingsRule = new DiagnosticDescriptor(
            DiagnosticId, AutoBindingsRuleTitle, AutoBindingsRuleMessageFormat,
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: AutoBindingsRuleDescription,
            helpLinkUri: HelpLinkUri);

        private static readonly DiagnosticDescriptor AutoBindingsWrongArgumentsRule = new DiagnosticDescriptor(
            DiagnosticId, AutoBindingsWrongArgumentsRuleTitle, AutoBindingsWrongArgumentsRuleMessageFormat,
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
            description: AutoBindingsWrongArgumentsRuleDescription, helpLinkUri: HelpLinkUri);

        private static readonly DiagnosticDescriptor AutoDisposeNotCalledRule = new DiagnosticDescriptor(
            DiagnosticId, AutoDisposeNotCalledTitle, AutoDisposeNotCalledMessageFormat,
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: AutoDisposeNotCalledDescription,
            helpLinkUri: HelpLinkUri);

        private static readonly DiagnosticDescriptor AutoDisposeCalledMoreThanOnceRule = new DiagnosticDescriptor(
            DiagnosticId, AutoDisposeMoreThanOnceTitle, AutoDisposeMoreThanOnceMessageFormat,
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
            description: AutoDisposeMoreThanOnceDescription, helpLinkUri: HelpLinkUri);

        private static readonly DiagnosticDescriptor LifetimeDisposableDirectDisposeRule = new DiagnosticDescriptor(
            DiagnosticId, LifetimeDisposableDirectDisposeTitle, LifetimeDisposableDirectDisposeMessageFormat,
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
            description: LifetimeDisposableDirectDisposeDescription, helpLinkUri: HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            AutoBindingsRule,
            AutoBindingsWrongArgumentsRule,
            AutoDisposeNotCalledRule,
            AutoDisposeCalledMoreThanOnceRule,
            LifetimeDisposableDirectDisposeRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

            if (context.ContainingSymbol is ITypeSymbol classSymbol 
                && (HasAttribute(classSymbol, GeneratedViewModelAttributeTemplate.MetaDataName)
                    || HasAttribute(classSymbol, GeneratedModelAttributeTemplate.MetaDataName)))
            {
                if (!TryGetHandwrittenPartOfClass(classSymbol, out var classNode))
                {
                    return;
                }

                var sw = Stopwatch.StartNew();
                
                ReportNotCallingHandleAutoBindingsInConstructor(context, classNode, classDeclarationSyntax);
                ReportNotCallingHandleAutoDisposeInDispose(context, classNode);
                ReportDirectlyCallingDisposeOnLifetimeDisposable(context, classNode);
                
                Console.Out.WriteLine($"ViewModel analyzer execution time: {sw.ElapsedMilliseconds}ms");
            }
        }

        private static void ReportNotCallingHandleAutoBindingsInConstructor(SyntaxNodeAnalysisContext context,
            SyntaxNode classNode, ClassDeclarationSyntax classDeclarationSyntax)
        {
            var constructors = classNode
                .ChildNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .ToImmutableArray();

            if (constructors.Length == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(AutoBindingsRule, classDeclarationSyntax.GetLocation()));

                return;
            }

            var constructorsChains = ConvertConstructorsIntoChains(constructors, context.SemanticModel);

            foreach (var chain in constructorsChains)
            {
                var firstConstructorInChain = chain.FirstOrDefault();

                var invocationsThroughWholeConstructorsChain =
                    chain
                        .SelectMany(c => c?.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
                        .SelectManyWhere(FlattenInvocationChains);
                
                var handleAutoBindingsCalls = invocationsThroughWholeConstructorsChain
                    .Where(i => i != null && FilterHandleAutoBindings(i))
                    .ToArray();
                
                if (handleAutoBindingsCalls.Length == 1)
                {
                    continue;
                }
        
                if (handleAutoBindingsCalls.Length == 0)
                {
                    var diagnostic = Diagnostic.Create(AutoBindingsRule, firstConstructorInChain?.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
                else
                {
                    foreach (var invocationSyntax in handleAutoBindingsCalls)
                    {
                        var diagnostic = Diagnostic.Create(AutoBindingsRule, invocationSyntax?.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }

            return;
            
            (bool, IEnumerable<InvocationExpressionSyntax?>) FlattenInvocationChains(InvocationExpressionSyntax? invocationExpressionSyntax)
            {
                if (invocationExpressionSyntax == null)
                {
                    return (false, Array.Empty<InvocationExpressionSyntax>());
                }
                
                if (!TryGetMethodNameIfItThisObjectsMethod(invocationExpressionSyntax, out var methodName) || string.IsNullOrEmpty(methodName))
                {
                    return (false, Array.Empty<InvocationExpressionSyntax>());
                }

                if (string.Equals(methodName, HandlingAutoBindingsMethodName))
                {
                    return (true, new [] { invocationExpressionSyntax });
                }

                SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax);

                if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                {
                    return (false, Array.Empty<InvocationExpressionSyntax>());
                }
                
                SyntaxNode? methodSyntax = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                var innerInvocations = methodSyntax?
                    .DescendantNodesAndSelf()
                    .OfType<InvocationExpressionSyntax>();

                if (innerInvocations == null)
                {
                    return (false, Array.Empty<InvocationExpressionSyntax>());
                }

                return (true, innerInvocations);
            }
            
            bool FilterHandleAutoBindings(InvocationExpressionSyntax invocationExpressionSyntax)
            {
                if (!TryGetMethodNameIfItThisObjectsMethod(invocationExpressionSyntax, out var methodName))
                {
                    return false;
                }

                if (string.IsNullOrEmpty(methodName) || !string.Equals(methodName, HandlingAutoBindingsMethodName))
                {
                    return false;
                }

                var symbolInfo = context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax);
                var mainSymbol = symbolInfo.Symbol;
                
                if (mainSymbol is not null)
                {
                    return CheckSymbolIsMethodWithName(mainSymbol, HandlingAutoBindingsMethodName);
                }

                var candidates = symbolInfo.CandidateSymbols;
                if (candidates.Length != 1)
                {
                    return false;
                }

                var candidateSymbol = candidates.First();
                if (!CheckSymbolIsMethodWithName(candidateSymbol, HandlingAutoBindingsMethodName))
                {
                    return false;
                }

                // If there is one candidate with legit CandidateReason - good chances that it's compilation error
                // so we just don't handle this situation in our analyzer - so programmer can fix that compiler error.
                // In this case our analyzer report most likely would be misleading.
                if (symbolInfo.CandidateReason == CandidateReason.None)
                {
                    return false;
                }

                return true;
            }
        }

        private static IEnumerable<IEnumerable<ConstructorDeclarationSyntax>> ConvertConstructorsIntoChains(
            ImmutableArray<ConstructorDeclarationSyntax> constructors, SemanticModel semanticModel)
        {
            var constructorMapping = new Dictionary<ConstructorDeclarationSyntax, ConstructorDeclarationSyntax>();

            foreach (var constructor in constructors)
            {
                var initializer = constructor.Initializer;
                if (initializer is not { ThisOrBaseKeyword: { Text: "this" } })
                {
                    continue;
                }
    
                var calledConstructorSymbol = semanticModel.GetSymbolInfo(initializer).Symbol as IMethodSymbol;

                var calledConstructor = constructors.FirstOrDefault(c =>
                    SymbolEqualityComparer.Default.Equals(semanticModel.GetDeclaredSymbol(c), calledConstructorSymbol));

                if (calledConstructor != null)
                {
                    constructorMapping[constructor] = calledConstructor;
                }
            }
            
            var rootConstructors = constructors.Where(c => !constructorMapping.ContainsValue(c));
            
            var list = new List<ConstructorDeclarationSyntax>(5);
            var chains = rootConstructors.Select(c =>
            {
                list.Clear();

                var current = c;
                list.Add(current);

                while (constructorMapping.TryGetValue(current, out var nextConstructor))
                {
                    current = nextConstructor;
                    list.Add(current);
                }

                return list.ToArray();
            });
            
            return chains;
        }

        private static void ReportNotCallingHandleAutoDisposeInDispose(SyntaxNodeAnalysisContext context,
            SyntaxNode classNode)
        {
            var disposeMethodSyntax = classNode
                .ChildNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(
                    method =>
                    {
                        bool isDisposeImplementation = method.Modifiers.Any(SyntaxKind.PublicKeyword);
                        isDisposeImplementation &= method.ReturnType is PredefinedTypeSyntax predefinedType &&
                                                   predefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword);
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
                        if (!TryGetMethodNameIfItThisObjectsMethod(invocationExpressionSyntax, out var methodName))
                        {
                            return false;
                        }

                        if (string.IsNullOrEmpty(methodName) || !string.Equals(methodName, HandlingAutoDisposeMethodName))
                        {
                            return false;
                        }

                        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax);
                        var mainSymbol = symbolInfo.Symbol;
                        if (mainSymbol is not null)
                        {
                            return CheckSymbolIsMethodWithName(mainSymbol, HandlingAutoDisposeMethodName);
                        }

                        var candidates = symbolInfo.CandidateSymbols;
                        if (candidates.Length != 1)
                        {
                            return false;
                        }

                        var candidateSymbol = candidates.First();
                        if (!CheckSymbolIsMethodWithName(candidateSymbol, HandlingAutoDisposeMethodName))
                        {
                            return false;
                        }

                        // If there is one candidate with legit CandidateReason - good chances that it's compilation error
                        // so we just don't handle this situation in our analyzer - so programmer can fix that compiler error.
                        // In this case our analyzer report most likely would be misleading.
                        if (symbolInfo.CandidateReason == CandidateReason.None)
                        {
                            return false;
                        }

                        return true;
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
                    var diagnostic =
                        Diagnostic.Create(AutoDisposeCalledMoreThanOnceRule, invocationSyntax.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private void ReportDirectlyCallingDisposeOnLifetimeDisposable(SyntaxNodeAnalysisContext context,
            SyntaxNode classNode)
        {
            var disposeMethodSyntax = classNode
                .ChildNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(
                    method =>
                    {
                        bool isDisposeImplementation = method.Modifiers.Any(SyntaxKind.PublicKeyword);
                        isDisposeImplementation &= method.ReturnType is PredefinedTypeSyntax predefinedType &&
                                                   predefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword);
                        isDisposeImplementation &= method.ParameterList.Parameters.Count == 0;
                        isDisposeImplementation &= method.Identifier.Text == DisposeMethodName;

                        return isDisposeImplementation;
                    })
                .FirstOrDefault();

            if (disposeMethodSyntax == null)
            {
                return;
            }

            if (IsDisposeCalledOnLifetimeDisposable(disposeMethodSyntax, out var location))
            {
                var diagnostic = Diagnostic.Create(LifetimeDisposableDirectDisposeRule, location);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool TryGetHandwrittenPartOfClass(ITypeSymbol classSymbol, [MaybeNullWhen(false)] out SyntaxNode result)
        {
            var className = classSymbol.Name;
            foreach (var declarationReference in classSymbol.DeclaringSyntaxReferences)
            {
                var classDeclaration = declarationReference
                    .GetSyntax()
                    .DescendantNodesAndSelf()
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault(classDeclaration => classDeclaration.Identifier.Value is string id && id == className);

                if (classDeclaration == null)
                {
                    continue;
                }

                if (classDeclaration.AttributeLists.Any(attributeList => attributeList.Attributes.Any(attributeSyntax =>
                        attributeSyntax.Name.ToFullString() == GeneratedViewModelAttributeTemplate.MetaDataName
                        || attributeSyntax.Name.ToFullString() == GeneratedModelAttributeTemplate.MetaDataName)))
                {
                    continue;
                }
                
                result = declarationReference.GetSyntax();
                return true;
            }

            result = default;
            return false;
        }

        private static bool HasAttribute(ISymbol classSymbol, string attributeMetaName)
        {
            return classSymbol.GetAttributes()
                .Any(ad => ad?.AttributeClass?.ToDisplayString() == attributeMetaName);
        }

        private static bool TryGetMethodNameIfItThisObjectsMethod(InvocationExpressionSyntax? invocation, [MaybeNullWhen(false)] out string methodName)
        {
            if (invocation?.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var target = memberAccess.Expression;

                if (target is ThisExpressionSyntax)
                {
                    methodName = memberAccess.Name.Identifier.Text;
                    return true;
                }
            }
            else if (invocation?.Expression is IdentifierNameSyntax identifierName)
            {
                methodName = identifierName.Identifier.Text;
                return true;
            }

            methodName = default;
            return false;
        }

        private static bool IsDisposeCalledOnLifetimeDisposable(MethodDeclarationSyntax methodSyntax, [MaybeNullWhen(false)] out Location location)
        {
            foreach (var invocation in methodSyntax.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is MemberAccessExpressionSyntax
                    {
                        Name: { Identifier: { Text: DisposeMethodName } }, Expression: IdentifierNameSyntax { Identifier: { Text: LifetimeDisposableName } }
                    } memberAccess)
                {
                    location = memberAccess.GetLocation();
                    return true;
                }
            }

            location = null;
            return false;
        }

        private static bool CheckSymbolIsMethodWithName(ISymbol symbol, string methodName)
        {
            if (symbol is not IMethodSymbol methodSymbol)
            {
                return false;
            }

            if (methodSymbol.Name != methodName)
            {
                return false;
            }

            return true;
        }
    }
}