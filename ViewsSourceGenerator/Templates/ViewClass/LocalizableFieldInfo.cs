namespace ViewsSourceGenerator
{
    internal readonly struct LocalizableFieldInfo
    {
        public readonly string LocalizationKey;
        private readonly string _fieldName;
        private readonly bool _isLocalizePlaceholder;
        private readonly bool _isFromField;

        public LocalizableFieldInfo(string fieldName, string localizationKey, bool isLocalizePlaceholder, bool isFromField)
        {
            LocalizationKey = localizationKey;
            _fieldName = fieldName;
            _isLocalizePlaceholder = isLocalizePlaceholder;
            _isFromField = isFromField;
        }

        public string PathToLocalizableText => 
            _isLocalizePlaceholder ?
                $"{_fieldName}.placeholder.GetComponent<TMPro.TextMeshProUGUI>().text" :
                $"{_fieldName}.text";

        public string GetLocalizationKey()
        {
            return _isFromField ? $"{LocalizationKey}" : $"\"{LocalizationKey}\"";
        }
    }
}
