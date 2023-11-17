namespace ViewsSourceGenerator
{
    internal readonly struct SubscribeOnObservableInfo
    {
        // Used by view-generation template only
        public readonly string MethodName;
        private readonly string? _filter;
        
        // Used by viewmodel-generating template or both view and viewmodel
        private readonly AutoCreationInfo _autoCreationInfo;

        public AutoCreationInfo AutoCreationInfo => _autoCreationInfo;

        public string ObservableName => _autoCreationInfo.ObservableName;

        public bool HasObservableArgument => _autoCreationInfo.HasObservableArgument;

        public bool HasPrivateCreations => _autoCreationInfo.HasPrivateCreations;
        
        public bool HasFilter => !string.IsNullOrEmpty(_filter);
        public string Filter => _filter ?? string.Empty;

        public SubscribeOnObservableInfo(string methodName, AutoCreationInfo autoCreationInfo, string? filter)
        {
            MethodName = methodName;
            _autoCreationInfo = autoCreationInfo;
            _filter = filter;
        }

        public string GetSubscriptionInnerCode()
        {
            return HasObservableArgument
                       ? $"{MethodName}"
                       : $"_ => {MethodName}()";
        }

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
