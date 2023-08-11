namespace ViewsSourceGenerator
{
    internal readonly struct SubViewInfo
    {
        public readonly string ViewFieldName;
        public readonly string ViewModelFieldName;

        public SubViewInfo(string viewFieldName, string viewModelFieldName)
        {
            ViewFieldName = viewFieldName;
            ViewModelFieldName = viewModelFieldName;
        }
    }
}