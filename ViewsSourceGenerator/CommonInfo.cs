using Microsoft.CodeAnalysis;

namespace ViewsSourceGenerator
{
    internal readonly struct CommonInfo
    {
        public readonly string ViewModelClassName;
        public readonly string ViewModelNamespaceName;
        public readonly INamedTypeSymbol ViewTypeSymbol;
        public readonly INamedTypeSymbol? ViewModelTypeSymbol;
        public readonly ViewModelButtonMethodCallInfo[] ViewModelMethodsToCall;
        public readonly ViewButtonMethodCallInfo[] ViewMethodsToCall;
        public readonly LocalizableFieldInfo[] LocalizationFieldInfos;
        public readonly LocalizableFieldInfo[] KeyFromFieldLocalizationFieldInfos;
        public readonly SubscribeOnObservableInfo[] MethodForAutoSubscription;
        public readonly ObservableBindingInfo[] ObservablesBindings;
        public readonly string[] AdditionalUsings;

        public bool IsNeedLocalization => LocalizationFieldInfos.Length > 0 || KeyFromFieldLocalizationFieldInfos.Length > 0;
        
        public CommonInfo(
            string viewModelClassName,
            string viewModelNamespaceName,
            INamedTypeSymbol viewTypeSymbol,
            INamedTypeSymbol? viewModelTypeSymbol,
            ViewModelButtonMethodCallInfo[] viewModelMethodsToCall,
            ViewButtonMethodCallInfo[] viewMethodsToCall,
            LocalizableFieldInfo[] localizationFieldInfos,
            LocalizableFieldInfo[] keyFromFieldLocalizationFieldInfos,
            SubscribeOnObservableInfo[] methodForAutoSubscription,
            ObservableBindingInfo[] observablesBindings,
            string[] additionalUsings)
        {
            ViewModelClassName = viewModelClassName;
            ViewModelNamespaceName = viewModelNamespaceName;
            ViewTypeSymbol = viewTypeSymbol;
            ViewModelTypeSymbol = viewModelTypeSymbol;
            ViewMethodsToCall = viewMethodsToCall;
            ViewModelMethodsToCall = viewModelMethodsToCall;
            LocalizationFieldInfos = localizationFieldInfos;
            KeyFromFieldLocalizationFieldInfos = keyFromFieldLocalizationFieldInfos;
            MethodForAutoSubscription = methodForAutoSubscription;
            ObservablesBindings = observablesBindings;
            AdditionalUsings = additionalUsings;
        }
    }
}