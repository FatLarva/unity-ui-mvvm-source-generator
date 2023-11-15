namespace ViewsSourceGenerator
{
    internal readonly struct SubscribeOnObservableInfo
    {
        // Used by view-generation template only
        public readonly string MethodName;
        
        // Used by viewmodel-generating template or both view and viewmodel
        private readonly AutoCreationInfo _autoCreationInfo;

        public AutoCreationInfo AutoCreationInfo => _autoCreationInfo;

        public string ObservableName => _autoCreationInfo.ObservableName;

        public bool HasObservableArgument => _autoCreationInfo.HasObservableArgument;

        public bool HasPrivateCreations => _autoCreationInfo.HasPrivateCreations;

        public bool HasPublicCreations => _autoCreationInfo.HasPublicCreations;

        public bool HasObservablesToDispose => HasPrivateCreations;

        public SubscribeOnObservableInfo(string methodName, AutoCreationInfo autoCreationInfo)
        {
            MethodName = methodName;
            _autoCreationInfo = autoCreationInfo;
        }

        public string GetAutoCreatedObserversPrivatePart() => _autoCreationInfo.GetAutoCreatedObserversPrivatePart();

        public string GetAutoCreatedObserversDisposePart() => _autoCreationInfo.GetAutoCreatedObserversDisposePart();

        public string GetAutoCreatedObserversPublicPart() => _autoCreationInfo.GetAutoCreatedObserversPublicPart();
        
        public bool IsEqualFromViewModelPoV(SubscribeOnObservableInfo otherLocalizationInfo)
        {
            return SubscribeOnObservableInfo.AreEqualFromViewModelPoV(this, otherLocalizationInfo);
        }

        public static bool AreEqualFromViewModelPoV(SubscribeOnObservableInfo a, SubscribeOnObservableInfo b)
        {
            var areEqual = AutoCreationInfo.AreEqualFromViewModelPoV(a._autoCreationInfo, b._autoCreationInfo);

            return areEqual;
        }
    }
}
