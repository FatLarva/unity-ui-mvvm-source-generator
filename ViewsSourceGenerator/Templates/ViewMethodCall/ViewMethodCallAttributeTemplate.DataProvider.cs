namespace ViewsSourceGenerator
{
    internal partial class ViewMethodCallAttributeTemplate
    {
        public const string AttributeName = "ViewMethodCallAttribute";
        public const string SourceFileName = AttributeName + "_g.cs";
        public const string MetaDataName = StaticConstants.AttributesNamespace + "." + AttributeName;
        
        public const string ClickCooldownMsParameterName = "ClickCooldownMs";
        public const string LongClickDurationMsParameterName = "LongClickDurationMs";
        public const string PassModelParameterName = "PassModel";
        public const string InteractionTypeParameterName = "InteractionType";

        public ViewMethodCallAttributeTemplate()
        {
        }
    }
}
