using System.Linq;

namespace ViewsSourceGenerator
{
    public partial class ViewModelClassTemplate
    {
        public string ClassName { get; }

        public string NamespaceName { get; }

        public string[] MethodsToCall { get; }
        
        public string[] LocalizationKeys { get; }
        
        public string[] PlaceholderLocalizationKeys { get; }

        public SubscribeOnObservableInfo[] SubscribeOnObservableInfos { get; }

        public bool NeedUniRx => LocalizationKeys.Length > 0 || PlaceholderLocalizationKeys.Length > 0 || SubscribeOnObservableInfos.Length > 0;
        
        public bool NeedLocalization => LocalizationKeys.Length > 0 || PlaceholderLocalizationKeys.Length > 0;
        
        public bool HasNamespace => !string.IsNullOrEmpty(NamespaceName);
        
        public bool HasAutoCreatedObservables => SubscribeOnObservableInfos.Any(info => info.ShouldCreateObservableInViewModel);
        
        public ViewModelClassTemplate(
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
