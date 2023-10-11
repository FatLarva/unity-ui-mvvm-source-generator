using System;

namespace ViewsSourceGenerator
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    internal class ViewModelMethodCallAttribute : Attribute
    {
        public string MethodName { get; }
        public string PassForwardThroughCommandName { get; set; }

        public ViewModelMethodCallAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}