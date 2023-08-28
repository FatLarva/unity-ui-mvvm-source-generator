using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using ViewsSourceGenerator.Linq;

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
            context.AddSource(LocalizeWithKeyFromFieldAttributeTemplate.SourceFileName, new LocalizeWithKeyFromFieldAttributeTemplate().TransformText());
            context.AddSource(SubscribeOnViewModelsObservableAttributeTemplate.SourceFileName, new SubscribeOnViewModelsObservableAttributeTemplate().TransformText());
            context.AddSource(BindToObservableAttributeTemplate.SourceFileName, new BindToObservableAttributeTemplate().TransformText());
            context.AddSource(BindingTypeEnumTemplate.SourceFileName, new BindingTypeEnumTemplate().TransformText());
            context.AddSource(AutoCreationFlagEnumTemplate.SourceFileName, new AutoCreationFlagEnumTemplate().TransformText());
            context.AddSource(GeneratedViewModelAttributeTemplate.SourceFileName, new GeneratedViewModelAttributeTemplate().TransformText());
            context.AddSource(SubViewAttributeTemplate.SourceFileName, new SubViewAttributeTemplate().TransformText());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxContextReceiver is ViewClassSyntaxReceiver receiver))
            {
                return;
            }
            
            if (receiver.IsEligible)
            {
                var sw = Stopwatch.StartNew();

                const string localizationProviderClassName = "LocalizationInterface.ILocalizationProvider";
                INamedTypeSymbol localizationProviderInterfaceSymbol = context.Compilation.GetTypeByMetadataName(localizationProviderClassName);
            
                if (localizationProviderInterfaceSymbol == null)
                {
                    var diagnostic = Diagnostic.Create(GetDiagnostic($"{localizationProviderClassName} should exist."), null);
                    context.ReportDiagnostic(diagnostic);
                
                    return;
                }

                /*var views = receiver.ViewsClassesToProcess.ToArray();

                var taskOne = Task.Run(() =>
                {
                    for (int i = 0; i < views.Length / 2; i++)
                    {
                        ProcessView(in context, views[i]);
                    }
                });
                
                var taskTwo = Task.Run(() =>
                {
                    for (int i = views.Length / 2; i < views.Length; i++)
                    {
                        ProcessView(in context, views[i]);
                    }
                });
                
                Task.WaitAll(taskOne, taskTwo);
                */
                
                foreach (var viewClass in receiver.ViewsClassesToProcess)
                {
                    ProcessView(in context, viewClass);
                }
                
                Console.Out.WriteLine($"ViewModel source generation took {sw.ElapsedMilliseconds}ms");
                sw.Stop();
            }
        }

        private void ProcessView(in GeneratorExecutionContext context, INamedTypeSymbol viewTypeSymbol)
        {
            Console.Out.WriteLine($"{viewTypeSymbol.Name} processing on thread: {Thread.CurrentThread.ManagedThreadId}");
            
            var attribute = GetSingleAttributeData(ViewModelGenerateAttributeTemplate.AttributeName, viewTypeSymbol);
            
            if (!TryGetNamedArgumentValue(attribute.NamedArguments, "ViewModelClassName", out string viewModelClassName))
            {
                viewModelClassName = viewTypeSymbol.Name + "Model";
            }
            
            if (!TryGetNamedArgumentValue(attribute.NamedArguments, "ViewModelNamespaceName", out string viewModelNamespaceName))
            {
                viewModelNamespaceName = GetFullNamespace(viewTypeSymbol);
            }

            if (!TryGetNamedArgumentValue(attribute.NamedArguments, "SkipViewModelGeneration", out bool skipViewModelGeneration))
            {
                skipViewModelGeneration = false;
            }

            var commonInfo = GatherCommonInfo(in context, viewTypeSymbol, viewModelClassName, viewModelNamespaceName);
            
            if (!skipViewModelGeneration)
            {
                GenerateViewModel(in context, in commonInfo);
            }

            GenerateView(in context, in commonInfo);
                
            /*var diagnostic1 = Diagnostic.Create(GetDiagnostic($"Type that needed ViewModel: {attribute.NamedArguments.Length}"), null);
            context.ReportDiagnostic(diagnostic1);*/
        }

        private CommonInfo GatherCommonInfo(in GeneratorExecutionContext context, INamedTypeSymbol viewTypeSymbol, string viewModelClassName, string viewModelNamespaceName)
        {
            INamedTypeSymbol viewModelTypeSymbol = context.Compilation.GetTypeByMetadataName($"{viewModelNamespaceName}.{viewModelClassName}");
            ButtonMethodCallInfo[] methodsToCall = GetButtonMethodCallInfo(viewTypeSymbol, viewModelTypeSymbol);
            LocalizableFieldInfo[] localizationFieldInfos = GetLocalizableFieldInfos(viewTypeSymbol);
            LocalizableFieldInfo[] localizableByKeyFromFieldInfos = GetLocalizableByKeyFromFieldInfos(viewTypeSymbol);
            SubscribeOnObservableInfo[] methodForAutoSubscription = GetMethodsForAutoSubscription(viewTypeSymbol);
            ObservableBindingInfo[] observablesBindings = GetObservablesBindingsInfos(viewTypeSymbol);

            return new CommonInfo(
                viewModelClassName,
                viewModelNamespaceName,
                viewTypeSymbol,
                viewModelTypeSymbol,
                methodsToCall,
                localizationFieldInfos,
                localizableByKeyFromFieldInfos,
                methodForAutoSubscription,
                observablesBindings);
        }

        private void GenerateViewModel(in GeneratorExecutionContext context, in CommonInfo commonInfo)
        {
            bool shouldImplementDisposeInterface = !IsIDisposableImplementedInHandwrittenPart(commonInfo.ViewModelTypeSymbol);
            
            LocalizableFieldInfo[] localizationInfos = commonInfo.LocalizationFieldInfos
                .Concat(commonInfo.KeyFromFieldLocalizationFieldInfos)
                .ToArray();
            
            var classTemplate = new ViewModelClassTemplate(
                commonInfo.ViewModelClassName,
                commonInfo.ViewModelNamespaceName,
                commonInfo.MethodsToCall,
                localizationInfos,
                commonInfo.MethodForAutoSubscription,
                commonInfo.ObservablesBindings,
                shouldImplementDisposeInterface);
            
            var classFileName = $"{commonInfo.ViewModelClassName}_g.cs";
                
            context.AddSource(classFileName, SourceText.From(classTemplate.TransformText(), Encoding.UTF8));
        }

        private void GenerateView(in GeneratorExecutionContext context, in CommonInfo commonInfo)
        {
            var viewTypeSymbol = commonInfo.ViewTypeSymbol; 
            var viewClassName = viewTypeSymbol.Name;
            var viewNamespaceName = GetFullNamespace(viewTypeSymbol);

            LocalizableFieldInfo[] localizationInfos = commonInfo.LocalizationFieldInfos
                .Concat(commonInfo.KeyFromFieldLocalizationFieldInfos)
                .ToArray();
            SubViewInfo[] subViewInfos = GetSubViewInfos(viewTypeSymbol);
            
            var classTemplate = new ViewClassTemplate(
                viewClassName,
                commonInfo.ViewModelClassName,
                viewNamespaceName,
                commonInfo.MethodsToCall,
                localizationInfos,
                commonInfo.MethodForAutoSubscription,
                commonInfo.ObservablesBindings,
                subViewInfos);
            var classFileName = $"{viewClassName}_g.cs";
                
            context.AddSource(classFileName, SourceText.From(classTemplate.TransformText(), Encoding.UTF8));
            
            /*var diagnostic = Diagnostic.Create(GetDiagnostic($"So far so good: {viewClassName}  {viewNamespaceName}"), null);
            context.ReportDiagnostic(diagnostic);*/
        }

        private SubViewInfo[] GetSubViewInfos(INamedTypeSymbol typeSymbol)
        {
            var result = typeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .SelectWhere(field =>
                {
                    const string attrName = SubViewAttributeTemplate.AttributeName;
                    var attributeData = GetSingleAttributeData(attrName, field);

                    if (attributeData == null)
                    {
                        return (false, default);
                    }
                    
                    if (TryGetNamedArgumentValue(attributeData.NamedArguments, "UseSameViewModel", out bool useSameViewModel) && useSameViewModel)
                    {
                        return (true, new SubViewInfo(field.Name, true));
                    }
                                
                    if (!TryGetNamedArgumentValue(attributeData.NamedArguments, "SubViewModelFieldName", out string viewModelFieldName))
                    {
                        viewModelFieldName = field.Type.Name + "Model";
                    }
                                
                    return (true, new SubViewInfo(field.Name, viewModelFieldName));
                })
                .ToArray();

            return result;
        }

        private LocalizableFieldInfo[] GetLocalizableFieldInfos(INamedTypeSymbol typeSymbol)
        {
            return GetLocalizableFieldsInfosByAttributeName(typeSymbol, LocalizeWithKeyAttributeTemplate.AttributeName, false);
        }
        
        private LocalizableFieldInfo[] GetLocalizableByKeyFromFieldInfos(INamedTypeSymbol typeSymbol)
        {
            return GetLocalizableFieldsInfosByAttributeName(typeSymbol, LocalizeWithKeyFromFieldAttributeTemplate.AttributeName, true);
        }

        private static LocalizableFieldInfo[] GetLocalizableFieldsInfosByAttributeName(INamedTypeSymbol typeSymbol, string attributeName, bool isFromField)
        {
            LocalizableFieldInfo[] result = typeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .SelectWhere((fieldSymbol, attrName, isField) =>
                {
                    var attributeData = GetSingleAttributeData(attrName, fieldSymbol);

                    if (attributeData == null)
                    {
                        return (false, default);
                    }

                    var localizationKey = attributeData.ConstructorArguments[0].Value as string;
                    var fieldName = fieldSymbol.Name;
                    
                    if (!TryGetNamedArgumentValue(attributeData.NamedArguments, "IsLocalizePlaceholder", out bool isLocalizePlaceholder))
                    {
                        isLocalizePlaceholder = false;
                    }

                    var finalLocalizationKey = isField ? localizationKey + "Meddler" : localizationKey;
                    var localizationKeyProvideFieldName = isField ? localizationKey : string.Empty;
                    
                    return (true, new LocalizableFieldInfo(fieldName, finalLocalizationKey, isLocalizePlaceholder, localizationKeyProvideFieldName));
                }, attributeName, isFromField)
                .ToArray();

            return result;
        }

        private ButtonMethodCallInfo[] GetButtonMethodCallInfo(INamedTypeSymbol viewTypeSymbol, INamedTypeSymbol viewModelTypeSymbol)
        {
            var attributeName = ViewModelMethodCallAttributeTemplate.AttributeName;
            
            ButtonMethodCallInfo[] result = viewTypeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .SelectWhere((fieldSymbol, attrName, viewModel) =>
                {
                    var attributeData = GetSingleAttributeData(attrName, fieldSymbol);

                    if (attributeData == null)
                    {
                        return (false, default);
                    }

                    var methodToCallName = attributeData.ConstructorArguments[0].Value as string;
                    var fieldName = fieldSymbol.Name;
                    var handwrittenMethod = viewModel
                        .GetMembers()
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault(method => method.Name == methodToCallName
                                                  && method.ReturnsVoid
                                                  && method.DeclaredAccessibility == Accessibility.Public
                                                  && !IsPartialMethod(method));
                    var shouldGenerateMethodWithPartialStuff = handwrittenMethod == null;

                    return (true, new ButtonMethodCallInfo(fieldName, methodToCallName, shouldGenerateMethodWithPartialStuff));
                }, attributeName, viewModelTypeSymbol)
                .ToArray();
            
            return result;
        }

        private static AttributeData GetSingleAttributeData(string attributeName, ISymbol fieldSymbol)
        {
            bool IsValidAttributeName(AttributeData ad) => ad.AttributeClass?.Name == attributeName;

            AttributeData attributeData = fieldSymbol
                .GetAttributes()
                .SingleOrDefault(IsValidAttributeName);
            
            return attributeData;
        }
        
        private static AttributeData[] GetMultipleAttributeData(string attributeName, ISymbol fieldSymbol)
        {
            bool IsValidAttributeName(AttributeData ad) => ad.AttributeClass?.Name == attributeName;

            AttributeData[] attributeData = fieldSymbol
                .GetAttributes()
                .Where(IsValidAttributeName)
                .ToArray();
            
            return attributeData;
        }

        private SubscribeOnObservableInfo[] GetMethodsForAutoSubscription(INamedTypeSymbol typeSymbol)
        {
            try
            {
                var attributeName = SubscribeOnViewModelsObservableAttributeTemplate.AttributeName;
                
                var result = typeSymbol
                    .GetMembers()
                    .OfType<IMethodSymbol>()
                    .SelectWhere((methodSymbol, attrName) =>
                    {
                        var attributeData = GetSingleAttributeData(attrName, methodSymbol);

                        if (attributeData == null)
                        {
                            return (false, default);
                        }
                        
                        var observableName = attributeData.ConstructorArguments[0].Value as string;

                        if (!TryGetNamedArgumentValue(attributeData.NamedArguments, AutoCreationFlagEnumTemplate.EnumName, out InnerAutoCreationFlag creationFlags))
                        {
                            creationFlags = InnerAutoCreationFlag.None;
                        }

                        var methodName = methodSymbol.Name;
                        var methodArgumentType = methodSymbol.Parameters.Any() ? methodSymbol.Parameters[0].Type.Name : "Unit";

                        var autoCreationInfo = new AutoCreationInfo(observableName, creationFlags, methodArgumentType);
                        
                        return (true, new SubscribeOnObservableInfo(methodName, autoCreationInfo));
                    }, attributeName)
                    .ToArray();

                return result;
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e);
            }
            
            return Array.Empty<SubscribeOnObservableInfo>();
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
        
        private static bool TryGetNamedArgumentValue<T>(ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments, string argumentName, out T argumentValue)
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
            var result = typeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .SelectManyWhere(field =>
                {
                    const string attrName = BindToObservableAttributeTemplate.AttributeName;

                    var bindToAttributes = GetMultipleAttributeData(attrName, field);
                    if (bindToAttributes is { Length: 0 })
                    {
                        return (false, default);
                    }
                    
                    var infos = bindToAttributes
                        .Select(
                            attribute =>
                            {
                                TryGetDelayFromNamedArgument(attribute.NamedArguments, out ObservableBindingDelaySettings? delaySettings);
                                
                                var observableName = attribute.ConstructorArguments[0].Value as string;
                                var bindingType = (InnerBindingType)attribute.ConstructorArguments[1].Value;
                                var isNegated = false;
                                
                                if (observableName.StartsWith("!"))
                                {
                                    observableName = observableName.Substring(1);
                                    isNegated = true;
                                }
                                
                                if (!TryGetNamedArgumentValue(attribute.NamedArguments, AutoCreationFlagEnumTemplate.EnumName, out InnerAutoCreationFlag creationFlags))
                                {
                                    creationFlags = InnerAutoCreationFlag.None;
                                }

                                var methodArgumentType = GetObservableTypeFromBindingType(bindingType);
                                var autoCreationInfo = new AutoCreationInfo(observableName, creationFlags, methodArgumentType);
                                
                                return new ObservableBindingInfo(field.Name, bindingType, isNegated, delaySettings, autoCreationInfo);
                            });
                    
                    return (true, infos);
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
        
        private static bool IsIDisposableImplementedInHandwrittenPart(INamedTypeSymbol viewModelClass)
        {
            var disposeMethod = viewModelClass?
                .GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(method => method.Name == "Dispose" && method.ReturnsVoid && method.DeclaredAccessibility == Accessibility.Public);

            return disposeMethod != null;
        }
        
        private static string GetFullNamespace(INamedTypeSymbol typeSymbol)
        {
            INamespaceSymbol namespaceSymbol = typeSymbol.ContainingNamespace;
            if (namespaceSymbol.IsGlobalNamespace)
            {
                return string.Empty;
            }

            string result = namespaceSymbol.Name;
            while (!namespaceSymbol.ContainingNamespace.IsGlobalNamespace)
            {
                namespaceSymbol = namespaceSymbol.ContainingNamespace;
                result = namespaceSymbol.Name + "." + result;
            }

            return result;
        }
        
        private static bool IsPartialMethod(IMethodSymbol methodSymbol)
        {
            var syntaxReference = methodSymbol?.DeclaringSyntaxReferences.FirstOrDefault();

            if (syntaxReference?.GetSyntax() is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                return methodDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);
            }

            return false;
        }
    }
}
