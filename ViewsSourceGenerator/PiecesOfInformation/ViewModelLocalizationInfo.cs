using System;
using ViewsSourceGenerator.Tools;

namespace ViewsSourceGenerator
{
    internal readonly struct ViewModelLocalizationInfo
    {
        public string LocalizationKey { get; init; }
        public string KeyProviderFieldName { get; init; }
        public bool IsProviderObservable { get; init; }

        public string LocalizedTextField => LocalizationKey.ToPascalCase();
        
        public string GetLocalizationKey()
        {
            return string.IsNullOrEmpty(KeyProviderFieldName) ? $"\"{LocalizationKey}\"" : $"{KeyProviderFieldName}";
        }
        
        public static bool AreEqualFromViewModelPoV(ViewModelLocalizationInfo a, ViewModelLocalizationInfo b)
        {
            var areEqual = string.Equals(a.LocalizationKey, b.LocalizationKey, StringComparison.Ordinal);
            areEqual &= string.Equals(a.KeyProviderFieldName, b.KeyProviderFieldName, StringComparison.Ordinal);

            return areEqual;
        }
    }
}
