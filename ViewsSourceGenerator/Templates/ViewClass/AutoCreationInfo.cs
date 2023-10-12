using ViewsSourceGenerator.Tools;

namespace ViewsSourceGenerator
{
    internal readonly struct AutoCreationInfo
    {
        public static readonly AutoCreationInfo Empty = new (AutoCreationFlag.None);
        
        public static AutoCreationInfo OnlyObservable(string observableName)
        {
            return new(observableName);
        }
        
        public readonly string ObservableName;

        private readonly AutoCreationFlag _creationFlags;
        private readonly string? _observableArgumentType;

        public bool HasObservableArgument => (_observableArgumentType is not (null or "Unit"));

        public bool HasPrivateCreations => _creationFlags.HasFlag(AutoCreationFlag.PrivateCommand) || _creationFlags.HasFlag(AutoCreationFlag.PrivateReactiveProperty);

        public bool HasPublicCreations => _creationFlags.HasFlag(AutoCreationFlag.PublicObservable) || _creationFlags.HasFlag(AutoCreationFlag.PublicReactiveProperty);

        public bool IsEmpty => _creationFlags == AutoCreationFlag.None;

        public AutoCreationInfo(string observableName, AutoCreationFlag creationFlags, string? observableArgumentType = null)
        {
            ObservableName = observableName;
            _creationFlags = creationFlags;
            _observableArgumentType = observableArgumentType;
        }
        
        private AutoCreationInfo(AutoCreationFlag creationFlags)
        {
            ObservableName = string.Empty;
            _creationFlags = creationFlags;
            _observableArgumentType = default;
        }
        
        private AutoCreationInfo(string observableName)
        {
            ObservableName = observableName;
            _creationFlags = AutoCreationFlag.None;
            _observableArgumentType = default;
        }

        public string GetPrivatePartFieldName()
        {
            if (IsEmpty)
            {
                return string.Empty;
            }
            
            switch (_creationFlags)
            {
                case var flags when flags.HasFlag(AutoCreationFlag.PrivateCommand):
                    return $"_{ObservableName.Decapitalize()}Cmd";

                case var flags when flags.HasFlag(AutoCreationFlag.PrivateReactiveProperty):
                    return $"_{ObservableName.Decapitalize()}";
            }

            return string.Empty;
        }
        
        public string GetAutoCreatedObserversPrivatePart()
        {
            if (IsEmpty)
            {
                return string.Empty;
            }
            
            switch (_creationFlags)
            {
                case var flags when flags.HasFlag(AutoCreationFlag.PrivateCommand):
                    return $"private readonly {GetReactiveCommandType()} {GetPrivatePartFieldName()} = new();";

                case var flags when flags.HasFlag(AutoCreationFlag.PrivateReactiveProperty):
                    return $"private readonly ReactiveProperty<{GetObservableArgumentType()}> {GetPrivatePartFieldName()} = new();";
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
                case var flags when flags.HasFlag(AutoCreationFlag.PrivateCommand):
                    return $"{GetPrivatePartFieldName()}.Execute();";
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
                case var flags when flags.HasFlag(AutoCreationFlag.PrivateCommand):
                    return $"{GetPrivatePartFieldName()}.AddTo(compositeDisposable);";

                case var flags when flags.HasFlag(AutoCreationFlag.PrivateReactiveProperty):
                    return $"{GetPrivatePartFieldName()}.AddTo(compositeDisposable);";
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
                case var flags when flags.HasFlag(AutoCreationFlag.PublicObservable) && flags.HasFlag(AutoCreationFlag.PrivateCommand):
                    return $"public IObservable<{GetObservableArgumentType()}> {ObservableName} => {GetPrivatePartFieldName()};";

                case var flags when flags.HasFlag(AutoCreationFlag.PublicObservable) && flags.HasFlag(AutoCreationFlag.PrivateReactiveProperty):
                    return $"public IObservable<{GetObservableArgumentType()}> {ObservableName} => {GetPrivatePartFieldName()};";

                case var flags when flags.HasFlag(AutoCreationFlag.PublicObservable):
                    return $"public readonly IObservable<{GetObservableArgumentType()}> {ObservableName};";

                case var flags when flags.HasFlag(AutoCreationFlag.PublicReactiveProperty) && flags.HasFlag(AutoCreationFlag.PrivateReactiveProperty):
                    return $"public IReadOnlyReactiveProperty<{GetObservableArgumentType()}> {ObservableName} => {GetPrivatePartFieldName()};";

                case var flags when flags.HasFlag(AutoCreationFlag.PublicReactiveProperty):
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