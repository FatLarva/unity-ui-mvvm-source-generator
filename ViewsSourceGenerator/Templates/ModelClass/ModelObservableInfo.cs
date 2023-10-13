namespace ViewsSourceGenerator
{
    internal readonly struct ModelObservableInfo
    {
        private readonly AutoCreationInfo _autoCreationInfo;

        public bool OnlyDisposing { get; }
        
        public string ObservableName => _autoCreationInfo.ObservableName;

        public bool HasObservableArgument => _autoCreationInfo.HasObservableArgument;

        public bool HasPrivateCreations => _autoCreationInfo.HasPrivateCreations;

        public bool HasPublicCreations => _autoCreationInfo.HasPublicCreations;

        public bool HasObservablesToDispose => HasPrivateCreations;

        public ModelObservableInfo(AutoCreationInfo autoCreationInfo, bool onlyDisposing)
        {
            OnlyDisposing = onlyDisposing;
            _autoCreationInfo = autoCreationInfo;
        }

        public string GetAutoCreatedObserversPrivatePart() => _autoCreationInfo.GetAutoCreatedObserversPrivatePart();

        public string GetAutoCreatedObserversDisposePart() => _autoCreationInfo.GetAutoCreatedObserversDisposePart();

        public string GetAutoCreatedObserversPublicPart() => _autoCreationInfo.GetAutoCreatedObserversPublicPart();
    }
}
