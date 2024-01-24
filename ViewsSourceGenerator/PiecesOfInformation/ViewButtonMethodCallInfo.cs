using ViewsSourceGenerator.Tools;

namespace ViewsSourceGenerator
{
    internal readonly struct ViewButtonMethodCallInfo
    {
        // Used by view-generation template only
        public string ButtonFieldName { get; init; }
        public int InactivePeriodMs { get; init; }
        public bool ShouldCheckForNull { get; init; }
        public bool ShouldPassModel { get; init; }
        public string MethodToCall { get; init; }
        public bool IsViewModelMethod { get; init; }
        
        public string LastClickFieldName => $"{ButtonFieldName.Decapitalize().Camel()}{(IsViewModelMethod ? "_Model" : "")}_{MethodToCall}LastClickTime";

        public string ButtonReference => ShouldCheckForNull ? $"{ButtonFieldName}.UINullChecked()?" : ButtonFieldName;
        public string CallMethodExpression => IsViewModelMethod ? $"model.{MethodToCall}()" : (ShouldPassModel ? $"{MethodToCall}(model)" : $"{MethodToCall}()");
    }
}
