using ViewsSourceGenerator.Tools;

namespace ViewsSourceGenerator
{
    internal readonly struct SubscribeOnObservableInfo
    {
        public readonly string MethodName;
        public readonly string ObservableName;
        
        private readonly InnerAutoCreationFlag _creationFlags;
        private readonly string _observableArgumentType;
        
        public bool HasObservableArgument => (_observableArgumentType is not (null or "Unit"));

        public bool HasPrivateCreations => _creationFlags.HasFlag(InnerAutoCreationFlag.PrivateCommand) || _creationFlags.HasFlag(InnerAutoCreationFlag.PrivateReactiveProperty);
        
        public bool HasPublicCreations => _creationFlags.HasFlag(InnerAutoCreationFlag.PublicObservable) || _creationFlags.HasFlag(InnerAutoCreationFlag.PublicReactiveProperty);

        public bool HasObservablesToDispose => HasPrivateCreations;

        public SubscribeOnObservableInfo(string methodName, string observableName, InnerAutoCreationFlag creationFlags, string observableArgumentType = null)
        {
            MethodName = methodName;
            ObservableName = observableName;
            _creationFlags = creationFlags;
            _observableArgumentType = observableArgumentType;
        }

        public string GetAutoCreatedObserversPrivatePart()
        {
            switch (_creationFlags)
            {
                case var flags when flags.HasFlag(InnerAutoCreationFlag.PrivateCommand):
                    return $"private readonly {GetReactiveCommandType()} _{ObservableName.Decapitalize()}Cmd = new();";
                
                case var flags when flags.HasFlag(InnerAutoCreationFlag.PrivateReactiveProperty):
                    return $"private readonly ReactiveProperty<{GetObservableArgumentType()}> _{ObservableName.Decapitalize()} = new();";
            }
            
            return string.Empty;
        }
        
        public string GetAutoCreatedObserversDisposePart()
        {
            switch (_creationFlags)
            {
                case var flags when flags.HasFlag(InnerAutoCreationFlag.PrivateCommand):
                    return $"_{ObservableName.Decapitalize()}Cmd.AddTo(compositeDisposable);";
                
                case var flags when flags.HasFlag(InnerAutoCreationFlag.PrivateReactiveProperty):
                    return $"_{ObservableName.Decapitalize()}.AddTo(compositeDisposable);";
            }
            
            return string.Empty;
        }
        
        public string GetAutoCreatedObserversPublicPart()
        {
            switch (_creationFlags)
            {
                case var flags when flags.HasFlag(InnerAutoCreationFlag.PublicObservable) && flags.HasFlag(InnerAutoCreationFlag.PrivateCommand):
                    return $"public IObservable<{GetObservableArgumentType()}> {ObservableName} => _{ObservableName.Decapitalize()}Cmd;";
                
                case var flags when flags.HasFlag(InnerAutoCreationFlag.PublicObservable) && flags.HasFlag(InnerAutoCreationFlag.PrivateReactiveProperty):
                    return $"public IObservable<{GetObservableArgumentType()}> {ObservableName} => _{ObservableName.Decapitalize()};";
                
                case var flags when flags.HasFlag(InnerAutoCreationFlag.PublicObservable):
                    return $"public readonly IObservable<{GetObservableArgumentType()}> {ObservableName};";
                
                case var flags when flags.HasFlag(InnerAutoCreationFlag.PublicReactiveProperty) && flags.HasFlag(InnerAutoCreationFlag.PrivateReactiveProperty):
                    return $"public IReadOnlyReactiveProperty<{GetObservableArgumentType()}> {ObservableName} => _{ObservableName.Decapitalize()};";
                
                case var flags when flags.HasFlag(InnerAutoCreationFlag.PublicReactiveProperty):
                    return $"public readonly IReadOnlyReactiveProperty<{GetObservableArgumentType()}> {ObservableName};";
            }
            
            return string.Empty;
        }
        
        private string GetReactiveCommandType()
        {
            if (_observableArgumentType is null or "Unit")
            {
                return "ReactiveCommand";
            }
            else
            {
                return $"ReactiveCommand<{_observableArgumentType}>";
            }
        }
        
        private string GetObservableArgumentType()
        {
            if (_observableArgumentType is null or "Unit")
            {
                return "Unit";
            }
            else
            {
                return _observableArgumentType;
            }
        }
    }
}
