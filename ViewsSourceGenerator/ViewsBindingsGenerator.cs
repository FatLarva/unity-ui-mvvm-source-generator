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
using ViewsSourceGenerator.Comparers;
using ViewsSourceGenerator.Extensions;
using ViewsSourceGenerator.Linq;
using ViewsSourceGenerator.Tools;

namespace ViewsSourceGenerator
{
    [Generator]
    internal class ViewsBindingsGenerator : ISourceGenerator
    {
        private const string CanBeNullAttributeName = "CanBeNullAttribute";
        private const string OutputFile = BuildTimeConstants.OutputFile;
        
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

        private static readonly SymbolDisplayFormat DefaultSymbolDisplayFormat = new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);
        
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ViewClassSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not ViewClassSyntaxReceiver receiver)
            {
                return;
            }

            using var outputRedirector = new OutputRedirector(OutputFile);

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

                var count = receiver.ViewsClassesToProcess.Count;
                Console.Out.WriteLine($"There are {count} view classes to process.");
                
                try
                {
                    var allViewModelInfos = new Dictionary<string, ViewModelGenerationInfo>(count, StringComparer.Ordinal);
                    
                    foreach (var viewClass in receiver.ViewsClassesToProcess)
                    {
                        ProcessView(in context, viewClass, allViewModelInfos);
                    }
                    
                    foreach (var viewModelGenerationInfo in allViewModelInfos.Values)
                    {
                        GenerateViewModel(in context, viewModelGenerationInfo);
                    }
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(exception);
                }
            
                Console.Out.WriteLine($"ViewModel source generation took {sw.ElapsedMilliseconds}ms");
                sw.Stop();
            }
        
            if (receiver.IsEligibleForModelClassesProcessing)
            {
                var sw = Stopwatch.StartNew();

                try
                {
                    foreach (var modelClass in receiver.ModelsClassesToProcess)
                    {
                        ProcessModel(in context, modelClass);
                    }
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(exception);
                }
            
                Console.Out.WriteLine($"Models source generation took {sw.ElapsedMilliseconds}ms");
                sw.Stop();
            }
        }

        private void ProcessView(in GeneratorExecutionContext context, INamedTypeSymbol viewTypeSymbol, Dictionary<string, ViewModelGenerationInfo> allViewModelInfos)
        {
            if (!TryGetSingleAttributeData(ViewModelGenerateAttributeTemplate.AttributeName, viewTypeSymbol, out var attribute))
            {
                return;
            }
            
            if (!TryGetNamedArgumentValue(attribute.NamedArguments, ViewModelGenerateAttributeTemplate.ViewModelClassNameParamName, out string? viewModelClassName, default))
            {
                viewModelClassName = viewTypeSymbol.Name + "Model";
            }
            
            if (!TryGetNamedArgumentValue(attribute.NamedArguments, ViewModelGenerateAttributeTemplate.ViewModelNamespaceNameParamName, out string? viewModelNamespaceName, default))
            {
                viewModelNamespaceName = viewTypeSymbol.GetFullNamespace();
            }

            if (!TryGetNamedArgumentValue(attribute.NamedArguments, ViewModelGenerateAttributeTemplate.SkipViewModelGenerationParamName, out bool skipViewModelGeneration, default))
            {
                skipViewModelGeneration = false;
            }

            var commonInfo = GatherCommonInfo(in context, viewTypeSymbol, viewModelClassName, viewModelNamespaceName);
            
            if (!skipViewModelGeneration)
            {
                ObtainViewModelInfo(in commonInfo, allViewModelInfos);
            }

            GenerateView(in context, in commonInfo);
                
            /*var diagnostic1 = Diagnostic.Create(GetDiagnostic($"Type that needed ViewModel: {attribute.NamedArguments.Length}"), null);
            context.ReportDiagnostic(diagnostic1);*/
        }
        
        private void ProcessModel(in GeneratorExecutionContext context, INamedTypeSymbol modelTypeSymbol)
        {
            GenerateModel(in context, modelTypeSymbol);

            /*var diagnostic1 = Diagnostic.Create(GetDiagnostic($"Type that needed ViewModel: {attribute.NamedArguments.Length}"), null);
            context.ReportDiagnostic(diagnostic1);*/
        }

        private CommonInfo GatherCommonInfo(in GeneratorExecutionContext context, INamedTypeSymbol viewTypeSymbol, string viewModelClassName, string viewModelNamespaceName)
        {
            INamedTypeSymbol? viewModelTypeSymbol = context.Compilation.GetTypeByMetadataName($"{viewModelNamespaceName}.{viewModelClassName}");
            ViewModelButtonMethodCallInfo[] viewModelMethodsToCall = GetViewModelButtonMethodCallInfo(viewTypeSymbol, viewModelTypeSymbol);
            ViewButtonMethodCallInfo[] viewMethodsToCall = GetViewButtonMethodCallInfo(viewTypeSymbol);
            LocalizableFieldInfo[] localizationFieldInfos = GetLocalizableFieldInfos(viewTypeSymbol);
            LocalizableFieldInfo[] localizableByKeyFromFieldInfos = GetLocalizableByKeyFromFieldInfos(viewTypeSymbol);
            (string[] additionalUsings, SubscribeOnObservableInfo[] methodForAutoSubscription) = GetMethodsForAutoSubscription(viewTypeSymbol);
            ObservableBindingInfo[] observablesBindings = GetObservablesBindingsInfos(viewTypeSymbol);

            return new CommonInfo(
                viewModelClassName,
                viewModelNamespaceName,
                viewTypeSymbol,
                viewModelTypeSymbol,
                viewModelMethodsToCall,
                viewMethodsToCall,
                localizationFieldInfos,
                localizableByKeyFromFieldInfos,
                methodForAutoSubscription,
                observablesBindings,
                additionalUsings);
        }

        private void GenerateViewModel(in GeneratorExecutionContext context, in ViewModelGenerationInfo viewModelInfo)
        {
            var creationInfosFromObservables = viewModelInfo
                .ObservableBindingInfos
                .Select(o => o.AutoCreationInfo);
            
            var creationInfosFromSubscribes = viewModelInfo
                .SubscribeInfos
                .Select(o => o.AutoCreationInfo);

            var overallCreationInfos = creationInfosFromObservables
                .Concat(creationInfosFromSubscribes)
                .Distinct()
                .ToArray();
            
            var classTemplate = new ViewModelClassTemplate(
                viewModelInfo.ClassName,
                viewModelInfo.NamespaceName,
                viewModelInfo.ButtonMethodInfos,
                viewModelInfo.LocalizationInfos,
                overallCreationInfos,
                viewModelInfo.Usings,
                viewModelInfo.ShouldImplementDisposeInterface);
            
            var classFileName = $"{viewModelInfo.ClassName}_g.cs";
                
            context.AddSource(classFileName, SourceText.From(classTemplate.TransformText(), Encoding.UTF8));
        }
        
        private void ObtainViewModelInfo(in CommonInfo commonInfo, Dictionary<string, ViewModelGenerationInfo> allViewModelInfos)
        {
            bool shouldImplementDisposeInterface = !IsIDisposableImplementedInHandwrittenPart(commonInfo.ViewModelTypeSymbol);

            var viewModelType = commonInfo.ViewModelTypeSymbol;
            
            ViewModelLocalizationInfo[] localizationInfos = commonInfo.LocalizationFieldInfos
                .Concat(commonInfo.KeyFromFieldLocalizationFieldInfos)
                .Distinct(LocalizableFieldInfoComparerFromViewModelPoV.Default)
                .Select(localizationFieldInfo => ConvertToViewModelLocalization(localizationFieldInfo, viewModelType))
                .ToArray();

            var needLocalization = commonInfo.IsNeedLocalization;
            var requiredUsings = needLocalization
                                     ? new[] { "System", "UniRx", "UnityEngine", "ViewModelGeneration", "LocalizationInterface" }
                                     : new[] { "System", "UniRx", "UnityEngine", "ViewModelGeneration" };

            var usings = GetUsings(commonInfo.ViewModelTypeSymbol, requiredUsings, commonInfo.AdditionalUsings);
            var viewModelInfos = commonInfo.ViewModelMethodsToCall
                .SelectMany(o => o.ViewModelInfos)
                .ToArray();
            
            
            var info = new ViewModelGenerationInfo(
                commonInfo.ViewModelClassName,
                commonInfo.ViewModelNamespaceName,
                viewModelInfos,
                localizationInfos,
                commonInfo.MethodForAutoSubscription,
                commonInfo.ObservablesBindings,
                usings,
                shouldImplementDisposeInterface);

            if (allViewModelInfos.TryGetValue(info.Id, out var otherInfo))
            {
                var mergedInfo = MergeInfos(in info, in otherInfo);
                allViewModelInfos[info.Id] = mergedInfo;
            }
            else
            {
                allViewModelInfos[info.Id] = info;
            }
        }

        private ViewModelLocalizationInfo ConvertToViewModelLocalization(
            LocalizableFieldInfo localizationFieldInfo,
            INamedTypeSymbol? viewModelTypeSymbol)
        {
            if (string.IsNullOrEmpty(localizationFieldInfo.KeyProviderFieldName) || viewModelTypeSymbol == null)
            {
                return new ViewModelLocalizationInfo { LocalizationKey = localizationFieldInfo.LocalizationKey, IsProviderObservable = false };
            }
            
            ISymbol? memberSymbol = viewModelTypeSymbol
                .GetMembers(localizationFieldInfo.KeyProviderFieldName)
                .FirstOrDefault();

            var isMemberObservable = memberSymbol != null && IsMemberObservable(memberSymbol);
            var keyCanBeNull = TryGetSingleAttributeData(CanBeNullAttributeName, memberSymbol, out _);
            
            return new ViewModelLocalizationInfo
            {
                LocalizationKey = localizationFieldInfo.LocalizationKey,
                KeyProviderFieldName = localizationFieldInfo.KeyProviderFieldName,
                IsProviderObservable = isMemberObservable,
                CanBeNull = keyCanBeNull,
            };
        }

        private ViewModelGenerationInfo MergeInfos(in ViewModelGenerationInfo info, in ViewModelGenerationInfo otherInfo)
        {
            return new ViewModelGenerationInfo(
                info.ClassName,
                info.NamespaceName,
                info.ButtonMethodInfos.Concat(otherInfo.ButtonMethodInfos).Distinct(ButtonMethodCallInfoComparerFromViewModelPoV.Default).ToArray(),
                info.LocalizationInfos.Concat(otherInfo.LocalizationInfos).Distinct(ViewModelLocalizationInfoComparerFromViewModelPoV.Default).ToArray(),
                info.SubscribeInfos.Concat(otherInfo.SubscribeInfos).Distinct(SubscribeOnObservableInfoComparerFromViewModelPoV.Default).ToArray(),
                info.ObservableBindingInfos.Concat(otherInfo.ObservableBindingInfos).Distinct(ObservableBindingInfoComparerFromViewModelPoV.Default).ToArray(),
                info.Usings.Concat(otherInfo.Usings).Distinct(StringComparer.Ordinal).ToArray(),
                info.ShouldImplementDisposeInterface || otherInfo.ShouldImplementDisposeInterface);
        }
        
        private static bool IsMemberObservable(ISymbol memberSymbol)
        {
            var type = memberSymbol switch
            {
                IFieldSymbol fieldSymbol => fieldSymbol.Type,
                IPropertySymbol propertySymbol => propertySymbol.Type,
                _ => null,
            };

            if (type == null)
            {
                return false;
            }
                
            return IsObservable(type as INamedTypeSymbol) || type.AllInterfaces.Any(IsObservable);

            bool IsObservable(INamedTypeSymbol? typeToCheck)
            {
                return typeToCheck is { OriginalDefinition: { Name: "IObservable" } } and { TypeArguments: { Length: 1 } };
            }
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
            SubViewsCollectionInfo[] subViewsCollectionInfos = GetSubViewsCollectionInfos(viewTypeSymbol);
            ViewButtonMethodCallInfo[] viewButtonCallInfos = commonInfo.ViewModelMethodsToCall
                .Select(
                    item => new ViewButtonMethodCallInfo
                    {
                        ButtonFieldName = item.ButtonFieldName,
                        ShouldCheckForNull = item.ShouldCheckForNull,
                        CallInfos = item.ViewInfos.Select(
                            info => new ViewButtonMethodCallInfo.MethodCallInfo
                            {
                                MethodToCall = info.MethodToCall,
                                InactivePeriodMs = info.InactivePeriodMs,
                                LongClickDurationMs = info.LongClickDurationMs,
                                ButtonInteractionType = info.ButtonInteractionType,
                                IsViewModelMethod = true,
                                ShouldPassModel = false,
                            }).ToArray(),
                    })
                .Concat(commonInfo.ViewMethodsToCall)
                .ToArray();
            viewButtonCallInfos = MergeViewButtonMethodCallInfos(viewButtonCallInfos);

            var requiredUsings = new[] { "System", "UniRx", "UniRx.Triggers", "Tools", "UnityEngine", "ViewModelGeneration" };
            var usings = GetUsings(commonInfo.ViewModelTypeSymbol, requiredUsings, commonInfo.AdditionalUsings);
            
            var classTemplate = new ViewClassTemplate(
                viewClassName,
                commonInfo.ViewModelClassName,
                viewNamespaceName,
                viewButtonCallInfos,
                localizationInfos,
                commonInfo.MethodForAutoSubscription,
                commonInfo.ObservablesBindings,
                subViewInfos,
                subViewsCollectionInfos,
                usings);
            var classFileName = $"{viewClassName}_g.cs";
                
            context.AddSource(classFileName, SourceText.From(classTemplate.TransformText(), Encoding.UTF8));
            
            /*var diagnostic = Diagnostic.Create(GetDiagnostic($"So far so good: {viewClassName}  {viewNamespaceName}"), null);
            context.ReportDiagnostic(diagnostic);*/
        }

        private ViewButtonMethodCallInfo[] MergeViewButtonMethodCallInfos(ViewButtonMethodCallInfo[] viewButtonCallInfos)
        {
            var infosByButtonNames = new Dictionary<string, ViewButtonMethodCallInfo>(viewButtonCallInfos.Length, StringComparer.Ordinal);

            foreach (var viewButtonCallInfo in viewButtonCallInfos)
            {
                if (!infosByButtonNames.TryGetValue(viewButtonCallInfo.ButtonFieldName, out var storedInfo))
                {
                    infosByButtonNames.Add(viewButtonCallInfo.ButtonFieldName, viewButtonCallInfo);
                }
                else
                {
                    infosByButtonNames[storedInfo.ButtonFieldName] = new ViewButtonMethodCallInfo
                    {
                        ButtonFieldName = storedInfo.ButtonFieldName,
                        ShouldCheckForNull = storedInfo.ShouldCheckForNull,
                        CallInfos = storedInfo.CallInfos
                            .Concat(viewButtonCallInfo.CallInfos)
                            .Distinct(ViewCallInfoComparer.Default)
                            .ToArray(),
                    };
                }
            }

            return infosByButtonNames.Values.ToArray();
        }

        private void GenerateModel(in GeneratorExecutionContext context, INamedTypeSymbol modelTypeSymbol)
        {
            var className = modelTypeSymbol.Name;
            var namespaceName = modelTypeSymbol.GetFullNamespace();
            bool shouldImplementDisposeInterface = !IsIDisposableImplementedInHandwrittenPart(modelTypeSymbol);
            ModelObservableInfo[] modelObservableInfos = GetModelObservableInfos(context, modelTypeSymbol);
            var requiredUsings = new[] { "System", "UniRx" };
            var usings = GetUsings(modelTypeSymbol, requiredUsings, Array.Empty<string>());
            
            var classTemplate = new ModelClassTemplate(
                className,
                namespaceName,
                modelObservableInfos,
                usings,
                shouldImplementDisposeInterface);
            
            var classFileName = $"{className}_g.cs";
                
            context.AddSource(classFileName, SourceText.From(classTemplate.TransformText(), Encoding.UTF8));
        }

        private SubViewInfo[] GetSubViewInfos(INamedTypeSymbol typeSymbol)
        {
            var result = typeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .SelectWhere(field =>
                {
                    const string attrName = SubViewAttributeTemplate.AttributeName;

                    if (!TryGetSingleAttributeData(attrName, field, out var attributeData))
                    {
                        return (false, default);
                    }
                    
                    var shouldCheckForNull = TryGetSingleAttributeData(CanBeNullAttributeName, field, out _);
                    
                    if (TryGetNamedArgumentValue(attributeData.NamedArguments, SubViewAttributeTemplate.UseSameViewModelParameterName, out bool useSameViewModel, false) && useSameViewModel)
                    {
                        return (true, new SubViewInfo { ViewFieldName = field.Name, UseSameViewModel = true, CheckForNull = shouldCheckForNull });
                    }
                                
                    if (!TryGetNamedArgumentValue(attributeData.NamedArguments, SubViewAttributeTemplate.SubViewModelFieldNameParameterName, out string? viewModelFieldName, default))
                    {
                        viewModelFieldName = field.Type.Name + "Model";
                    }
                                
                    return (true, new SubViewInfo { ViewFieldName = field.Name, ViewModelFieldName = viewModelFieldName, CheckForNull = shouldCheckForNull });
                })
                .ToArray();

            return result;
        }
        
        private SubViewsCollectionInfo[] GetSubViewsCollectionInfos(INamedTypeSymbol typeSymbol)
        {
            var result = typeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .SelectWhere(field =>
                {
                    const string attrName = SubViewsCollectionAttributeTemplate.AttributeName;

                    if (!TryGetSingleAttributeData(attrName, field, out var attributeData))
                    {
                        return (false, default);
                    }

                    if (!TryGetConstructorArgument(attributeData, 0, out int bindingMethodObject))
                    {
                        return (false, default);
                    }

                    var shouldCheckForNull = TryGetSingleAttributeData(CanBeNullAttributeName, field, out _);
                    var bindingMethod = (SubViewsBindingMethod)bindingMethodObject;
                    string? viewModelFieldName = string.Empty;
                    if (bindingMethod != SubViewsBindingMethod.SameModel)
                    {
                        if (!TryGetNamedArgumentValue(attributeData.NamedArguments, SubViewsCollectionAttributeTemplate.SubViewModelFieldNameParameterName, out viewModelFieldName, default))
                        {
                            return (false, default);
                        }
                    }
                    
                    switch (bindingMethod)
                    {
                        case SubViewsBindingMethod.SameModel:
                            return (
                                       true,
                                       new SubViewsCollectionInfo
                                       {
                                           ViewFieldName = field.Name,
                                           CheckForNull = shouldCheckForNull,
                                           BindingMethod = SubViewsBindingMethod.SameModel,
                                       });
                        case SubViewsBindingMethod.Index:
                            return (
                                       true,
                                       new SubViewsCollectionInfo
                                       {
                                           ViewFieldName = field.Name,
                                           ViewModelFieldName = viewModelFieldName,
                                           CheckForNull = shouldCheckForNull,
                                           BindingMethod = SubViewsBindingMethod.Index,
                                       });
                        case SubViewsBindingMethod.FieldMatch:
                            if (!TryGetNamedArgumentValue(attributeData.NamedArguments, SubViewsCollectionAttributeTemplate.ViewBindingFieldNameParameterName, out string? viewMatchingFieldName, default))
                            {
                                return (false, default);
                            }
                            
                            if (!TryGetNamedArgumentValue(attributeData.NamedArguments, SubViewsCollectionAttributeTemplate.ViewModelBindingFieldNameParameterName, out string? viewModelMatchingFieldName, default))
                            {
                                return (false, default);
                            }
                            
                            return (
                                       true,
                                       new SubViewsCollectionInfo
                                       {
                                           ViewFieldName = field.Name,
                                           ViewModelFieldName = viewModelFieldName,
                                           CheckForNull = shouldCheckForNull,
                                           BindingMethod = SubViewsBindingMethod.FieldMatch,
                                           ViewBindingFieldName = viewMatchingFieldName,
                                           ViewModelBindingFieldName = viewModelMatchingFieldName,
                                       });
                        case SubViewsBindingMethod.WithMethod:
                            if (!TryGetNamedArgumentValue(attributeData.NamedArguments, SubViewsCollectionAttributeTemplate.MatchingMethodNameParameterName, out string? matchingMethodName, default))
                            {
                                return (false, default);
                            }
                            
                            return (
                                       true,
                                       new SubViewsCollectionInfo
                                       {
                                           ViewFieldName = field.Name,
                                           ViewModelFieldName = viewModelFieldName,
                                           CheckForNull = shouldCheckForNull,
                                           BindingMethod = SubViewsBindingMethod.WithMethod,
                                           MatchingMethodName = matchingMethodName,
                                       });
                    }

                    return (false, default);
                })
                .ToArray();

            return result;
        }

        private LocalizableFieldInfo[] GetLocalizableFieldInfos(INamedTypeSymbol typeSymbol)
        {
            return GetLocalizableFieldsInfosByAttributeName(typeSymbol, LocalizeWithKeyAttributeTemplate.AttributeName, LocalizeWithKeyAttributeTemplate.IsLocalizePlaceholderParamName, false);
        }
        
        private LocalizableFieldInfo[] GetLocalizableByKeyFromFieldInfos(INamedTypeSymbol typeSymbol)
        {
            return GetLocalizableFieldsInfosByAttributeName(typeSymbol, LocalizeWithKeyFromFieldAttributeTemplate.AttributeName, LocalizeWithKeyFromFieldAttributeTemplate.IsLocalizePlaceholderParamName, true);
        }

        private static LocalizableFieldInfo[] GetLocalizableFieldsInfosByAttributeName(INamedTypeSymbol typeSymbol, string attributeName, string parameterName, bool isFromField)
        {
            LocalizableFieldInfo[] result = typeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .SelectWhere((fieldSymbol, attrName, paramName, isField) =>
                {
                    if (!TryGetSingleAttributeData(attrName, fieldSymbol, out var attributeData))
                    {
                        return (false, default);
                    }

                    if (!TryGetConstructorArgument(attributeData, 0, out string? localizationKey))
                    {
                        return (false, default);
                    }
                    
                    var fieldName = fieldSymbol.Name;

                    TryGetNamedArgumentValue(attributeData.NamedArguments, paramName, out bool isLocalizePlaceholder, false);

                    var finalLocalizationKey = isField ? localizationKey + "Meddler" : localizationKey;
                    var localizationKeyProvideFieldName = isField ? localizationKey : string.Empty;
                    var shouldCheckForNull = TryGetSingleAttributeData(CanBeNullAttributeName, fieldSymbol, out _);

                    return (true, new LocalizableFieldInfo
                                {
                                    ViewFieldName = fieldName,
                                    IsLocalizePlaceholder = isLocalizePlaceholder,
                                    LocalizationKey = finalLocalizationKey,
                                    KeyProviderFieldName = localizationKeyProvideFieldName,
                                    CheckForNull = shouldCheckForNull,
                                });
                }, attributeName, parameterName, isFromField)
                .ToArray();

            return result;
        }
        
        private static string[] GetUsings(ITypeSymbol? modelTypeSymbol, IEnumerable<string> requiredUsings, IEnumerable<string> additionalUsings)
        {
            if (modelTypeSymbol == null)
            {
                return Array.Empty<string>();
            }
            
            var resultList = new List<string>(20);
            
            foreach (var declarationReference in modelTypeSymbol.DeclaringSyntaxReferences)
            {
                SyntaxTree syntaxTree = declarationReference.SyntaxTree;
                SyntaxNode root = syntaxTree.GetRoot();

                var middleResult = root
                    .DescendantNodes()
                    .OfType<UsingDirectiveSyntax>()
                    .Select(usingDirective => usingDirective.ToFullString().TrimEnd());
                
                resultList.AddRange(middleResult);
            }

            foreach (var requiredUsing in requiredUsings)
            {
                resultList.Add($"using {requiredUsing};");
            }
            
            foreach (var requiredUsing in additionalUsings)
            {
                resultList.Add($"using {requiredUsing};");
            }
            
            var result = resultList.Distinct(StringComparer.Ordinal).ToArray();

            return result;
        }

        private ViewModelButtonMethodCallInfo[] GetViewModelButtonMethodCallInfo(INamedTypeSymbol viewTypeSymbol, INamedTypeSymbol? viewModelTypeSymbol)
        {
            ViewModelButtonMethodCallInfo[] result = viewTypeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .SelectWhere((fieldSymbol, viewModel) =>
                {
                    var attributeName = ViewModelMethodCallAttributeTemplate.AttributeName;
                    var maybeAttributeDatas = GetMultipleAttributeData(attributeName, fieldSymbol);

                    if (maybeAttributeDatas is not { Length: > 0 } attributeDatas)
                    {
                        return (false, default);
                    }

                    var fieldName = fieldSymbol.Name;
                    var shouldCheckForNull = TryGetSingleAttributeData(CanBeNullAttributeName, fieldSymbol, out _);

                    var viewInfos = new ViewModelButtonMethodCallInfo.ViewInfo[attributeDatas.Length].AsSpan();
                    var viewModelInfos = new ViewModelButtonMethodCallInfo.ViewModelInfo[attributeDatas.Length].AsSpan();
                    var actualInfosCount = 0;
                    
                    foreach (var attributeData in attributeDatas)
                    {
                        if (!TryGetConstructorArgument(attributeData, 0, out string? methodToCallName))
                        {
                            continue;
                        }

                        TryGetNamedArgumentValue(
                            attributeData.NamedArguments,
                            ViewModelMethodCallAttributeTemplate.ClickCooldownMsParameterName,
                            out int clickCooldown,
                            0);

                        TryGetNamedArgumentValue(
                            attributeData.NamedArguments,
                            ViewModelMethodCallAttributeTemplate.InteractionTypeParameterName,
                            out ButtonClickType buttonClickType,
                            ButtonClickType.Click);
                        
                        TryGetNamedArgumentValue(
                                attributeData.NamedArguments,
                                ViewModelMethodCallAttributeTemplate.LongClickDurationMsParameterName,
                                out int longClickDurationMs,
                                1500);

                        bool shouldGenerateMethodWithPartialStuff;
                        AutoCreationInfo autoCreationInfo;
                        if (TryGetNamedArgumentValue(
                                attributeData.NamedArguments,
                                ViewModelMethodCallAttributeTemplate.PassForwardThroughCommandNameParameterName,
                                out string? passThroughCommandName,
                                default))
                        {
                            autoCreationInfo = new AutoCreationInfo(passThroughCommandName, AutoCreationFlag.WrappedCommand);
                            shouldGenerateMethodWithPartialStuff = false;
                        }
                        else
                        {
                            autoCreationInfo = AutoCreationInfo.Empty;

                            var handwrittenMethod = viewModel?
                                .GetMembers()
                                .OfType<IMethodSymbol>()
                                .FirstOrDefault(
                                    method => method.Name == methodToCallName
                                           && method is { ReturnsVoid: true, DeclaredAccessibility: Accessibility.Public }
                                           && !IsPartialMethod(method));
                            shouldGenerateMethodWithPartialStuff = handwrittenMethod == null;
                        }

                        viewInfos[actualInfosCount] = new ViewModelButtonMethodCallInfo.ViewInfo
                        {
                            MethodToCall = methodToCallName,
                            InactivePeriodMs = clickCooldown,
                            ButtonInteractionType = buttonClickType,
                            LongClickDurationMs = longClickDurationMs,
                        };

                        viewModelInfos[actualInfosCount] = new ViewModelButtonMethodCallInfo.ViewModelInfo
                        {
                            MethodToCall = methodToCallName,
                            ShouldGenerateMethodWithPartialStuff = shouldGenerateMethodWithPartialStuff,
                            AutoCreationInfo = autoCreationInfo,
                        };
                        
                        actualInfosCount++;
                    }

                    viewInfos = viewInfos.Slice(0, actualInfosCount);
                    viewModelInfos = viewModelInfos.Slice(0, actualInfosCount);
                    
                    return (true, new ViewModelButtonMethodCallInfo
                        {
                            ButtonFieldName = fieldName,
                            ShouldCheckForNull = shouldCheckForNull,
                            ViewInfos = viewInfos.ToArray(),
                            ViewModelInfos = viewModelInfos.ToArray(),
                        });
                }, viewModelTypeSymbol)
                .ToArray();
            
            return result;
        }
        
        private ViewButtonMethodCallInfo[] GetViewButtonMethodCallInfo(INamedTypeSymbol viewTypeSymbol)
        {
            ViewButtonMethodCallInfo[] result = viewTypeSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .SelectWhere(fieldSymbol =>
                {
                    var maybeAttributeDatas = GetMultipleAttributeData(ViewMethodCallAttributeTemplate.AttributeName, fieldSymbol);

                    if (maybeAttributeDatas is not { Length: > 0 } attributeDatas)
                    {
                        return (false, default);
                    }

                    var fieldName = fieldSymbol.Name;
                    var callInfos = new ViewButtonMethodCallInfo.MethodCallInfo[attributeDatas.Length];
                    var actualCallInfosCount = 0;
                    foreach (var attributeData in attributeDatas)
                    {
                        if (!TryGetConstructorArgument(attributeData, 0, out string? methodToCallName))
                        {
                            continue;
                        }

                        TryGetNamedArgumentValue(
                            attributeData.NamedArguments,
                            ViewMethodCallAttributeTemplate.ClickCooldownMsParameterName,
                            out int clickCooldown,
                            0);

                        TryGetNamedArgumentValue(
                            attributeData.NamedArguments,
                            ViewMethodCallAttributeTemplate.PassModelParameterName,
                            out bool passModel,
                            false);

                        TryGetNamedArgumentValue(
                            attributeData.NamedArguments,
                            ViewMethodCallAttributeTemplate.InteractionTypeParameterName,
                            out ButtonClickType buttonClickType,
                            ButtonClickType.Click);

                        TryGetNamedArgumentValue(
                            attributeData.NamedArguments,
                            ViewModelMethodCallAttributeTemplate.LongClickDurationMsParameterName,
                            out int longClickDurationMs,
                            1500);

                        callInfos[actualCallInfosCount++] = new ViewButtonMethodCallInfo.MethodCallInfo
                        {
                            MethodToCall = methodToCallName,
                            InactivePeriodMs = clickCooldown,
                            LongClickDurationMs = longClickDurationMs,
                            ShouldPassModel = passModel,
                            ButtonInteractionType = buttonClickType,
                            IsViewModelMethod = false,
                        };
                    }

                    callInfos = callInfos.AsSpan(0, actualCallInfosCount).ToArray();
                    var shouldCheckForNull = TryGetSingleAttributeData(CanBeNullAttributeName, fieldSymbol, out _);

                    return (true, new ViewButtonMethodCallInfo
                               {
                                   ButtonFieldName = fieldName,
                                   ShouldCheckForNull = shouldCheckForNull,
                                   CallInfos = callInfos,
                               });
                })
                .ToArray();
            
            return result;
        }

        private static bool TryGetSingleAttributeData(string attributeName, ISymbol? fieldSymbol, [NotNullWhen(true)] out AttributeData? attributeData)
        {
            bool IsValidAttributeName(AttributeData? ad) => string.Equals(ad?.AttributeClass?.Name, attributeName, StringComparison.Ordinal);

            try
            {
                attributeData = fieldSymbol?
                    .GetAttributes()
                    .SingleOrDefault(IsValidAttributeName);

                return attributeData != null;
            }
            catch (Exception)
            {
                // Suppress everything here.
            }

            attributeData = default;
            return false;
        }
        
        private static AttributeData[] GetMultipleAttributeData(string attributeName, ISymbol? fieldSymbol)
        {
            bool IsValidAttributeName(AttributeData? ad) => string.Equals(ad?.AttributeClass?.Name, attributeName, StringComparison.Ordinal);

            try
            {
                AttributeData[]? attributeData = fieldSymbol?
                    .GetAttributes()
                    .Where(IsValidAttributeName)
                    .ToArray();
                
                return attributeData ?? Array.Empty<AttributeData>();
            }
            catch (Exception)
            {
                return Array.Empty<AttributeData>();
            }
        }

        private (string[] additionalUsings, SubscribeOnObservableInfo[] infos) GetMethodsForAutoSubscription(INamedTypeSymbol typeSymbol)
        {
            try
            {
                var intermediateResult = typeSymbol
                    .GetMembers()
                    .OfType<IMethodSymbol>()
                    .SelectWhere(methodSymbol =>
                    {
                        var attributeName = SubscribeOnViewModelsObservableAttributeTemplate.AttributeName;
                        if (!TryGetSingleAttributeData(attributeName, methodSymbol, out var attributeData))
                        {
                            return (false, default);
                        }

                        if (!TryGetConstructorArgument(attributeData, 0, out string? observableName))
                        {
                            return (false, default);
                        }

                        TryGetNamedArgumentValue(
                            attributeData.NamedArguments,
                            SubscribeOnViewModelsObservableAttributeTemplate.AutoCreationFlagParameterName,
                            out AutoCreationFlag creationFlags,
                            AutoCreationFlag.None);

                        TryGetNamedArgumentValue(
                            attributeData.NamedArguments,
                            SubscribeOnViewModelsObservableAttributeTemplate.FilterParameterName,
                            out string? filter,
                            null);

                        var methodName = methodSymbol.Name;
                        var methodArgumentType = methodSymbol.Parameters.Any() ? methodSymbol.Parameters[0].Type : null;
                        var methodArgumentTypeName = TranslateType(methodArgumentType) ?? "Unit";

                        var autoCreationInfo = new AutoCreationInfo(observableName, creationFlags, methodArgumentTypeName);
                        
                        return (true, (new SubscribeOnObservableInfo(methodName, autoCreationInfo, filter), methodArgumentType));
                    }).ToArray();

                string[] additionalUsings = intermediateResult
                    .SelectWhere(tuple =>
                    {
                        var argType = tuple.methodArgumentType;
                        if (argType == null)
                        {
                            return (false, string.Empty);
                        }
                        
                        if (argType.SpecialType == SpecialType.System_Nullable_T || argType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                        {
                            argType = ((INamedTypeSymbol)argType).TypeArguments[0];
                        }
                        else if (argType is INamedTypeSymbol { Arity: > 0, NullableAnnotation: NullableAnnotation.Annotated } namedArgType)
                        {
                            argType = namedArgType.TypeArguments[0];
                        }
                        
                        if (argType.SpecialType != SpecialType.None)
                        {
                            return (false, string.Empty);
                        }

                        var typeNamespace = argType.GetFullNamespace();
                        if (string.IsNullOrEmpty(typeNamespace))
                        {
                            return (false, string.Empty);
                        }
                        
                        return (true, typeNamespace);
                    }).ToArray();
                
                var infos = intermediateResult.Select(tuple => tuple.Item1).ToArray();
                
                return (additionalUsings, infos);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
            
            return (Array.Empty<string>(), Array.Empty<SubscribeOnObservableInfo>());
        }
        
        private ModelObservableInfo[] GetModelObservableInfos(GeneratorExecutionContext context, INamedTypeSymbol typeSymbol)
        {
            try
            {
                var result = typeSymbol
                    .GetMembers()
                    .OfType<IPropertySymbol>()
                    .SelectWhere((propertySymbol, ctx, type) =>
                    {
                        if (propertySymbol.DeclaredAccessibility is not (Accessibility.Internal or Accessibility.Public))
                        {
                            return (false, default);
                        }

                        if (propertySymbol.Type is not INamedTypeSymbol { IsGenericType: true, Arity: 1 } propertyType)
                        {
                            return (false, default);
                        }

                        if (!IsExpressionBodiedProperty(propertySymbol))
                        {
                            return (false, default);
                        }

                        ITypeSymbol observableGenericType = propertyType.TypeArguments[0];
                        
                        if (!CheckPropertyTypeValidity(propertyType, observableGenericType, ctx.Compilation, out string genericTypeName))
                        {
                            return (false, default);
                        }

                        AutoCreationInfo autoCreationInfo;
                        
                        var isCommand = propertySymbol.Name.EndsWith("Cmd");
                        if (isCommand)
                        {
                            var observableName = propertySymbol.Name.Decapitalize().Remove("Cmd");
                            var methodArgumentType = genericTypeName;
                            
                            autoCreationInfo = new AutoCreationInfo(observableName, AutoCreationFlag.PrivateCommand, methodArgumentType);
                        }
                        else
                        {
                            var observableName = propertySymbol.Name.Decapitalize();
                            var methodArgumentType = genericTypeName;
                            
                            autoCreationInfo = new AutoCreationInfo(observableName, AutoCreationFlag.PrivateReactiveProperty, methodArgumentType);
                        }

                        var generatingFieldName = autoCreationInfo.GetPrivatePartFieldName();
                        var alreadyHasPrivateField = type.GetMembers().Any(member => string.Equals(member.Name, generatingFieldName, StringComparison.Ordinal));
                        
                        return (true, new ModelObservableInfo(autoCreationInfo, alreadyHasPrivateField));
                    }, context, typeSymbol)
                    .ToArray();

                return result;
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e);
            }
            
            return Array.Empty<ModelObservableInfo>();
        }

        private static bool IsExpressionBodiedProperty(IPropertySymbol propertySymbol)
        {
            var propertyDeclarationSyntax = propertySymbol
                .DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax())
                .OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault();
            
            return propertyDeclarationSyntax?.ExpressionBody != null;
        }

        private static bool CheckPropertyTypeValidity(INamedTypeSymbol propertyType, ITypeSymbol observableGenericType, Compilation compilation, out string name)
        {
            var comparer = SymbolEqualityComparer.Default;

            var observableType = compilation.GetTypeByMetadataName("System.IObservable`1")?.Construct(observableGenericType);
            var reactivePropertyType = compilation.GetTypeByMetadataName("UniRx.IReadOnlyReactiveProperty`1")?.Construct(observableGenericType);
        
            var isValid = comparer.Equals(propertyType, observableType);
            isValid |= comparer.Equals(propertyType, reactivePropertyType);

            name = observableGenericType.ToDisplayString(DefaultSymbolDisplayFormat);

            return isValid;
        }

        private static bool TryGetNamedArgumentValue<T>(
            ImmutableArray<KeyValuePair<string, TypedConstant>>? namedArguments,
            string argumentName,
            [NotNullWhen(true)] out T? argumentValue,
            T? defaultValue)
        {
            if (!namedArguments.HasValue || string.IsNullOrEmpty(argumentName))
            {
                argumentValue = defaultValue;
                return false;
            }
            
            foreach (var kvp in namedArguments)
            {
                if (!string.Equals(kvp.Key, argumentName, StringComparison.Ordinal))
                {
                    continue;
                }

                var typedConstant = kvp.Value;
                if (typedConstant.IsNull || typedConstant.Value == null)
                {
                    continue;
                }

                argumentValue = (T)typedConstant.Value;
                return true;
            }
            
            argumentValue = defaultValue;
            return false;
        }
        
        private static bool TryGetConstructorArgument<T>(
            AttributeData? attributeData,
            int argumentIndex,
            [NotNullWhen(true)] out T? argumentValue,
            T? defaultValue = default)
        {
            if (attributeData?.ConstructorArguments is not { Length: >= 0 } ctorArgs || ctorArgs.Length <= argumentIndex)
            {
                argumentValue = defaultValue;
                return false;
            }

            if (ctorArgs[argumentIndex].Value is not T argument)
            {
                argumentValue = defaultValue;
                return false;
            }

            argumentValue = argument;
            return true;
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
                    
                    var infos = bindToAttributes.SelectWhere(GatherBindingInfo, fieldSymbol);
                    
                    return (true, infos);
                })
                .ToArray();

            return result;

            (bool include, ObservableBindingInfo result) GatherBindingInfo(AttributeData attribute, IFieldSymbol field)
            {
                ObservableBindingDelaySettings? delaySettings = null;
                if (TryGetNamedArgumentValue(
                        attribute.NamedArguments,
                        BindToObservableAttributeTemplate.DelaySecondsParamName,
                        out int delaySeconds,
                        default))
                {
                    delaySettings = new ObservableBindingDelaySettings(false, delaySeconds);
                }
                else if (TryGetNamedArgumentValue(
                             attribute.NamedArguments,
                             BindToObservableAttributeTemplate.DelayFramesParamName,
                             out int delayFrames,
                             default))
                {
                    delaySettings = new ObservableBindingDelaySettings(true, delayFrames);
                }

                if (!TryGetConstructorArgument(attribute, 0, out string? observableName))
                {
                    return (false, default);
                }
                
                if (!TryGetConstructorArgument(attribute, 1, out int bindingTypeObject))
                {
                    return (false, default);
                }
                
                var bindingType = (BindingType)bindingTypeObject;
                            
                var isNegated = false;
                            
                if (observableName.StartsWith("!"))
                {
                    observableName = observableName.Substring(1);
                    isNegated = true;
                }

                TryGetNamedArgumentValue(
                    attribute.NamedArguments,
                    AutoCreationFlagEnumTemplate.EnumName,
                    out AutoCreationFlag creationFlags,
                    AutoCreationFlag.None);
                
                AutoCreationInfo autoCreationInfo;
                if (creationFlags != AutoCreationFlag.None)
                {
                    var methodArgumentType = GetObservableTypeFromBindingType(bindingType);
                    autoCreationInfo = new AutoCreationInfo(observableName, creationFlags, methodArgumentType);
                }
                else
                {
                    autoCreationInfo = AutoCreationInfo.OnlyObservable(observableName);
                }
                            
                var isCollection = CheckFieldIsCollection(field);
                var shouldCheckForNull = TryGetSingleAttributeData(CanBeNullAttributeName, field, out _);
                
                return (true, new ObservableBindingInfo(field.Name, bindingType, isNegated, isCollection, shouldCheckForNull, delaySettings, autoCreationInfo));
            }
        }

        private static bool CheckFieldIsCollection(IFieldSymbol fieldSymbol)
        {
            var fieldType = fieldSymbol.Type;

            return fieldType is { TypeKind: TypeKind.Array } or { OriginalDefinition: { SpecialType: SpecialType.System_Collections_Generic_IList_T } };
        }

        private static string GetObservableTypeFromBindingType(BindingType bindingType)
        {
            switch (bindingType)
            {
                case BindingType.Text:
                    return "string";
                case BindingType.ImageFill:
                    return "float";
                case BindingType.GameObjectActivity:
                    return "bool";
                case BindingType.Activity:
                    return "bool";
                case BindingType.Color:
                    return "Color";
                case BindingType.Sprite:
                    return "Sprite";
                case BindingType.Enabled:
                    return "bool";
                case BindingType.Interactable:
                    return "bool";
                case BindingType.Alpha:
                    return "float";
                case BindingType.EffectColor:
                    return "Color";
                default:
                    throw new ArgumentOutOfRangeException(nameof(bindingType), bindingType, $"Undefined binding type: {bindingType}");
            }
        }
        
        private static string? TranslateType(ITypeSymbol? typeSymbol)
        {
            if (typeSymbol == null)
            {
                return null;
            }
            
            return typeSymbol.SpecialType switch
            {
                SpecialType.System_Boolean => "bool",
                SpecialType.System_Char => "char",
                SpecialType.System_SByte => "sbyte",
                SpecialType.System_Byte => "byte",
                SpecialType.System_Int16 => "short",
                SpecialType.System_UInt16 => "ushort",
                SpecialType.System_Int32 => "int",
                SpecialType.System_UInt32 => "uint",
                SpecialType.System_Int64 => "long",
                SpecialType.System_UInt64 => "ulong",
                SpecialType.System_Single => "float",
                SpecialType.System_Double => "double",
                SpecialType.System_Decimal => "decimal",
                SpecialType.System_String => "string",
                _ => typeSymbol.ToDisplayString(DefaultSymbolDisplayFormat),
            };
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
