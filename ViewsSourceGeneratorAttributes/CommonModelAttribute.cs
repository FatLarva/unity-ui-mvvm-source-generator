using System;

namespace ViewsSourceGenerator
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal class CommonModelAttribute : Attribute
    {
        public CommonModelAttribute()
        {
        }
    }
}