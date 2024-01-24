using System;
using ViewsSourceGenerator.Tools;

namespace ViewsSourceGenerator
{
    internal readonly struct ViewModelButtonMethodCallInfo
    {
        // Used by view-generation template only
        public string ButtonFieldName { get; init; }
        public int InactivePeriodMs { get; init; }
        public bool ShouldCheckForNull { get; init; }
        
        // Used by viewmodel-generating template or both view and viewmodel
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
        
        public bool IsEqualFromViewModelPoV(ViewModelButtonMethodCallInfo otherLocalizationInfo)
        {
            return ViewModelButtonMethodCallInfo.AreEqualFromViewModelPoV(this, otherLocalizationInfo);
        }

        public static bool AreEqualFromViewModelPoV(ViewModelButtonMethodCallInfo a, ViewModelButtonMethodCallInfo b)
        {
            var areEqual = a.ShouldGenerateMethodWithPartialStuff == b.ShouldGenerateMethodWithPartialStuff;
            areEqual &= string.Equals(a.MethodToCall, b.MethodToCall, StringComparison.Ordinal);
            areEqual &= AutoCreationInfo.AreEqualFromViewModelPoV(a.AutoCreationInfo, b.AutoCreationInfo);

            return areEqual;
        }
    }
}
