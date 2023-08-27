namespace ViewsSourceGenerator
{
    internal partial class ViewClassTemplate
    {
        private string ClassName { get; }
        
        private string ViewModelClassName { get; }

        private string NamespaceName { get; }

        private ButtonMethodCallInfo[] ButtonMethodCallInfo { get; }
        
        private LocalizableFieldInfo[] LocalizationFieldInfos { get; }
        
        private SubscribeOnObservableInfo[] SubscribeOnObservableInfos { get; }

        private ObservableBindingInfo[] ObservableBindingInfos { get; }
        
        private SubViewInfo[] SubViewInfos { get; }

        private bool HasNamespace => !string.IsNullOrEmpty(NamespaceName);

        internal ViewClassTemplate(
            string className,
            string viewModelClassName,
            string namespaceName,
            ButtonMethodCallInfo[] buttonMethodCallInfo,
            LocalizableFieldInfo[] localizationFieldInfos,
            SubscribeOnObservableInfo[] subscribeOnObservableInfos,
            ObservableBindingInfo[] observableBindingInfos,
            SubViewInfo[] subViewInfos)
        {
            ClassName = className;
            ViewModelClassName = viewModelClassName;
            NamespaceName = namespaceName;
            ButtonMethodCallInfo = buttonMethodCallInfo;
            LocalizationFieldInfos = localizationFieldInfos;
            SubscribeOnObservableInfos = subscribeOnObservableInfos;
            ObservableBindingInfos = observableBindingInfos;
            SubViewInfos = subViewInfos;
        }
    }
}
