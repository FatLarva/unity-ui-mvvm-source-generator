﻿namespace ViewsSourceGenerator
{
    internal partial class ViewModelMethodCallAttributeTemplate
    {
        public const string AttributeName = "ViewModelMethodCallAttribute";
        public const string SourceFileName = AttributeName + "_g.cs";
        public const string MetaDataName = StaticConstants.AttributesNamespace + "." + AttributeName;
        
        public const string ClickCooldownMsParameterName = "ClickCooldownMs";
        public const string PassForwardThroughCommandNameParameterName = "PassForwardThroughCommandName";

        public ViewModelMethodCallAttributeTemplate()
        {
        }
    }
}
