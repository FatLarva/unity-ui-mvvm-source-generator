using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using ViewsSourceGenerator.Extensions;
using ViewsSourceGenerator.Linq;
using ViewsSourceGenerator.Tools;

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

        private static void OnPostInitialization(GeneratorPostInitializationContext context)
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
            context.AddSource(CommonModelAttributeTemplate.SourceFileName, new CommonModelAttributeTemplate().TransformText());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not ViewClassSyntaxReceiver receiver)
            {
                return;
            }
            
            const string localizationProviderClassName = "LocalizationInterface.ILocalizationProvider";
            INamedTypeSymbol? localizationProviderInterfaceSymbol = context.Compilation.GetTypeByMetadataName(localizationProviderClassName);
            
            if (localizationProviderInterfaceSymbol == null)
            {
                var diagnostic = Diagnostic.Create(GetDiagnostic($"{localizationProviderClassName} should exist."), null);
                context.ReportDiagnostic(diagnostic);
                
                return;
            }
            
            if (receiver.IsEligibleForViewClassesProcessing)
            {
                var sw = Stopwatch.StartNew();

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
            
            if (receiver.IsEligibleForModelClassesProcessing)
            {
                var sw = Stopwatch.StartNew();

                foreach (var modelClass in receiver.ModelsClassesToProcess)
                {
                    ProcessModel(in context, modelClass);
                }
                
                Console.Out.WriteLine($"Models source generation took {sw.ElapsedMilliseconds}ms");
                sw.Stop();
            }
        }

        private void ProcessView(in GeneratorExecutionContext context, INamedTypeSymbol viewTypeSymbol)
        {
            var attribute = GetSingleAttributeData(ViewModelGenerateAttributeTemplate.AttributeName, viewTypeSymbol);
            
            if (!TryGetNamedArgumentValue(attribute?.NamedArguments, "ViewModelClassName", out string? viewModelClassName))
            {
                viewModelClassName = viewTypeSymbol.Name + "Model";
            }
            
            if (!TryGetNamedArgumentValue(attribute?.NamedArguments, "ViewModelNamespaceName", out string? viewModelNamespaceName))
            {
                viewModelNamespaceName = viewTypeSymbol.GetFullNamespace();
            }

            if (!TryGetNamedArgumentValue(attribute?.NamedArguments, "SkipViewModelGeneration", out bool skipViewModelGeneration))
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
        
        private void ProcessModel(in GeneratorExecutionContext context, INamedTypeSymbol modelTypeSymbol)
        {
            var attribute = GetSingleAttributeData(CommonModelAttributeTemplate.AttributeName, modelTypeSymbol);
            
            /*if (!TryGetNamedArgumentValue(attribute.NamedArguments, "ViewModelClassName", out string viewModelClassName))
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
            }*/

            GenerateModel(in context, modelTypeSymbol);
            
            /*var commonInfo = GatherCommonInfo(in context, viewTypeSymbol, viewModelClassName, viewModelNamespaceName);
            
            if (!skipViewModelGeneration)
            {
                GenerateModel(in context, in commonInfo);
            }*/

            /*var diagnostic1 = Diagnostic.Create(GetDiagnostic($"Type that needed ViewModel: {attribute.NamedArguments.Length}"), null);
            context.ReportDiagnostic(diagnostic1);*/
        }

        private void GenerateModel(in GeneratorExecutionContext context, INamedTypeSymbol modelTypeSymbol)
        {
            var className = modelTypeSymbol.Name;
            var namespaceName = modelTypeSymbol.GetFullNamespace();
            bool shouldImplementDisposeInterface = !IsIDisposableImplementedInHandwrittenPart(modelTypeSymbol);
            ModelObservableInfo[] modelObservableInfos = GetModelObservableInfos(context, modelTypeSymbol);
            
            var classTemplate = new ModelClassTemplate(
                className,
                namespaceName,
                modelObservableInfos,
                shouldImplementDisposeInterface);
            
            var classFileName = $"{className}_g.cs";
                
            context.AddSource(classFileName, SourceText.From(classTemplate.TransformText(), Encoding.UTF8));
        }

        private CommonInfo GatherCommonInfo(in GeneratorExecutionContext context, INamedTypeSymbol viewTypeSymbol, string viewModelClassName, string viewModelNamespaceName)
        {
            INamedTypeSymbol? viewModelTypeSymbol = context.Compilation.GetTypeByMetadataName($"{viewModelNamespaceName}.{viewModelClassName}");
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
            var viewNamespaceName = viewTypeSymbol.GetFullNamespace();

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
                                
                    if (!TryGetNamedArgumentValue(attributeData.NamedArguments, "SubViewModelFieldName", out string? viewModelFieldName))
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

                    if (attributeData.ConstructorArguments[0].Value is not string { Length: > 0 } localizationKey)
                    {
                        return (false, default);
                    }
                    
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

        private ButtonMethodCallInfo[] GetButtonMethodCallInfo(INamedTypeSymbol viewTypeSymbol, INamedTypeSymbol? viewModelTypeSymbol)
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

                    if (attributeData.ConstructorArguments[0].Value is not string methodToCallName)
                    {
                        return (false, default);
                    }
                    
                    var fieldName = fieldSymbol.Name;
                    bool shouldGenerateMethodWithPartialStuff;
                    
                    AutoCreationInfo autoCreationInfo;
                    if (TryGetNamedArgumentValue(attributeData.NamedArguments, "PassForwardThroughCommandName", out string? passThroughCommandName))
                    {
                        autoCreationInfo = new AutoCreationInfo(passThroughCommandName, InnerAutoCreationFlag.WrappedCommand);
                        shouldGenerateMethodWithPartialStuff = false;
                    }
                    else
                    {
                        autoCreationInfo = AutoCreationInfo.Empty;
                        
                        var handwrittenMethod = viewModel?
                            .GetMembers()
                            .OfType<IMethodSymbol>()
                            .FirstOrDefault(method => method.Name == methodToCallName
                                                      && method is { ReturnsVoid: true, DeclaredAccessibility: Accessibility.Public }
                                                      && !IsPartialMethod(method));
                        shouldGenerateMethodWithPartialStuff = handwrittenMethod == null;
                    }

                    return (true, new ButtonMethodCallInfo(fieldName, methodToCallName, shouldGenerateMethodWithPartialStuff, autoCreationInfo));
                }, attributeName, viewModelTypeSymbol)
                .ToArray();
            
            return result;
        }

        private static AttributeData? GetSingleAttributeData(string attributeName, ISymbol fieldSymbol)
        {
            bool IsValidAttributeName(AttributeData ad) => ad.AttributeClass?.Name == attributeName;

            AttributeData? attributeData = fieldSymbol
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

                        if (attributeData.ConstructorArguments[0].Value is not string observableName)
                        {
                            return (false, default);

                        }

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
        
        private ModelObservableInfo[] GetModelObservableInfos(GeneratorExecutionContext context, INamedTypeSymbol typeSymbol)
        {
            try
            {
                var result = typeSymbol
                    .GetMembers()
                    .OfType<IPropertySymbol>()
                    .SelectWhere((propertySymbol, ctx) =>
                    {
                        if (propertySymbol.DeclaredAccessibility is not (Accessibility.Internal or Accessibility.Public))
                        {
                            return (false, default);
                        }

                        if (propertySymbol.Type is not INamedTypeSymbol { IsGenericType: true, Arity: 1 } propertyType)
                        {
                            return (false, default);
                        }
                        
                        INamedTypeSymbol? observableGenericType = propertyType.TypeArguments[0] as INamedTypeSymbol;
                        if (!CheckPropertyTypeValidity(propertyType, observableGenericType, ctx.Compilation))
                        {
                            return (false, default);
                        }

                        var creationFlags = propertySymbol.Name.EndsWith("Cmd") ? InnerAutoCreationFlag.PrivateCommand : InnerAutoCreationFlag.PrivateReactiveProperty;
                        
                        var observableName = "_" + propertySymbol.Name.Decapitalize();
                        var methodArgumentType = propertyType.Name;

                        var autoCreationInfo = new AutoCreationInfo(observableName, creationFlags, methodArgumentType);
                        
                        return (true, new ModelObservableInfo(autoCreationInfo));
                    }, context)
                    .ToArray();

                return result;
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e);
            }
            
            return Array.Empty<ModelObservableInfo>();
        }

        private static bool CheckPropertyTypeValidity(INamedTypeSymbol propertyType, INamedTypeSymbol? observableGenericType, Compilation compilation)
        {
            if (observableGenericType == null)
            {
                return false;
            }
            var comparer = SymbolEqualityComparer.Default;

            var isValid = comparer.Equals(propertyType, compilation.GetTypeByMetadataName($"UniRx.IObservable<{observableGenericType}>"));
            isValid |= comparer.Equals(propertyType, compilation.GetTypeByMetadataName($"UniRx.IReadOnlyReactiveProperty<{observableGenericType}>"));

            return isValid;
        }

        private static bool TryGetDelayFromNamedArgument(ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments, out ObservableBindingDelaySettings? delaySettings)
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
        
        private static bool TryGetNamedArgumentValue<T>(ImmutableArray<KeyValuePair<string, TypedConstant>>? namedArguments, string argumentName, [NotNullWhen(true)] out T? argumentValue)
        {
            if (namedArguments.HasValue)
            {
                foreach (var kvp in namedArguments)
                {
                    if (string.Equals(kvp.Key, argumentName, StringComparison.Ordinal))
                    {
                        argumentValue = (T)kvp.Value.Value!;
                    
                        return true;
                    }
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
                .SelectManyWhere(fieldSymbol =>
                {
                    const string attrName = BindToObservableAttributeTemplate.AttributeName;

                    var bindToAttributes = GetMultipleAttributeData(attrName, fieldSymbol);
                    if (bindToAttributes is { Length: 0 })
                    {
                        return (false, Array.Empty<ObservableBindingInfo>());
                    }
                    
                    var infos = bindToAttributes
                        .SelectWhere(
                            (attribute, field) =>
                            {
                                if (!TryGetDelayFromNamedArgument(attribute.NamedArguments, out ObservableBindingDelaySettings? delaySettings))
                                {
                                    delaySettings = null;
                                }
                                
                                if (attribute.ConstructorArguments[0].Value is not string { Length: > 0 } observableName)
                                {
                                    return (false, default);
                                }
                                
                                if (attribute.ConstructorArguments[1].Value is not InnerBindingType bindingType)
                                {
                                    return (false, default);
                                }
                                
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

                                AutoCreationInfo autoCreationInfo;
                                if (creationFlags != InnerAutoCreationFlag.None)
                                {
                                    var methodArgumentType = GetObservableTypeFromBindingType(bindingType);
                                    autoCreationInfo = new AutoCreationInfo(observableName, creationFlags, methodArgumentType);
                                }
                                else
                                {
                                    autoCreationInfo = AutoCreationInfo.OnlyObservable(observableName);
                                }
                                
                                return (true, new ObservableBindingInfo(field.Name, bindingType, isNegated, delaySettings, autoCreationInfo));
                            }, fieldSymbol);
                    
                    return (true, infos);
                })
                .ToArray();

            return result;
        }

        private static string GetObservableTypeFromBindingType(InnerBindingType bindingType)
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
        
        private static bool IsIDisposableImplementedInHandwrittenPart(INamedTypeSymbol? viewModelClass)
        {
            var disposeMethod = viewModelClass?
                .GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(method => method is { Name: "Dispose", ReturnsVoid: true, DeclaredAccessibility: Accessibility.Public });

            return disposeMethod != null;
        }
        
        private static bool IsPartialMethod(IMethodSymbol methodSymbol)
        {
            var syntaxReference = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();

            if (syntaxReference?.GetSyntax() is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                return methodDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);
            }

            return false;
        }
    }
}
