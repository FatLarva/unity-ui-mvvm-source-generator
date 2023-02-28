namespace ViewsSourceGenerator
{
    public readonly struct LocalizableFieldInfo
    {
        public readonly string FieldName;
        public readonly string LocalizationKey;

        public LocalizableFieldInfo(string fieldName, string localizationKey)
        {
            FieldName = fieldName;
            LocalizationKey = localizationKey;
        }
    }
}