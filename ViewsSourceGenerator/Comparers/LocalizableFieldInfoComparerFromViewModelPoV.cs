using System.Collections.Generic;

namespace ViewsSourceGenerator.Comparers
{
    internal class LocalizableFieldInfoComparerFromViewModelPoV : IEqualityComparer<LocalizableFieldInfo>
    {
        public static readonly LocalizableFieldInfoComparerFromViewModelPoV Default = new();
        
        public bool Equals(LocalizableFieldInfo x, LocalizableFieldInfo y)
        {
            return LocalizableFieldInfo.AreEqualFromViewModelPoV(x, y);
        }

        public int GetHashCode(LocalizableFieldInfo obj)
        {
            unchecked
            {
                return (obj.LocalizationKey.GetHashCode() * 397) ^ obj.KeyProviderFieldName.GetHashCode();
            }
        }
    }
}