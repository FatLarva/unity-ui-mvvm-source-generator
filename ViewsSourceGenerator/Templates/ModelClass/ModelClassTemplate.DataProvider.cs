using System.Linq;

namespace ViewsSourceGenerator
{
    internal partial class ModelClassTemplate
    {
        private string ClassName { get; }

        private string NamespaceName { get; }

        private ModelObservableInfo[] ModelObservableInfos { get; }
        
        private bool HasNamespace => !string.IsNullOrEmpty(NamespaceName);

        private bool HasObservablesToDispose => ModelObservableInfos.Any(info => info.HasObservablesToDispose);

        private bool ShouldImplementDisposeInterface { get; }

        internal ModelClassTemplate(
            string className,
            string namespaceName,
            ModelObservableInfo[] modelObservableInfos,
            bool shouldImplementDisposeInterface)
        {
            ClassName = className;
            NamespaceName = namespaceName;
            ModelObservableInfos = modelObservableInfos;
            ShouldImplementDisposeInterface = shouldImplementDisposeInterface;
        }

        private string GetHandleAutoBindingsDefinition()
        { 
            return "private void HandleAutoBindings()";
        }
    }
}
