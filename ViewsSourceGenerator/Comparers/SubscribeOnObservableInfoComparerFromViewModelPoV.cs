using System.Collections.Generic;

namespace ViewsSourceGenerator.Comparers
{
    internal class SubscribeOnObservableInfoComparerFromViewModelPoV : IEqualityComparer<SubscribeOnObservableInfo>
    {
        public static readonly SubscribeOnObservableInfoComparerFromViewModelPoV Default = new();
        
        public bool Equals(SubscribeOnObservableInfo x, SubscribeOnObservableInfo y)
        {
            return SubscribeOnObservableInfo.AreEqualFromViewModelPoV(x, y);
        }

        public int GetHashCode(SubscribeOnObservableInfo obj)
        {
            return obj.AutoCreationInfo.GetHashCode();
        }
    }
}