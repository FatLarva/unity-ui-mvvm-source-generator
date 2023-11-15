using System.Collections.Generic;

namespace ViewsSourceGenerator.Comparers
{
    internal class ObservableBindingInfoComparerFromViewModelPoV : IEqualityComparer<ObservableBindingInfo>
    {
        public static readonly ObservableBindingInfoComparerFromViewModelPoV Default = new();
        
        public bool Equals(ObservableBindingInfo x, ObservableBindingInfo y)
        {
            return ObservableBindingInfo.AreEqualFromViewModelPoV(x, y);
        }

        public int GetHashCode(ObservableBindingInfo obj)
        {
            unchecked
            {
                return obj.AutoCreationInfo.GetHashCode();
            }
        }
    }
}