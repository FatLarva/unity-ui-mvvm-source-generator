using System.Collections.Generic;

namespace ViewsSourceGenerator.Comparers
{
    internal class ViewCallInfoComparer : IEqualityComparer<ViewButtonMethodCallInfo.MethodCallInfo>
    {
        public static readonly ViewCallInfoComparer Default = new();

        public bool Equals(ViewButtonMethodCallInfo.MethodCallInfo x, ViewButtonMethodCallInfo.MethodCallInfo y)
        {
            return ViewButtonMethodCallInfo.MethodCallInfo.AreEqualFromViewModelPoV(x, y);
        }

        public int GetHashCode(ViewButtonMethodCallInfo.MethodCallInfo obj)
        {
            unchecked
            {
                var hashCode = obj.InactivePeriodMs;
                hashCode = (hashCode * 397) ^ obj.LongClickDurationMs;
                hashCode = (hashCode * 397) ^ obj.ShouldPassModel.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.MethodToCall.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)obj.ButtonInteractionType;
                hashCode = (hashCode * 397) ^ obj.IsViewModelMethod.GetHashCode();
                return hashCode;
            }
        }
    }
}