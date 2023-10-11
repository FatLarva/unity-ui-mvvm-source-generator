using System;

namespace ViewsSourceGenerator
{
    internal readonly struct ObservableBindingInfo
    {
        private readonly string _fieldName;
        private readonly BindingType _bindingType;
        private readonly bool _isInversed;
        private readonly ObservableBindingDelaySettings? _delaySettings;
        private readonly AutoCreationInfo _autoCreationInfo;

        public string ObservableName => _autoCreationInfo.ObservableName;

        public bool HasObservableArgument => _autoCreationInfo.HasObservableArgument;

        public bool HasPrivateCreations => _autoCreationInfo.HasPrivateCreations;

        public bool HasPublicCreations => _autoCreationInfo.HasPublicCreations;

        public bool HasObservablesToDispose => HasPrivateCreations;

        public ObservableBindingInfo(string fieldName, BindingType bindingType, bool isInversed, ObservableBindingDelaySettings? delaySettings, AutoCreationInfo autoCreationInfo)
        {
            _fieldName = fieldName;
            _bindingType = bindingType;
            _isInversed = isInversed;
            _delaySettings = delaySettings;
            _autoCreationInfo = autoCreationInfo;
        }
        
        public string GetAutoCreatedObserversPrivatePart() => _autoCreationInfo.GetAutoCreatedObserversPrivatePart();

        public string GetAutoCreatedObserversDisposePart() => _autoCreationInfo.GetAutoCreatedObserversDisposePart();

        public string GetAutoCreatedObserversPublicPart() => _autoCreationInfo.GetAutoCreatedObserversPublicPart();

        public string GenerateAssignment(string observedValueName)
        {
            if (_isInversed)
            {
                return GenerateInversedAssignment(observedValueName);
            }
            else
            {
                return GenerateStraightAssignment(observedValueName);
            }
        }
        
        private string GenerateInversedAssignment(string observedValueName)
        {
            switch (_bindingType)
            {
                case BindingType.Text:
                    return $"{_fieldName}.text = {observedValueName}";
                case BindingType.ImageFill:
                    return $"{_fieldName}.fillAmount = 1 - {observedValueName}";
                case BindingType.GameObjectActivity:
                    return $"{_fieldName}.gameObject.SetActive(!{observedValueName})";
                case BindingType.Activity:
                    return $"{_fieldName}.SetActive(!{observedValueName})";
                case BindingType.Color:
                    return $"{_fieldName}.color = {observedValueName}";
                case BindingType.Sprite:
                    return $"{_fieldName}.sprite = {observedValueName}";
                case BindingType.Enabled:
                    return $"{_fieldName}.enabled = !{observedValueName}";
                case BindingType.Interactable:
                    return $"{_fieldName}.interactable = !{observedValueName}";
                case BindingType.Alpha:
                    return $"{_fieldName}.alpha = 1 - {observedValueName}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private string GenerateStraightAssignment(string observedValueName)
        {
            switch (_bindingType)
            {
                case BindingType.Text:
                    return $"{_fieldName}.text = {observedValueName}";
                case BindingType.ImageFill:
                    return $"{_fieldName}.fillAmount = {observedValueName}";
                case BindingType.GameObjectActivity:
                    return $"{_fieldName}.gameObject.SetActive({observedValueName})";
                case BindingType.Activity:
                    return $"{_fieldName}.SetActive({observedValueName})";
                case BindingType.Color:
                    return $"{_fieldName}.color = {observedValueName}";
                case BindingType.Sprite:
                    return $"{_fieldName}.sprite = {observedValueName}";
                case BindingType.Enabled:
                    return $"{_fieldName}.enabled = {observedValueName}";
                case BindingType.Interactable:
                    return $"{_fieldName}.interactable = {observedValueName}";
                case BindingType.Alpha:
                    return $"{_fieldName}.alpha = {observedValueName}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public string DelayIfNeeded()
        {
            if (!_delaySettings.HasValue)
            {
                return string.Empty;
            }

            if (_delaySettings.Value.IsFrames)
            {
                return $"\n\t\t\t\t.DelayFrame({_delaySettings.Value.Delay})";
            }
            else
            {
                return $"\n\t\t\t\t.Delay(TimeSpan.FromSeconds({_delaySettings.Value.Delay}))";
            }
        }
    }
}
