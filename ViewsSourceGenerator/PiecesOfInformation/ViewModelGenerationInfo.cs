using System.Linq;
using ViewsSourceGenerator.Comparers;

namespace ViewsSourceGenerator
{
    internal struct ViewModelGenerationInfo
    {
        public string ClassName { get; }
        public string NamespaceName { get; }
        public ButtonMethodCallInfo[] ButtonMethodInfos { get; }
        public LocalizableFieldInfo[] LocalizationInfos { get; }
        public SubscribeOnObservableInfo[] SubscribeInfos { get; }
        public ObservableBindingInfo[] ObservableBindingInfos { get; }
        public string[] Usings { get; }
        public bool ShouldImplementDisposeInterface { get; }

        public readonly string Id;      
        
        public ViewModelGenerationInfo(
            string className,
            string namespaceName,
            ButtonMethodCallInfo[] buttonMethodInfos,
            LocalizableFieldInfo[] localizationInfos,
            SubscribeOnObservableInfo[] subscribeInfos,
            ObservableBindingInfo[] observableBindingInfos,
            string[] usings,
            bool shouldImplementDisposeInterface)
        {
            ClassName = className;
            NamespaceName = namespaceName;
            
            ButtonMethodInfos = buttonMethodInfos.Distinct(ButtonMethodCallInfoComparerFromViewModelPoV.Default).ToArray();
            LocalizationInfos = localizationInfos.Distinct(LocalizableFieldInfoComparerFromViewModelPoV.Default).ToArray();
            SubscribeInfos = subscribeInfos.Distinct(SubscribeOnObservableInfoComparerFromViewModelPoV.Default).ToArray();
            ObservableBindingInfos = observableBindingInfos.Distinct(ObservableBindingInfoComparerFromViewModelPoV.Default).ToArray();
            Usings = usings;
            ShouldImplementDisposeInterface = shouldImplementDisposeInterface;

            Id = $"{NamespaceName}{ClassName}";
        }
    }
}