using System;

namespace ViewModelGeneration
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class GeneratedViewModelAttribute : Attribute
    {
        public GeneratedViewModelAttribute()
        {
        }
    }
}