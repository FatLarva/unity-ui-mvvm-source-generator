using System;

namespace ViewModelGeneration
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class CommonModelAttribute : Attribute
    {
        public CommonModelAttribute()
        {
        }
    }
}