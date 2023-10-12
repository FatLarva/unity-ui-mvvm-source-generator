using System;

namespace ViewModelGeneration
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class GeneratedModelAttribute : Attribute
    {
        public GeneratedModelAttribute()
        {
        }
    }
}