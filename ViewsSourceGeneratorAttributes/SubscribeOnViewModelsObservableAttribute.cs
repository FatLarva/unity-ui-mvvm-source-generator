using System;

namespace ViewsSourceGenerator
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    internal class SubscribeOnViewModelsObservableAttribute : Attribute
    {
        public string ObservableName;
        public ViewsSourceGenerator.AutoCreationFlag AutoCreationFlag { get; set; }

        public SubscribeOnViewModelsObservableAttribute(string observableName)
        {
            ObservableName = observableName;
        }
    }
}