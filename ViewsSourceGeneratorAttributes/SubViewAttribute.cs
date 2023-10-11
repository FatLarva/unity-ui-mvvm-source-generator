using System;

namespace ViewsSourceGenerator
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal class SubViewAttribute : Attribute
    {
        public string SubViewModelFieldName { get; set; }
        public bool UseSameViewModel { get; set; }

        public SubViewAttribute()
        {
        }
    }
}