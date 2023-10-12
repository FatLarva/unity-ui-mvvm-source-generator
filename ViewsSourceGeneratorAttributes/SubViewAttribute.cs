using System;

namespace ViewModelGeneration
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SubViewAttribute : Attribute
    {
        public string SubViewModelFieldName { get; set; }
        public bool UseSameViewModel { get; set; }

        public SubViewAttribute()
        {
        }
    }
}