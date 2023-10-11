using System;

namespace ViewsSourceGenerator
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    internal class LocalizeWithKeyFromFieldAttribute : Attribute
    {
        public string LocalizationKeyProvidingFieldName { get; }
        public bool IsLocalizePlaceholder { get; set; }

        public LocalizeWithKeyFromFieldAttribute(string localizationKeyProvidingFieldName)
        {
            LocalizationKeyProvidingFieldName = localizationKeyProvidingFieldName;
        }
    }
}