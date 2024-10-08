﻿namespace ViewsSourceGenerator
{
    internal partial class ViewClassTemplate
    {
        private string ClassName { get; }
        
        private string ViewModelClassName { get; }

        private string NamespaceName { get; }
        
        private string[] Usings { get; }

        private ViewButtonMethodCallInfo[] ButtonMethodCallInfo { get; }
        
        private LocalizableFieldInfo[] LocalizationFieldInfos { get; }
        
        private SubscribeOnObservableInfo[] SubscribeOnObservableInfos { get; }

        private ObservableBindingInfo[] ObservableBindingInfos { get; }
        
        private SubViewInfo[] SubViewInfos { get; }
        
        private SubViewsCollectionInfo[] SubViewsCollectionInfos { get; }

        private bool HasNamespace => !string.IsNullOrEmpty(NamespaceName);

        internal ViewClassTemplate(
            string className,
            string viewModelClassName,
            string namespaceName,
            ViewButtonMethodCallInfo[] buttonMethodCallInfo,
            LocalizableFieldInfo[] localizationFieldInfos,
            SubscribeOnObservableInfo[] subscribeOnObservableInfos,
            ObservableBindingInfo[] observableBindingInfos,
            SubViewInfo[] subViewInfos,
            SubViewsCollectionInfo[] subViewsCollectionInfos,
            string[] usings)
        {
            ClassName = className;
            ViewModelClassName = viewModelClassName;
            NamespaceName = namespaceName;
            ButtonMethodCallInfo = buttonMethodCallInfo;
            LocalizationFieldInfos = localizationFieldInfos;
            SubscribeOnObservableInfos = subscribeOnObservableInfos;
            ObservableBindingInfos = observableBindingInfos;
            SubViewInfos = subViewInfos;
            SubViewsCollectionInfos = subViewsCollectionInfos;
            Usings = usings;
        }
    }
}
