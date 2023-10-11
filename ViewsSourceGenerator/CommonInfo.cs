using Microsoft.CodeAnalysis;

namespace ViewsSourceGenerator
{
    internal readonly struct CommonInfo
    {
        public readonly string ViewModelClassName;
        public readonly string ViewModelNamespaceName;
        public readonly INamedTypeSymbol ViewTypeSymbol;
        public readonly INamedTypeSymbol? ViewModelTypeSymbol;
        public readonly ButtonMethodCallInfo[] MethodsToCall;
        public readonly LocalizableFieldInfo[] LocalizationFieldInfos;
        public readonly LocalizableFieldInfo[] KeyFromFieldLocalizationFieldInfos;
        public readonly SubscribeOnObservableInfo[] MethodForAutoSubscription;
        public readonly ObservableBindingInfo[] ObservablesBindings;

        public CommonInfo(string viewModelClassName, string viewModelNamespaceName,
            INamedTypeSymbol viewTypeSymbol, INamedTypeSymbol? viewModelTypeSymbol,
            ButtonMethodCallInfo[] methodsToCall,
            LocalizableFieldInfo[] localizationFieldInfos, LocalizableFieldInfo[] keyFromFieldLocalizationFieldInfos,
            SubscribeOnObservableInfo[] methodForAutoSubscription, ObservableBindingInfo[] observablesBindings)
        {
            ViewModelClassName = viewModelClassName;
            ViewModelNamespaceName = viewModelNamespaceName;
            ViewTypeSymbol = viewTypeSymbol;
            ViewModelTypeSymbol = viewModelTypeSymbol;
            MethodsToCall = methodsToCall;
            LocalizationFieldInfos = localizationFieldInfos;
            KeyFromFieldLocalizationFieldInfos = keyFromFieldLocalizationFieldInfos;
            MethodForAutoSubscription = methodForAutoSubscription;
            ObservablesBindings = observablesBindings;
        }
    }
}