namespace ViewsSourceGenerator
{
    internal readonly struct SubscribeOnObservableInfo
    {
        public readonly string MethodName;

        private readonly AutoCreationInfo _autoCreationInfo;

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
    }
}
