namespace ViewsSourceGenerator
{
    public partial class ViewClassTemplate
    {
        public string ClassName { get; }
        
        public string ViewModelClassName { get; }

        public string NamespaceName { get; }

        public ButtonMethodCallInfo[] ButtonMethodCallInfo { get; }
        
        public LocalizableFieldInfo[] LocalizableFieldInfos { get; }
        
        public LocalizableFieldInfo[] LocalizablePlaceholderFieldInfos { get; }

        public SubscribeOnObservableInfo[] SubscribeOnObservableInfos { get; }

        public bool HasNamespace => !string.IsNullOrEmpty(NamespaceName);

        public ViewClassTemplate(
            string className,
            string viewModelClassName,
            string namespaceName,
            ButtonMethodCallInfo[] buttonMethodCallInfo,
            LocalizableFieldInfo[] localizableFieldInfos,
            LocalizableFieldInfo[] localizablePlaceholdersFieldInfos,
            SubscribeOnObservableInfo[] subscribeOnObservableInfos)
        {
            ClassName = className;
            ViewModelClassName = viewModelClassName;
            NamespaceName = namespaceName;
            ButtonMethodCallInfo = buttonMethodCallInfo;
            LocalizableFieldInfos = localizableFieldInfos;
            LocalizablePlaceholderFieldInfos = localizablePlaceholdersFieldInfos;
            SubscribeOnObservableInfos = subscribeOnObservableInfos;
        }
    }
}
