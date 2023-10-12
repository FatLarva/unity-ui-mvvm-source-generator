using System;

namespace ViewModelGeneration
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class ViewModelMethodCallAttribute : Attribute
    {
        public string MethodName { get; }
        public string PassForwardThroughCommandName { get; set; }

        public ViewModelMethodCallAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}