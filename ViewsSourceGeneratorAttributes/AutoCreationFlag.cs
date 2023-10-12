using System;

namespace ViewModelGeneration
{
    [Flags]
    public enum AutoCreationFlag
    {
        None = 0,
        PublicObservable = 1 << 0,
        PublicReactiveProperty = 1 << 1,
        PrivateReactiveProperty = 1 << 2,
        PrivateCommand = 1 << 3,
        
        WrappedCommand = PublicObservable | PrivateCommand,
        WrappedReactiveProperty = PublicReactiveProperty | PrivateReactiveProperty,
    }
}
