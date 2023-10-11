using System;

namespace ViewsSourceGenerator
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    internal class LocalizeWithKeyAttribute : Attribute
    {
        public string LocalizationKey { get; }
        public bool IsLocalizePlaceholder { get; set; }

        public LocalizeWithKeyAttribute(string localizationKey)
        {
            LocalizationKey = localizationKey;
        }
    }
}