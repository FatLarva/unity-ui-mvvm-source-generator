namespace ViewsSourceGenerator
{
    internal readonly struct LocalizableFieldInfo
    {
        public readonly string LocalizationKey;
        private readonly string _viewFieldName;
        private readonly bool _isLocalizePlaceholder;
        private readonly string _keyProviderFieldName;

        public LocalizableFieldInfo(string viewFieldName, string localizationKey, bool isLocalizePlaceholder, string keyProviderFieldName)
        {
            LocalizationKey = localizationKey;
            _viewFieldName = viewFieldName;
            _isLocalizePlaceholder = isLocalizePlaceholder;
            _keyProviderFieldName = keyProviderFieldName;
        }

        public string PathToLocalizableText => 
            _isLocalizePlaceholder ?
                $"{_viewFieldName}.placeholder.GetComponent<TMPro.TextMeshProUGUI>().text" :
                $"{_viewFieldName}.text";

        public string GetLocalizationKey()
        {
            return string.IsNullOrEmpty(_keyProviderFieldName) ? $"\"{LocalizationKey}\"" : $"{_keyProviderFieldName}";
        }
    }
}
