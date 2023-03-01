using System;

namespace ViewsSourceGenerator
{
    internal readonly struct ObservableBindingInfo
    {
        public readonly string FieldName;
        public readonly string ObservableName;
        public readonly InnerBindingType BindingType;

        public ObservableBindingInfo(string fieldName, string observableName, InnerBindingType bindingType)
        {
            FieldName = fieldName;
            ObservableName = observableName;
            BindingType = bindingType;
        }

        public string GenerateAssignment(string observedValueName)
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
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
