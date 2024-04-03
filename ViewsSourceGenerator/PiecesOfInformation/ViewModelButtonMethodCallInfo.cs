using System;

namespace ViewsSourceGenerator
{
    internal readonly struct ViewModelButtonMethodCallInfo
    {
        public readonly struct ViewInfo
        {
            public int InactivePeriodMs { get; init; }
            public int LongClickDurationMs { get; init; }
            public ButtonClickType ButtonInteractionType { get; init; }
            public string MethodToCall { get; init; }
        }
        
        public readonly struct ViewModelInfo
        {
            public string MethodToCall { get; init; }
            public bool ShouldGenerateMethodWithPartialStuff { get; init; }
            public AutoCreationInfo AutoCreationInfo { get; init; }
            
            public bool HasPrivateCreations => AutoCreationInfo.HasPrivateCreations;

            public bool HasPublicCreations => AutoCreationInfo.HasPublicCreations;

            public bool HasObservablesToDispose => HasPrivateCreations;
        
            public bool HasPassForwardCommands => !AutoCreationInfo.IsEmpty;

            public string GetAutoCreatedObserversPrivatePart() => AutoCreationInfo.GetAutoCreatedObserversPrivatePart();

            public string GetAutoCreatedObserversDisposePart() => AutoCreationInfo.GetAutoCreatedObserversDisposePart();

            public string GetAutoCreatedObserversPublicPart() => AutoCreationInfo.GetAutoCreatedObserversPublicPart();
        
            public string GetCallingCommandPart() => AutoCreationInfo.GetCallingCommandPart();
        
            public bool IsEqualFromViewModelPoV(ViewModelInfo otherLocalizationInfo)
            {
                return ViewModelInfo.AreEqualFromViewModelPoV(this, otherLocalizationInfo);
            }

            public static bool AreEqualFromViewModelPoV(ViewModelInfo a, ViewModelInfo b)
            {
                var areEqual = a.ShouldGenerateMethodWithPartialStuff == b.ShouldGenerateMethodWithPartialStuff;
                areEqual &= string.Equals(a.MethodToCall, b.MethodToCall, StringComparison.Ordinal);
                areEqual &= AutoCreationInfo.AreEqualFromViewModelPoV(a.AutoCreationInfo, b.AutoCreationInfo);

                return areEqual;
            }
        }
        
        // Used by view-generation template only
        public string ButtonFieldName { get; init; }
        public bool ShouldCheckForNull { get; init; }

        public ViewInfo[] ViewInfos { get; init; }
        public ViewModelInfo[] ViewModelInfos { get; init; }
    }
}
