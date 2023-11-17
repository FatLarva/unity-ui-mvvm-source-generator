using System.Collections.Generic;

namespace ViewsSourceGenerator.Comparers
{
    internal class ViewModelLocalizationInfoComparerFromViewModelPoV : IEqualityComparer<ViewModelLocalizationInfo>
    {
        public static readonly ViewModelLocalizationInfoComparerFromViewModelPoV Default = new();
        
        public bool Equals(ViewModelLocalizationInfo x, ViewModelLocalizationInfo y)
        {
            return ViewModelLocalizationInfo.AreEqualFromViewModelPoV(x, y);
        }

        public int GetHashCode(ViewModelLocalizationInfo obj)
        {
            unchecked
            {
                return (obj.LocalizationKey.GetHashCode() * 397) ^ obj.KeyProviderFieldName.GetHashCode();
            }
        }
    }
}