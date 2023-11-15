using System.Collections.Generic;

namespace ViewsSourceGenerator.Comparers
{
    internal class ButtonMethodCallInfoComparerFromViewModelPoV : IEqualityComparer<ButtonMethodCallInfo>
    {
        public static readonly ButtonMethodCallInfoComparerFromViewModelPoV Default = new();
        
        public bool Equals(ButtonMethodCallInfo x, ButtonMethodCallInfo y)
        {
            return ButtonMethodCallInfo.AreEqualFromViewModelPoV(x, y);
        }

        public int GetHashCode(ButtonMethodCallInfo obj)
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