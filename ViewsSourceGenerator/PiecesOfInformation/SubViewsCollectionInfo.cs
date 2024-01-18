using System;
using System.Collections.Generic;

namespace ViewsSourceGenerator
{
    internal readonly struct SubViewsCollectionInfo
    {
        public string ViewFieldName { get; init; }
        public string? ViewModelFieldName { get; init; }
        
        public string? ViewBindingFieldName { get; init; }
        public string? ViewModelBindingFieldName { get; init; }
        public string? MatchingMethodName { get; init; }
        public SubViewsBindingMethod BindingMethod { get; init; }
        public bool CheckForNull { get; init; }

        public IEnumerable<string> GetInitCall()
        {
            var indentation = CheckForNull ? "\t" : string.Empty;
            
            if (CheckForNull)
            {
                yield return $"if ({ViewFieldName} != null)";
                yield return "{";
            }
            
            Func<IEnumerable<string>> initializationCodeProvider = BindingMethod switch
            {
                SubViewsBindingMethod.SameModel => SameModelInit,
                SubViewsBindingMethod.Index => MatchByIndexInit,
                SubViewsBindingMethod.FieldMatch => MatchByFieldsInit,
                SubViewsBindingMethod.WithMethod => MatchWithMethodInit,
                _ => throw new ArgumentOutOfRangeException(),
            };
            
            foreach (var statement in initializationCodeProvider.Invoke())
            {
                yield return indentation + statement;
            }

            if (CheckForNull)
            {
                yield return "}";
            }
        }

        public IEnumerable<string> GetDeinitCall()
        {
            var indentation = CheckForNull ? "\t" : string.Empty;
            
            if (CheckForNull)
            {
                yield return $"if ({ViewFieldName} != null)";
                yield return "{";
            }
            
            yield return indentation + $"foreach (var view in { ViewFieldName })";
            yield return indentation + "{";
            yield return indentation + "\tview.Deinit();";
            yield return indentation + "}";
            
            if (CheckForNull)
            {
                yield return "}";
            }
        }
        
        private IEnumerable<string> SameModelInit()
        {
            yield return $"foreach (var view in { ViewFieldName })";
            yield return "{";
            yield return "\tview.Init(model);";
            yield return "}";
        }
        
        private IEnumerable<string> MatchByIndexInit()
        {
            yield return $"for (int i = 0; i < { ViewFieldName }.Length; i++)";
            yield return "{";
            yield return $"\t{ ViewFieldName }[i].Init(model.{ ViewModelFieldName }[i]);";
            yield return "}";
        }
        
        private IEnumerable<string> MatchByFieldsInit()
        {
            yield return $"foreach (var view in { ViewFieldName })";
            yield return "{";
            yield return $"\tforeach (var viewModel in model.{ ViewModelFieldName })";
            yield return "\t{";
            yield return $"\t\tif (view.{ ViewBindingFieldName } == viewModel.{ ViewModelBindingFieldName })";
            yield return "\t\t{";
            yield return "\t\t\tview.Init(viewModel);";
            yield return "\t\t\tbreak;";
            yield return "\t\t}";
            yield return "\t}";
            yield return "}";
        }
        
        private IEnumerable<string> MatchWithMethodInit()
        {
            yield return $"foreach (var view in { ViewFieldName })";
            yield return "{";
            yield return $"\tforeach (var viewModel in model.{ ViewModelFieldName })";
            yield return "\t{";
            yield return $"\t\tif ({ MatchingMethodName }(view, viewModel))";
            yield return "\t\t{";
            yield return "\t\t\tview.Init(viewModel);";
            yield return "\t\t\tbreak;";
            yield return "\t\t}";
            yield return "\t}";
            yield return "}";
        }
    }
}