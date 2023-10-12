using System;

namespace ViewModelGeneration
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class SubscribeOnViewModelsObservableAttribute : Attribute
    {
        public string ObservableName;
        public ViewModelGeneration.AutoCreationFlag AutoCreationFlag { get; set; }

        public SubscribeOnViewModelsObservableAttribute(string observableName)
        {
            ObservableName = observableName;
        }
    }
}