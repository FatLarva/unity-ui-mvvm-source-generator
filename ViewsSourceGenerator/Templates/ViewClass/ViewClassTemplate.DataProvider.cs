namespace ViewsSourceGenerator
{
    internal partial class ViewClassTemplate
    {
        internal string ClassName { get; }
        
        internal string ViewModelClassName { get; }

        internal string NamespaceName { get; }

        internal ButtonMethodCallInfo[] ButtonMethodCallInfo { get; }
        
        internal LocalizableFieldInfo[] LocalizableFieldInfos { get; }
        
        internal LocalizableFieldInfo[] LocalizablePlaceholderFieldInfos { get; }

        internal SubscribeOnObservableInfo[] SubscribeOnObservableInfos { get; }
        
        internal ObservableBindingInfo[] ObservableBindingInfos { get; }

        internal bool HasNamespace => !string.IsNullOrEmpty(NamespaceName);

        internal ViewClassTemplate(
            string className,
            string viewModelClassName,
            string namespaceName,
            ButtonMethodCallInfo[] buttonMethodCallInfo,
            LocalizableFieldInfo[] localizableFieldInfos,
            LocalizableFieldInfo[] localizablePlaceholdersFieldInfos,
            SubscribeOnObservableInfo[] subscribeOnObservableInfos,
            ObservableBindingInfo[] observableBindingInfos)
        {
            ClassName = className;
            ViewModelClassName = viewModelClassName;
            NamespaceName = namespaceName;
            ButtonMethodCallInfo = buttonMethodCallInfo;
            LocalizableFieldInfos = localizableFieldInfos;
            LocalizablePlaceholderFieldInfos = localizablePlaceholdersFieldInfos;
            SubscribeOnObservableInfos = subscribeOnObservableInfos;
            ObservableBindingInfos = observableBindingInfos;
        }
    }
}
