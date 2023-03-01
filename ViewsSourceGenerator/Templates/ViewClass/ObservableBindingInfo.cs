using System;

namespace ViewsSourceGenerator
{
    internal readonly struct ObservableBindingInfo
    {
        public readonly string FieldName;
        public readonly string ObservableName;
        public readonly BindingType BindingType;

        public ObservableBindingInfo(string fieldName, string observableName, BindingType bindingType)
        {
            FieldName = fieldName;
            ObservableName = observableName;
            BindingType = bindingType;
        }

        public string GenerateAssignment(string observedValueName)
        {
            switch (BindingType)
            {
                case BindingType.Text:
                    return $"{FieldName}.text = {observedValueName}";
                case BindingType.ImageFill:
                    return $"{FieldName}.fillAmount = {observedValueName}";
                case BindingType.GameObjectActivity:
                    return $"{FieldName}.gameObject.SetActive({observedValueName})";
                case BindingType.Activity:
                    return $"{FieldName}.SetActive({observedValueName})";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
