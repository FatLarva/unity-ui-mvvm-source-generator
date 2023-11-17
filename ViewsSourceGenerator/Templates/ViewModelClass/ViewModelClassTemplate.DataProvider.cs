using System.Linq;

namespace ViewsSourceGenerator
{
    internal partial class ViewModelClassTemplate
    {
        private string ClassName { get; }

        private string NamespaceName { get; }
        
        private string[] Usings { get; }

        private ButtonMethodCallInfo[] ButtonMethodCallInfos { get; }

        private ViewModelLocalizationInfo[] LocalizationInfos { get; }

        private AutoCreationInfo[] CreationInfos { get; }
        
        private bool NeedLocalization => LocalizationInfos.Length > 0;

        private bool HasNamespace => !string.IsNullOrEmpty(NamespaceName);

        private bool HasObservablesToDispose => CreationInfos.Any(info => info.HasPrivateCreations)
                                             || ButtonMethodCallInfos.Any(info => info.HasObservablesToDispose)
                                             || NeedLocalization;

        private bool ShouldImplementDisposeInterface { get; }

        internal ViewModelClassTemplate(
            string className,
            string namespaceName,
            ButtonMethodCallInfo[] buttonMethodCallInfos,
            ViewModelLocalizationInfo[] localizationInfos,
            AutoCreationInfo[] autoCreationInfos,
            string[] usings,
            bool shouldImplementDisposeInterface)
        {
            ClassName = className;
            NamespaceName = namespaceName;
            ButtonMethodCallInfos = buttonMethodCallInfos;
            LocalizationInfos = localizationInfos;
            CreationInfos = autoCreationInfos;
            Usings = usings;
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
