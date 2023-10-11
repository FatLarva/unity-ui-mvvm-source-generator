namespace ViewsSourceGenerator
{
    using System;

    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    internal class BindToObservableAttribute : Attribute
    {
        public string ObservableName { get; }
        public ViewsSourceGenerator.BindingType BindingType { get; }
        public ViewsSourceGenerator.AutoCreationFlag AutoCreationFlag { get; set; }
        public int DelayFrames { get; set; }
        public int DelaySeconds { get; set; }

        public BindToObservableAttribute(string observableName, ViewsSourceGenerator.BindingType bindingType)
        {
            ObservableName = observableName;
            BindingType = bindingType;
        }
    }
}
