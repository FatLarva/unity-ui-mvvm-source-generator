using ViewsSourceGenerator.Tools;

namespace ViewsSourceGenerator
{
    internal readonly struct AutoCreationInfo
    {
        public static readonly AutoCreationInfo Empty = new AutoCreationInfo(InnerAutoCreationFlag.None);
        
        public static AutoCreationInfo OnlyObservable(string observableName)
        {
            return new(observableName);
        }
        
        public readonly string ObservableName;

        private readonly InnerAutoCreationFlag _creationFlags;
        private readonly string _observableArgumentType;

        public bool HasObservableArgument => (_observableArgumentType is not (null or "Unit"));

        public bool HasPrivateCreations => _creationFlags.HasFlag(InnerAutoCreationFlag.PrivateCommand) || _creationFlags.HasFlag(InnerAutoCreationFlag.PrivateReactiveProperty);

        public bool HasPublicCreations => _creationFlags.HasFlag(InnerAutoCreationFlag.PublicObservable) || _creationFlags.HasFlag(InnerAutoCreationFlag.PublicReactiveProperty);

        public bool IsEmpty => _creationFlags == InnerAutoCreationFlag.None;

        public AutoCreationInfo(string observableName, InnerAutoCreationFlag creationFlags, string observableArgumentType = null)
        {
            ObservableName = observableName;
            _creationFlags = creationFlags;
            _observableArgumentType = observableArgumentType;
        }
        
        private AutoCreationInfo(InnerAutoCreationFlag creationFlags)
        {
            ObservableName = default;
            _creationFlags = creationFlags;
            _observableArgumentType = default;
        }
        
        private AutoCreationInfo(string observableName)
        {
            ObservableName = observableName;
            _creationFlags = InnerAutoCreationFlag.None;
            _observableArgumentType = default;
        }

        public string GetAutoCreatedObserversPrivatePart()
        {
            if (IsEmpty)
            {
                return string.Empty;
            }
            
            switch (_creationFlags)
            {
                case var flags when flags.HasFlag(InnerAutoCreationFlag.PrivateCommand):
                    return $"private readonly {GetReactiveCommandType()} _{ObservableName.Decapitalize()}Cmd = new();";

                case var flags when flags.HasFlag(InnerAutoCreationFlag.PrivateReactiveProperty):
                    return $"private readonly ReactiveProperty<{GetObservableArgumentType()}> _{ObservableName.Decapitalize()} = new();";
            }

            return string.Empty;
        }
        
        public string GetCallingCommandPart()
        {
            if (IsEmpty)
            {
                return string.Empty;
            }
            
            switch (_creationFlags)
            {
                case var flags when flags.HasFlag(InnerAutoCreationFlag.PrivateCommand):
                    return $"_{ObservableName.Decapitalize()}Cmd.Execute();";
            }

            return string.Empty;
        }

        public string GetAutoCreatedObserversDisposePart()
        {
            if (IsEmpty)
            {
                return string.Empty;
            }

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
            if (IsEmpty)
            {
                return string.Empty;
            }

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