namespace ViewsSourceGenerator
{
    internal readonly struct SubViewInfo
    {
        public readonly string ViewFieldName;
        public readonly string ViewModelFieldName;
        public readonly bool UseSameViewModel;

        public SubViewInfo(string viewFieldName, string viewModelFieldName)
        {
            ViewFieldName = viewFieldName;
            ViewModelFieldName = viewModelFieldName;
            UseSameViewModel = default;
        }
        
        public SubViewInfo(string viewFieldName, bool useSameViewModel)
        {
            ViewFieldName = viewFieldName;
            ViewModelFieldName = default;
            UseSameViewModel = useSameViewModel;
        }
    }
}