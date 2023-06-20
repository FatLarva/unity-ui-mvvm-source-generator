namespace ViewsSourceGenerator
{
    internal readonly struct ObservableBindingDelaySettings
    {
        public readonly bool IsFrames;
        public readonly int Delay;

        public ObservableBindingDelaySettings(bool isFrames, int delay)
        {
            IsFrames = isFrames;
            Delay = delay;
        }
    }
}