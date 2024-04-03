using System;
using System.Collections.Generic;
using System.Linq;
using ViewsSourceGenerator.Tools;

namespace ViewsSourceGenerator
{
    internal readonly struct ViewButtonMethodCallInfo
    {
        // Used by view-generation template only
        public string ButtonFieldName { get; init; }
        public bool ShouldCheckForNull { get; init; }
        public string ButtonReference => ShouldCheckForNull ? $"{ButtonFieldName}.UINullChecked()?" : ButtonFieldName;
        public MethodCallInfo[] CallInfos { get; init; }

        private bool HasLongClick => CallInfos.Any(info => info.ButtonInteractionType == ButtonClickType.LongClick);

        public string GetLastClickFieldName(int index)
        {
            ref var callInfo = ref CallInfos[index];
            
            return $"{ButtonFieldName.Decapitalize().Camel()}{(callInfo.IsViewModelMethod ? "_Model" : "")}_{callInfo.MethodToCall}LastClickTime";
        }
        
        public IEnumerable<string> GetSubscribing()
        {
            string keeperName = string.Empty;
            string upStreamName = string.Empty;
            
            if (HasLongClick)
            {
                keeperName = $"{ButtonFieldName.Decapitalize()}LongClickKeeper";
                upStreamName = $"{ButtonFieldName.Decapitalize()}UpStream";
                
                yield return $"var { keeperName } = new { LongTapKeeperTemplate.ClassName }();";
                yield return $"var { upStreamName } = { ButtonReference }.OnPointerUpAsObservable();";
            }
            
            for (var i = 0; i < CallInfos.Length; i++) { 
                var callInfo = CallInfos[i];

                yield return ButtonReference;
                foreach (var statement in GetProperStreamForClickType(callInfo.ButtonInteractionType, HasLongClick, keeperName, upStreamName, callInfo.LongClickDurationMs))
                {
                    yield return "\t" + statement;
                }
                
                if (callInfo.InactivePeriodMs > 0) { 
                    var lastClickFieldName = GetLastClickFieldName(i);
                    yield return $"\t.Where(_ => (Time.realtimeSinceStartup - { lastClickFieldName }) * 1000 > { callInfo.InactivePeriodMs })";
                    yield return $"\t.Do(_ => { lastClickFieldName } = Time.realtimeSinceStartup)";
                }
                
                foreach (var statement in GetProperSubscriptionForClickType(callInfo.ButtonInteractionType, keeperName, callInfo.CallMethodExpression))
                {
                    yield return "\t" + statement;
                }
                yield return "\t.AddTo(_oneInitDisposable);";
            } 
        }
        
        private IEnumerable<string> GetProperStreamForClickType(ButtonClickType interactionType, bool hasLongClick, string keeperName, string upStreamName, int longClickDurationMs)
        {
            switch (interactionType)
            {
                case ButtonClickType.Click:
                    yield return ".OnClickAsObservable()";
                    if (hasLongClick)
                    {
                        yield return $".Where(_ => !{ keeperName }.WasLongTap)";
                    }
                    yield break;
                
                case ButtonClickType.LongClick:
                    yield return ".OnPointerDownAsObservable()";
                    yield return $".Do(_ => { keeperName }.WasLongTap = false)";
                    yield return ".SelectMany(_ =>";
                    yield return "\tObservable";
                    yield return $"\t\t.Timer(TimeSpan.FromMilliseconds({longClickDurationMs}))";
                    yield return $"\t\t.TakeUntil({ upStreamName }))";
                    yield break;
                
                case ButtonClickType.PointerDown:
                    yield return ".OnPointerDownAsObservable()";
                    yield break;
                
                case ButtonClickType.PointerUp:
                    yield return ".OnPointerUpAsObservable()";
                    yield break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(interactionType), interactionType, null);
            }
        }
        private IEnumerable<string> GetProperSubscriptionForClickType(ButtonClickType interactionType, string keeperName, string callMethodExpression)
        {
            switch (interactionType)
            {
                case ButtonClickType.Click:
                case ButtonClickType.PointerDown:
                case ButtonClickType.PointerUp:
                    yield return $".Subscribe(_ => { callMethodExpression })";
                    yield break;
                
                case ButtonClickType.LongClick:
                    yield return ".Subscribe(_ => {";
                    yield return $"\t{ keeperName }.WasLongTap = true;";
                    yield return $"\t{ callMethodExpression };";
                    yield return "})";
                    yield break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(interactionType), interactionType, null);
            }
        }

        public readonly struct MethodCallInfo
        {
            public int InactivePeriodMs { get; init; }
            public int LongClickDurationMs { get; init; }
            public bool ShouldPassModel { get; init; }
            public string MethodToCall { get; init; }
            public ButtonClickType ButtonInteractionType { get; init; }
            public bool IsViewModelMethod { get; init; }

            public string CallMethodExpression =>
                IsViewModelMethod ? $"model.{MethodToCall}()" : (ShouldPassModel ? $"{MethodToCall}(model)" : $"{MethodToCall}()");

            public static bool AreEqualFromViewModelPoV(MethodCallInfo x, MethodCallInfo y)
            {
                return x.InactivePeriodMs == y.InactivePeriodMs
                    && x.LongClickDurationMs == y.LongClickDurationMs
                    && x.ShouldPassModel == y.ShouldPassModel
                    && x.ButtonInteractionType == y.ButtonInteractionType
                    && x.IsViewModelMethod == y.IsViewModelMethod
                    && string.Equals(x.MethodToCall, y.MethodToCall, StringComparison.Ordinal);
            }
        }
    }
}
