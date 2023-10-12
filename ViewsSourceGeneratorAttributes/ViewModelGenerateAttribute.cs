using System;

namespace ViewModelGeneration
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class ViewModelGenerateAttribute : Attribute
    {
        public string ViewModelClassName { get; set; }
        public string ViewModelNamespaceName { get; set; }
        public bool SkipViewModelGeneration { get; set; }

        public ViewModelGenerateAttribute()
        {
        }
    }
}