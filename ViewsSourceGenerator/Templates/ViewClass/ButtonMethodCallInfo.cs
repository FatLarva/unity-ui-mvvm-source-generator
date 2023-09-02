namespace ViewsSourceGenerator
{
    internal readonly struct ButtonMethodCallInfo
    {
        public readonly string ButtonFieldName;
        public readonly string MethodToCall;
        public readonly bool ShouldGenerateMethodWithPartialStuff;
        private readonly AutoCreationInfo _autoCreationInfo;

        public bool HasPrivateCreations => _autoCreationInfo.HasPrivateCreations;

        public bool HasPublicCreations => _autoCreationInfo.HasPublicCreations;

        public bool HasObservablesToDispose => HasPrivateCreations;
        
        public bool HasPassForwardCommands => !_autoCreationInfo.IsEmpty;
        
        public ButtonMethodCallInfo(string buttonFieldName, string methodToCall, bool shouldGenerateMethodWithPartialStuff, AutoCreationInfo autoCreationInfo)
        {
            ButtonFieldName = buttonFieldName;
            MethodToCall = methodToCall;
            ShouldGenerateMethodWithPartialStuff = shouldGenerateMethodWithPartialStuff;
            _autoCreationInfo = autoCreationInfo;
        }
        
        public string GetAutoCreatedObserversPrivatePart() => _autoCreationInfo.GetAutoCreatedObserversPrivatePart();

        public string GetAutoCreatedObserversDisposePart() => _autoCreationInfo.GetAutoCreatedObserversDisposePart();

        public string GetAutoCreatedObserversPublicPart() => _autoCreationInfo.GetAutoCreatedObserversPublicPart();
        
        public string GetCallingCommandPart() => _autoCreationInfo.GetCallingCommandPart();
    }
}
