using ViewsSourceGenerator.Tools;

namespace ViewsSourceGenerator
{
    internal readonly struct ButtonMethodCallInfo
    {
        public string ButtonFieldName { get; init; }
        public string MethodToCall { get; init; }
        public bool ShouldGenerateMethodWithPartialStuff { get; init; }
        public AutoCreationInfo AutoCreationInfo { get; init; }
        public int InactivePeriodMs { get; init; }
        public bool ShouldCheckForNull { get; init; }

        public bool HasPrivateCreations => AutoCreationInfo.HasPrivateCreations;

        public bool HasPublicCreations => AutoCreationInfo.HasPublicCreations;

        public bool HasObservablesToDispose => HasPrivateCreations;
        
        public bool HasPassForwardCommands => !AutoCreationInfo.IsEmpty;
        public string LastClickFieldName => $"{ButtonFieldName.Decapitalize().Camel()}LastClickTime";

        public string GetAutoCreatedObserversPrivatePart() => AutoCreationInfo.GetAutoCreatedObserversPrivatePart();

        public string GetAutoCreatedObserversDisposePart() => AutoCreationInfo.GetAutoCreatedObserversDisposePart();

        public string GetAutoCreatedObserversPublicPart() => AutoCreationInfo.GetAutoCreatedObserversPublicPart();
        
        public string GetCallingCommandPart() => AutoCreationInfo.GetCallingCommandPart();
    }
}
