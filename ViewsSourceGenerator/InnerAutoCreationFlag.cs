using System;

namespace ViewsSourceGenerator
{
    [Flags]
    internal enum InnerAutoCreationFlag
    {
        PublicObservable = 1 << 0,
        PublicReactiveProperty = 1 << 1,
        PublicCommand = 1 << 2,
        PrivateReactiveProperty = 1 << 3,
        PrivateCommand = 1 << 4,
    }
}
