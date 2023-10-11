using System;

namespace ViewsSourceGenerator
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    internal class ViewModelGenerateAttribute : Attribute
    {
        public string ViewModelClassName { get; set; }
        public string ViewModelNamespaceName { get; set; }
        public bool SkipViewModelGeneration { get; set; }

        public ViewModelGenerateAttribute()
        {
        }
    }
}