using System;
using System.Collections.Generic;
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
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxContextReceiver is ViewClassSyntaxReceiver receiver))
            {
                return;
            }
            
            if (receiver.IsEligible)
            {
                foreach (var viewClass in receiver.ViewsClassesToProcess)
                {
                    ProcessView(in context, viewClass);
                }
            }
        }

        private void ProcessView(in GeneratorExecutionContext context, INamedTypeSymbol viewClass)
        {
            const string localizationProviderClassName = "LocalizationInterface.ILocalizationProvider";
            INamedTypeSymbol localizationProviderInterfaceSymbol = context.Compilation.GetTypeByMetadataName(localizationProviderClassName);
            
            if (localizationProviderInterfaceSymbol == null)
            {
                var diagnostic = Diagnostic.Create(GetDiagnostic($"{localizationProviderClassName} should exist."), null);
                context.ReportDiagnostic(diagnostic);
                
                return;
            }
            
            var typeSymbol = viewClass;
            
            INamedTypeSymbol attributeSymbol = context.Compilation.GetTypeByMetadataName(ViewModelGenerateAttributeTemplate.AttributeName);
            
            var attribute = typeSymbol.GetAttributes().Single(
                ad =>
                    attributeSymbol!.Equals(ad.AttributeClass, SymbolEqualityComparer.Default));
            
            var viewModelClassName = (string)attribute.ConstructorArguments[0].Value;
            var viewModelNamespaceName = (string)attribute.ConstructorArguments[1].Value;
            
            GenerateViewModel(in context, typeSymbol, viewModelClassName, viewModelNamespaceName);
            GenerateView(in context, typeSymbol, viewModelClassName);
                
            /*var diagnostic1 = Diagnostic.Create(GetDiagnostic($"Type that needed ViewModel: {attribute.NamedArguments.Length}"), null);
            context.ReportDiagnostic(diagnostic1);*/
        }

        private void GenerateViewModel(in GeneratorExecutionContext context, INamedTypeSymbol typeSymbol, string viewModelClassName, string viewModelNamespaceName)
        {
            var methodsToCall = GetMethodsToCall(typeSymbol);
            var localizationKeys = GetFieldsToLocalize(typeSymbol);
            var placeholderLocalizationKeys = GetFieldsToLocalizePlaceholders(typeSymbol);
            var methodForAutoSubscription = GetMethodsForAutoSubscription(typeSymbol);

            var classTemplate = new ViewModelClassTemplate(
                viewModelClassName,
                viewModelNamespaceName,
                methodsToCall,
                localizationKeys,
                placeholderLocalizationKeys,
                methodForAutoSubscription);
            var classFileName = $"{viewModelClassName}_g.cs";
                
            context.AddSource(classFileName, SourceText.From(classTemplate.TransformText(), Encoding.UTF8));
        }
        
        private void GenerateView(in GeneratorExecutionContext context, INamedTypeSymbol typeSymbol, string viewModelClassName)
        {
            var viewClassName = typeSymbol.Name;
            var viewNamespaceName = typeSymbol.ContainingNamespace.ToDisplayString();
            
            var methodsToCall = GetButtonMethodCallInfo(typeSymbol);
            var fieldsToLocalize = GetLocalizableFieldInfos(typeSymbol);
            var fieldsToLocalizePlaceholders = GetLocalizablePlaceholdersFieldInfos(typeSymbol);
            var methodForAutoSubscription = GetMethodsForAutoSubscription(typeSymbol);
            var observablesBindings = GetObservablesBindingsInfos(typeSymbol);
            
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

            var result = methodsWithAttribute
                .Select(method =>
                {
                    var attribute = method
                        .GetAttributes()
                        .Single(ad => ad.AttributeClass?.Name == SubscribeOnViewModelsObservableAttributeTemplate.AttributeName);

                    var observableName = attribute.ConstructorArguments[0].Value as string;
                    var shouldCreateObservableInViewModel = (bool)attribute.ConstructorArguments[1].Value!;
                    var methodName = method.Name;
                    var methodArgumentType = method.Parameters.Any() ? method.Parameters[0].Type.Name : "Unit";
                    
                    return new SubscribeOnObservableInfo(methodName, observableName, shouldCreateObservableInViewModel, methodArgumentType);
                })
                .ToArray();

            return result;
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
                .Select(field =>
                {
                    var attribute = field.GetAttributes().Single(ad => ad.AttributeClass?.Name == BindToObservableAttributeTemplate.AttributeName);
                    var observableName = attribute.ConstructorArguments[0].Value as string;
                    var bindingType = (BindingType)attribute.ConstructorArguments[1].Value;
                    
                    return new ObservableBindingInfo(field.Name, observableName, bindingType);
                })
                .ToArray();

            return result;
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
    }
}
