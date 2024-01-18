namespace ViewsSourceGenerator
{
    internal partial class SubViewsCollectionAttributeTemplate
    {
        public const string AttributeName = "SubViewsCollectionAttribute";
        public const string SourceFileName = AttributeName + "_g.cs";
        public const string MetaDataName = StaticConstants.AttributesNamespace + "." + AttributeName;
        
        public const string SubViewModelFieldNameParameterName = "SubViewModelsProvidingFieldName";
        public const string SubViewsBindingMethodParameterName = "SubViewsBindingMethod";
        public const string MatchingMethodNameParameterName = "MatchingMethodName";
        public const string ViewBindingFieldNameParameterName = "ViewBindingFieldName";
        public const string ViewModelBindingFieldNameParameterName = "ViewModelBindingFieldName";

        public SubViewsCollectionAttributeTemplate()
        {
        }
    }
}
