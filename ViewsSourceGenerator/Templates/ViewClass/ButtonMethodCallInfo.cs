namespace ViewsSourceGenerator
{
    internal readonly struct ButtonMethodCallInfo
    {
        public readonly string ButtonFieldName;
        public readonly string MethodToCall;
        public readonly bool ShouldGenerateMethodWithPartialStuff;

        public ButtonMethodCallInfo(string buttonFieldName, string methodToCall, bool shouldGenerateMethodWithPartialStuff)
        {
            ButtonFieldName = buttonFieldName;
            MethodToCall = methodToCall;
            ShouldGenerateMethodWithPartialStuff = shouldGenerateMethodWithPartialStuff;
        }
    }
}
