namespace ViewsSourceGenerator
{
    internal readonly struct SubViewInfo
    {
        public string ViewFieldName { get; init; }
        public string ViewModelFieldName { get; init; }
        public bool UseSameViewModel { get; init; }
        public bool CheckForNull { get; init; }

        public string GetInitCall()
        {
            return UseSameViewModel
                       ? $"{ViewFieldName}.Init(model);"
                       : $"{ViewFieldName}.Init(model.{ViewModelFieldName});";
        }
        
        public string GetDeinitCall()
        {
            return $"{ViewFieldName}.Deinit();";
        }
    }
}