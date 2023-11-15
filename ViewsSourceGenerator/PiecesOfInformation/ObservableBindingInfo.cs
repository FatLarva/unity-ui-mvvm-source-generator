using System;

namespace ViewsSourceGenerator
{
    internal readonly struct ObservableBindingInfo
    {
        // Used by view-generation template only
        private readonly string _fieldName;
        private readonly BindingType _bindingType;
        private readonly bool _isInversed;
        private readonly bool _isCollection;
        private readonly bool _shouldCheckForNull;
        private readonly ObservableBindingDelaySettings? _delaySettings;
        
        // Used by viewmodel-generating template or both view and viewmodel
        private readonly AutoCreationInfo _autoCreationInfo;

        public AutoCreationInfo AutoCreationInfo => _autoCreationInfo;

        public string ObservableName => _autoCreationInfo.ObservableName;

        public bool HasObservableArgument => _autoCreationInfo.HasObservableArgument;

        public bool HasPrivateCreations => _autoCreationInfo.HasPrivateCreations;

        public bool HasPublicCreations => _autoCreationInfo.HasPublicCreations;

        public bool HasObservablesToDispose => HasPrivateCreations;
        
        public string FieldName => _fieldName;
        
        public bool CheckForNull => _shouldCheckForNull;

        public ObservableBindingInfo(string fieldName, BindingType bindingType, bool isInversed, bool isCollection, bool shouldCheckForNull, ObservableBindingDelaySettings? delaySettings, AutoCreationInfo autoCreationInfo)
        {
            _fieldName = fieldName;
            _bindingType = bindingType;
            _isInversed = isInversed;
            _isCollection = isCollection;
            _shouldCheckForNull = shouldCheckForNull;
            _delaySettings = delaySettings;
            _autoCreationInfo = autoCreationInfo;
        }
        
        public string GetAutoCreatedObserversPrivatePart() => _autoCreationInfo.GetAutoCreatedObserversPrivatePart();

        public string GetAutoCreatedObserversDisposePart() => _autoCreationInfo.GetAutoCreatedObserversDisposePart();

        public string GetAutoCreatedObserversPublicPart() => _autoCreationInfo.GetAutoCreatedObserversPublicPart();

        public string GenerateAssignment(string observedValueName)
        {
            if (_isCollection)
            {
                var singleAssignment = GenerateSingleAssignment(observedValueName, "item");
                return $"foreach (var item in {_fieldName}) {{ {singleAssignment} }}";
            }
            else
            {
                return GenerateSingleAssignment(observedValueName);
            }
        }

        private string GenerateSingleAssignment(string observedValueName, string? assigneeName = null)
        {
            string nameToUse = assigneeName ?? _fieldName;
            
            if (_isInversed)
            {
                return GenerateInversedAssignment(observedValueName, nameToUse);
            }
            else
            {
                return GenerateStraightAssignment(observedValueName, nameToUse);
            }
        }

        private string GenerateInversedAssignment(string observedValueName, string nameToUse)
        {
            switch (_bindingType)
            {
                case BindingType.Text:
                    return $"{nameToUse}.text = {observedValueName};";
                case BindingType.ImageFill:
                    return $"{nameToUse}.fillAmount = 1 - {observedValueName};";
                case BindingType.GameObjectActivity:
                    return $"{nameToUse}.gameObject.SetActive(!{observedValueName});";
                case BindingType.Activity:
                    return $"{nameToUse}.SetActive(!{observedValueName});";
                case BindingType.Color:
                    return $"{nameToUse}.color = {observedValueName};";
                case BindingType.Sprite:
                    return $"{nameToUse}.sprite = {observedValueName};";
                case BindingType.Enabled:
                    return $"{nameToUse}.enabled = !{observedValueName};";
                case BindingType.Interactable:
                    return $"{nameToUse}.interactable = !{observedValueName};";
                case BindingType.Alpha:
                    return $"{nameToUse}.alpha = 1 - {observedValueName};";
                case BindingType.EffectColor:
                    return $"{nameToUse}.effectColor = {observedValueName};";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private string GenerateStraightAssignment(string observedValueName, string nameToUse)
        {
            switch (_bindingType)
            {
                case BindingType.Text:
                    return $"{nameToUse}.text = {observedValueName};";
                case BindingType.ImageFill:
                    return $"{nameToUse}.fillAmount = {observedValueName};";
                case BindingType.GameObjectActivity:
                    return $"{nameToUse}.gameObject.SetActive({observedValueName});";
                case BindingType.Activity:
                    return $"{nameToUse}.SetActive({observedValueName});";
                case BindingType.Color:
                    return $"{nameToUse}.color = {observedValueName};";
                case BindingType.Sprite:
                    return $"{nameToUse}.sprite = {observedValueName};";
                case BindingType.Enabled:
                    return $"{nameToUse}.enabled = {observedValueName};";
                case BindingType.Interactable:
                    return $"{nameToUse}.interactable = {observedValueName};";
                case BindingType.Alpha:
                    return $"{nameToUse}.alpha = {observedValueName};";
                case BindingType.EffectColor:
                    return $"{nameToUse}.effectColor = {observedValueName};";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public string DelayIfNeeded()
        {
            if (_delaySettings is not {} assuredDelaySettings)
            {
                return string.Empty;
            }

            var delay = assuredDelaySettings.Delay;
            if (assuredDelaySettings.IsFrames)
            {
                return $"\n\t\t\t\t.DelayFrame({delay})";
            }
            else
            {
                return $"\n\t\t\t\t.Delay(TimeSpan.FromSeconds({delay}))";
            }
        }
        
        public bool IsEqualFromViewModelPoV(ObservableBindingInfo otherBindingInfo)
        {
            return ObservableBindingInfo.AreEqualFromViewModelPoV(this, otherBindingInfo);
        }

        public static bool AreEqualFromViewModelPoV(ObservableBindingInfo a, ObservableBindingInfo b)
        {
            var areEqual = AutoCreationInfo.AreEqualFromViewModelPoV(a._autoCreationInfo, b._autoCreationInfo);

            return areEqual;
        }
    }
}
