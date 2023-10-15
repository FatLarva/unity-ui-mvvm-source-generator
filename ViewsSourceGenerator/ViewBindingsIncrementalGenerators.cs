using System;
using Microsoft.CodeAnalysis;

namespace ViewsSourceGenerator
{
    [Generator]
    public class ViewBindingsIncrementalGenerators : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(
                pIContext =>
                {
                    Console.Out.WriteLine($"Hello from Incremental source generator");
                    pIContext.AddSource(ViewModelMethodCallAttributeTemplate.SourceFileName, new ViewModelMethodCallAttributeTemplate().TransformText());
                    pIContext.AddSource(ViewModelGenerateAttributeTemplate.SourceFileName, new ViewModelGenerateAttributeTemplate().TransformText());
                    pIContext.AddSource(LocalizeWithKeyAttributeTemplate.SourceFileName, new LocalizeWithKeyAttributeTemplate().TransformText());
                    pIContext.AddSource(LocalizeWithKeyFromFieldAttributeTemplate.SourceFileName, new LocalizeWithKeyFromFieldAttributeTemplate().TransformText());
                    pIContext.AddSource(SubscribeOnViewModelsObservableAttributeTemplate.SourceFileName, new SubscribeOnViewModelsObservableAttributeTemplate().TransformText());
                    pIContext.AddSource(BindToObservableAttributeTemplate.SourceFileName, new BindToObservableAttributeTemplate().TransformText());
                    pIContext.AddSource(BindingTypeEnumTemplate.SourceFileName, new BindingTypeEnumTemplate().TransformText());
                    pIContext.AddSource(AutoCreationFlagEnumTemplate.SourceFileName, new AutoCreationFlagEnumTemplate().TransformText());
                    pIContext.AddSource(GeneratedViewModelAttributeTemplate.SourceFileName, new GeneratedViewModelAttributeTemplate().TransformText());
                    pIContext.AddSource(SubViewAttributeTemplate.SourceFileName, new SubViewAttributeTemplate().TransformText());
                    pIContext.AddSource(CommonModelAttributeTemplate.SourceFileName, new CommonModelAttributeTemplate().TransformText());
                    pIContext.AddSource(GeneratedModelAttributeTemplate.SourceFileName, new GeneratedModelAttributeTemplate().TransformText());
                });
        }
    }
}