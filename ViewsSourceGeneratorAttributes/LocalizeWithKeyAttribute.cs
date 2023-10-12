using System;

namespace ViewModelGeneration
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class LocalizeWithKeyAttribute : Attribute
    {
        public string LocalizationKey { get; }
        public bool IsLocalizePlaceholder { get; set; }

        public LocalizeWithKeyAttribute(string localizationKey)
        {
            LocalizationKey = localizationKey;
        }
    }
}