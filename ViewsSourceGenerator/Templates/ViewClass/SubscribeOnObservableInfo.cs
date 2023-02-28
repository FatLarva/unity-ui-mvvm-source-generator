namespace ViewsSourceGenerator
{
    public readonly struct SubscribeOnObservableInfo
    {
        public readonly string MethodName;
        public readonly string ObservableName;
        public readonly bool ShouldCreateObservableInViewModel;
        public readonly string ObservableArgumentType;
        
        public bool HasObservableArgument => (ObservableArgumentType is not (null or "Unit"));

        public SubscribeOnObservableInfo(string methodName, string observableName, bool shouldCreateObservableInViewModel, string observableArgumentType = null)
        {
            MethodName = methodName;
            ObservableName = observableName;
            ShouldCreateObservableInViewModel = shouldCreateObservableInViewModel;
            ObservableArgumentType = observableArgumentType;
        }
        
        public string GetReactiveCommandType()
        {
            if (ObservableArgumentType is null or "Unit")
            {
                return "ReactiveCommand";
            }
            else
            {
                return $"ReactiveCommand<{ObservableArgumentType}>";
            }
        }
        
        public string GetObservableArgumentType()
        {
            if (ObservableArgumentType is null or "Unit")
            {
                return "Unit";
            }
            else
            {
                return ObservableArgumentType;
            }
        }
    }
}
