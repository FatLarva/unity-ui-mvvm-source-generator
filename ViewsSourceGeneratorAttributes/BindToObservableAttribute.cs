namespace ViewModelGeneration
{
    using System;

    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class BindToObservableAttribute : Attribute
    {
        public string ObservableName { get; }
        public ViewModelGeneration.BindingType BindingType { get; }
        public ViewModelGeneration.AutoCreationFlag AutoCreationFlag { get; set; }
        public int DelayFrames { get; set; }
        public int DelaySeconds { get; set; }

        public BindToObservableAttribute(string observableName, ViewModelGeneration.BindingType bindingType)
        {
            ObservableName = observableName;
            BindingType = bindingType;
        }
    }
}
