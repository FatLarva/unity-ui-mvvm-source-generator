using System.Linq;

namespace ViewsSourceGenerator
{
    internal partial class ViewModelClassTemplate
    {
        private string ClassName { get; }

        private string NamespaceName { get; }

        private ButtonMethodCallInfo[] ButtonMethodCallInfos { get; }

        private LocalizableFieldInfo[] LocalizationFieldInfos { get; }

        private SubscribeOnObservableInfo[] SubscribeOnObservableInfos { get; }
        
        private ObservableBindingInfo[] ObservableBindingInfos { get; }

        private bool NeedLocalization => LocalizationFieldInfos.Length > 0;

        private bool HasNamespace => !string.IsNullOrEmpty(NamespaceName);

        private bool HasObservablesToDispose => SubscribeOnObservableInfos.Any(info => info.HasObservablesToDispose) || ObservableBindingInfos.Any(info => info.HasObservablesToDispose) || NeedLocalization;

        private bool ShouldImplementDisposeInterface { get; }

        internal ViewModelClassTemplate(
            string className,
            string namespaceName,
            ButtonMethodCallInfo[] buttonMethodCallInfos,
            LocalizableFieldInfo[] localizationFieldInfos,
            SubscribeOnObservableInfo[] subscribeOnObservableInfos,
            ObservableBindingInfo[] observableBindingInfos,
            bool shouldImplementDisposeInterface)
        {
            ClassName = className;
            NamespaceName = namespaceName;
            ButtonMethodCallInfos = buttonMethodCallInfos;
            LocalizationFieldInfos = localizationFieldInfos;
            SubscribeOnObservableInfos = subscribeOnObservableInfos;
            ObservableBindingInfos = observableBindingInfos;
            ShouldImplementDisposeInterface = shouldImplementDisposeInterface;
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
