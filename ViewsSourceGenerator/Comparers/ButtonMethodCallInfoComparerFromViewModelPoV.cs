using System.Collections.Generic;

namespace ViewsSourceGenerator.Comparers
{
    internal class ButtonMethodCallInfoComparerFromViewModelPoV : IEqualityComparer<ViewModelButtonMethodCallInfo>
    {
        public static readonly ButtonMethodCallInfoComparerFromViewModelPoV Default = new();
        
        public bool Equals(ViewModelButtonMethodCallInfo x, ViewModelButtonMethodCallInfo y)
        {
            return ViewModelButtonMethodCallInfo.AreEqualFromViewModelPoV(x, y);
        }

        public int GetHashCode(ViewModelButtonMethodCallInfo obj)
        {
            unchecked
            {
                var hashCode = obj.MethodToCall.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.ShouldGenerateMethodWithPartialStuff.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.AutoCreationInfo.GetHashCode();
                return hashCode;
            }
        }
    }
}