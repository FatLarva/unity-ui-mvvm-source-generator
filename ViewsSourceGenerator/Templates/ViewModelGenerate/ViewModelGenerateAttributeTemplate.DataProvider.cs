namespace ViewsSourceGenerator
{
    internal partial class ViewModelGenerateAttributeTemplate
    {
        public const string AttributeName = "ViewModelGenerateAttribute";
        public const string SourceFileName = AttributeName + "_g.cs";
        public const string MetaDataName = StaticConstants.AttributesNamespace + "." + AttributeName;
        
        public const string ViewModelClassNameParamName = "ViewModelClassName";
        public const string ViewModelNamespaceNameParamName = "ViewModelNamespaceName";
        public const string SkipViewModelGenerationParamName = "SkipViewModelGeneration";
        

        public ViewModelGenerateAttributeTemplate()
        {
        }
    }
}
