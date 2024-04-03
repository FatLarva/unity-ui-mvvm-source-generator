using System.Collections.Generic;

namespace ViewsSourceGenerator.Comparers
{
    internal class ButtonMethodCallInfoComparerFromViewModelPoV : IEqualityComparer<ViewModelButtonMethodCallInfo.ViewModelInfo>
    {
        public static readonly ButtonMethodCallInfoComparerFromViewModelPoV Default = new();
        
        public bool Equals(ViewModelButtonMethodCallInfo.ViewModelInfo x, ViewModelButtonMethodCallInfo.ViewModelInfo y)
        {
            return ViewModelButtonMethodCallInfo.ViewModelInfo.AreEqualFromViewModelPoV(x, y);
        }

        public int GetHashCode(ViewModelButtonMethodCallInfo.ViewModelInfo obj)
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