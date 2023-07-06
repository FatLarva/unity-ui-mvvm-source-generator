using System.Linq;

namespace ViewsSourceGenerator
{
    internal partial class ViewModelClassTemplate
    {
        private string ClassName { get; }

        private string NamespaceName { get; }

        private string[] MethodsToCall { get; }

        private string[] LocalizationKeys { get; }

        private string[] PlaceholderLocalizationKeys { get; }

        private SubscribeOnObservableInfo[] SubscribeOnObservableInfos { get; }

        private bool NeedLocalization => LocalizationKeys.Length > 0 || PlaceholderLocalizationKeys.Length > 0;

        private bool HasNamespace => !string.IsNullOrEmpty(NamespaceName);

        private bool HasObservablesToDispose => SubscribeOnObservableInfos.Any(info => info.HasObservablesToDispose);
        
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

        private string GetHandleAutoBindingsDefinition()
        {
            if (NeedLocalization)
            {
                return "private void HandleAutoBindings(ILocalizationProvider localizationProvider)";
            }
            
            return "private void HandleAutoBindings()";
        }
    }
}
