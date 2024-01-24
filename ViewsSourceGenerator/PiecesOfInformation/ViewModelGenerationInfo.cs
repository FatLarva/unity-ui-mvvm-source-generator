using System.Linq;
using ViewsSourceGenerator.Comparers;

namespace ViewsSourceGenerator
{
    internal struct ViewModelGenerationInfo
    {
        public string ClassName { get; }
        public string NamespaceName { get; }
        public ViewModelButtonMethodCallInfo[] ButtonMethodInfos { get; }
        public ViewModelLocalizationInfo[] LocalizationInfos { get; }
        public SubscribeOnObservableInfo[] SubscribeInfos { get; }
        public ObservableBindingInfo[] ObservableBindingInfos { get; }
        public string[] Usings { get; }
        public bool ShouldImplementDisposeInterface { get; }

        public readonly string Id;      
        
        public ViewModelGenerationInfo(
            string className,
            string namespaceName,
            ViewModelButtonMethodCallInfo[] buttonMethodInfos,
            ViewModelLocalizationInfo[] localizationInfos,
            SubscribeOnObservableInfo[] subscribeInfos,
            ObservableBindingInfo[] observableBindingInfos,
            string[] usings,
            bool shouldImplementDisposeInterface)
        {
            ClassName = className;
            NamespaceName = namespaceName;
            
            ButtonMethodInfos = buttonMethodInfos.Distinct(ButtonMethodCallInfoComparerFromViewModelPoV.Default).ToArray();
            LocalizationInfos = localizationInfos;
            SubscribeInfos = subscribeInfos.Distinct(SubscribeOnObservableInfoComparerFromViewModelPoV.Default).ToArray();
            ObservableBindingInfos = observableBindingInfos.Distinct(ObservableBindingInfoComparerFromViewModelPoV.Default).ToArray();
            Usings = usings;
            ShouldImplementDisposeInterface = shouldImplementDisposeInterface;

            Id = $"{NamespaceName}{ClassName}";
        }
    }
}