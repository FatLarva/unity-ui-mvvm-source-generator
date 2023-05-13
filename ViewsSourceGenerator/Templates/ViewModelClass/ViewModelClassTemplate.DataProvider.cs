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

        internal bool NeedLocalization => LocalizationKeys.Length > 0 || PlaceholderLocalizationKeys.Length > 0;
        
        internal bool HasNamespace => !string.IsNullOrEmpty(NamespaceName);
        
        internal bool HasObservablesToDispose => SubscribeOnObservableInfos.Any(info => info.HasObservablesToDispose);
        
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

        private string GetCtorDefinition()
        {
            if (NeedLocalization)
            {
                return $"private {ClassName}(ILocalizationProvider localizationProvider)";
            }
            
            return $"private {ClassName}()";
        }
    }
}
