using System;

namespace ViewsSourceGenerator
{
    internal readonly struct LocalizableFieldInfo
    {
        // Used by view-generation template only
        public string ViewFieldName  { get; init; }
        public bool IsLocalizePlaceholder  { get; init; }
        public bool CheckForNull { get; init; }
        
        // Used by viewmodel-generating template or both view and viewmodel
        public string LocalizationKey { get; init; }
        public string KeyProviderFieldName  { get; init; }

        public string PathToLocalizableText => 
            IsLocalizePlaceholder ?
                $"{ViewFieldName}.placeholder.GetComponent<TMPro.TextMeshProUGUI>().text" :
                $"{ViewFieldName}.text";

        public bool IsEqualFromViewModelPoV(LocalizableFieldInfo otherLocalizationInfo)
        {
            return LocalizableFieldInfo.AreEqualFromViewModelPoV(this, otherLocalizationInfo);
        }

        public static bool AreEqualFromViewModelPoV(LocalizableFieldInfo a, LocalizableFieldInfo b)
        {
            var areEqual = string.Equals(a.LocalizationKey, b.LocalizationKey, StringComparison.Ordinal);
            areEqual &= string.Equals(a.KeyProviderFieldName, b.KeyProviderFieldName, StringComparison.Ordinal);

            return areEqual;
        }
    }
}
