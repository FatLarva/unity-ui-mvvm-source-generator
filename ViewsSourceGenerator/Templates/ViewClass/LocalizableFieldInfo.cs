namespace ViewsSourceGenerator
{
    internal readonly struct LocalizableFieldInfo
    {
        public string LocalizationKey { get; init; }
        public string ViewFieldName  { get; init; }
        public bool IsLocalizePlaceholder  { get; init; }
        public string KeyProviderFieldName  { get; init; }
        public bool CheckForNull { get; init; }

        public string PathToLocalizableText => 
            IsLocalizePlaceholder ?
                $"{ViewFieldName}.placeholder.GetComponent<TMPro.TextMeshProUGUI>().text" :
                $"{ViewFieldName}.text";


        public string GetLocalizationKey()
        {
            return string.IsNullOrEmpty(KeyProviderFieldName) ? $"\"{LocalizationKey}\"" : $"{KeyProviderFieldName}";
        }
    }
}
