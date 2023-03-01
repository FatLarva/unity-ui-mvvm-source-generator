using System.Linq;

namespace ViewsSourceGenerator
{
    internal partial class ViewModelClassTemplate
    {
        internal string ClassName { get; }

        internal string NamespaceName { get; }

        internal string[] MethodsToCall { get; }
        
        internal string[] LocalizationKeys { get; }
        
        internal string[] PlaceholderLocalizationKeys { get; }

        internal SubscribeOnObservableInfo[] SubscribeOnObservableInfos { get; }

        internal bool NeedUniRx => LocalizationKeys.Length > 0 || PlaceholderLocalizationKeys.Length > 0 || SubscribeOnObservableInfos.Length > 0;
        
        internal bool NeedLocalization => LocalizationKeys.Length > 0 || PlaceholderLocalizationKeys.Length > 0;
        
        internal bool HasNamespace => !string.IsNullOrEmpty(NamespaceName);
        
        internal bool HasAutoCreatedObservables => SubscribeOnObservableInfos.Any(info => info.ShouldCreateObservableInViewModel);
        
        internal ViewModelClassTemplate(
            string className,
            string namespaceName,
            string[] methodsToCall,
            string[] localizationKeys,
            string[] placeholderLocalizationKeys,
            SubscribeOnObservableInfo[] subscribeOnObservableInfos)
        {
            ClassName = className;
            NamespaceName = namespaceName;
            MethodsToCall = methodsToCall;
            LocalizationKeys = localizationKeys;
            PlaceholderLocalizationKeys = placeholderLocalizationKeys;
            SubscribeOnObservableInfos = subscribeOnObservableInfos;
        }
    }
}
