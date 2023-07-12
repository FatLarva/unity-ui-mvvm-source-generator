using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ViewsSourceGenerator
{
    [Generator]
    internal class ViewsBindingsGenerator : ISourceGenerator
    {
        private const string DiagnosticId = "ViewsBindingsGenerator";
        private const string Category = "InitializationSafety";
        private const string HelpLinkUri = "";

        private static DiagnosticDescriptor GetDiagnostic(string message)
        {
            return new DiagnosticDescriptor(
                DiagnosticId,
                message,
                message,
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: message,
                helpLinkUri: HelpLinkUri);
        }
        
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization(OnPostInitialization);
            context.RegisterForSyntaxNotifications(() => new ViewClassSyntaxReceiver());
        }

        private void OnPostInitialization(GeneratorPostInitializationContext context)
        {
            context.AddSource(ViewModelMethodCallAttributeTemplate.SourceFileName, new ViewModelMethodCallAttributeTemplate().TransformText());
            context.AddSource(ViewModelGenerateAttributeTemplate.SourceFileName, new ViewModelGenerateAttributeTemplate().TransformText());
            context.AddSource(LocalizeWithKeyAttributeTemplate.SourceFileName, new LocalizeWithKeyAttributeTemplate().TransformText());
            context.AddSource(LocalizePlaceholderWithKeyAttributeTemplate.SourceFileName, new LocalizePlaceholderWithKeyAttributeTemplate().TransformText());
            context.AddSource(SubscribeOnViewModelsObservableAttributeTemplate.SourceFileName, new SubscribeOnViewModelsObservableAttributeTemplate().TransformText());
            context.AddSource(BindToObservableAttributeTemplate.SourceFileName, new BindToObservableAttributeTemplate().TransformText());
            context.AddSource(BindingTypeEnumTemplate.SourceFileName, new BindingTypeEnumTemplate().TransformText());
            context.AddSource(AutoCreationFlagEnumTemplate.SourceFileName, new AutoCreationFlagEnumTemplate().TransformText());
            context.AddSource(GeneratedViewModelAttributeTemplate.SourceFileName, new GeneratedViewModelAttributeTemplate().TransformText());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxContextReceiver is ViewClassSyntaxReceiver receiver))
            {
                return;
            }
            
            if (receiver.IsEligible)
            {
                const string localizationProviderClassName = "LocalizationInterface.ILocalizationProvider";
                INamedTypeSymbol localizationProviderInterfaceSymbol = context.Compilation.GetTypeByMetadataName(localizationProviderClassName);
            
                if (localizationProviderInterfaceSymbol == null)
                {
                    var diagnostic = Diagnostic.Create(GetDiagnostic($"{localizationProviderClassName} should exist."), null);
                    context.ReportDiagnostic(diagnostic);
                
                    return;
                }
                
                foreach (var viewClass in receiver.ViewsClassesToProcess)
                {
                    ProcessView(in context, viewClass);
                }
            }
        }

        private void ProcessView(in GeneratorExecutionContext context, INamedTypeSymbol viewClass)
        {
            var typeSymbol = viewClass;
            
            INamedTypeSymbol attributeSymbol = context.Compilation.GetTypeByMetadataName(ViewModelGenerateAttributeTemplate.MetaDataName);
            if (attributeSymbol == null)
            {
                var diagnostic = Diagnostic.Create(GetDiagnostic($"Cannot find symbol by MetadataName: {ViewModelGenerateAttributeTemplate.MetaDataName}"), null);
                context.ReportDiagnostic(diagnostic);
            }
            
            var attribute = typeSymbol.GetAttributes().Single(
                ad =>
                    attributeSymbol!.Equals(ad.AttributeClass, SymbolEqualityComparer.Default));
            
            if (!TryGetNamedArgumentValue(attribute.NamedArguments, "ViewModelClassName", out string viewModelClassName))
            {
                viewModelClassName = typeSymbol.Name + "Model";
            }
            
            if (!TryGetNamedArgumentValue(attribute.NamedArguments, "ViewModelNamespaceName", out string viewModelNamespaceName))
            {
                viewModelNamespaceName = GetFullNamespace(typeSymbol);
            }

            if (!TryGetNamedArgumentValue(attribute.NamedArguments, "SkipViewModelGeneration", out bool skipViewModelGeneration))
            {
                skipViewModelGeneration = false;
            }

            if (!skipViewModelGeneration)
            {
                GenerateViewModel(in context, typeSymbol, viewModelClassName, viewModelNamespaceName);
            }

            GenerateView(in context, typeSymbol, viewModelClassName);
                
            /*var diagnostic1 = Diagnostic.Create(GetDiagnostic($"Type that needed ViewModel: {attribute.NamedArguments.Length}"), null);
            context.ReportDiagnostic(diagnostic1);*/
        }

        private void GenerateViewModel(in GeneratorExecutionContext context, INamedTypeSymbol viewTypeSymbol, string viewModelClassName, string viewModelNamespaceName)
        {
            string[] methodsToCall = GetMethodsToCall(viewTypeSymbol);
            string[] localizationKeys = GetFieldsToLocalize(viewTypeSymbol);
            string[] placeholderLocalizationKeys = GetFieldsToLocalizePlaceholders(viewTypeSymbol);
            SubscribeOnObservableInfo[] methodForAutoSubscription = GetMethodsForAutoSubscription(viewTypeSymbol);
            ObservableBindingInfo[] observablesBindings = GetObservablesBindingsInfos(viewTypeSymbol);

            INamedTypeSymbol viewModelClass = context.Compilation.GetTypeByMetadataName($"{viewModelNamespaceName}.{viewModelClassName}");
            
            bool shouldImplementDisposeInterface = !IsIDisposableImplementedInHandwrittenPart(viewModelClass);
            
            var classTemplate = new ViewModelClassTemplate(
                viewModelClassName,
                viewModelNamespaceName,
                methodsToCall,
                localizationKeys,
                placeholderLocalizationKeys,
                methodForAutoSubscription,
                observablesBindings,
                shouldImplementDisposeInterface);
            
            var classFileName = $"{viewModelClassName}_g.cs";
                
            context.AddSource(classFileName, SourceText.From(classTemplate.TransformText(), Encoding.UTF8));
        }

        private void GenerateView(in GeneratorExecutionContext context, INamedTypeSymbol viewTypeSymbol, string viewModelClassName)
        {
            var viewClassName = viewTypeSymbol.Name;
            var viewNamespaceName = GetFullNamespace(viewTypeSymbol);
            
            ButtonMethodCallInfo[] methodsToCall = GetButtonMethodCallInfo(viewTypeSymbol);
            LocalizableFieldInfo[] fieldsToLocalize = GetLocalizableFieldInfos(viewTypeSymbol);
            LocalizableFieldInfo[] fieldsToLocalizePlaceholders = GetLocalizablePlaceholdersFieldInfos(viewTypeSymbol);
            SubscribeOnObservableInfo[] methodForAutoSubscription = GetMethodsForAutoSubscription(viewTypeSymbol);
            ObservableBindingInfo[] observablesBindings = GetObservablesBindingsInfos(viewTypeSymbol);
            
            var classTemplate = new ViewClassTemplate(
                viewClassName,
                viewModelClassName,
                viewNamespaceName,
                methodsToCall,
                fieldsToLocalize,
                fieldsToLocalizePlaceholders,
                methodForAutoSubscription,
                observablesBindings);
            var classFileName = $"{viewClassName}_g.cs";
                
            context.AddSource(classFileName, SourceText.From(classTemplate.TransformText(), Encoding.UTF8));
            
            /*var diagnostic = Diagnostic.Create(GetDiagnostic($"So far so good: {viewClassName}  {viewNamespaceName}"), null);
            context.ReportDiagnostic(diagnostic);*/
        }

        private LocalizableFieldInfo[] GetLocalizableFieldInfos(INamedTypeSymbol typeSymbol)
        {
            List<IFieldSymbol> fieldsWithAttribute = typeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.GetAttributes().Any(ad => ad.AttributeClass?.Name == LocalizeWithKeyAttributeTemplate.AttributeName))
                .ToList();
            
            if (fieldsWithAttribute.Count == 0)
            {
                return Array.Empty<LocalizableFieldInfo>();
            }
            
            var result = fieldsWithAttribute
                .Select(field => (field.GetAttributes().Single(ad => ad.AttributeClass?.Name == LocalizeWithKeyAttributeTemplate.AttributeName).ConstructorArguments[0].Value as string, field))
                .Select(pair => new LocalizableFieldInfo(pair.field.Name, pair.Item1))
                .ToArray();

            return result;
        }
        
        private LocalizableFieldInfo[] GetLocalizablePlaceholdersFieldInfos(INamedTypeSymbol typeSymbol)
        {
            List<IFieldSymbol> fieldsWithAttribute = typeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.GetAttributes().Any(ad => ad.AttributeClass?.Name == LocalizePlaceholderWithKeyAttributeTemplate.AttributeName))
                .ToList();
            
            if (fieldsWithAttribute.Count == 0)
            {
                return Array.Empty<LocalizableFieldInfo>();
            }
            
            var result = fieldsWithAttribute
                .Select(field => (field.GetAttributes().Single(ad => ad.AttributeClass?.Name == LocalizePlaceholderWithKeyAttributeTemplate.AttributeName).ConstructorArguments[0].Value as string, field))
                .Select(pair => new LocalizableFieldInfo(pair.field.Name, pair.Item1))
                .ToArray();

            return result;
        }

        private ButtonMethodCallInfo[] GetButtonMethodCallInfo(INamedTypeSymbol typeSymbol)
        {
            List<IFieldSymbol> fieldsWithCallMethodAttribute = typeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.GetAttributes().Any(ad => ad.AttributeClass?.Name == ViewModelMethodCallAttributeTemplate.AttributeName))
                .ToList();

            if (fieldsWithCallMethodAttribute.Count == 0)
            {
                return Array.Empty<ButtonMethodCallInfo>();
            }

            var result = fieldsWithCallMethodAttribute
                .Select(field => (field.GetAttributes().Single(ad => ad.AttributeClass?.Name == ViewModelMethodCallAttributeTemplate.AttributeName).ConstructorArguments[0].Value as string, field))
                .Select(pair => new ButtonMethodCallInfo(pair.field.Name, pair.Item1))
                .ToArray();

            return result;
        }
        
        private SubscribeOnObservableInfo[] GetMethodsForAutoSubscription(INamedTypeSymbol typeSymbol)
        {
            List<IMethodSymbol> methodsWithAttribute = typeSymbol
                .GetMembers()
                .OfType<IMethodSymbol>()
                .Where(f => f.GetAttributes().Any(ad => ad.AttributeClass?.Name == SubscribeOnViewModelsObservableAttributeTemplate.AttributeName))
                .ToList();

            if (methodsWithAttribute.Count == 0)
            {
                return Array.Empty<SubscribeOnObservableInfo>();
            }
            
            try
            {
                var result = methodsWithAttribute
                    .Select(method =>
                    {
                        var attribute = method
                            .GetAttributes()
                            .Single(ad => ad.AttributeClass?.Name == SubscribeOnViewModelsObservableAttributeTemplate.AttributeName);

                        var observableName = attribute.ConstructorArguments[0].Value as string;

                        if (!TryGetFlagsFromNamedArgument(attribute.NamedArguments, out var creationFlags))
                        {
                            creationFlags = InnerAutoCreationFlag.None;
                        }

                        var methodName = method.Name;
                        var methodArgumentType = method.Parameters.Any() ? method.Parameters[0].Type.Name : "Unit";

                        var autoCreationInfo = new AutoCreationInfo(observableName, creationFlags, methodArgumentType);
                        
                        return new SubscribeOnObservableInfo(methodName, autoCreationInfo);
                    })
                    .ToArray();

                return result;
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e);
            }
            
            return Array.Empty<SubscribeOnObservableInfo>();
        }

        private bool TryGetFlagsFromNamedArgument(ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments, out InnerAutoCreationFlag flags)
        {
            foreach (var kvp in namedArguments)
            {
                if (string.Equals(kvp.Key, AutoCreationFlagEnumTemplate.EnumName, StringComparison.Ordinal))
                {
                    flags = (InnerAutoCreationFlag)kvp.Value.Value!;
                
                    return true;
                }
            }

            flags = default;

            return false;
        }
        
        private bool TryGetDelayFromNamedArgument(ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments, out ObservableBindingDelaySettings? delaySettings)
        {
            foreach (var kvp in namedArguments)
            {
                if (string.Equals(kvp.Key, "DelaySeconds", StringComparison.Ordinal))
                {
                    delaySettings = new ObservableBindingDelaySettings(false, (int)kvp.Value.Value!);
                
                    return true;
                }
                
                if (string.Equals(kvp.Key, "DelayFrames", StringComparison.Ordinal))
                {
                    delaySettings = new ObservableBindingDelaySettings(true, (int)kvp.Value.Value!);
                
                    return true;
                }
            }

            delaySettings = default;

            return false;
        }
        
        private bool TryGetNamedArgumentValue<T>(ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments, string argumentName, out T argumentValue)
        {
            foreach (var kvp in namedArguments)
            {
                if (string.Equals(kvp.Key, argumentName, StringComparison.Ordinal))
                {
                    argumentValue = (T)kvp.Value.Value!;
                
                    return true;
                }
            }

            argumentValue = default;

            return false;
        }

        private ObservableBindingInfo[] GetObservablesBindingsInfos(INamedTypeSymbol typeSymbol)
        {
            List<IFieldSymbol> fieldsWithAttribute = typeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.GetAttributes().Any(ad => ad.AttributeClass?.Name == BindToObservableAttributeTemplate.AttributeName))
                .ToList();
            
            if (fieldsWithAttribute.Count == 0)
            {
                return Array.Empty<ObservableBindingInfo>();
            }
            
            var result = fieldsWithAttribute
                .SelectMany(field =>
                {
                    var bindToAttributes = field.GetAttributes().Where(ad => ad.AttributeClass?.Name == BindToObservableAttributeTemplate.AttributeName);

                    return bindToAttributes
                        .Select(
                            attribute =>
                            {
                                TryGetDelayFromNamedArgument(attribute.NamedArguments, out var delaySettings);
                                
                                var observableName = attribute.ConstructorArguments[0].Value as string;
                                var bindingType = (InnerBindingType)attribute.ConstructorArguments[1].Value;
                                var isNegated = false;
                                
                                if (observableName.StartsWith("!"))
                                {
                                    observableName = observableName.Substring(1);
                                    isNegated = true;
                                }
                                
                                if (!TryGetFlagsFromNamedArgument(attribute.NamedArguments, out var creationFlags))
                                {
                                    creationFlags = InnerAutoCreationFlag.None;
                                }

                                var methodArgumentType = GetObservableTypeFromBindingType(bindingType);

                                var autoCreationInfo = new AutoCreationInfo(observableName, creationFlags, methodArgumentType);
                                
                                return new ObservableBindingInfo(field.Name, bindingType, isNegated, delaySettings, autoCreationInfo);
                            });
                })
                .ToArray();

            return result;
        }

        private string GetObservableTypeFromBindingType(InnerBindingType bindingType)
        {
            switch (bindingType)
            {
                case InnerBindingType.Text:
                    return "string";
                case InnerBindingType.ImageFill:
                    return "float";
                case InnerBindingType.GameObjectActivity:
                    return "bool";
                case InnerBindingType.Activity:
                    return "bool";
                case InnerBindingType.Color:
                    return "Color";
                case InnerBindingType.Sprite:
                    return "Sprite";
                case InnerBindingType.Enabled:
                    return "bool";
                case InnerBindingType.Interactable:
                    return "bool";
                case InnerBindingType.Alpha:
                    return "float";
                default:
                    throw new ArgumentOutOfRangeException(nameof(bindingType), bindingType, $"Undefined binding type: {bindingType}");
            }
        }

        private string[] GetFieldsToLocalize(INamedTypeSymbol typeSymbol)
        {
            List<IFieldSymbol> fieldsWithAttribute = typeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.GetAttributes().Any(ad => ad.AttributeClass?.Name == LocalizeWithKeyAttributeTemplate.AttributeName))
                .ToList();
            
            if (fieldsWithAttribute.Count == 0)
            {
                return Array.Empty<string>();
            }
            
            var result = fieldsWithAttribute
                .Select(field => field.GetAttributes().Single(ad => ad.AttributeClass?.Name == LocalizeWithKeyAttributeTemplate.AttributeName))
                .Select(ad => ad.ConstructorArguments[0].Value as string)
                .ToArray();

            return result;
        }
        
        private string[] GetFieldsToLocalizePlaceholders(INamedTypeSymbol typeSymbol)
        {
            List<IFieldSymbol> fieldsWithAttribute = typeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.GetAttributes().Any(ad => ad.AttributeClass?.Name == LocalizePlaceholderWithKeyAttributeTemplate.AttributeName))
                .ToList();
            
            if (fieldsWithAttribute.Count == 0)
            {
                return Array.Empty<string>();
            }
            
            var result = fieldsWithAttribute
                .Select(field => field.GetAttributes().Single(ad => ad.AttributeClass?.Name == LocalizePlaceholderWithKeyAttributeTemplate.AttributeName))
                .Select(ad => ad.ConstructorArguments[0].Value as string)
                .ToArray();

            return result;
        }

        private string[] GetMethodsToCall(INamedTypeSymbol typeSymbol)
        {
            List<IFieldSymbol> fieldsWithCallMethodAttribute = typeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.GetAttributes().Any(ad => ad.AttributeClass?.Name == ViewModelMethodCallAttributeTemplate.AttributeName))
                .ToList();

            if (fieldsWithCallMethodAttribute.Count == 0)
            {
                return Array.Empty<string>();
            }

            var result = fieldsWithCallMethodAttribute
                .Select(field => field.GetAttributes().Single(ad => ad.AttributeClass?.Name == ViewModelMethodCallAttributeTemplate.AttributeName))
                .Select(ad => ad.ConstructorArguments[0].Value as string)
                .ToArray();

            return result;
        }
        
        private static bool IsIDisposableImplementedInHandwrittenPart(INamedTypeSymbol viewModelClass)
        {
            var disposeMethod = viewModelClass?.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(method => method.Name == "Dispose" && method.ReturnsVoid && method.DeclaredAccessibility == Accessibility.Public);

            return disposeMethod != null;
        }
        
        private static string GetFullNamespace(INamedTypeSymbol typeSymbol)
        {
            INamespaceSymbol namespaceSymbol = typeSymbol.ContainingNamespace;
            if (namespaceSymbol.IsGlobalNamespace)
                return string.Empty;

            string result = namespaceSymbol.Name;
            while (!namespaceSymbol.ContainingNamespace.IsGlobalNamespace)
            {
                namespaceSymbol = namespaceSymbol.ContainingNamespace;
                result = namespaceSymbol.Name + "." + result;
            }

            return result;
        }
    }
}
