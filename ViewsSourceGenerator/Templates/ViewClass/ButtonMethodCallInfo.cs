namespace ViewsSourceGenerator
{
    public readonly struct ButtonMethodCallInfo
    {
        public readonly string ButtonFieldName;
        public readonly string MethodToCall;

        public ButtonMethodCallInfo(string buttonFieldName, string methodToCall)
        {
            ButtonFieldName = buttonFieldName;
            MethodToCall = methodToCall;
        }
    }
}