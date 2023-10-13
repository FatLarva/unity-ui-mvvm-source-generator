using System.Collections.Generic;
using System.Linq;

namespace ViewsSourceGenerator
{
    internal partial class ModelClassTemplate
    {
        private string ClassName { get; }

        private string NamespaceName { get; }

        private ModelObservableInfo[] ModelObservableInfos { get; }
        
        private List<string> Usings { get; }

        private bool HasNamespace => !string.IsNullOrEmpty(NamespaceName);

        private bool HasObservablesToDispose => ModelObservableInfos.Any(info => info.HasObservablesToDispose);

        private bool ShouldImplementDisposeInterface { get; }

        internal ModelClassTemplate(
            string className,
            string namespaceName,
            ModelObservableInfo[] modelObservableInfos,
            List<string> usings,
            bool shouldImplementDisposeInterface)
        {
            ClassName = className;
            NamespaceName = namespaceName;
            ModelObservableInfos = modelObservableInfos;
            Usings = usings;
            ShouldImplementDisposeInterface = shouldImplementDisposeInterface;
        }

        private string GetHandleAutoBindingsDefinition()
        { 
            return "private void HandleAutoBindings()";
        }
    }
}
