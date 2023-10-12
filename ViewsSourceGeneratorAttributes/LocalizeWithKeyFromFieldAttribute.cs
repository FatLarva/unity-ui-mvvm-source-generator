using System;

namespace ViewModelGeneration
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class LocalizeWithKeyFromFieldAttribute : Attribute
    {
        public string LocalizationKeyProvidingFieldName { get; }
        public bool IsLocalizePlaceholder { get; set; }
        

        public LocalizeWithKeyFromFieldAttribute(string localizationKeyProvidingFieldName)
        {
            LocalizationKeyProvidingFieldName = localizationKeyProvidingFieldName;
        }
    }
}