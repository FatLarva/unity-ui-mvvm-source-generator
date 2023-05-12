using System;

namespace ViewsSourceGenerator
{
    internal readonly struct ObservableBindingInfo
    {
        public readonly string FieldName;
        public readonly string ObservableName;
        public readonly InnerBindingType BindingType;
        public readonly bool IsInversed;

        public ObservableBindingInfo(string fieldName, string observableName, InnerBindingType bindingType, bool isInversed)
        {
            FieldName = fieldName;
            ObservableName = observableName;
            BindingType = bindingType;
            IsInversed = isInversed;
        }

        public string GenerateAssignment(string observedValueName)
        {
            if (IsInversed)
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
            switch (BindingType)
            {
                case InnerBindingType.Text:
                    return $"{FieldName}.text = {observedValueName}";
                case InnerBindingType.ImageFill:
                    return $"{FieldName}.fillAmount = 1 - {observedValueName}";
                case InnerBindingType.GameObjectActivity:
                    return $"{FieldName}.gameObject.SetActive(!{observedValueName})";
                case InnerBindingType.Activity:
                    return $"{FieldName}.SetActive(!{observedValueName})";
                case InnerBindingType.Color:
                    return $"{FieldName}.color = {observedValueName}";
                case InnerBindingType.Sprite:
                    return $"{FieldName}.sprite = {observedValueName}";
                case InnerBindingType.Enabled:
                    return $"{FieldName}.enabled = !{observedValueName}";
                case InnerBindingType.Interactable:
                    return $"{FieldName}.interactable = !{observedValueName}";
                case InnerBindingType.Alpha:
                    return $"{FieldName}.alpha = 1 - {observedValueName}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private string GenerateStraightAssignment(string observedValueName)
        {
            switch (BindingType)
            {
                case InnerBindingType.Text:
                    return $"{FieldName}.text = {observedValueName}";
                case InnerBindingType.ImageFill:
                    return $"{FieldName}.fillAmount = {observedValueName}";
                case InnerBindingType.GameObjectActivity:
                    return $"{FieldName}.gameObject.SetActive({observedValueName})";
                case InnerBindingType.Activity:
                    return $"{FieldName}.SetActive({observedValueName})";
                case InnerBindingType.Color:
                    return $"{FieldName}.color = {observedValueName}";
                case InnerBindingType.Sprite:
                    return $"{FieldName}.sprite = {observedValueName}";
                case InnerBindingType.Enabled:
                    return $"{FieldName}.enabled = {observedValueName}";
                case InnerBindingType.Interactable:
                    return $"{FieldName}.interactable = {observedValueName}";
                case InnerBindingType.Alpha:
                    return $"{FieldName}.alpha = {observedValueName}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
